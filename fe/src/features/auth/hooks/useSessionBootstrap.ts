// useSessionBootstrap.ts — hook khôi phục phiên khi mở / tải lại ứng dụng (lớp hook).
// Chứa: gọi refreshSession() một lần (trình duyệt tự gửi cookie refresh), trả isPending để chờ.
// Dùng ở: SessionBootstrap. Cookie không có / hết hạn thì tiếp tục như khách, không báo lỗi.
import { useEffect, useState } from "react";
import { refreshSession } from "@/lib/apiClient";

export function useSessionBootstrap() {
  const [isPending, setIsPending] = useState(true);

  useEffect(() => {
    /*
    isCancelled là cờ trả lời câu hỏi: “lần chạy effect này đã bị dọn (cleanup) chưa?” 
    Nếu đã bị dọn thì khi API trả về, effect đó không được set state nữa.

    Effect lần 1 chạy  → isCancelled₁ = false, gọi refreshSession() → Promise P
      React dọn lần 1    → isCancelled₁ = true
    Effect lần 2 chạy  → isCancelled₂ = false, gọi refreshSession() → vẫn P (pendingRefresh)
      P xong → .finally của lần 1: isCancelled₁ = true  → bỏ qua
          → .finally của lần 2: isCancelled₂ = false → setIsPending(false) ✔
    
    */
    let isCancelled = false;

    /*
    - void là một toán tử của JavaScript: nó chạy biểu thức rồi bỏ kết quả đi (trả về undefined). 
      Ở đây nó chỉ để nói rõ ý định: “tôi gọi hàm trả Promise nhưng cố ý không await nó”.

    - Vì sao không await: hàm trong useEffect không được là async. React cần hàm đó trả về hàm cleanup (hoặc không trả gì). 
      Một hàm async luôn trả về Promise, React sẽ hiểu sai. */
    void refreshSession()
      .catch(() => {
        // Khách chưa có cookie cũng nhận 401: vẫn mở ứng dụng bình thường, không báo lỗi.
      })
      .finally(() => {
        if (!isCancelled) {
          setIsPending(false);
        }
      });

    return () => {
      isCancelled = true;
    };
  }, []);

  return { isPending };
}
