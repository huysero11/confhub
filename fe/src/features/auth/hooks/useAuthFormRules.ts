// useAuthFormRules.ts — hook quy tắc nhập liệu dùng chung cho các form tài khoản (lớp hook).
// Chứa: rule bắt buộc, email, mật khẩu, họ tên, đơn vị, nhập lại mật khẩu (trùng BR01–BR04 của backend).
// Dùng ở: các form trong features/auth/components. Chữ lỗi theo ngôn ngữ hiện tại (qua t).
import type { FormInstance } from "antd";
import type { Rule } from "antd/es/form";
import { useTranslation } from "react-i18next";

export function useAuthFormRules(form: FormInstance) {
  const { t } = useTranslation();

  // ---------------- QUY TẮC BẮT BUỘC ----------------

  const requiredRule: Rule = {
    required: true,
    whitespace: true,
    message: t("validation.NotEmptyValidator"),
  };

  // ---------------- THÔNG TIN CÁ NHÂN VÀ ĐƠN VỊ ----------------

  const emailRules: Rule[] = [
    requiredRule,
    {
      type: "email",
      max: 256,
      transform: (email: string | undefined) => email?.trim(),
      message: t("validation.email"),
    },
  ];

  const fullNameRules: Rule[] = [
    requiredRule,
    {
      min: 2,
      max: 100,
      transform: (fullName: string | undefined) => fullName?.trim(),
      message: t("validation.fullName"),
    },
  ];

  const organizationRules: Rule[] = [
    requiredRule,
    {
      max: 200,
      transform: (organization: string | undefined) => organization?.trim(),
      message: t("validation.organization"),
    },
  ];

  // ---------------- MẬT KHẨU VÀ XÁC NHẬN ----------------

  const passwordRules: Rule[] = [
    requiredRule,
    {
      min: 8,
      max: 128,
      pattern: /^(?=.*\p{L})(?=.*\p{Nd}).*$/su,
      message: t("validation.password"),
    },
  ];

  function confirmRules(passwordField: string): Rule[] {
    return [
      requiredRule,
      {
        validator: async (_rule, confirmation: string) => {
          if (confirmation !== form.getFieldValue(passwordField)) {
            throw new Error(t("validation.confirm"));
          }
        },
      },
    ];
  }

  // ---------------- BỘ QUY TẮC CHO FORM ----------------

  return {
    requiredRule,
    emailRules,
    passwordRules,
    fullNameRules,
    organizationRules,
    confirmRules,
  };
}
