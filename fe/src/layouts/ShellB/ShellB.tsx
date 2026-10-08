// ShellB.tsx — khung Shell B: header + vùng nội dung, bọc mọi trang công khai / người tham dự.
// Chứa: Layout của antd cao tối thiểu bằng màn hình; <Outlet/> là chỗ trang con hiện vào.
// Dùng ở: app/router.tsx (route cha "/").
import { Layout, theme } from "antd";
import { Outlet } from "react-router";
import { ShellBHeader } from "@/layouts/ShellB/ShellBHeader";

export function ShellB() {
  const { token } = theme.useToken();

  return (
    <Layout style={{ minHeight: "100vh" }}>
      <ShellBHeader />
      <Layout.Content
        style={{
          width: "100%",
          // Nội dung không giãn quá rộng trên màn hình lớn, canh giữa.
          maxWidth: token.screenXL,
          marginInline: "auto",
          padding: token.paddingLG,
        }}
      >
        <Outlet />
      </Layout.Content>
    </Layout>
  );
}
