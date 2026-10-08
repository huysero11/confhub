// themeTokens.ts — nơi DUY NHẤT khai màu, chữ, bo góc của ứng dụng (design token của antd).
// Chứa: kiểu ThemeMode, token dùng chung, bảng màu cho từng nền (PROJECT.md mục 9).
// Dùng ở: ThemeProvider. Component không gõ màu trực tiếp mà lấy qua theme.useToken().
import type { ThemeConfig } from "antd";

export type ThemeMode = "light" | "dark";

// Lấy type của ThemeConfig.token, nhưng đảm bảo nó không phải null hoặc undefined.
type ThemeTokens = NonNullable<ThemeConfig["token"]>;

// Giống nhau ở cả hai nền.
const sharedTokens: ThemeTokens = {
  fontFamily: "'Be Vietnam Pro', system-ui, sans-serif",
  fontSize: 15,
  borderRadius: 8,
};

const colorTokensByMode: Record<ThemeMode, ThemeTokens> = {
  light: {
    colorPrimary: "#0E5A62",
    colorBgLayout: "#F7F8F7",
    colorBgContainer: "#FFFFFF",
    colorText: "#17232B",
    colorTextSecondary: "#5E6E75",
    colorBorder: "#D8DEDF",
    colorWarning: "#B26B00",
    colorError: "#B3261E",
    colorSuccess: "#2E6B45",
  },
  dark: {
    colorPrimary: "#4FB3BF",
    colorBgLayout: "#14181A",
    colorBgContainer: "#1D2326",
    colorText: "#E6EAEB",
    colorTextSecondary: "#9AA8AD",
    colorBorder: "#333C40",
    colorWarning: "#E0A33D",
    colorError: "#F2887F",
    colorSuccess: "#68B98A",
  },
};

export function getThemeTokens(mode: ThemeMode): ThemeTokens {
  return { ...sharedTokens, ...colorTokensByMode[mode] };
}
