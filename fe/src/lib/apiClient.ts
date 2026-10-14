// apiClient.ts — lớp lib gọi API bằng axios, gắn token và chuẩn hóa ProblemDetails.
// Các file api của từng nhóm dùng apiClient; bootstrap và xử lý 401 dùng chung refreshSession.
import axios, { type AxiosError, type InternalAxiosRequestConfig } from "axios";
import type { SessionResponse } from "@/lib/authSession";
import { ApiError, isApiError } from "@/lib/apiError";
import { clearSession, getAccessToken, getSessionRevision, setSession } from "@/lib/authSession";

// ---------------------------------------- CẤU HÌNH AXIOS ----------------------------------------

/*
Mở rộng type có sẵn của package axios và thêm field mới vào InternalAxiosRequestConfig
  là isRetryAfterRefresh
*/
declare module "axios" {
  interface InternalAxiosRequestConfig {
    isRetryAfterRefresh?: boolean;
  }
}

// Cùng địa chỉ (qua proxy của Vite) nên trình duyệt tự gửi cookie, không cần withCredentials.
export const apiClient = axios.create({ baseURL: "/api" });

// Trước mỗi request: gắn access token (nếu đang đăng nhập).
apiClient.interceptors.request.use(attachAccessToken);

// Response lỗi: 401 thì làm mới phiên rồi gửi lại 1 lần; còn lại đổi thành ApiError.
apiClient.interceptors.response.use((response) => response, handleResponseError);

// -------------------------------------- REQUEST: GẮN TOKEN --------------------------------------

function attachAccessToken(config: InternalAxiosRequestConfig) {
  const rawAccessToken = getAccessToken();

  if (rawAccessToken) {
    config.headers.Authorization = `Bearer ${rawAccessToken}`;
  }

  return config;
}

// ------------------------------- RESPONSE LỖI: ĐỔI THÀNH ApiError -------------------------------

function handleResponseError(error: unknown) {
  if (!axios.isAxiosError(error)) {
    throw error;
  }

  if (shouldRefreshAndRetry(error)) {
    return refreshAndRetry(error.config);
  }

  throw createApiError(error);
}

function createApiError(error: AxiosError): ApiError {
  if (!error.response) {
    return new ApiError(0, "Network");
  }

  const { status, data: payload } = error.response;

  if (!isRecord(payload)) {
    return new ApiError(status, "Network");
  }

  const fieldErrors: Record<string, string[]> = {};

  if (isRecord(payload.errors)) {
    for (const [field, codes] of Object.entries(payload.errors)) {
      if (Array.isArray(codes) && codes.every((code) => typeof code === "string")) {
        const fieldName = field.charAt(0).toLowerCase() + field.slice(1);

        fieldErrors[fieldName] = codes;
      }
    }
  }

  const code = typeof payload.code === "string" ? payload.code : "Unknown";

  return new ApiError(status, code, fieldErrors);
}

function isRecord(payload: unknown): payload is Record<string, unknown> {
  return typeof payload === "object" && payload !== null && !Array.isArray(payload);
}

// ----------------------------- 401: LÀM MỚI PHIÊN RỒI GỬI LẠI 1 LẦN -----------------------------

/*
- error is AxiosError & { config: InternalAxiosRequestConfig } 
  được gọi là type predicate.
  Ý nghĩa: Nếu hàm này trả true, TypeScript sẽ coi error chắc chắn có kiểu AxiosError và chắc chắn có config.
*/
function shouldRefreshAndRetry(
  error: AxiosError,
): error is AxiosError & { config: InternalAxiosRequestConfig } {
  return (
    error.response?.status === 401 &&
    error.config !== undefined &&
    !error.config.isRetryAfterRefresh &&
    !error.config.url?.startsWith("/auth/") &&
    Boolean(error.config.headers.Authorization)
  );
}

async function refreshAndRetry(config: InternalAxiosRequestConfig) {
  // Một request khác có thể đã làm mới xong trước khi lỗi 401 này trở về.
  if (config.headers.Authorization === `Bearer ${getAccessToken()}`) {
    try {
      await refreshSession();
    } catch {
      expireSession();
    }
  }

  if (!getAccessToken()) {
    expireSession();
  }

  config.isRetryAfterRefresh = true;

  try {
    return await apiClient.request(config);
  } catch (failure: unknown) {
    if (isApiError(failure) && failure.status === 401) {
      expireSession();
    }

    throw failure;
  }
}

function expireSession(): never {
  clearSession();
  window.location.assign("/login?reason=sessionExpired");

  throw new ApiError(401, "SessionExpired");
}

// ----------------------------- REFRESH TOKEN: DÙNG CHUNG 1 REQUEST ------------------------------
/*

Vấn đề cần giải quyết: Refresh token chỉ dùng được một lần. Mỗi lần gọi /auth/refresh, backend:
- Nhận cookie cũ, hủy nó;
- Trả về cookie mới.

Nếu cùng một cookie cũ được gửi lên 2 lần, backend hiểu là token bị đánh cắp và thu hồi mọi phiên (BR12). Người dùng bị đá ra ngoài.
Mà trong app, có lúc 2 chỗ cùng gọi refreshSession() gần như cùng lúc:
- StrictMode chạy effect 2 lần;
- Từ nhóm 2 trở đi: 2 API cùng nhận 401 khi token hết hạn.

Không có pendingRefresh thì sao
t=0ms   chỗ A gọi refreshSession() → gửi request 1 (cookie X)
t=1ms   chỗ B gọi refreshSession() → gửi request 2 (cookie X)   ← vẫn cookie X,
                                                                  vì request 1 chưa trả về
t=50ms  backend nhận request 1: hủy X, trả cookie Y          → OK
t=51ms  backend nhận request 2: cookie X đã bị hủy mà vẫn dùng → "bị đánh cắp" → thu hồi hết

*/
/*
Kiểm soát bằng biến pendingRefresh: lưu lời gọi refresh đang chạy (Promise); null = không có gì đang chạy.
Đặt biến ở ngoài hàm để gọi 2 lần thì nó không bị ảnh hưởng, và trước khi gọi endpoint thì kiểm tra biến đó trước xem đã gọi chưa

*/
let pendingRefresh: Promise<SessionResponse> | null = null;

export function refreshSession(): Promise<SessionResponse> {
  if (pendingRefresh) {
    return pendingRefresh;
  }

  const startingRevision = getSessionRevision();

  pendingRefresh = requestSessionRefresh()
    .then((nextSession) => {
      // Không phục hồi phiên cũ nếu người dùng đã đăng xuất/đăng nhập trong lúc chờ.
      if (getSessionRevision() !== startingRevision) {
        throw new ApiError(401, "SessionExpired");
      }

      setSession(nextSession);

      return nextSession;
    })
    .catch((failure: unknown) => {
      if (getSessionRevision() === startingRevision) {
        clearSession();
      }

      throw failure;
    })
    .finally(() => {
      pendingRefresh = null;
    });

  return pendingRefresh;
}

async function requestSessionRefresh(): Promise<SessionResponse> {
  const response = await apiClient.post<SessionResponse>("/auth/refresh");

  return response.data;
}
