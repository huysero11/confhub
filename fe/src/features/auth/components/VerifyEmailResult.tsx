// VerifyEmailResult.tsx — component kết quả xác thực cho VerifyEmailPage.
// Hook query giữ kết quả theo token; phân biệt link hỏng với lỗi mạng/hệ thống để không báo sai.
import { Alert, Button, Flex, Result, Spin } from "antd";
import { getErrorMessage } from "@/i18n/errorMessage";
import { useTranslation } from "react-i18next";
import { Link, useNavigate } from "react-router";
import { useVerifyEmail } from "../hooks/useVerifyEmail";
import { isApiError } from "@/lib/apiError";
import { ResendVerificationForm } from "./ResendVerificationForm";

type VerifyEmailResultProps = { rawToken: string };

export function VerifyEmailResult({ rawToken }: VerifyEmailResultProps) {
  // Trạng thái xác thực email từ hook
  const navigate = useNavigate();
  const verification = useVerifyEmail(rawToken);
  const { t } = useTranslation();
  const code = isApiError(verification.error) ? verification.error.code : "Unknown";

  // Liên kết thiếu token hoặc không còn hiệu lực
  if (!rawToken || code === "TokenInvalid") {
    return (
      <Flex vertical gap="middle">
        {/* Thông báo liên kết không hợp lệ */}
        <Result
          status="error"
          title={t("auth.verifyEmail.invalidTitle")}
          subTitle={t("auth.verifyEmail.invalidDescription")}
        />

        {/* Form yêu cầu liên kết mới */}
        <ResendVerificationForm />
      </Flex>
    );
  }

  // Đang chờ kết quả xác thực
  if (verification.isPending) {
    return <Spin aria-label={t("auth.loading")} />;
  }

  // Lỗi mạng hoặc hệ thống
  if (verification.isError) {
    return <Alert type="error" showIcon message={getErrorMessage(t, verification.error)} />;
  }

  // Đã xác thực nhưng tài khoản cần được duyệt
  if (verification.data.status === "PendingApproval") {
    return (
      <Result
        status="info"
        title={t("auth.verifyEmail.pendingTitle")}
        subTitle={t("auth.verifyEmail.pendingDescription")}
        extra={<Link to="/">{t("auth.verifyEmail.backHome")}</Link>}
      />
    );
  }

  // Xác thực thành công, có thể đăng nhập
  return (
    <Result
      status="success"
      title={t("auth.verifyEmail.successTitle")}
      subTitle={t("auth.verifyEmail.successDescription")}
      extra={
        <Button
          type="primary"
          onClick={() => {
            void navigate("/login");
          }}
        >
          {t("auth.header.login")}
        </Button>
      }
    />
  );
}
