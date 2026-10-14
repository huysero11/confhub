// useLogout.ts — hook mutation đăng xuất cho UserMenu.
// Luôn xóa phiên và cache kể cả API lỗi để giao diện không giữ dữ liệu người vừa thoát.
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useNavigate } from "react-router";
import { logout } from "../api";
import { clearSession } from "@/lib/authSession";

export function useLogout() {
  const queryClient = useQueryClient();
  const navigate = useNavigate();

  return useMutation({
    mutationFn: logout,
    onSettled: () => {
      clearSession();
      queryClient.clear();

      void navigate("/", { replace: true });
    },
  });
}
