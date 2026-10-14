// CheckEmailNotice.tsx — component thông báo đã đăng ký cho CheckEmailPage.
// Hiện email, khóa gửi lại ngay 60 giây; mutation dùng thông báo chung để không lộ tài khoản.
import { RESEND_COOLDOWN_MS } from "../resendCooldown";
import { useState } from "react";
import { Alert, App, Flex, Space, Typography } from "antd";
import { getErrorMessage } from "@/i18n/errorMessage";
import { useTranslation } from "react-i18next";
import { Link } from "react-router";
import { useResendVerification } from "../hooks/useResendVerification";
import { ResendCountdownButton } from "./ResendCountdownButton";
import { ResendVerificationForm } from "./ResendVerificationForm";

type CheckEmailNoticeProps = { email: string };

export function CheckEmailNotice({ email }: CheckEmailNoticeProps) {
  const { t } = useTranslation();
  const { message } = App.useApp();
  const resend = useResendVerification();
  const [availableAt, setAvailableAt] = useState(() => Date.now() + RESEND_COOLDOWN_MS);

  if (!email) {
    return <ResendVerificationForm />;
  }

  const handleResendCountdownButtonClicked = () => {
    // Chưa bấm được
    if (resend.isPending || Date.now() < availableAt) {
      return;
    }

    resend.mutate(email, {
      onSuccess: () => {
        setAvailableAt(Date.now() + RESEND_COOLDOWN_MS);
        void message.success(t("auth.checkEmail.resent"));
      },
    });
  };

  return (
    <Flex vertical gap="middle">
      {/* Địa chỉ nhận mail */}
      <Typography.Paragraph>
        {t("auth.checkEmail.sentTo")}{" "}
        <Typography.Text strong style={{ overflowWrap: "anywhere" }}>
          {email}
        </Typography.Text>
      </Typography.Paragraph>

      {/* Thời hạn liên kết */}
      <Typography.Paragraph type="secondary">
        {t("auth.checkEmail.linkValidity")}
      </Typography.Paragraph>

      {/* Kết quả gửi lại mail */}
      {resend.isSuccess && <Alert type="success" showIcon message={t("auth.checkEmail.resent")} />}
      {resend.error && <Alert type="error" showIcon message={getErrorMessage(t, resend.error)} />}

      {/* Nút gửi lại có đếm ngược */}
      <ResendCountdownButton
        key={availableAt}
        availableAt={availableAt}
        loading={resend.isPending}
        onClick={handleResendCountdownButtonClicked}
      />

      {/* Liên kết đăng kí lại khi bị sai email */}
      <Space wrap>
        <Typography.Text>{t("auth.checkEmail.wrongEmail")}</Typography.Text>
        <Link to="/register">{t("auth.checkEmail.registerAgain")}</Link>
      </Space>
    </Flex>
  );
}
