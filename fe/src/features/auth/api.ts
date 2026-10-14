// api.ts — lớp api của nhóm tài khoản: gọi mỗi endpoint UC01/UC02 bằng axios.
// Hook dùng các hàm này qua TanStack Query; apiClient gắn token và chuẩn hóa lỗi.
import type { SessionResponse } from "@/lib/authSession";
import { apiClient } from "@/lib/apiClient";
import type {
  LoginRequest,
  RegisterRequest,
  RegisterResponse,
  ResetPasswordRequest,
  VerifyEmailResponse,
} from "./types";

export async function register(request: RegisterRequest) {
  const response = await apiClient.post<RegisterResponse>("/auth/register", request);

  return response.data;
}

export async function verifyEmail(rawToken: string) {
  const response = await apiClient.post<VerifyEmailResponse>("/auth/verify-email", {
    token: rawToken,
  });

  return response.data;
}

export async function resendVerification(email: string) {
  await apiClient.post("/auth/resend-verification", { email });
}

export async function forgotPassword(email: string) {
  await apiClient.post("/auth/forgot-password", { email });
}

export async function resetPassword(request: ResetPasswordRequest) {
  await apiClient.post("/auth/reset-password", request);
}

export async function login(request: LoginRequest) {
  const response = await apiClient.post<SessionResponse>("/auth/login", request);

  return response.data;
}

export async function logout() {
  await apiClient.post("/auth/logout");
}
