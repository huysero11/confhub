// main.tsx — điểm vào của ứng dụng: index.html nạp file này đầu tiên.
// Chứa: nạp phông chữ, CSS chung, khởi tạo song ngữ; gắn provider + router vào phần tử #root.
import "@ant-design/v5-patch-for-react-19";
import "@fontsource/be-vietnam-pro/400.css";
import "@fontsource/be-vietnam-pro/500.css";
import "@fontsource/be-vietnam-pro/600.css";
import "@/theme/global.css";
import "@/i18n/i18n";
import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { RouterProvider } from "react-router";
import { AppProviders } from "@/app/AppProviders";
import { router } from "@/app/router";

const rootElement = document.getElementById("root");
if (!rootElement) {
  throw new Error("Không tìm thấy phần tử #root trong index.html");
}

// StrictMode: ở chế độ phát triển React chạy mỗi component 2 lần để lộ lỗi sớm.
createRoot(rootElement).render(
  <StrictMode>
    <AppProviders>
      <RouterProvider router={router} />
    </AppProviders>
  </StrictMode>,
);
