// NotFoundPage.tsx — trang 404: hiện khi địa chỉ không khớp route nào (route "*").
// Chứa: Result của antd (hình + tiêu đề + mô tả) và nút về trang chủ.
// Dùng ở: app/router.tsx.
import { Button, Result } from "antd";
import { useTranslation } from "react-i18next";
import { useNavigate } from "react-router";

export function NotFoundPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();

  return (
    <Result
      status="404"
      title={t("notFound.title")}
      subTitle={t("notFound.subtitle")}
      extra={
        <Button type="primary" onClick={() => void navigate("/")}>
          {t("notFound.backHome")}
        </Button>
      }
    />
  );
}
