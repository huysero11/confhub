// apiError.ts — lỗi đã chuẩn hóa ở lớp lib, không chứa chữ hiển thị.
// apiClient tạo lỗi; hook và component dùng code để dịch thông báo, gán lỗi theo ô.
// ---------------- CẤU TRÚC LỖI API ----------------

export class ApiError extends Error {
  readonly status: number;
  readonly code: string;
  readonly fieldErrors: Record<string, string[]>;

  constructor(status: number, code: string, fieldErrors: Record<string, string[]> = {}) {
    super(code);
    this.name = "ApiError";
    this.status = status;
    this.code = code;
    this.fieldErrors = fieldErrors;
  }
}

// ---------------- NHẬN DIỆN LỖI API ----------------

export function isApiError(failure: unknown): failure is ApiError {
  return failure instanceof ApiError;
}
