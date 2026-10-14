// RegisterPage.tsx — lớp page của route tài khoản, ghép thẻ và component nghiệp vụ.
// Router dùng trang này; tham số URL được truyền xuống component, không gọi API tại page.
import { useTranslation } from "react-i18next";
import { AuthCard } from "../components/AuthCard";
import { RegisterForm } from "../components/RegisterForm";

export function RegisterPage() {
  const { t } = useTranslation();

  return (
    <AuthCard title={t("auth.register.title")} description={t("auth.register.subtitle")} guestOnly>
      <RegisterForm />
    </AuthCard>
  );
}
