// mockMatchMedia.ts — giả lập window.matchMedia cho môi trường test (jsdom không có sẵn).
// Chứa: hàm mockMatchMedia(prefersDark) — quyết định hệ điều hành "đang" dùng nền tối hay sáng.
// Dùng ở: test/setup.ts (mặc định nền sáng) và test cần giả nền tối của hệ điều hành.
export function mockMatchMedia(prefersDark: boolean): void {
  Object.defineProperty(window, "matchMedia", {
    writable: true,
    configurable: true,
    value: (query: string): MediaQueryList => ({
      // Chỉ câu hỏi "nền tối?" mới trả true (khi prefersDark); mọi câu hỏi khác của antd trả false.
      matches: prefersDark && query === "(prefers-color-scheme: dark)",
      media: query,
      onchange: null,
      addListener: () => undefined,
      removeListener: () => undefined,
      addEventListener: () => undefined,
      removeEventListener: () => undefined,
      dispatchEvent: () => false,
    }),
  });
}
