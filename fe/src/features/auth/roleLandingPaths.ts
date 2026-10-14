// roleLandingPaths.ts — bảng điều hướng sau đăng nhập của nhóm tài khoản.
// useLogin dùng hàm thuần này; chỉ đổi bảng khi các shell quản lý đã được triển khai.
import type { CurrentUser, RoleCode } from "@/lib/authSession";

// ---------------- TRANG MẶC ĐỊNH THEO VAI TRÒ ----------------

export const roleLandingPaths: Record<RoleCode, string> = {
  Attendee: "/",
  Organizer: "/",
  Supplier: "/",
  Staff: "/",
  Admin: "/",
};

// ---------------- CHỌN ĐƯỜNG DẪN SAU ĐĂNG NHẬP ----------------

export function resolvePostLoginPath(user: CurrentUser, returnUrl: string | null) {
  // Trình duyệt coi dấu gạch chéo ngược và ký tự điều khiển như thành phần URL đặc biệt.
  if (returnUrl?.startsWith("/") && !returnUrl.startsWith("//") && !/[\\\s]/u.test(returnUrl)) {
    return returnUrl;
  }

  return roleLandingPaths[user.role];
}
