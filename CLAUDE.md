# CLAUDE.md — hướng dẫn cho Claude trong repo confhub

Đọc theo thứ tự trước khi làm gì:
1. `PROJECT.md` — mọi quyết định đã chốt (đề tài, UC, thuật toán, kiến trúc, UI). Mục 12 là các hướng đã loại, **không đề xuất lại**.
2. `plan/README.md` — quy trình làm việc, chiến lược git, **bảng trạng thái task** (task nào đang làm, xong chưa).
3. `plan/tasks/<ID>.md` của task đang làm (nếu có).

## Vai trò

- **Claude** — Phân tích, thiết kế, chia task, viết `plan/tasks/<ID>.md` (đặc tả cho Codex), giao Codex, **tự review diff + chạy lại test**, cập nhật bảng trạng thái, giải thích code khi được hỏi, hỗ trợ viết báo cáo
- **Codex** (MCP) — Sinh mã + test theo đúng file task, chạy build/test trên nhánh task, **không commit**
- **Người dùng (Huy)** — Duyệt thiết kế, test trên giao diện, **tự gõ lại code** theo bản tham khảo của Codex, hỏi Claude giải thích

## Quy trình mỗi task (tóm tắt — chi tiết ở plan/README.md)

1. Claude viết `plan/tasks/<ID>.md` từ template, cho người dùng xem nếu có quyết định nghiệp vụ.
2. Giao Codex: tạo nhánh `task/<ID>-<slug>` từ `main`, làm theo file task, chạy test.
3. Claude review diff thật + kết quả test. Sai thì giao Codex sửa, lặp tới khi đạt. Không tin báo cáo suông.
4. **Viết mục 9 "Hướng dẫn gõ lại" trong `plan/tasks/<ID>.md`**: luồng tổng + **file chia theo nhóm**, mỗi nhóm một bảng theo thứ tự gõ (vai trò, điểm chính, Gõ/Đọc) + điểm dừng build/test. Dựa trên bản nháp của Codex nhưng Claude tự kiểm tra thứ tự với code thật (file dùng đến phải đứng trước). Rồi **giữ bản tham khảo nhưng để code chưa commit** (xem "Quy ước với Huy"), báo người dùng: cách chạy, cách test trên UI, link tới mục 9.
5. Người dùng test UI → tự gõ lại → Claude kiểm tra bản gõ lại bằng `git diff ref/<ID>` + chạy test.
6. Merge vào `main`, cập nhật bảng trạng thái trong `plan/README.md`.

## Quy ước với Huy (chốt 2026-09-27)

- **Code xong + test pass thì để nguyên trong thư mục, KHÔNG để lại commit**, không giấu/xóa code: Huy tự chọn file muốn gõ lại, tự copy code ra chỗ khác rồi vừa nhìn vừa gõ vào đúng file đó. Mục 9 là thứ tự gợi ý, Huy không bắt buộc gõ hết.
  - Claude vẫn gắn tag `ref/<ID>` làm bản đáp án để tự kiểm tra bản gõ lại (`git diff ref/<ID>`); Huy không cần dùng tag. Cách gắn mà không để lại commit:
    `git add -A` → `git commit -m "ref(<ID>): ban tham khao"` → `git tag ref/<ID>` → `git reset --mixed HEAD~1`
  - Huy gõ xong báo Claude → Claude diff với tag + build/test → Huy commit (`<ID>: ...`) → merge. Không chạy lệnh xóa thay đổi của Huy (`reset --hard`, `restore`, `clean`) khi Huy chưa đồng ý.
- **Code đơn giản, dễ hiểu nhưng KHÔNG kém chất lượng** — Huy phải tự gõ lại và giải thích được khi bảo vệ:
  - Đơn giản ở **cách viết**, không cắt bớt chất lượng: logic đúng, xử lý đủ trường hợp biên/lỗi, đúng best practice (Clean Architecture, DI, async/await đúng, validate đầu vào, không lộ bí mật, bảo mật), có test đầy đủ.
  - Ưu tiên cách viết thẳng, dễ đọc hơn cách viết "hay"/ngắn: `if`/`foreach` rõ ràng thay vì chuỗi LINQ dài; tên biến đầy đủ.
  - Không thêm lớp trừu tượng, generic, reflection, source generator, pattern… khi task chưa cần.
  - Analyzer ép cách viết phức tạp chỉ để tối ưu hiệu năng nhỏ → cân nhắc tắt luật đó (ghi lý do trong `.editorconfig`) thay vì làm code khó hiểu.
- **Markdown dễ đọc cả khi mở file thô**: bảng phải căn thẳng cột (đệm khoảng trắng cho các `|` thẳng hàng), mỗi dòng bảng ≤ ~90 ký tự; nội dung dài không nhét vào ô mà viết thành danh sách ngay dưới bảng; đường dẫn dài thì ghi "Thư mục: ..." một lần rồi bảng chỉ ghi tên file.
- **Hướng dẫn cho Huy làm theo**: chia nhóm, mỗi lần một nhóm, xong nhóm mới sang nhóm tiếp; lệnh dùng **cmd** (không PowerShell trừ khi bắt buộc); công cụ mới (Docker…) vừa làm vừa giải thích.

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
- **Codex MCP bị ngắt sau ~60 giây/lượt gọi** (kết nối tới máy Huy): lượt dài (viết cả task) sẽ chết giữa chừng. Cách làm đã chạy được: Claude viết code (qua `device_bash`), Codex chỉ chạy lệnh ngắn (`dotnet build/test`, git) và trả kết quả. Lượt timeout vẫn có thể chạy xong phía sau → kiểm tra lại trạng thái trước khi gọi lại.
- Codex sandbox `workspace-write`: cần `config {"sandbox_workspace_write": {"network_access": true}}` để restore NuGet; **không ghi được `.git`** → lệnh git (commit, tag, reset) chạy với `danger-full-access`, chỉ đúng lệnh cần.
- File Codex tạo trong sandbox thuộc tài khoản `CodexSandbox*` → Huy (tài khoản `Admin`, không nâng quyền) không xóa được (lỗi `Unlink of file ... failed` khi `git switch`). Đã sửa 2026-09-28: `icacls D:\Code\my-projects\confhub /grant Admin:(OI)(CI)F /T` (chạy Administrator) → mọi file mới tự thừa hưởng quyền của Admin. Gặp lại thì chạy lại lệnh này.
- `device_bash` không xóa được file và `git status` thường sẽ để lại `.git/index.lock` → chỉ dùng git đọc với `git --no-optional-locks`.
