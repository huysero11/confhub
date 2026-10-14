// useRegister.ts — hook mutation tạo tài khoản và mở trang kiểm tra email.
// RegisterForm dùng trạng thái gửi/lỗi; email đích lấy từ response đã chuẩn hóa của backend.
import { useMutation } from "@tanstack/react-query";
import { useNavigate } from "react-router";
import { register } from "../api";

export function useRegister() {
  const navigate = useNavigate();

  return useMutation({
    mutationFn: register,
    onSuccess: (registration) => {
      void navigate(`/register/check-email?email=${encodeURIComponent(registration.email)}`);
    },
  });
}
