// useLogin.ts — hook mutation đăng nhập, lưu phiên rồi điều hướng nội bộ.
// LoginForm dùng để nhận trạng thái gửi/lỗi; địa chỉ đích được kiểm tra tập trung.
import { useMutation } from "@tanstack/react-query";
import { useNavigate } from "react-router";
import { login } from "../api";
import { resolvePostLoginPath } from "../roleLandingPaths";
import { setSession } from "@/lib/authSession";

export function useLogin(returnUrl: string | null) {
  const navigate = useNavigate();

  return useMutation({
    mutationFn: login,
    onSuccess: (session) => {
      setSession(session);

      void navigate(resolvePostLoginPath(session.user, returnUrl), { replace: true });
    },
  });
}
