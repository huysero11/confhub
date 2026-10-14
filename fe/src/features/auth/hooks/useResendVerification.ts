// useResendVerification.ts — hook mutation gửi lại email xác thực (BR09).
// Hai component gửi lại dùng chung; thông báo thành công không tiết lộ tài khoản tồn tại.
import { useMutation } from "@tanstack/react-query";
import { resendVerification } from "../api";

export function useResendVerification() {
  return useMutation({ mutationFn: resendVerification });
}
