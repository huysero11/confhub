// App.tsx — component gốc TẠM THỜI để thử token và nút đổi nền.
// Bước 6 sẽ xóa file này và thay bằng router + Shell B.
import { Button, Flex, theme, Typography } from "antd";
import { ThemeToggle } from "@/components/ThemeToggle";

export function App() {
  const { token } = theme.useToken();

  return (
    <Flex
      vertical
      align="start"
      gap="middle"
      style={{ minHeight: "100vh", padding: token.paddingLG, background: token.colorBgLayout }}
    >
      <Typography.Title level={2}>ConfHub — Hội nghị, phiên, diễn giả</Typography.Title>
      <Button type="primary">Nút mẫu</Button>
      <ThemeToggle />
    </Flex>
  );
}
