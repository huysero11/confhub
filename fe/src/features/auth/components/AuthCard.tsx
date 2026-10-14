// AuthCard.tsx — component khung thẻ giữa trang cho các trang tài khoản.
// Chứa tiêu đề, mô tả, nội dung; trang chỉ dành cho khách tự chuyển về trang chủ khi có phiên.
import type { ReactNode } from "react";
import { Card, Flex, Typography, theme } from "antd";
import { Navigate } from "react-router";
import { useAuthSession } from "../hooks/useAuthSession";

type AuthCardProps = {
  title: string;
  description?: string;
  guestOnly?: boolean;
  children: ReactNode;
};

export function AuthCard({ title, description, guestOnly = false, children }: AuthCardProps) {
  const session = useAuthSession();
  const { token } = theme.useToken();

  if (guestOnly && session) {
    return <Navigate to="/" replace />;
  }

  return (
    <Card style={{ maxWidth: token.screenXS, width: "100%", marginInline: "auto" }}>
      <Flex vertical gap="middle">
        <div>
          <Typography.Title level={2} style={{ marginTop: token.marginXXS }}>
            {title}
          </Typography.Title>
          {description && (
            <Typography.Paragraph type="secondary">{description}</Typography.Paragraph>
          )}
        </div>

        {children}
      </Flex>
    </Card>
  );
}
