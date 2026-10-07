// main.tsx — điểm vào của ứng dụng: index.html nạp file này đầu tiên.
// Chứa: nạp CSS chung, gắn component gốc vào phần tử #root.
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
    <App />
  </StrictMode>,
);
