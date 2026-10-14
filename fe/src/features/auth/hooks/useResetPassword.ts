// useResetPassword.ts — hook mutation đặt mật khẩu bằng token trong email.
// ResetPasswordForm dùng trạng thái thành công hoặc lỗi liên kết không còn hiệu lực.
import { useMutation } from "@tanstack/react-query";
import { resetPassword } from "../api";

export function useResetPassword() {
  return useMutation({ mutationFn: resetPassword });
}
