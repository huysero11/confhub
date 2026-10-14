// VerifyEmailPage.tsx — lớp page của route tài khoản, ghép thẻ và component nghiệp vụ.
// Router dùng trang này; tham số URL được truyền xuống component, không gọi API tại page.
import { useTranslation } from "react-i18next";
import { useSearchParams } from "react-router";
import { AuthCard } from "../components/AuthCard";
import { VerifyEmailResult } from "../components/VerifyEmailResult";

export function VerifyEmailPage() {
  const { t } = useTranslation();
  const [searchParams] = useSearchParams();
  const rawToken = searchParams.get("token") ?? "";

  return (
    <AuthCard title={t("auth.verifyEmail.title")}>
      <VerifyEmailResult rawToken={rawToken} />
    </AuthCard>
  );
}
