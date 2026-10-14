// errorMessage.ts — hàm dịch lỗi dùng chung (lớp i18n).
// Chứa: getErrorMessage(t, failure) — lấy chữ theo mã lỗi backend (errors.<code>), mã lạ thì lỗi chung.
// Dùng ở: các form, trang và hook cần hiện lỗi cho người dùng.
import type { TFunction } from "i18next";
import { isApiError } from "@/lib/apiError";

export function getErrorMessage(t: TFunction, failure: unknown): string {
  const code = isApiError(failure) ? failure.code : "Unknown";

  return t(`errors.${code}`, { defaultValue: t("errors.Unknown") });
}
