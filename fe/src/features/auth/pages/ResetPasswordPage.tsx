// ResetPasswordPage.tsx — lớp page của route tài khoản, ghép thẻ và component nghiệp vụ.
// Router dùng trang này; tham số URL được truyền xuống component, không gọi API tại page.
import { useTranslation } from "react-i18next";
import { useSearchParams } from "react-router";
import { AuthCard } from "../components/AuthCard";
import { ResetPasswordForm } from "../components/ResetPasswordForm";

export function ResetPasswordPage() {
  const { t } = useTranslation();
  const [searchParams] = useSearchParams();
  const rawToken = searchParams.get("token") ?? "";

  return (
    <AuthCard title={t("auth.resetPassword.title")} description={t("auth.resetPassword.subtitle")}>
      <ResetPasswordForm key={rawToken} rawToken={rawToken} />
    </AuthCard>
  );
}
