// useAuthSession.ts — hook đọc phiên trong bộ nhớ, không gọi API.
// Component header và AuthCard dùng để phản ứng ngay khi đăng nhập/đăng xuất.
import { useSyncExternalStore } from "react";
import { getSession, subscribe } from "@/lib/authSession";

/*
useSyncExternalStore(
  subscribe,
  getSnapshot,
  getServerSnapshot?
);

Giúp React theo dõi kho này và tự động render lại khi session thay đổi.

- subscribe: đăng ký nghe thay đổi
- getSnapshot: lấy dữ liệu hiện tại

*/

// Hàm này trả ra giá trị của getSession
export function useAuthSession() {
  return useSyncExternalStore(subscribe, getSession);
}
