// renderApp.tsx — vẽ cả ứng dụng (provider + router) trong test, mở sẵn ở một địa chỉ.
// Chứa: hàm renderApp(path) dùng createMemoryRouter (địa chỉ nằm trong bộ nhớ, không cần trình duyệt).
// Dùng ở: các file *.test.tsx cần đi qua route thật. Bọc StrictMode như main.tsx để bắt lỗi gọi API 2 lần.
import { render, screen, waitFor } from "@testing-library/react";
import { StrictMode } from "react";
import { QueryClient } from "@tanstack/react-query";
import { afterEach, expect } from "vitest";
import { createMemoryRouter, RouterProvider } from "react-router";
import { AppProviders } from "@/app/AppProviders";
import i18n from "i18next";
import { appRoutes } from "@/app/router";

const queryClients: QueryClient[] = [];
afterEach(() => {
  for (const queryClient of queryClients) {
    queryClient.clear();
  }
  queryClients.length = 0;
});

export async function renderApp(path = "/") {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false, refetchOnWindowFocus: false, gcTime: Infinity },
      mutations: { retry: false },
    },
  });
  queryClients.push(queryClient);
  // memoryRouter là router mà history/url nằm trong memory, phù hợp để test
  const memoryRouter = createMemoryRouter(appRoutes, { initialEntries: [path] });

  const renderedApp = render(
    <StrictMode>
      <AppProviders queryClient={queryClient}>
        <RouterProvider router={memoryRouter} />
      </AppProviders>
    </StrictMode>,
  );
  // Chờ SessionBootstrap khôi phục phiên xong (vòng quay "đang tải" biến mất) rồi mới trả về.
  await waitFor(() => {
    expect(screen.queryByLabelText(i18n.t("auth.loading"))).not.toBeInTheDocument();
  });
  return { ...renderedApp, memoryRouter, queryClient };
}
