// ResetPasswordForm.tsx — component đổi mật khẩu bằng token cho ResetPasswordPage.
// Chứa form, kết quả thành công và lỗi token; mọi lần gọi API đều qua hook mutation.
import { Alert, Button, Flex, Form, Input, Result } from "antd";
import { useTranslation } from "react-i18next";
import { Link, useNavigate } from "react-router";
import { useResetPassword } from "../hooks/useResetPassword";
import { useAuthFormRules } from "../hooks/useAuthFormRules";
import { useFormErrors } from "../hooks/useFormErrors";
import { isApiError } from "@/lib/apiError";
import { getErrorMessage } from "@/i18n/errorMessage";

type ResetPasswordFormProps = { rawToken: string };
type PasswordFields = { newPassword: string; confirmPassword: string };

export function ResetPasswordForm({ rawToken }: ResetPasswordFormProps) {
  // Trạng thái form và hook gọi API
  const navigate = useNavigate();
  const { t } = useTranslation();
  const [form] = Form.useForm<PasswordFields>();
  const resetPassword = useResetPassword();
  const rules = useAuthFormRules(form);

  useFormErrors(form, resetPassword.error);

  // Liên kết thiếu token hoặc không còn hiệu lực
  if (
    !rawToken ||
    (isApiError(resetPassword.error) && resetPassword.error.code === "TokenInvalid")
  ) {
    return (
      <Result
        status="error"
        title={t("auth.verifyEmail.invalidTitle")}
        subTitle={t("auth.resetPassword.invalidDescription")}
        extra={<Link to="/forgot-password">{t("auth.resetPassword.requestAgain")}</Link>}
      />
    );
  }

  // Đặt lại mật khẩu thành công
  if (resetPassword.isSuccess) {
    return (
      <Result
        status="success"
        title={t("auth.resetPassword.successTitle")}
        subTitle={t("auth.resetPassword.successDescription")}
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

  // Form đặt mật khẩu mới
  return (
    <Flex vertical gap="middle">
      {/* Lỗi từ backend */}
      {resetPassword.error && (
        <Alert type="error" showIcon message={getErrorMessage(t, resetPassword.error)} />
      )}

      <Form
        form={form}
        aria-label={t("auth.resetPassword.title")}
        layout="vertical"
        disabled={resetPassword.isPending}
        onFinish={(fields: PasswordFields) => {
          // Gửi mật khẩu mới kèm token
          if (!resetPassword.isPending) {
            resetPassword.mutate({ token: rawToken, newPassword: fields.newPassword });
          }
        }}
      >
        {/* Mật khẩu mới và xác nhận */}
        <Form.Item
          name="newPassword"
          label={t("auth.fields.newPassword")}
          rules={rules.passwordRules}
          extra={t("validation.passwordHint")}
        >
          <Input.Password
            autoComplete="new-password"
            placeholder={t("auth.placeholders.password")}
          />
        </Form.Item>
        <Form.Item
          name="confirmPassword"
          label={t("auth.fields.confirmPassword")}
          dependencies={["newPassword"]}
          rules={rules.confirmRules("newPassword")}
        >
          <Input.Password
            autoComplete="new-password"
            placeholder={t("auth.placeholders.confirmPassword")}
          />
        </Form.Item>

        {/* Nút gửi */}
        <Button type="primary" htmlType="submit" block loading={resetPassword.isPending}>
          {t("auth.resetPassword.submit")}
        </Button>
      </Form>
    </Flex>
  );
}
