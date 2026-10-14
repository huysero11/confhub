// LoginForm.tsx — form đăng nhập (lớp component), dùng trong LoginPage.
// Chứa: ô email + mật khẩu, Alert lỗi theo mã backend; chưa xác thực email thì có nút gửi lại email
//       xác thực tới đúng email đang nhập. Lưu phiên + chuyển trang nằm ở hook useLogin.
import { Alert, Button, Flex, Form, Input, Space, Typography } from "antd";
import { useTranslation } from "react-i18next";
import { Link } from "react-router";
import { getErrorMessage } from "@/i18n/errorMessage";
import { isApiError } from "@/lib/apiError";
import { useFormErrors } from "../hooks/useFormErrors";
import { useAuthFormRules } from "../hooks/useAuthFormRules";
import { useLogin } from "../hooks/useLogin";
import { useResendVerification } from "../hooks/useResendVerification";
import type { LoginRequest } from "../types";

type LoginFormProps = {
  returnUrl: string | null;
  reason: string | null;
};

export function LoginForm({ returnUrl, reason }: LoginFormProps) {
  const { t } = useTranslation();
  const [form] = Form.useForm<LoginRequest>();
  const login = useLogin(returnUrl);
  const resend = useResendVerification();
  const rules = useAuthFormRules(form);

  useFormErrors(form, login.error);

  // Cho phép gửi lại email khi tài khoản chưa xác thực
  const isUnverified = isApiError(login.error) && login.error.code === "AccountUnverified";

  function resendVerificationEmail() {
    const email = form.getFieldValue("email");

    if (resend.isPending || typeof email !== "string") {
      return;
    }

    resend.mutate(email.trim());
  }

  function submit(fields: LoginRequest) {
    if (login.isPending) {
      return;
    }

    // Reset trạng thái của mutation
    resend.reset();

    login.mutate({ email: fields.email.trim(), password: fields.password });
  }

  return (
    <Flex vertical gap="middle">
      {/* Cảnh báo phiên hết hạn */}
      {reason === "sessionExpired" && (
        <Alert type="warning" showIcon message={t("errors.SessionExpired")} />
      )}

      {/* Lỗi từ BE và nút gửi lại email */}
      {login.error && (
        <Alert
          type="error"
          showIcon
          message={getErrorMessage(t, login.error)}
          description={
            isUnverified && (
              <Button size="small" loading={resend.isPending} onClick={resendVerificationEmail}>
                {t("auth.checkEmail.resend")}
              </Button>
            )
          }
        />
      )}

      {/* Thông báo resend thành công */}
      {resend.isSuccess && <Alert type="success" showIcon message={t("auth.checkEmail.resent")} />}

      <Form
        form={form}
        aria-label={t("auth.login.title")}
        layout="vertical"
        disabled={login.isPending}
        onFinish={submit}
      >
        {/* Thông tin đăng nhập */}
        <Form.Item name="email" label={t("auth.fields.email")} rules={rules.emailRules}>
          <Input autoComplete="username" placeholder={t("auth.placeholders.email")} />
        </Form.Item>
        <Form.Item
          name="password"
          label={t("auth.fields.password")}
          rules={[rules.requiredRule, { max: 128, message: t("validation.passwordMax") }]}
        >
          <Input.Password autoComplete="current-password" />
        </Form.Item>

        {/* Liên kết quên mật khẩu */}
        <Form.Item>
          <Link to="/forgot-password">{t("auth.login.forgotPassword")}</Link>
        </Form.Item>

        {/* Nút gửi */}
        <Button type="primary" htmlType="submit" block loading={login.isPending}>
          {t("auth.header.login")}
        </Button>
      </Form>

      {/* Liên kết sang trang đăng kí */}
      <Space wrap>
        <Typography.Text>{t("auth.login.noAccount")}</Typography.Text>
        <Link to="/register">{t("auth.header.register")}</Link>
      </Space>
    </Flex>
  );
}
