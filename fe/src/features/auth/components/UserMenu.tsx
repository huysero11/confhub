// UserMenu.tsx — component menu người dùng trong ShellBHeader.
// Hiện tên, email, vai trò và đăng xuất qua hook; chưa có mục hồ sơ hoặc quản lý.
import { Avatar, Button, Dropdown, Space, Typography, theme } from "antd";
import { UserOutlined } from "@ant-design/icons";
import { useTranslation } from "react-i18next";
import { useAuthSession } from "../hooks/useAuthSession";
import { useLogout } from "../hooks/useLogout";

export function UserMenu() {
  // Phiên hiện tại và hook đăng xuất
  const session = useAuthSession();
  const logout = useLogout();
  const { t } = useTranslation();
  const { token } = theme.useToken();

  // Chỉ hiện menu khi có phiên đăng nhập
  if (!session) {
    return null;
  }

  // Thông tin tài khoản và thao tác đăng xuất trong menu
  return (
    <Dropdown
      trigger={["click"]}
      menu={{
        items: [
          { key: "email", label: session.user.email, disabled: true },
          { key: "role", label: t(`role.${session.user.role}.name`), disabled: true },
          { type: "divider" },
          {
            // Mục đăng xuất
            key: "logout",
            label: t("auth.userMenu.logout"),
            danger: true,
            disabled: logout.isPending,
            onClick: () => {
              // Gửi yêu cầu đăng xuất
              if (!logout.isPending) {
                logout.mutate();
              }
            },
          },
        ],
      }}
    >
      {/* Nút mở menu: ảnh đại diện và tên người dùng */}
      <Button
        type="text"
        loading={logout.isPending}
        aria-label={t("auth.userMenu.label", { name: session.user.fullName })}
      >
        <Space>
          <Avatar size="small" icon={<UserOutlined />} />
          <Typography.Text ellipsis style={{ maxWidth: token.screenXS / 3 }}>
            {session.user.fullName}
          </Typography.Text>
        </Space>
      </Button>
    </Dropdown>
  );
}
