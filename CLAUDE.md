# CLAUDE.md — hướng dẫn cho Claude trong repo confhub

Đọc theo thứ tự trước khi làm gì:
1. `PROJECT.md` — mọi quyết định đã chốt (đề tài, UC, thuật toán, kiến trúc, UI). Mục 12 là các hướng đã loại, **không đề xuất lại**.
2. `plan/README.md` — quy trình làm việc, chiến lược git, **bảng trạng thái task** (task nào đang làm, xong chưa).
3. `plan/tasks/<ID>.md` của task đang làm (nếu có).

## Vai trò

| Ai | Làm gì |
|---|---|
| **Claude** | Phân tích, thiết kế, chia task, viết `plan/tasks/<ID>.md` (đặc tả cho Codex), giao Codex, **tự review diff + chạy lại test**, cập nhật bảng trạng thái, giải thích code khi được hỏi, hỗ trợ viết báo cáo |
| **Codex** (MCP) | Sinh mã + test theo đúng file task, chạy build/test, commit trên nhánh task |
| **Người dùng (Huy)** | Duyệt thiết kế, test trên giao diện, **tự gõ lại code** theo bản tham khảo của Codex, hỏi Claude giải thích |

## Quy trình mỗi task (tóm tắt — chi tiết ở plan/README.md)

1. Claude viết `plan/tasks/<ID>.md` từ template, cho người dùng xem nếu có quyết định nghiệp vụ.
2. Giao Codex: tạo nhánh `task/<ID>-<slug>` từ `main`, làm theo file task, chạy test.
3. Claude review diff thật + kết quả test. Sai thì giao Codex sửa, lặp tới khi đạt. Không tin báo cáo suông.
4. **Viết mục 9 "Hướng dẫn gõ lại" trong `plan/tasks/<ID>.md`**: luồng tổng + bảng file theo thứ tự gõ (vai trò, điểm chính, Gõ/Đọc) + điểm dừng build/test. Dựa trên bản nháp của Codex nhưng Claude tự kiểm tra thứ tự với code thật (file dùng đến phải đứng trước). Rồi gắn tag `ref/<ID>`, báo người dùng: cách chạy, cách test trên UI, link tới mục 9.
5. Người dùng test UI → tự gõ lại → Claude kiểm tra bản gõ lại bằng `git diff ref/<ID>` + chạy test.
6. Merge vào `main`, cập nhật bảng trạng thái trong `plan/README.md`.

## Quy tắc

- **Làm tăng dần theo nhóm** (plan/README.md mục 3): tài liệu, giao diện, bảng/cột, hạ tầng chỉ làm tới nhóm đang làm. Trước khi code nhóm nào phải xong task thiết kế nhóm đó (D7–D14).

- Không kết luận khi chưa có bằng chứng (code, log, test).
- Chỉ hỏi người dùng khi cần quyết định nghiệp vụ, credential, thao tác phá hủy, hoặc bị chặn thật sự.
- Khi báo kết quả: ghi rõ file/path và bằng chứng verification. Giải thích gom theo nhóm chức năng.
- Khi giải thích code cho người dùng: đi từ luồng tổng (request đi qua những lớp nào) rồi mới vào chi tiết từng file.
- Quyết định mới được chốt → cập nhật `PROJECT.md` (đúng mục) để các chat sau biết.
- Báo cáo khóa luận viết trên Overleaf; biểu đồ bằng PlantUML đặt trong `docs/diagrams/`.

## Môi trường

- Repo trên máy Windows: `D:\Code\my-projects\confhub`. Codex chạy trên Windows (có dotnet/node).
- Shell `device_bash` của Claude là VM Linux, **không có dotnet** → build/test .NET phải giao Codex chạy.
- SQL Server local: `127.0.0.1,1433`.
