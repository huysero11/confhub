// ResendVerificationForm.tsx — component nhập email để lấy link mới khi link xác thực hỏng.
// Dùng ở VerifyEmailResult và CheckEmailNotice thiếu email; lỗi theo ô qua hook, khóa gửi lại theo BR08.
import { RESEND_COOLDOWN_MS } from "../resendCooldown";
import { useState } from "react";
import { Alert, App, Flex, Form, Input } from "antd";
import { useTranslation } from "react-i18next";
import { useResendVerification } from "../hooks/useResendVerification";
import { useAuthFormRules } from "../hooks/useAuthFormRules";
import { useFormErrors } from "../hooks/useFormErrors";
import { getErrorMessage } from "@/i18n/errorMessage";
import { ResendCountdownButton } from "./ResendCountdownButton";

export function ResendVerificationForm() {
  const { t } = useTranslation();
  const { message } = App.useApp();
  const [form] = Form.useForm<{ email: string }>();
  const resend = useResendVerification();
  const rules = useAuthFormRules(form);
  const [availableAt, setAvailableAt] = useState(0);

  useFormErrors(form, resend.error);

  const submit = ({ email }: { email: string }) => {
    if (resend.isPending || Date.now() < availableAt) {
      return;
    }

    resend.mutate(email.trim(), {
      onSuccess: () => {
        setAvailableAt(Date.now() + RESEND_COOLDOWN_MS);
        void message.success(t("auth.checkEmail.resent"));
      },
    });
  };

  return (
    <Flex vertical gap="middle">
      {/* Kết quả gửi lại email */}
      {resend.isSuccess && <Alert type="success" showIcon message={t("auth.checkEmail.resent")} />}
      {resend.error && <Alert type="error" showIcon message={getErrorMessage(t, resend.error)} />}

      <Form
        form={form}
        aria-label={t("auth.checkEmail.resend")}
        layout="vertical"
        disabled={resend.isPending}
        onFinish={submit}
      >
        <Form.Item name="email" label={t("auth.fields.email")} rules={rules.emailRules}>
          <Input autoComplete="email" placeholder={t("auth.placeholders.email")} />
        </Form.Item>

        {/* Nút gửi lại có đếm ngược */}
        <ResendCountdownButton
          key={availableAt}
          availableAt={availableAt}
          htmlType="submit"
          loading={resend.isPending}
        />
      </Form>
    </Flex>
  );
}
