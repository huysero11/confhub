// useFormErrors.ts — hook giữ lỗi trên form tài khoản luôn đúng (lớp hook).
// Chứa: (1) gán lỗi backend vào đúng ô — ApiError.fieldErrors, EmailTaken → ô email;
//       (2) Đổi ngôn ngữ thì kiểm tra lại các ô đã chạm để lỗi hiện bằng ngôn ngữ mới.
// Dùng ở: RegisterForm, LoginForm, ResetPasswordForm, ResendVerificationForm, ForgotPasswordForm.
import { useEffect, useRef } from "react";
import type { FormInstance } from "antd";
import { useTranslation } from "react-i18next";
import { getErrorMessage } from "@/i18n/errorMessage";
import { ApiError, isApiError } from "@/lib/apiError";

export function useFormErrors(form: FormInstance, failure: unknown) {
  const { t, i18n } = useTranslation();
  const previousLanguage = useRef(i18n.language);

  useEffect(() => {
    let isCancelled = false;

    async function showErrors() {
      // ---------------- KIỂM TRA LẠI KHI ĐỔI NGÔN NGỮ ----------------

      // antd giữ nguyên chuỗi lỗi đang hiện → đổi ngôn ngữ thì kiểm tra lại bằng rule (đã dịch) mới.
      // Làm TRƯỚC khi gán lỗi backend, nếu không lần kiểm tra lại sẽ xóa mất lỗi backend.
      if (previousLanguage.current !== i18n.language) {
        previousLanguage.current = i18n.language;
        await form.validateFields({ dirty: true }).catch(() => undefined);
      }

      // ---------------- GÁN LỖI BACKEND VÀO TỪNG Ô ----------------

      if (isCancelled || !isApiError(failure)) {
        return;
      }

      const fields = [];

      for (const [fieldName, codes] of Object.entries(failure.fieldErrors)) {
        const errors = [];

        for (const code of codes) {
          // Mã của validator backend (vd EmailValidator) nằm ở validation.*, mã nghiệp vụ ở errors.*.
          const fallbackMessage = getErrorMessage(t, new ApiError(failure.status, code));

          errors.push(t(`validation.${code}`, { defaultValue: fallbackMessage }));
        }

        fields.push({ name: fieldName, errors });
      }

      if (failure.code === "EmailTaken") {
        fields.push({ name: "email", errors: [getErrorMessage(t, failure)] });
      }

      if (fields.length > 0) {
        form.setFields(fields);
      }
    }

    // Bỏ qua kết quả cũ khi effect chạy lại hoặc form bị tháo khỏi giao diện.
    void showErrors();

    return () => {
      isCancelled = true;
    };
  }, [failure, form, i18n.language, t]);
}
