// ThemeToggle.tsx — nút đổi nền sáng / tối.
// Chứa: một Button của antd, icon mặt trời / mặt trăng theo chế độ đang dùng.
// Là một chỗ dùng context của useThemeMode
// Dùng ở: header của Shell B (bước 6).
import { MoonOutlined, SunOutlined } from "@ant-design/icons";
import { Button } from "antd";
import { useThemeMode } from "@/theme/useThemeMode";
import { useTranslation } from "react-i18next";

export function ThemeToggle() {
  const { t } = useTranslation();
  const { mode, toggleMode } = useThemeMode();
  const isDark = mode === "dark";
  const label = isDark ? t("shell.themeToLight") : t("shell.themeToDark");

  return (
    <Button
      type="text"
      icon={isDark ? <SunOutlined /> : <MoonOutlined />}
      aria-label={label}
      title={label}
      onClick={toggleMode}
    />
  );
}
