// ShellBHeader.tsx — thanh trên cùng của Shell B (trang công khai / người tham dự).
// Chứa: tên ứng dụng (link về trang chủ) bên trái; nút đổi ngôn ngữ và đổi nền bên phải.
// Component chọn nút Đăng nhập / Đăng ký hoặc menu theo phiên trong bộ nhớ.
// Dùng ở: layouts/ShellB/ShellB.tsx.
import { Flex, Layout, theme } from "antd";
import { useTranslation } from "react-i18next";
import { Link } from "react-router";
import { LanguageToggle } from "@/components/LanguageToggle";
import { ThemeToggle } from "@/components/ThemeToggle";
import { GuestHeaderActions } from "@/features/auth/components/GuestHeaderActions";
import { UserMenu } from "@/features/auth/components/UserMenu";
import { useAuthSession } from "@/features/auth/hooks/useAuthSession";

export function ShellBHeader() {
  // Ngôn ngữ, giao diện và phiên đăng nhập
  const { t } = useTranslation();
  const { token } = theme.useToken();
  const session = useAuthSession();

  return (
    <Layout.Header
      style={{
        display: "flex",
        alignItems: "center",
        justifyContent: "space-between",
        flexWrap: "wrap",
        height: "auto",
        gap: token.marginSM,
        paddingBlock: token.paddingSM,
        lineHeight: token.lineHeight,
        // Header của antd mặc định nền xanh đậm → đổi sang màu bề mặt theo nền sáng / tối.
        background: token.colorBgContainer,
        borderBottom: `${token.lineWidth}px ${token.lineType} ${token.colorBorder}`,
        paddingInline: token.paddingLG,
      }}
    >
      {/* Tên ứng dụng và liên kết về trang chủ */}
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

      {/* Tùy chọn hiển thị và thao tác tài khoản */}
      <Flex align="center" gap="small" wrap>
        {/* Ngôn ngữ và nền sáng / tối */}
        <LanguageToggle />
        <ThemeToggle />

        {/* Menu người dùng hoặc nút dành cho khách */}
        {session ? <UserMenu /> : <GuestHeaderActions />}
      </Flex>
    </Layout.Header>
  );
}
