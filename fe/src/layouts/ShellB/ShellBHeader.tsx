// ShellBHeader.tsx — thanh trên cùng của Shell B (trang công khai / người tham dự).
// Chứa: tên ứng dụng (link về trang chủ) bên trái; nút đổi ngôn ngữ và đổi nền bên phải.
// T1.2 thêm nút Đăng nhập / Đăng ký và menu tài khoản.
// Dùng ở: layouts/ShellB/ShellB.tsx.
import { Flex, Layout, theme } from "antd";
import { useTranslation } from "react-i18next";
import { Link } from "react-router";
import { LanguageToggle } from "@/components/LanguageToggle";
import { ThemeToggle } from "@/components/ThemeToggle";

export function ShellBHeader() {
  const { t } = useTranslation();
  const { token } = theme.useToken();

  return (
    <Layout.Header
      style={{
        display: "flex",
        alignItems: "center",
        justifyContent: "space-between",
        // Header của antd mặc định nền xanh đậm → đổi sang màu bề mặt theo nền sáng / tối.
        background: token.colorBgContainer,
        borderBottom: `${token.lineWidth}px ${token.lineType} ${token.colorBorder}`,
        paddingInline: token.paddingLG,
      }}
    >
      <Link
        to="/"
        style={{
          color: token.colorPrimary,
          fontSize: token.fontSizeHeading4,
          fontWeight: token.fontWeightStrong,
        }}
      >
        {t("app.name")}
      </Link>
      <Flex align="center" gap="small">
        <LanguageToggle />
        <ThemeToggle />
      </Flex>
    </Layout.Header>
  );
}
