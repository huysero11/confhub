// ThemeProvider.tsx — giữ chế độ sáng / tối của cả ứng dụng.
// Chứa: đọc lựa chọn đã lưu (hoặc theo hệ điều hành), lưu khi người dùng đổi,
//       bọc ConfigProvider của antd với token của nền đang dùng.
// Dùng ở: main.tsx (bước 6 chuyển vào app/AppProviders.tsx).
import { ConfigProvider, theme } from "antd";
import { type ReactNode, useEffect, useMemo, useState } from "react";
import { getThemeTokens, type ThemeMode } from "@/theme/themeTokens";
import { ThemeModeContext } from "@/theme/useThemeMode";

const THEME_STORAGE_KEY = "confhub.theme";

// Lần đầu: theo cài đặt của hệ điều hành. Đã từng chọn: dùng lựa chọn đã lưu.
function readInitialMode(): ThemeMode {
  const savedMode = window.localStorage.getItem(THEME_STORAGE_KEY);
  if (savedMode === "light" || savedMode === "dark") {
    return savedMode;
  }

  const prefersDark = window.matchMedia("(prefers-color-scheme: dark)").matches;
  return prefersDark ? "dark" : "light";
}

type ThemeProviderProps = {
  children: ReactNode;
};

export function ThemeProvider({ children }: ThemeProviderProps) {
  const [mode, setMode] = useState<ThemeMode>(readInitialMode);

  // Ghi chế độ lên thẻ <html data-theme="..."> để CSS riêng (nếu có) dựa vào.
  useEffect(() => {
    document.documentElement.dataset.theme = mode;
  }, [mode]);

  const contextValue = useMemo(() => {
    function toggleMode() {
      const nextMode: ThemeMode = mode === "dark" ? "light" : "dark";
      window.localStorage.setItem(THEME_STORAGE_KEY, nextMode);
      setMode(nextMode);
    }

    return { mode, toggleMode };
  }, [mode]);

  return (
    <ThemeModeContext.Provider value={contextValue}>
      <ConfigProvider
        theme={{
          // Đổi algorithm là antd tự tính lại cả bảng màu cho nền tối, không viết CSS thứ hai.
          algorithm: mode === "dark" ? theme.darkAlgorithm : theme.defaultAlgorithm,
          cssVar: true,
          token: getThemeTokens(mode),
        }}
      >
        {children}
      </ConfigProvider>
    </ThemeModeContext.Provider>
  );
}
