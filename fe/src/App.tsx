// App.tsx — component gốc TẠM THỜI để thử token, nút đổi nền và đổi ngôn ngữ.
// Bước 6 sẽ xóa file này và thay bằng router + Shell B.
import { DatePicker, Flex, theme, Typography } from "antd";
import { useTranslation } from "react-i18next";
import { LanguageToggle } from "@/components/LanguageToggle";
import { ThemeToggle } from "@/components/ThemeToggle";

export function App() {
  const { t } = useTranslation();
  const { token } = theme.useToken();

  return (
    <Flex
      vertical
      align="start"
      gap="middle"
      style={{ minHeight: "100vh", padding: token.paddingLG, background: token.colorBgLayout }}
    >
      <Typography.Title level={2}>{t("home.title")}</Typography.Title>
      <Typography.Text type="secondary">{t("home.subtitle")}</Typography.Text>
      <DatePicker />
      <Flex gap="small">
        <LanguageToggle />
        <ThemeToggle />
      </Flex>
    </Flex>
  );
}
