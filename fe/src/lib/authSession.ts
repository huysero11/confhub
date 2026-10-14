// authSession.ts — kho phiên trong bộ nhớ thuộc lớp lib; tải lại trang sẽ mất token.
// apiClient và hook cập nhật phiên; useAuthSession đăng ký thay đổi qua useSyncExternalStore.

// ----------------------------------------- KIỂU DỮ LIỆU -----------------------------------------
export type RoleCode = "Attendee" | "Organizer" | "Supplier" | "Staff" | "Admin";

export type CurrentUser = {
  id: string;
  email: string;
  fullName: string;
  role: RoleCode;
  organization: string | null;
  permissions: string[];
};

export type SessionResponse = {
  accessToken: string;
  expiresIn: number;
  user: CurrentUser;
};

// ------------------------------------- DỮ LIỆU TRONG BỘ NHỚ -------------------------------------
let session: SessionResponse | null = null;
let revision = 0; // Tăng mỗi lần phiên đổi (đăng nhập, đăng xuất, làm mới)
const listeners = new Set<() => void>(); // Danh sách các hàm cần gọi khi session thay đổi

// ------------------------------------------ ĐỌC PHIÊN -------------------------------------------
export function getSession() {
  return session;
}

export function getAccessToken() {
  return session?.accessToken ?? null;
}

export function getSessionRevision() {
  return revision;
}

// --------------------------- GHI PHIÊN: ĐĂNG NHẬP, LÀM MỚI, ĐĂNG XUẤT ---------------------------
export function setSession(nextSession: SessionResponse) {
  session = nextSession;
  notifySubscribers();
}

export function clearSession() {
  session = null;
  notifySubscribers();
}

// ------------------------ THEO DÕI THAY ĐỔI (useSyncExternalStore dùng) -------------------------
/*
- React tự truyền một hàm listener vào subscribe.
- Bạn lưu hàm đó trong listeners.
- Khi session thay đổi, bạn gọi các listener để thông báo cho React.
- Hàm return dùng để hủy đăng ký khi không cần theo dõi nữa. 
*/
export function subscribe(listener: () => void) {
  listeners.add(listener);

  return () => {
    listeners.delete(listener);
  };
}

function notifySubscribers() {
  revision += 1;

  for (const listener of listeners) {
    /* 
      Gọi các listener là callback mà react tự truyền vào, 
      các hàm này sẽ báo cho các component là dữ liệu đã thay đổi
    */
    listener();
  }
}
