// CheckEmailPage.tsx — lớp page của route tài khoản, ghép thẻ và component nghiệp vụ.
// Router dùng trang này; tham số URL được truyền xuống component, không gọi API tại page.
import { useTranslation } from "react-i18next";
import { useSearchParams } from "react-router";
import { AuthCard } from "../components/AuthCard";
import { CheckEmailNotice } from "../components/CheckEmailNotice";

export function CheckEmailPage() {
  const { t } = useTranslation();
  const [searchParams] = useSearchParams();
  const email = searchParams.get("email")?.trim() ?? "";

  return (
    <AuthCard title={t("auth.checkEmail.title")}>
      <CheckEmailNotice key={email} email={email} />
    </AuthCard>
  );
}
