// router.tsx — khai báo các route của ứng dụng (react-router).
// Chứa: appRoutes (danh sách route, test dùng lại) và router (bản dùng thật, theo địa chỉ trình duyệt).
// Dùng ở: main.tsx truyền router vào RouterProvider; test/renderApp.tsx dùng appRoutes.
import { createBrowserRouter, type RouteObject } from "react-router";
import { ShellB } from "@/layouts/ShellB/ShellB";
import { HomePage } from "@/pages/HomePage";
import { NotFoundPage } from "@/pages/NotFoundPage";

export const appRoutes: RouteObject[] = [
  {
    path: "/",
    element: <ShellB />,
    children: [
      // index: hiện khi địa chỉ đúng bằng route cha ("/").
      { index: true, element: <HomePage /> },
      // "*" nằm trong ShellB → trang 404 vẫn có header.
      { path: "*", element: <NotFoundPage /> },
    ],
  },
];

export const router = createBrowserRouter(appRoutes);
