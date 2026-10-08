// renderApp.tsx — vẽ cả ứng dụng (provider + router) trong test, mở sẵn ở một địa chỉ.
// Chứa: hàm renderApp(path) dùng createMemoryRouter (địa chỉ nằm trong bộ nhớ, không cần trình duyệt).
// Dùng ở: các file *.test.tsx cần đi qua route thật.
import { render } from "@testing-library/react";
import { createMemoryRouter, RouterProvider } from "react-router";
import { AppProviders } from "@/app/AppProviders";
import { appRoutes } from "@/app/router";

export function renderApp(path = "/") {
  const memoryRouter = createMemoryRouter(appRoutes, { initialEntries: [path] });

  return render(
    <AppProviders>
      <RouterProvider router={memoryRouter} />
    </AppProviders>,
  );
}
