// AppProviders.tsx — gom mọi provider bọc ngoài cùng ứng dụng, theo đúng thứ tự.
// Chứa: ThemeProvider (sáng / tối + ConfigProvider của antd). T1.2 thêm provider cho việc gọi API.
// Dùng ở: main.tsx và test/renderApp.tsx.
import type { ReactNode } from "react";
import { ThemeProvider } from "@/theme/ThemeProvider";

type AppProvidersProps = {
  children: ReactNode;
};

export function AppProviders({ children }: AppProvidersProps) {
  return <ThemeProvider>{children}</ThemeProvider>;
}
