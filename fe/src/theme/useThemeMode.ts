// useThemeMode.ts — hook để component đọc và đổi chế độ sáng / tối, là useContext custom.
// Chứa: ThemeModeContext (nơi ThemeProvider đặt giá trị vào) và hook useThemeMode (nơi lấy ra).
// Dùng ở: ThemeToggle và bất kỳ component nào cần biết đang ở nền nào.
import { createContext, useContext } from "react";
import type { ThemeMode } from "@/theme/themeTokens";

export type ThemeModeContextValue = {
  mode: ThemeMode;
  toggleMode: () => void;
};

// null = chưa có ThemeProvider bọc bên ngoài.
export const ThemeModeContext = createContext<ThemeModeContextValue | null>(null);

export function useThemeMode(): ThemeModeContextValue {
  const contextValue = useContext(ThemeModeContext);
  if (!contextValue) {
    throw new Error("useThemeMode phải được gọi bên trong <ThemeProvider>.");
  }

  return contextValue;
}
