// GuestHeaderActions.tsx — component hai nút điều hướng dành cho khách.
// ShellBHeader dùng cùng menu phiên; chữ hiển thị lấy từ i18n.
import { Button, Space } from "antd";
import { useTranslation } from "react-i18next";
import { useNavigate } from "react-router";

export function GuestHeaderActions() {
  const navigate = useNavigate();
  const { t } = useTranslation();

  return (
    <Space wrap>
      <Button
        onClick={() => {
          void navigate("/login");
        }}
      >
        {t("auth.header.login")}
      </Button>
      <Button
        type="primary"
        onClick={() => {
          void navigate("/register");
        }}
      >
        {t("auth.header.register")}
      </Button>
    </Space>
  );
}
