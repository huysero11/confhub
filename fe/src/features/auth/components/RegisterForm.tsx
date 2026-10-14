// RegisterForm.tsx — component form đăng ký UC01 dùng trong RegisterPage.
// Chọn vai trò, kiểm tra BR01–BR04 và hiển thị lỗi từ hook; chỉ gửi các trường backend nhận.
import { Alert, Button, Flex, Form, Input, Radio, Space, Typography, theme } from "antd";
import { useTranslation } from "react-i18next";
import { Link } from "react-router";
import { useRegister } from "../hooks/useRegister";
import { useAuthFormRules } from "../hooks/useAuthFormRules";
import { useFormErrors } from "../hooks/useFormErrors";
import { isApiError } from "@/lib/apiError";
import { getErrorMessage } from "@/i18n/errorMessage";
import type { RegisterRequest } from "../types";

type RegisterFields = RegisterRequest & { confirmPassword: string };
const registerRoles: RegisterRequest["role"][] = ["Attendee", "Organizer", "Supplier"];

export function RegisterForm() {
  const { t } = useTranslation();
  const { token } = theme.useToken();
  const [form] = Form.useForm<RegisterFields>();
  const registration = useRegister();
  const rules = useAuthFormRules(form);

  useFormErrors(form, registration.error);

  // role ở radio được chọn, useWatch là hook của form để theo dõi một trường, nó đổi thì render lại
  const selectedRole = Form.useWatch("role", form) as RegisterFields["role"] | undefined;
  const requiresOrganization = selectedRole === "Organizer" || selectedRole === "Supplier";

  function submit(fields: RegisterFields) {
    if (registration.isPending) {
      return;
    }

    registration.mutate({
      fullName: fields.fullName.trim(),
      email: fields.email.trim(),
      password: fields.password,
      role: fields.role,
      organization: requiresOrganization ? fields.organization?.trim() : undefined,
    });
  }

  return (
    <Flex vertical gap="middle">
      {/* Lỗi từ BE */}
      {registration.error && (
        <Alert
          type="error"
          showIcon
          message={getErrorMessage(t, registration.error)}
          description={
            isApiError(registration.error) &&
            registration.error.code === "EmailTaken" && (
              <Space wrap>
                <Link to="/login">{t("auth.header.login")}</Link>
                <Link to="/forgot-password">{t("auth.login.forgotPassword")}</Link>
              </Space>
            )
          }
        />
      )}

      <Form
        form={form}
        aria-label={t("auth.register.title")}
        layout="vertical"
        initialValues={{ role: "Attendee" }}
        onFinish={submit}
        disabled={registration.isPending}
      >
        {/* Chọn vai trò */}
        <Form.Item name="role" label={t("auth.register.role")} rules={[rules.requiredRule]}>
          <Radio.Group style={{ width: "100%" }}>
            <Flex vertical gap="small">
              {registerRoles.map((role) => (
                <Radio
                  key={role}
                  value={role}
                  style={{
                    border: `${token.lineWidth}px ${token.lineType} ${selectedRole === role ? token.colorPrimary : token.colorBorder}`,
                    borderRadius: token.borderRadius,
                    padding: token.paddingSM,
                  }}
                >
                  <Flex vertical>
                    <Typography.Text strong>{t(`role.${role}.name`)}</Typography.Text>
                    <Typography.Text type="secondary">
                      {t(`role.${role}.description`)}
                    </Typography.Text>
                  </Flex>
                </Radio>
              ))}
            </Flex>
          </Radio.Group>
        </Form.Item>

        {/* Ghi chú về việc duyệt tài khoản của vai trò Organizer và Supplier */}
        {requiresOrganization && (
          <Form.Item>
            <Alert type="warning" showIcon message={t("auth.register.approval")} />
          </Form.Item>
        )}

        {/* Thông tin cá nhân và đơn vị */}
        <Form.Item name="fullName" label={t("auth.fields.fullName")} rules={rules.fullNameRules}>
          <Input autoComplete="name" placeholder={t("auth.placeholders.fullName")} />
        </Form.Item>
        {requiresOrganization && (
          <Form.Item
            name="organization"
            label={t("auth.fields.organization")}
            rules={rules.organizationRules}
            preserve={false}
          >
            <Input autoComplete="organization" placeholder={t("auth.placeholders.organization")} />
          </Form.Item>
        )}

        {/* Thông tin đăng nhập và xác nhận mật khẩu */}
        <Form.Item name="email" label={t("auth.fields.email")} rules={rules.emailRules}>
          <Input autoComplete="email" placeholder={t("auth.placeholders.email")} />
        </Form.Item>
        <Form.Item
          name="password"
          label={t("auth.fields.password")}
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
          dependencies={["password"]}
          rules={rules.confirmRules("password")}
        >
          <Input.Password
            autoComplete="new-password"
            placeholder={t("auth.placeholders.confirmPassword")}
          />
        </Form.Item>

        {/* Nút gửi */}
        <Button type="primary" htmlType="submit" block loading={registration.isPending}>
          {t("auth.register.submit")}
        </Button>
      </Form>

      {/* Liên kết sang trang đăng nhập */}
      <Space wrap>
        <Typography.Text>{t("auth.register.haveAccount")}</Typography.Text>
        <Link to="/login">{t("auth.header.login")}</Link>
      </Space>
    </Flex>
  );
}
