# Kế hoạch & trạng thái task

> "Bộ nhớ chung" giữa các chat. **Task đổi trạng thái → cập nhật mục 3 và nhật ký mục 5.**
> Mẹo: đọc file này ở chế độ xem trước của VS Code (`Ctrl+Shift+V`).

## 0. Đang ở đâu

- **Vừa xong:** T1.4 (đăng nhập, JWT, refresh token, phân quyền) — 2026-10-07; backend G1 xong (9 API `api/auth`)
- **Đang làm:** chưa có task đang code (chờ viết đặc tả T1.3)
- **Tiếp theo:** T1.3 (khung FE) → T1.2 (FE tài khoản) → G1 xong; viết phần Tài khoản trên Overleaf
- **Nhánh đang mở:** không có sau khi merge `task/T1.4-login` (bản đáp án T1.4: tag `ref/T1.4`)

## 1. Quy trình một task

```
Claude viết plan/tasks/<ID>.md
   → Codex code + test trên nhánh task/<ID>-<slug>
   → Claude review diff + chạy lại test (lặp tới khi đạt)
   → Claude viết mục 9 "Hướng dẫn gõ lại" (thứ tự file + vai trò + điểm dừng)
   → tag ref/<ID>  (bản tham khảo đã kiểm) rồi bỏ commit: code để nguyên, chưa commit
   → Huy test trên giao diện
   → Huy chọn file muốn gõ lại, tự copy ra chỗ khác rồi gõ lại tại chỗ (thứ tự gợi ý: mục 9), gõ xong mới commit
   → Claude so bản gõ lại với ref/<ID> + chạy test, giải thích khi được hỏi
   → merge vào main, cập nhật bảng trạng thái
```

Task nhỏ thuần cấu hình (như G0) có thể **làm tay**: Claude hướng dẫn từng nhóm lệnh → Huy làm → Claude kiểm tra diff → merge. Không có tag ref.

**Báo cáo tuần cho GVHD** (Google Docs): tab `Tuần N` ghi tổng quan + khoảng ngày (từ … đến …); mỗi task/nhóm task xong trong tuần là một sub-tab riêng (vd `G-0`). Claude soạn Markdown → dán bằng *Edit → Paste from Markdown*.

**Trạng thái:**
- `todo` — chưa bắt đầu
- `spec` — Claude đang viết file task
- `codex` — Codex đang làm
- `review` — Claude đang kiểm
- `ui-test` — Huy test giao diện
- `retype` — Huy đang gõ lại / làm tay
- `done` — đã merge `main`
- `dropped` — bỏ (ghi lý do)

## 2. Chiến lược git

**Một nhánh cho mỗi task**, không chia nhánh theo cả nhóm chức năng: nhánh sống ngắn, merge sớm, ít xung đột.

- **`main`** — chỉ chứa code Huy đã gõ lại và test pass. Luôn chạy được.
- **Nhánh task** — `task/<ID>-<slug>`, ví dụ `task/T1.1-jwt-auth`. Tạo từ `main` mới nhất.
- **Commit** — Codex **không commit**. Chỉ Huy commit (`<ID>: ...`) sau khi gõ lại.
- **Tag `ref/<ID>`** — bản đáp án Claude dùng để kiểm tra bản gõ lại (Huy không cần dùng). Tạo mà không để lại commit (code vẫn hiện màu trong VS Code):
  `git add -A` → `git commit -m "ref(<ID>): ban tham khao"` → `git tag ref/<ID>` → `git reset --mixed HEAD~1`
- **Merge** — `git merge --no-ff --no-edit task/<...>`: luôn có commit merge, lịch sử thấy rõ từng task.

**Xem bản tham khảo khi gõ lại** (không cần copy file ra ngoài):

```bash
git show ref/T1.1:be/src/ConfHub.Api/Program.cs     # xem 1 file bản tham khảo
git diff ref/T1.1 -- be/src/ConfHub.Api/Program.cs  # bản mình gõ khác chỗ nào
git diff ref/T1.1 --stat                             # tổng quan khác biệt cả task
```

VS Code: GitLens → "Compare with…" chọn tag `ref/<ID>`. Muốn copy ra thì đặt ngoài repo hoặc trong `scratch/` (đã gitignore).

**Vì sao vẫn chia nhánh dù task sau chứa code task trước?** Nhánh để cách ly *việc đang dở*:
- `main` luôn chạy được; code đang dở nằm ở nhánh, hỏng thì bỏ nhánh.
- Mỗi nhánh là diff của đúng 1 task → dễ review, giải thích, chụp ảnh cho báo cáo.
- Hai việc độc lập chạy song song được (Huy gõ lại T1.1, Codex sinh T2.1).

Task sau **luôn tách từ `main`** → xây trên code của Huy, không lệch với code Codex.

## 3. Bảng task

- Tái cấu trúc 2026-09-25 theo 57 UC (P1–P5) và ERD đã chốt; **từ đây áp dụng quy tắc mục 4**.
- **Giai đoạn A — Dự án công nghệ** (15 tuần hiện tại: nộp tài liệu + slide). **Giai đoạn B — Code** (chủ yếu ở khóa luận; G0 và G2 có thể làm sớm để thăm dò rủi ro).
- UC tham chiếu `docs/design/use-cases.md`; bảng tham chiếu `docs/design/erd.md`.

### Nguyên tắc: làm tăng dần theo nhóm chức năng (lát cắt dọc)

Mỗi nhóm chức năng đi đủ 4 lớp **cùng lúc**, chỉ trong phạm vi nhóm đó:

- **Tài liệu** — đặc tả UC + biểu đồ hoạt động/tuần tự của đúng các UC trong nhóm (cũng là phần báo cáo).
- **Giao diện** — thiết kế đúng các màn hình của nhóm (`docs/ui/<nhóm>.html`) trước khi code FE.
- **CSDL** — migration chỉ tạo bảng/cột nhóm cần. Nhóm sau thêm cột → migration mới `<ID>_<MoTa>`.
- **Hạ tầng** — service (DI) và container compose thêm ở task đầu tiên dùng đến: Seq ở T0.5, RabbitMQ ở T0.4, Hangfire T5.1, Redis T5.2, MinIO T8.1.

`erd.md` và `use-cases.md` là **bản đồ tổng**; chi tiết từng nhóm chốt trong task thiết kế của nhóm.

---

### Giai đoạn A — Tài liệu (Claude + Huy, không giao Codex)

Task thiết kế nhóm (D7–D14) chạy trước code; code của nhóm bắt đầu khi task thiết kế nhóm `done`. **Biểu đồ** (`.puml`) vẫn vẽ ngay khi làm nhóm nào, nhưng Huy **kiểm sau** (15 tuần môn học chưa phải nộp biểu đồ) — không chặn code; task thiết kế `done` khi đặc tả + giao diện đủ để code. *Chưa rõ 4 tài liệu môn yêu cầu gì → khi có đề bài/mẫu thì ánh xạ lại.*

| ID  | Việc                                   | Phụ thuộc  | Trạng thái |
|-----|----------------------------------------|------------|------------|
| D1  | ERD tổng                               | –          | done       |
| D2  | Biểu đồ UC                             | –          | done       |
| D3  | ~~Đặc tả UC P1–P3 một lượt~~           | –          | dropped    |
| D4  | ~~Biểu đồ hoạt động/tuần tự một lượt~~ | –          | dropped    |
| D5  | Kiến trúc hệ thống                     | D1         | todo       |
| D6  | Slide trình bày                        | D1, D2, D5 | todo       |
| D7  | Thiết kế G1 Tài khoản                  | D1, D2     | done       |
| D8  | Thiết kế G3 Hội nghị, phiên            | D7         | todo       |
| D9  | Thiết kế G4 Đăng ký                    | D8         | todo       |
| D10 | Thiết kế G5 Xếp lịch                   | D9         | todo       |
| D11 | Thiết kế G6 Dịch vụ                    | D10        | todo       |
| D12 | Thiết kế G7 Vận hành tại chỗ           | D11        | todo       |
| D13 | Thiết kế G8 Hỏi đáp và AI              | D10        | todo       |
| D14 | Thiết kế G9 Hỗ trợ                     | D12, D13   | todo       |

**Chi tiết:**
- **D1** — ERD tổng (bản đồ): bảng + cột (`erd.md`) → vẽ PlantUML `docs/diagrams/erd.puml` (đã vẽ 2026-09-29, chờ duyệt).
- **D2** — Biểu đồ UC tổng quát + phân rã theo 7 tác nhân (57 UC): `docs/diagrams/uc-*.puml`, kiểu chung `_style.iuml`.
- **D3, D4** — dropped: thay bằng thiết kế theo nhóm D7–D14.
- **D5** — Sơ đồ thành phần, luồng sự kiện, triển khai + khung 3 shell giao diện.
- **D6** — Phụ thuộc thêm các task thiết kế nhóm đã làm.
- **D7–D14** — UC: D7 (UC01–02) · D8 (UC03–08) · D9 (UC09–14) · D10 (UC15–19) · D11 (UC20–28) · D12 (UC29–32) · D13 (UC33–40) · D14 (UC41–47). Mỗi task tạo: `docs/specs/<nhóm>.md` (đặc tả UC) · `docs/diagrams/<nhóm>-*.puml` (hoạt động, tuần tự) · `docs/ui/<nhóm>.html` (giao diện) · cập nhật `erd.md` nếu cột thay đổi.

---

### Giai đoạn B — Code

#### G0 — Nền tảng

| ID   | Việc                                    | Phụ thuộc | Trạng thái |
|------|-----------------------------------------|-----------|------------|
| T0.1 | Git                                     | –         | done       |
| T0.2 | Khung solution backend                  | T0.1      | done       |
| T0.3 | ~~Khung frontend~~                      | –         | dropped    |
| T0.4 | Hạ tầng sự kiện (RabbitMQ, MassTransit) | T0.5      | done       |
| T0.5 | Mẫu nền tảng backend                    | T0.2      | done       |

**Chi tiết:**
- **T0.1** — `.gitattributes`, `.gitignore`, repo GitHub. Huy làm tay. `docker-compose.yml` + Seq đã dời sang T0.5 (khung chưa ghi log; máy Huy chưa có Docker).
- **T0.2** — Huy làm tay **phần khung**: `ConfHub.slnx`, 4 lớp Domain/Application/Infrastructure/Api + `ConfHub.Scheduling`, project reference, `global.json` (SDK 10), `Directory.Build.props` (Nullable, TreatWarningsAsErrors, StyleCop + Roslynator), `.editorconfig`, endpoint `/health` + `tests/ConfHub.Api.IntegrationTests`. Các mẫu code tách sang T0.5.
- **T0.3** — dropped 2026-09-29: G0 chưa có gì dùng tới frontend → chuyển thành **T1.3** trong G1 (làm ngay trước T1.2).
- **T0.4** — Đặc tả: `plan/tasks/T0.4.md`. RabbitMQ vào compose; EF Core + DbContext + SQL Server + migration đầu tiên (chỉ bảng kỹ thuật của MassTransit); MassTransit 8.x + EF Transactional Outbox/Inbox + retry; test chứng minh đường ống. **Không** có gửi mail (sang T1.1).
- **T0.5** — Đặc tả: `plan/tasks/T0.5.md`. BaseEntity + domain event, MediatR 12.x + ValidationBehavior, exception → ProblemDetails, Serilog → Seq, OpenAPI + Scalar; **Docker Compose + Seq**. Không có CSDL: DbContext sang T0.4, Repository/Specification sang T1.1. Quy trình chuẩn Codex → Huy gõ lại.

#### G1 — Tài khoản (P1: UC01, UC02)

| ID   | Việc                               | Phụ thuộc                | Trạng thái |
|------|------------------------------------|--------------------------|------------|
| T1.1 | BE: đăng ký, xác thực email        | D7, T0.2, T0.4, T0.5, D1 | done       |
| T1.2 | FE: đăng ký / đăng nhập            | T1.3, T1.4               | todo       |
| T1.3 | FE: khung frontend                 | T0.1                     | todo       |
| T1.4 | BE: đăng nhập, JWT, phân quyền     | T1.1                     | done       |

**Chi tiết:**
- **T1.1** — Tách 2026-09-29 (phần đăng nhập sang T1.4). Nền persistence nhận từ T0.5: IRepository/IReadRepository + Specification, SaveChangesInterceptor (audit), Guid tuần tự. User/Role/UserToken + migration + seed 5 vai trò, danh mục quyền là hằng số. Đăng ký (chọn Người tham dự / BTC / NCC), băm mật khẩu `PasswordHasher<T>`, xác thực email (token băm, hạn 24 giờ, gửi lại có giới hạn), quên mật khẩu. **Nhận từ T0.4 (phương án B):** smtp4dev vào compose + MailKit + consumer gửi email; domain event → publish qua outbox trước `SaveChanges`. Kiểm tra lại: tắt RabbitMQ thì `/health` phải báo `Unhealthy`. Rate limit cho endpoint đăng ký / gửi lại mail.
- **T1.4** — Đăng nhập (chặn `Unverified` / `PendingApproval` / `Locked`), access token JWT 15 phút + refresh token 7 ngày trong cookie `HttpOnly` (xoay vòng, lưu băm trong UserToken), làm mới, đăng xuất (thu hồi). Phân quyền theo permission tự viết: `[MustHavePermission]` + PolicyProvider + AuthorizationHandler, quyền trong claim JWT. Rate limit đăng nhập. Seed tài khoản mẫu cho 5 vai trò (dev) để dùng tới khi có UC20.
- **T1.2** — Đăng ký / xác thực email / đăng nhập / đăng xuất / quên mật khẩu, interceptor tự làm mới token, chặn route theo quyền.
- **T1.3** — Khung frontend (chuyển từ T0.3): Vite React TS, antd v5 token sáng/tối, i18n vi/en, 3 shell A/B/C + router rỗng. Làm trước T1.2.

#### G2 — Thuật toán bản console (làm sớm, độc lập)

| ID   | Việc                                | Phụ thuộc | Trạng thái |
|------|-------------------------------------|-----------|------------|
| T2.1 | Mô hình dữ liệu + ràng buộc + hàm f | T0.2      | todo       |
| T2.2 | Giai đoạn 1 tham lam + Hungarian    | T2.1      | todo       |
| T2.3 | Giai đoạn 2 cải thiện cục bộ        | T2.2      | todo       |
| T2.4 | Chương trình thực nghiệm            | T2.3      | todo       |

**Chi tiết:**
- **T2.1** — Mô hình dữ liệu, tập mốc T, ma trận W, kiểm tra H1–H5, hàm f chuẩn hóa, bộ sinh dữ liệu có cụm chủ đề.
- **T2.2** — Tham lam + Hungarian + M1, M2. Test ví dụ chạy tay f = 35.
- **T2.3** — Q1–Q3, khởi động lại, mô phỏng luyện kim, phạt xáo trộn. Test ví dụ f = 5.
- **T2.4** — M1–M4 × 3 quy mô → CSV chỉ số, phân tích độ nhạy.

#### G3 — Hội nghị, phòng, phiên, diễn giả (P1: UC03–UC08)

| ID   | Việc                             | Phụ thuộc  | Trạng thái |
|------|----------------------------------|------------|------------|
| T3.1 | BE: hội nghị, ngày, phòng, track | D8, T1.4   | todo       |
| T3.2 | BE: phiên, diễn giả, giờ rảnh    | T3.1       | todo       |
| T3.3 | FE Shell A (BTC)                 | T3.2, T1.2 | todo       |
| T3.4 | FE diễn giả                      | T3.2, T1.2 | todo       |

**Chi tiết:**
- **T3.1** — Conference, ConferenceDay, BreakSlot, Room, RoomEquipment, ServiceItem (cơ bản), Track.
- **T3.2** — Session, SessionSpeaker (mời nhiều diễn giả / phản hồi), SpeakerAvailability.
- **T3.3** — Hội nghị, ngày, khung nghỉ, phòng, phiên, mời diễn giả.
- **T3.4** — Lời mời, khai giờ rảnh.

#### G4 — Đăng ký (P1: UC09–UC14)

| ID   | Việc                               | Phụ thuộc | Trạng thái |
|------|------------------------------------|-----------|------------|
| T4.1 | BE: vé, đăng ký, duyệt             | D9, T3.2  | todo       |
| T4.2 | FE Shell B: tìm hội nghị, đăng ký  | T4.1      | todo       |
| T4.3 | FE Shell A: loại vé, duyệt đăng ký | T4.1      | todo       |

**Chi tiết:**
- **T4.1** — TicketType, xem hội nghị công khai, Registration (chế độ ăn), SessionInterest, duyệt / danh sách chờ, sự kiện RegistrationApproved.
- **T4.2** — Danh sách + tìm kiếm hội nghị, chương trình, form đăng ký + tick phiên quan tâm.
- **T4.3** — Cấu hình loại vé, duyệt đăng ký.

#### G5 — Xếp lịch trong hệ thống (P1: UC15–UC19)

| ID   | Việc                            | Phụ thuộc       | Trạng thái |
|------|---------------------------------|-----------------|------------|
| T5.1 | Job Hangfire chạy thuật toán    | D10, T2.3, T4.1 | todo       |
| T5.2 | Lưới lịch kéo thả + công bố     | T5.1            | todo       |
| T5.3 | Lịch cá nhân (UC18)             | T5.2            | todo       |
| T5.4 | Xếp lại hạn chế xáo trộn (UC19) | T5.2            | todo       |

**Chi tiết:**
- **T5.1** — Lưu ScheduleVersion + ScheduleItem, phiên chưa xếp kèm lý do.
- **T5.2** — Kiểm ràng buộc tức thời + ghim + công bố + cache Redis.
- **T5.3** — Lịch cá nhân + cảnh báo trùng giờ.
- **T5.4** — Xếp lại + sự kiện ScheduleChanged.

**— Hết P1 —**

#### G6 — Dịch vụ đi kèm (P2: UC20–UC28)

| ID   | Việc                                          | Phụ thuộc  | Trạng thái |
|------|-----------------------------------------------|------------|------------|
| T6.1 | QT quản lý tài khoản (UC20)                   | D11, T1.2  | todo       |
| T6.2 | Danh mục dịch vụ, voucher, thiết bị (UC21–24) | T4.1, T6.1 | todo       |
| T6.3 | Tổng hợp nhu cầu + gửi đơn (UC25–26)          | T6.2, T5.2 | todo       |
| T6.4 | NCC xử lý đơn + tính lại (UC27–28)            | T6.3, T5.4 | todo       |

**Chi tiết:**
- **T6.1** — Duyệt BTC/NCC, khóa/mở.
- **T6.2** — Danh mục dịch vụ, khung nghỉ có suất, chọn suất → Voucher, yêu cầu thiết bị + duyệt.
- **T6.3** — Cộng (tiêu hao) / đỉnh đồng thời − sẵn có (cho thuê); tạo và gửi đơn theo NCC.
- **T6.4** — NCC cập nhật giao hàng; tính lại khi ScheduleChanged → PreviousQuantity, NeedsReview.

#### G7 — Vận hành tại chỗ (P2: UC29–UC32)

| ID   | Việc                                       | Phụ thuộc | Trạng thái |
|------|--------------------------------------------|-----------|------------|
| T7.1 | Vé + voucher QR (UC29)                     | D12, T6.2 | todo       |
| T7.2 | Shell C: quét check-in, voucher (UC30–31)  | T7.1      | todo       |
| T7.3 | Check-in phòng + dashboard realtime (UC32) | T7.2      | todo       |

**Chi tiết:**
- **T7.2** — Quét check-in cổng, quét voucher chống dùng lại.
- **T7.3** — Check-in cửa phòng + dashboard SignalR, cảnh báo sắp đầy.

**— Hết P2 —**

#### G8 — Hỏi đáp và AI (P3: UC33–UC40)

| ID   | Việc                                   | Phụ thuộc  | Trạng thái |
|------|----------------------------------------|------------|------------|
| T8.1 | Diễn giả tải slide lên MinIO (UC33–34) | D13, T5.2  | todo       |
| T8.2 | Hỏi đáp realtime (UC35, UC37)          | T5.2       | todo       |
| T8.3 | Lớp AI + gom nhóm câu hỏi (UC36)       | T8.2       | todo       |
| T8.4 | Trợ lý RAG theo phiên (UC38)           | T8.1, T8.3 | todo       |
| T8.5 | Đánh giá + tóm tắt phản hồi (UC39–40)  | T8.3       | todo       |

**Chi tiết:**
- **T8.1** — Xem phiên được giao, tải slide + tóm tắt.
- **T8.2** — SignalR group theo phiên, bình chọn, kiểm duyệt.
- **T8.3** — Lớp trừu tượng AI (embedding + LLM).
- **T8.4** — Trích text, chia đoạn, DocumentChunk, ngưỡng tương đồng.

**— Hết P3 —**

#### G9 — Hỗ trợ (P4: UC41–UC47)

| ID   | Việc                                     | Phụ thuộc | Trạng thái |
|------|------------------------------------------|-----------|------------|
| T9.1 | Hồ sơ, hủy đăng ký (UC41–42)             | D14, T4.2 | todo       |
| T9.2 | Thông báo hàng loạt, dashboard (UC43–44) | T7.3      | todo       |
| T9.3 | Vai trò / quyền, tham số (UC45–46)       | T6.1      | todo       |
| T9.4 | Nhận hàng, bàn giao thiết bị (UC47)      | T7.2      | todo       |

#### G10 — Mở rộng (P5: UC48–UC57)

Chỉ chia task khi P1–P4 xong.

#### G11 — Thực nghiệm chính thức

| ID    | Việc                                  | Phụ thuộc  | Trạng thái |
|-------|---------------------------------------|------------|------------|
| T11.1 | Thực nghiệm M1–M4 cho chương đánh giá | T2.4, T5.4 | todo       |

**Chi tiết:**
- **T11.1** — 11 chỉ số, phân tích độ nhạy; bảng + biểu đồ.

---

**Thứ tự cắt giảm nếu chậm:** G10 → G9 → T8.5 → T6.4 (NCC chỉ còn xem đơn).
**Giữ bằng mọi giá:** P1 (G1–G5), T6.3, G7, T8.4, T11.1.

## 4. Quy tắc giữ plan không rối

1. **Không đổi/tái sử dụng mã task.** Task bỏ → `dropped` kèm lý do. Task phát sinh → mã mới cuối nhóm (vd `T3.5`), không chèn số.
2. **Mỗi lúc chỉ 1 task ở `codex`/`review`**, trừ khi 2 task không phụ thuộc nhau (vd G1 và G2).
3. **Chỉ bắt đầu task khi mọi task trong cột Phụ thuộc đã `done`** (đã merge `main`).
4. Đặc tả `plan/tasks/<ID>.md` được Huy duyệt mới giao Codex. Đổi phạm vi → sửa file task trước, rồi mới sửa code.
5. Mọi thay đổi bảng task / quyết định thiết kế → ghi 1 dòng nhật ký; quyết định lâu dài → cập nhật `PROJECT.md`.
6. Cuối mỗi chat: mục 0 và bảng trạng thái phải đúng thực tế (nhánh nào tồn tại, task nào dở).

## 5. Nhật ký

**2026-09-25**
- Dựng khung điều phối (CLAUDE.md, AGENTS.md, plan/). Đã gửi GVHD danh sách UC.
- Bản nháp ERD tổng thể `docs/design/erd.md` (66 bảng, 9 nhóm), chờ chốt Q1–Q7.
- Chốt 57 UC theo ưu tiên P1–P5 (`docs/design/use-cases.md`); ERD còn 48 bảng (39 cho P1–P3). 15 tuần hiện tại là môn Dự án công nghệ: nộp 4 tài liệu + slide, code để giai đoạn khóa luận.
- ERD gộp 3 bảng (RefreshToken→UserToken, Supplier→cột User, RegistrationService→Voucher): 45 bảng, 35 nghiệp vụ cho P1–P3. Đối chiếu 3 khóa luận khóa trước: 11–26 bảng.
- ERD rút còn 36 bảng (26 nghiệp vụ P1–P3): gộp ConferenceDay, ConferenceSpeaker, SessionMaterial, ServiceOffering, Permission, RolePermission, UserRole, ConferenceMember, PollOption.
- ERD: thêm lại ConferenceDay (mỗi ngày khung giờ riêng); bỏ CompanyName/TaxCode/Address/Interests khỏi User (dùng Organization); mô tả từng cột. 37 bảng, ERD nộp môn 27.
- ERD rút cột: chỉ giữ cột có UC/thuật toán dùng, bỏ cột suy ra được (tổng tiền, số phiên đã xếp, EndAt, VoteCount…). Thanh toán ngoài phạm vi; diễn giả phải có tài khoản trước khi được mời.
- Chốt ERD (1 diễn giả/phiên, 36 bảng, nộp môn 26). Tái cấu trúc bảng task: giai đoạn A (D1–D6) + giai đoạn B (G0–G11 theo P1→P5).
- ERD: quay lại cho phép nhiều diễn giả/phiên (SessionSpeaker, slide theo từng diễn giả). 37 bảng, nộp môn 27.
- Rà khung TD.Microservice.ServiceBase → `docs/design/base-reference.md`. Chốt DDD nhẹ + event-driven cho hiệu ứng phụ, .NET 10, MassTransit EF Outbox.
- Thêm bước bắt buộc: mỗi task có mục 9 "Hướng dẫn gõ lại" trước khi tag ref.
- Nguyên tắc làm tăng dần theo nhóm. Thêm D7–D14, D3/D4 dropped. Task code đầu mỗi nhóm phụ thuộc task thiết kế nhóm.

**2026-09-27**
- Bắt đầu G0 phần backend: Huy làm tay T0.1 + khung T0.2 (không Codex, không tag ref; Claude review diff trước khi merge). Khung chạy thử trước với SDK 10.0.112. T0.3 để sau.
- Dời `docker-compose.yml` + Seq khỏi T0.1 (máy chưa có Docker, khung chưa ghi log). Cài .NET 10 SDK 10.0.401.
- T0.1 done: merge `task/T0.1-git` (--no-ff).
- T0.2 (phần khung) done: merge `task/T0.2-be-skeleton`. `dotnet build` pass, `dotnet test` 1/1 pass. Sửa 2 lỗi StyleCop từ code template (SA1512 comment + dòng trống, SA1518 thiếu newline cuối file).
- Tách mẫu nền tảng backend (BaseEntity, Specification, MediatR, ExceptionMiddleware, Serilog→Seq, OpenAPI) + Docker Compose/Seq thành **T0.5** (quy trình Codex chuẩn). T0.4 và T1.1 chuyển phụ thuộc sang T0.5. Đổi định dạng file này cho dễ đọc (bảng ngắn + chi tiết).
- T0.5 → `spec`: viết `plan/tasks/T0.5.md`. Chia phần CSDL: DbContext/SQL Server sang T0.4 (Outbox là thứ đầu tiên cần), Repository/Specification/audit/Guid tuần tự sang T1.1 (aggregate đầu tiên) — đúng nguyên tắc không làm trước.
- Huy duyệt đặc tả T0.5 → `codex`. Huy sẽ tự gõ lại theo mục 9 (chia nhóm file + thứ tự); Docker giải thích từng bước khi test tay. T0.3 để sau.
- T0.5: Codex bị ngắt kết nối sau 60 giây/lượt nên không chạy được lượt dài → Claude viết code, Codex chỉ chạy build/test/git (commit tiền tố `claude(T0.5)`). Build 0 warning; test 14/14 pass. Tắt RCS1194 (exception không cần đủ 3 constructor chuẩn). Viết mục 9, tag `ref/T0.5` → `ui-test`.
- Quy ước mới (ghi vào CLAUDE.md, AGENTS.md): task sau **không để lại commit** — tag `ref/<ID>` rồi `reset --mixed` để code hiện màu trong VS Code; **code đơn giản, dễ hiểu** là ưu tiên số 1. T0.5 đã commit trước quy ước này.
- T0.5 đơn giản hóa (ValidationBehavior, ApiExceptionHandler dùng `foreach`; tắt CA1848, dùng `logger.LogError`), build 0 warning, test 14/14. Gắn lại tag `ref/T0.5` (85eb4dc) rồi `reset --mixed` về `fd8b07d`: code T0.5 để chưa commit. Căn thẳng bảng trong mọi file markdown; thêm quy tắc markdown vào CLAUDE.md.
- Gõ lại: không giấu code; Huy tự chọn file, tự copy ra chỗ khác rồi gõ lại tại chỗ. Tag `ref/<ID>` chỉ để Claude kiểm tra.
- 2026-09-28: T0.5 đổi xử lý lỗi sang `ExceptionHandlingMiddleware` tự viết (`try/catch`, tự ghi JSON), bỏ `IExceptionHandler`/`AddProblemDetails`. Build 0 warning, test 16/16. Ghi nguyên tắc "tự viết hay dùng thư viện" vào PROJECT.md mục 8.
- T0.5 done: Huy test tay (Docker + Seq + Scalar) đạt, gõ lại xong; bản gõ khớp `ref/T0.5` (so từng file, bỏ qua kiểu xuống dòng), build 0 warning, test 16/16. Merge `task/T0.5-be-foundation` vào main.
- Merge T0.5 vào main (78b38b3), đẩy tag `ref/T0.5`. Sự cố: file Codex tạo trong sandbox không xóa được → cấp quyền `Admin:(OI)(CI)F` cho cả repo; ghi vào CLAUDE.md mục Môi trường.
- T0.4 chọn phương án B: T0.4 chỉ làm đường ống sự kiện (EF Core/SQL Server + MassTransit Outbox/Inbox + RabbitMQ); smtp4dev + consumer gửi mail chuyển sang T1.1, nơi đầu tiên dùng tới (ghi vào chi tiết T1.1 để không quên). MassTransit ghim 8.5.10 (Apache-2.0, hỗ trợ net10/EF Core 10).
- Huy duyệt đặc tả T0.4 → `codex`. Tên migration không được có dấu `.` → `T0_4_MassTransitOutbox` (ghi quy ước vào AGENTS.md).
- T0.4 code xong: build 0 warning, test 19/19 (Infrastructure 3 test với SQL Server thật, chạy lại 2 lần ổn định). Sửa: luật `const` PascalCase, `Migrations` là code tự sinh, test chờ tin nhắn bằng vòng kiểm tra. Tag `ref/T0.4` → `ui-test`.
- T0.4 test tay: tắt RabbitMQ `/health` vẫn `Healthy` — health check MassTransit chỉ theo dõi hàng đợi của consumer, T0.4 chưa có consumer. Bỏ bước này khỏi mục 7, chuyển kiểm tra sang T1.1.
- T0.4: cấu hình RabbitMQ đổi sang Options pattern (`RabbitMqOptions`, kiểm tra khi khởi động) theo đề xuất của Huy; chuỗi kết nối giữ `GetConnectionString`. Thêm 3 test cấu hình → 22/22 pass. Quy ước mới: dùng pattern thì comment tên + mục đích (CLAUDE.md, AGENTS.md).
- 2026-09-29 T0.4 done: Huy gõ lại; lần kiểm tra đầu phát hiện 2 lỗi (tên chuỗi kết nối `DefaultConnection` ≠ `ConfHub`; `ConfHubDbContext` thiếu constructor nhận `DbContextOptions`) → 7 test lỗi. Huy sửa, kiểm lại: khác bản đáp án chỉ ở comment/tên biến + thêm `ApplyConfigurationsFromAssembly` (giữ, dùng từ T1.1); build 0 warning, test 22/22. Merge vào main.
- T0.4 merge vào main (827ba62), đẩy tag `ref/T0.4`. T0.3 → `dropped`, chuyển thành T1.3 (khung FE) trong G1 vì G0 không dùng frontend; T1.2 phụ thuộc T1.3. **G0 hoàn tất.**
- G1 bắt đầu. Huy chốt 6 quyết định (ghi PROJECT.md mục 8): tự đăng ký 3 vai trò (BTC/NCC chờ QT duyệt); chặn đăng nhập khi chưa xác thực email; quên mật khẩu làm ở G1; refresh token cookie `HttpOnly`, access token trong bộ nhớ; rate limit thay khóa tài khoản; **tách T1.1** → T1.1 (đăng ký, xác thực email) + **T1.4** (đăng nhập, JWT, phân quyền). T1.2 phụ thuộc T1.4; T3.1 chuyển phụ thuộc sang T1.4.
- D1: vẽ `docs/diagrams/erd.puml` (27 bảng P1–P3, gom 7 gói). D2: `uc-tong-quat.puml` (57 UC, 8 gói phân hệ) + 7 biểu đồ theo tác nhân; UC P5 nét đứt nền xám. Kiểu chung `docs/diagrams/_style.iuml`. Đã render thử bằng PlantUML 1.2025.4, không lỗi.
- D7: `docs/specs/g1-account.md` (15 quy tắc BR01–BR15, đặc tả UC01/UC02, API 9 endpoint, mã lỗi, sự kiện email) + 6 biểu đồ `g1-*.puml` (trạng thái, 3 hoạt động, 2 tuần tự) + mockup `docs/ui/g1-account.html` (8 màn hình, sáng/tối, vi/en). Quyết định kỹ thuật: token email do consumer sinh (sự kiện chỉ mang UserId → token gốc không vào CSDL/outbox); refresh token đã dùng bị gửi lại → thu hồi hết. `User.Organization` cho phép null (bắt buộc với BTC/NCC). D1, D2, D7 → `review` chờ Huy duyệt.
- Huy đồng ý 4/5 điểm cần xem ở D7 (refresh token dùng lại → thu hồi hết; đăng ký báo email đã dùng; quên MK khi chưa xác thực → gửi lại mail xác thực; QT từ chối → `Locked`). Điểm còn lại Huy giao Claude chọn: **giữ phương án consumer sinh token** (so với handler sinh token gửi kèm message: token sẽ nằm trong outbox/RabbitMQ/hàng đợi `_error`). Bổ sung vào `g1-account.md` mục 7: lý do, 4 bước của consumer, cách xử lý khi gửi mail lỗi; ghi PROJECT.md mục 8. T1.1 phải có test chứng minh consumer rollback token khi gửi mail lỗi.
- Quy ước mới (Huy): biểu đồ vẫn vẽ theo nhóm đang làm nhưng Huy kiểm sau, không chặn code. D1, D2, D7 → `done` (biểu đồ chờ Huy kiểm).
- T1.1 → `spec`: viết `plan/tasks/T1.1.md`. Chốt kỹ thuật: Repository Ardalis + publish domain event trong `EfRepository.SaveChangesAsync` (không dùng interceptor vì vòng phụ thuộc DbContext ↔ IPublishEndpoint) → quy ước **ghi dữ liệu nghiệp vụ luôn qua repository**; Guid tuần tự dùng sẵn của EF Core SQL Server; `CreatedAt` qua `AuditInterceptor` + `TimeProvider`; `UserToken` là aggregate riêng; consumer gọi MediatR command (logic ở Application).
- Sửa `g1-account.md` cho khớp code T0.5: mã lỗi PascalCase (`EmailTaken`, `TokenInvalid`…); token sai trả 422 (`DomainException`); bỏ `ResendTooSoon` — gửi lại trong 60 giây thì consumer bỏ qua im lặng (báo lỗi sẽ lộ email có tài khoản); message đổi tên `SendVerificationEmailMessage` / `SendPasswordResetEmailMessage`; email gồm cả vi + en. Cập nhật theo: `g1-hd-dang-ky.puml`, `g1-td-dang-ky.puml`, `g1-td-dang-nhap.puml`, mockup.
- T1.1 code xong trên `task/T1.1-register`: Claude viết code (Codex bị ngắt 60 giây/lượt), Codex chạy `dotnet add package` (Ardalis.Specification 9.3.1, Ardalis.Specification.EntityFrameworkCore 9.3.1, MailKit 4.18.1), build, `migrations add T1_1_Accounts`, test. Build 0 warning; test 68/68 pass, chạy 2 lần ổn định (trong đó có test chứng minh consumer chạy trong transaction: gửi mail lỗi → token bị rollback → thử lại gửi được). Khác đặc tả: `AccountErrorCodes` đặt ở Domain; thêm `EmailTokenIssuer` (bước 2–4 consumer dùng chung); tắt Required ngầm định của ASP.NET Core để lỗi thiếu trường đi qua FluentValidation. Viết mục 9, tag `ref/T1.1` → `ui-test`.
- Thêm `README.md` ở gốc repo: lệnh chạy máy dev + bảng địa chỉ / cổng (API, Scalar, smtp4dev, RabbitMQ, Seq, SQL Server). Task nào thêm container / cổng mới thì cập nhật bảng này.
- Quy ước mục 9 (Huy): nhóm gõ lại theo **use case** (lát cắt dọc qua các lớp), không theo lớp. Viết lại mục 9 của T1.1: nhóm 1 Domain (Huy đã gõ) → 2 Đăng ký → 3 Gửi mail + xác thực → 4 Gửi lại mail → 5 Quên / đặt lại mật khẩu → 6 Rate limit → 7 Test. Sửa theo: CLAUDE.md, AGENTS.md, `_TEMPLATE.md`.
- 2026-10-01 T1.1: Huy gõ xong nhóm 1–2. Rà tên: giữ đổi tên của Huy cho domain event / method (`EmailVerificationRequested`, `RequestEmailVerification()` — cùng mẫu `PasswordResetRequested`); trả lại `SendVerificationEmailMessage` (gửi đi là *email xác thực*) và `TokenPurpose.Refresh` (khớp ERD, không lặp chữ Token); sửa typo `uerId`, `null!` → `null`, lambda `u =>` → `user =>`, `HasLetterAndDigit` về private. Ghi quy ước đặt tên vào `T1.1.md` mục 9 + AGENTS.md. Build 0 warning, test 68/68. Gắn lại tag `ref/T1.1` (501a8af).
- Quy ước mục 9 (Huy): trong mỗi nhóm use case, gõ **từ chỗ gọi xuống chỗ được gọi** (outside-in) để biết tham số được truyền thế nào; chấp nhận báo đỏ tạm, build ở điểm dừng. Viết lại T1.1 mục 9 từ nhóm 3 (tách: 3 gửi mail, 4 xác thực, 5 gửi lại, 6 quên/đặt lại, 7 rate limit, 8 test). Sửa theo: CLAUDE.md, AGENTS.md, `_TEMPLATE.md`.
- Mục 9 (Huy): thêm dòng `↳ gặp:` dưới mỗi file — tên của dự án mà file đó dùng (theo thứ tự xuất hiện) → file chứa nó, để gặp tên nào mở file đó gõ luôn. Ghi ở hướng dẫn, không ghi vào code. Đã thêm cho T1.1 nhóm 3–7.
- 2026-10-04 Đổi tên `SecureToken` → `RandomToken` (rõ nghĩa hơn; không dùng `TokenService` vì T1.4 có JWT và class này là static thuần). `EmailTokenIssuer`: biến kiểu `UserToken` đặt đồng bộ `latestUserToken` / `oldUserTokens` / `newUserToken`, `lifeTime` → `lifetime`, sửa comment cooldown. Gắn lại `ref/T1.1`.
- T1.1: Huy gõ xong nhóm 3. Tách `Infrastructure/DependencyInjection.cs` theo mục (đề xuất của Huy): `Persistence/PersistenceServiceRegistration.cs`, `Messaging/MessagingServiceRegistration.cs`, `Email/EmailServiceRegistration.cs`; file gốc chỉ còn gọi `AddPersistence/AddMessaging/AddEmail`. Giữ route tường minh `[Route("api/auth")]` thay vì `[controller]` (URL là hợp đồng với FE, tên nhiều từ cần kebab-case). Quy ước ghi AGENTS.md.
- T1.1: Huy gõ xong nhóm 4, 5, 7 (nhóm 6 giống nhóm 3–4 nên chỉ đọc). Kiểm tra lần cuối: phát hiện `VerifyEmailCommandHandler` thiếu `SaveChangesAsync` (xác thực không được lưu) → thêm lại; trả tiêu đề email xác thực về song ngữ; đồng bộ tên `userTokenRepository` / `userToken` / `emailTokenIssuer` sang 2 handler đặt lại mật khẩu; sửa 3 comment chưa chính xác. Build 0 warning, test 68/68. Gắn lại `ref/T1.1`. Chờ Huy commit + merge.
- 2026-10-04 T1.1 → `done`: Huy commit `c0ab1e1`, merge `--no-ff` vào `main` (`7fcb61b`). Viết báo cáo tuần G-1 phần 1 (D1, D2, D7, T1.1) cho Huy dán vào Google Docs. Tiếp theo: đặc tả T1.4.
- 2026-10-05 T1.4 → `spec`: viết `plan/tasks/T1.4.md`. Chốt: gói JwtBearer đặt ở Infrastructure (Api dùng lại); `FallbackPolicy` = phải đăng nhập (endpoint công khai ghi rõ `AllowAnonymous`); `me` ở `CurrentUserController` riêng; **không migration**, chưa tạo danh mục quyền (G1 chưa có quyền nghiệp vụ → T3.1); tài khoản mẫu seed lúc khởi động ở Development (`DevSeed`), tạo bằng `User.CreateActive`; thêm mã lỗi `Unauthorized` / `Forbidden` vào `g1-account.md`. Codex kết nối lại bằng tài khoản mới (tool `codex`), thư mục mặc định `C:\Windows\System32` → luôn ghi đường dẫn repo trong prompt.
- T1.4 (Huy): làm `ICurrentUser` (Application) + `HttpCurrentUser` (Api) ngay ở T1.4 thay vì chờ T3.1; `/me` dùng làm mẫu. Phân biệt: `ICurrentUser` = ai đang gọi (từ JWT), repository = dữ liệu User; use case chưa đăng nhập và consumer không dùng.
- 2026-10-05 T1.4: nhánh `task/T1.4-login` đã có sẵn code (tạo 10:32, không qua phiên này). Claude đọc toàn bộ mã nguồn: khớp đặc tả, chưa thấy lỗi. Codex báo `Connection closed` → build/test/`has-pending-model-changes` **chưa chạy lại**; chưa gắn `ref/T1.4`, chưa viết mục 9.
- 2026-10-05 Codex nối lại bằng tool chạy nền (`codex_start` / `codex_status`, truyền `cwd` = gốc repo) → hết giới hạn 60 giây/lượt. T1.4 chạy lại: build lỗi 1 `using` thừa ở `SessionApiTests.cs` (Claude xóa) → build 0 warning / 0 error, test 99/99 (Application 39, Infrastructure 15, Api 45), `has-pending-model-changes`: không đổi model.
- 2026-10-05 T1.4 → `retype`: viết mục 9 (8 nhóm: nền lỗi → đăng nhập → làm mới / đăng xuất → kiểm JWT + `ICurrentUser` + `/me` → permission → rate limit → tài khoản mẫu → test), sửa `g1-td-dang-nhap.puml` (`RefreshSessionCommand`), gắn tag `ref/T1.4`.
- T1.4 mục 9 (Huy nhắc): bỏ nhóm "Nền" — không gom file theo lớp ở đầu. Mã lỗi, `UnauthorizedException`, `ForbiddenException`, nhánh 401 / 403 của middleware chuyển vào nhóm Đăng nhập (ngay sau handler ném chúng); `User.CreateActive` chuyển vào nhóm Tài khoản mẫu (sau `DevAccountSeeder` gọi nó). Còn 7 nhóm, đánh số lại 1–52. Quy tắc cho các task sau: **không có nhóm "nền / chuẩn bị"**, mọi file nằm ở use case đầu tiên dùng nó.
- 2026-10-05 Quyết định (Huy): **bỏ Mapster**. Map tay trên DTO; danh sách dùng `Query.Select` của Specification (từ T3.1). Đã sửa `AGENTS.md`, `base-reference.md`, `T0.5.md`, ghi PROJECT.md mục 12.
- 2026-10-06 T1.4: kiểm bản gõ lại nhóm 1 — sửa `Verify` ngược tham số, `Role.Name` → `Role.Code`, 403 cho chưa xác thực / chờ duyệt, thêm lại băm giả khi email không tồn tại. Rà tên toàn `be/src` (Huy yêu cầu): spec của UserToken theo mẫu `<Entity>By<Khóa>Spec` (`UserTokenByHashSpec`, `ActiveUserTokensSpec`, `LatestUserTokenSpec`); hàm trả exception đặt `Create…Exception()`; `SessionResult.AccessTokenExpiresInSeconds`; `SmtpOptions.UseStartTls` (đổi cả khóa appsettings); `CreateFixedWindowPartition`, `GetRateLimitOptions`; `RefreshTokenCookie.CookiePath` / `BuildCookieOptions`; biến local viết tắt đổi rõ nghĩa. Build 0 warning, test 99/99. Gắn lại `ref/T1.4` (afe9c21).
- Quy ước đặt tên bổ sung: hàm trả về exception / đối tượng mới bắt đầu bằng động từ (`Create…`, `Build…`); biến local không viết tắt (`res`, `x`, `value`, `result` → nói rõ nội dung); chuỗi token gốc luôn có tiền tố `raw` (`rawToken`, `rawRefreshToken`).
- 2026-10-06 T1.4: kiểm bản gõ lại nhóm 2–4. Sửa: thiếu `MapInboundClaims = false` trong `AuthenticationExtensions` (claim `sub` bị đổi tên → `/me` 401); `RevokeAllSessionAsync` → `RevokeAllSessionsAsync`; `userClaim` → `userIdClaim`; `hashedRefreshToken` → `tokenHash` (đồng bộ các handler khác); `CreateSessionExpiredException` về dạng block. Comment của Huy giữ nguyên. Build 0 warning, test 99/99.
- 2026-10-07 T1.4 → `done`: Huy gõ xong nhóm 5–6, Claude kiểm lần cuối bằng diff với `ref/T1.4` (chỉ còn khác comment / cách xuống dòng; sửa 1 thông báo lỗi của `DevAccountSeeder`). Build 0 warning, test 99/99, không đổi model. Huy commit + merge `--no-ff` vào `main`. Viết báo cáo tuần G-1 phần 2 (T1.4). G1 còn T1.3, T1.2.
