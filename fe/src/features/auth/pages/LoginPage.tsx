// LoginPage.tsx — lớp page của route tài khoản, ghép thẻ và component nghiệp vụ.
// Router dùng trang này; tham số URL được truyền xuống component, không gọi API tại page.
import { useTranslation } from "react-i18next";
import { useSearchParams } from "react-router";
import { AuthCard } from "../components/AuthCard";
import { LoginForm } from "../components/LoginForm";

export function LoginPage() {
  const { t } = useTranslation();
  const [searchParams] = useSearchParams();

  return (
    <AuthCard title={t("auth.login.title")} description={t("auth.login.subtitle")} guestOnly>
      {/* 
        - returnUrl là trang cần quay lại sau khi đăng nhập. 
          VD người dùng vào /manage và bắt đăng nhập, thì đăng nhập xong sẽ vào lại trang đấy luôn
        - reason là lí do cần đăng nhập lại, VD hiện giờ chỉ có 1 giá trị là sessionExpired ở expireSession() ở apiClient.ts
      */}
      <LoginForm returnUrl={searchParams.get("returnUrl")} reason={searchParams.get("reason")} />
    </AuthCard>
  );
}
