// HomePage.tsx — trang chủ TẠM (route "/"): tiêu đề + 1 dòng mô tả.
// Trang chủ thật (danh sách hội nghị) làm ở nhóm G4.
// Dùng ở: app/router.tsx, hiện trong <Outlet/> của Shell B.
import { Flex, Typography } from "antd";
import { useTranslation } from "react-i18next";

export function HomePage() {
  const { t } = useTranslation();

  return (
    <Flex vertical gap="small">
      <Typography.Title level={2}>{t("home.title")}</Typography.Title>
      <Typography.Paragraph type="secondary">{t("home.subtitle")}</Typography.Paragraph>
    </Flex>
  );
}
