// main.tsx — điểm vào của ứng dụng: index.html nạp file này đầu tiên.
// Chứa: nạp CSS chung, gắn component gốc vào phần tử #root.
import "@ant-design/v5-patch-for-react-19";
import "@fontsource/be-vietnam-pro/400.css";
import "@fontsource/be-vietnam-pro/500.css";
import "@fontsource/be-vietnam-pro/600.css";
import { ThemeProvider } from "@/theme/ThemeProvider";
import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import "@/theme/global.css";
import { App } from "@/App";

const rootElement = document.getElementById("root");
if (!rootElement) {
  throw new Error("Không tìm thấy phần tử #root trong index.html");
}

// StrictMode: ở chế độ phát triển React chạy mỗi component 2 lần để lộ lỗi sớm.
createRoot(rootElement).render(
  <StrictMode>
    <ThemeProvider>
      <App />
    </ThemeProvider>
  </StrictMode>,
);
