// SessionBootstrap.tsx — component chờ khôi phục phiên từ cookie xong mới vẽ ứng dụng.
// Trong lúc chờ (useSessionBootstrap) hiện Spin phủ toàn trang. Dùng ở: AppProviders.
import type { ReactNode } from "react";
import { Flex, Spin } from "antd";
import { useTranslation } from "react-i18next";
import { useSessionBootstrap } from "../hooks/useSessionBootstrap";

// children ở đây là RouterProvider, xem ở main.tsx
type SessionBootstrapProps = {
  children: ReactNode;
};

export function SessionBootstrap({ children }: SessionBootstrapProps) {
  // Đây là hook trung gian để gọi api refresh tokens
  const bootstrap = useSessionBootstrap();
  const { t } = useTranslation();

  if (bootstrap.isPending) {
    return (
      <Flex align="center" justify="center" style={{ minHeight: "100vh" }}>
        <Spin aria-label={t("auth.loading")} />
      </Flex>
    );
  }

  return children;
}
