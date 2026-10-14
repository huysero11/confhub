// router.tsx — khai báo các route của ứng dụng (react-router).
// Chứa: appRoutes (danh sách route, test dùng lại) và router (bản dùng thật, theo địa chỉ trình duyệt).
// Dùng ở: main.tsx truyền router vào RouterProvider; test/renderApp.tsx dùng appRoutes.
import { createBrowserRouter, type RouteObject } from "react-router";
import { ShellB } from "@/layouts/ShellB/ShellB";
import { HomePage } from "@/pages/HomePage";
import { NotFoundPage } from "@/pages/NotFoundPage";
import { RegisterPage } from "@/features/auth/pages/RegisterPage";
import { CheckEmailPage } from "@/features/auth/pages/CheckEmailPage";
import { VerifyEmailPage } from "@/features/auth/pages/VerifyEmailPage";
import { LoginPage } from "@/features/auth/pages/LoginPage";
import { ForgotPasswordPage } from "@/features/auth/pages/ForgotPasswordPage";
import { ResetPasswordPage } from "@/features/auth/pages/ResetPasswordPage";

// ---------------- DANH SÁCH ROUTE ----------------

export const appRoutes: RouteObject[] = [
  {
    path: "/",
    element: <ShellB />,
    children: [
      { index: true, element: <HomePage /> },

      // Đăng ký và xác thực email
      { path: "register", element: <RegisterPage /> },
      { path: "register/check-email", element: <CheckEmailPage /> },
      { path: "verify-email", element: <VerifyEmailPage /> },

      // Đăng nhập và khôi phục mật khẩu
      { path: "login", element: <LoginPage /> },
      { path: "forgot-password", element: <ForgotPasswordPage /> },
      { path: "reset-password", element: <ResetPasswordPage /> },

      // Đường dẫn không khớp route nào
      { path: "*", element: <NotFoundPage /> },
    ],
  },
];

// ---------------- ROUTER THEO ĐỊA CHỈ TRÌNH DUYỆT ----------------

export const router = createBrowserRouter(appRoutes);
