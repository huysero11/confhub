// ForgotPasswordPage.tsx — lớp page của route tài khoản, ghép thẻ và component nghiệp vụ.
// Router dùng trang này; tham số URL được truyền xuống component, không gọi API tại page.
import { useTranslation } from "react-i18next";
import { AuthCard } from "../components/AuthCard";
import { ForgotPasswordForm } from "../components/ForgotPasswordForm";

export function ForgotPasswordPage() {
  const { t } = useTranslation();

  return (
    <AuthCard
      title={t("auth.forgotPassword.title")}
      description={t("auth.forgotPassword.subtitle")}
      guestOnly
    >
      <ForgotPasswordForm />
    </AuthCard>
  );
}
