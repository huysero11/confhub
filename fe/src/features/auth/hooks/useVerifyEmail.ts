// useVerifyEmail.ts — hook query xác thực một lần cho từng token.
// VerifyEmailResult dùng cache vô hạn để StrictMode không tiêu thụ token lần thứ hai.
import { useQuery } from "@tanstack/react-query";
import { verifyEmail } from "../api";

export function useVerifyEmail(rawToken: string) {
  return useQuery({
    queryKey: ["auth", "verify-email", rawToken],
    queryFn: () => verifyEmail(rawToken),
    enabled: rawToken.length > 0,
    retry: false,
    retryOnMount: false,
    staleTime: Infinity,
    gcTime: Infinity,
  });
}
