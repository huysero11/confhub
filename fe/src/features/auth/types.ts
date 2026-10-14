// types.ts — kiểu dữ liệu của nhóm tài khoản, khớp request/response backend.
// Dùng chung cho các lớp api, hook và component; không chứa logic giao diện.
import type { RoleCode } from "@/lib/authSession";

export type RegisterRequest = {
  fullName: string;
  email: string;
  password: string;
  role: Extract<RoleCode, "Attendee" | "Organizer" | "Supplier">;
  organization?: string;
};

export type RegisterResponse = { email: string };
export type VerifyEmailResponse = { status: "Active" | "PendingApproval" };
export type LoginRequest = { email: string; password: string };
export type ResetPasswordRequest = { token: string; newPassword: string };
