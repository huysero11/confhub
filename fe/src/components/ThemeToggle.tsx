// ThemeToggle.tsx — nút đổi nền sáng / tối.
// Chứa: một Button của antd, icon mặt trời / mặt trăng theo chế độ đang dùng.
// Là một chỗ dùng context của useThemeMode
// Dùng ở: header của Shell B (bước 6).
import { MoonOutlined, SunOutlined } from "@ant-design/icons";
import { Button } from "antd";
import { useThemeMode } from "@/theme/useThemeMode";

export function ThemeToggle() {
  const { mode, toggleMode } = useThemeMode();
  const isDark = mode === "dark";

  // Tạm viết chữ thẳng; bước 5 đổi sang i18n.
  const label = isDark ? "Chuyển sang nền sáng" : "Chuyển sang nền tối";

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
