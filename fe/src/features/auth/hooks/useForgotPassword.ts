// useForgotPassword.ts — hook mutation yêu cầu email đặt lại mật khẩu.
// ForgotPasswordForm dùng để hiện thông báo chung khi backend chấp nhận yêu cầu.
import { useMutation } from "@tanstack/react-query";
import { forgotPassword } from "../api";

export function useForgotPassword() {
  return useMutation({ mutationFn: forgotPassword });
}
