// ForgotPasswordForm.tsx — component yêu cầu email đặt lại mật khẩu cho ForgotPasswordPage.
// Gửi qua hook; khi thành công chỉ hiện thông báo chung theo BR09, không suy đoán tài khoản.
import { Alert, Button, Flex, Form, Input } from "antd";
import { useTranslation } from "react-i18next";
import { Link } from "react-router";
import { useForgotPassword } from "../hooks/useForgotPassword";
import { useAuthFormRules } from "../hooks/useAuthFormRules";
import { useFormErrors } from "../hooks/useFormErrors";
import { getErrorMessage } from "@/i18n/errorMessage";

export function ForgotPasswordForm() {
  // Trạng thái form và hook gọi API
  const { t } = useTranslation();
  const [form] = Form.useForm<{ email: string }>();
  const forgotPassword = useForgotPassword();
  const rules = useAuthFormRules(form);

  useFormErrors(form, forgotPassword.error);

  return (
    <Flex vertical gap="middle">
      {/* Kết quả gửi yêu cầu hoặc form nhập email */}
      {forgotPassword.isSuccess ? (
        <Alert type="success" showIcon message={t("auth.forgotPassword.sent")} />
      ) : (
        <>
          {/* Lỗi từ backend */}
          {forgotPassword.error && (
            <Alert type="error" showIcon message={getErrorMessage(t, forgotPassword.error)} />
          )}

          {/* Email nhận liên kết đặt lại mật khẩu */}
          <Form
            form={form}
            aria-label={t("auth.forgotPassword.title")}
            layout="vertical"
            disabled={forgotPassword.isPending}
            onFinish={({ email }: { email: string }) => {
              // Gửi yêu cầu đặt lại mật khẩu
              if (!forgotPassword.isPending) {
                forgotPassword.mutate(email.trim());
              }
            }}
          >
            <Form.Item name="email" label={t("auth.fields.email")} rules={rules.emailRules}>
              <Input autoComplete="email" placeholder={t("auth.placeholders.email")} />
            </Form.Item>

            {/* Nút gửi */}
            <Button type="primary" htmlType="submit" block loading={forgotPassword.isPending}>
              {t("auth.forgotPassword.submit")}
            </Button>
          </Form>
        </>
      )}

      {/* Liên kết về trang đăng nhập */}
      <Link to="/login">{t("auth.forgotPassword.backToLogin")}</Link>
    </Flex>
  );
}
