// vite.config.ts — cấu hình Vite (máy chủ phát triển + đóng gói).
// Chứa: plugin React, alias "@/" trỏ tới src, cổng cố định của máy chủ phát triển, cấu hình test (Vitest).
/// <reference types="vitest/config" />
import { fileURLToPath, URL } from "node:url";
import react from "@vitejs/plugin-react";
import { defineConfig } from "vite";

export default defineConfig({
  plugins: [react()],
  resolve: {
    // Viết import '@/theme/...' thay vì '../../theme/...'.
    alias: { "@": fileURLToPath(new URL("./src", import.meta.url)) },
  },
  server: {
    // Cố định 5173: backend ghép link trong email theo địa chỉ này (Frontend:BaseUrl).
    // strictPort: cổng bận thì báo lỗi, không tự nhảy sang cổng khác.
    port: 5173,
    strictPort: true,
  },
  test: {
    // jsdom: giả lập trình duyệt (DOM, localStorage) để chạy test component ngoài trình duyệt.
    environment: "jsdom",
    setupFiles: ["./src/test/setup.ts"],
  },
});
