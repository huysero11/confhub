// setup.ts — chạy trước mỗi file test (khai ở vite.config.ts, mục test.setupFiles).
// Chứa: thêm các phép so sánh cho DOM (toBeInTheDocument...), khởi tạo song ngữ,
//       đưa mọi trạng thái dùng chung về ban đầu trước mỗi test.
import "@testing-library/jest-dom/vitest";
import { cleanup } from "@testing-library/react";
import { afterEach, beforeEach, vi } from "vitest";
import { changeAppLanguage } from "@/i18n/i18n";
import { mockMatchMedia } from "@/test/mockMatchMedia";
import { clearSession } from "@/lib/authSession";
import { apiMock } from "@/test/apiMock";

beforeEach(async () => {
  clearSession();
  apiMock.reset();
  apiMock.onPost("/auth/refresh").reply(401, { code: "SessionExpired" });
  // Mặc định: hệ điều hành nền sáng, chưa lưu lựa chọn nào, giao diện tiếng Việt.
  mockMatchMedia(false);
  window.localStorage.clear();
  await changeAppLanguage("vi");
  window.localStorage.clear();
});

afterEach(() => {
  // Gỡ giao diện của test vừa chạy để test sau bắt đầu từ trang trống.
  cleanup();
  clearSession();
  vi.unstubAllGlobals();
  vi.restoreAllMocks();
  vi.useRealTimers();
});
