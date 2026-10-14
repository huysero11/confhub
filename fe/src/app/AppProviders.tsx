// AppProviders.tsx — gom mọi provider bọc ngoài cùng ứng dụng, theo đúng thứ tự.
// Chứa: TanStack Query, ThemeProvider, App antd và khôi phục phiên trước khi vẽ router.
// Dùng ở: main.tsx và test/renderApp.tsx.
import { type ReactNode, useState } from "react";
import { App } from "antd";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { ThemeProvider } from "@/theme/ThemeProvider";
import { SessionBootstrap } from "@/features/auth/components/SessionBootstrap";

type AppProvidersProps = {
  children: ReactNode;
  queryClient?: QueryClient;
};

export function AppProviders({ children, queryClient }: AppProvidersProps) {
  /*
    Cho vào useState để nó chỉ được tạo một lần cho instance của AppProviders
    Nếu viết queryClient = new QueryClient() ở nhiều chỗ thì khi rerender sẽ mất cache
  */
  const [defaultQueryClient] = useState(() => {
    return new QueryClient({
      defaultOptions: {
        queries: { retry: false, refetchOnWindowFocus: false },
        mutations: { retry: false },
      },
    });
  });

  return (
    <QueryClientProvider client={queryClient ?? defaultQueryClient}>
      <ThemeProvider>
        {/* Đây là App của antd cung cấp message, notification, modal,... */}
        <App>
          {/* SessionBootstrap chưa kiểm tra session xong thì chưa cho app thật render. */}
          <SessionBootstrap>{children}</SessionBootstrap>
        </App>
      </ThemeProvider>
    </QueryClientProvider>
  );
}
