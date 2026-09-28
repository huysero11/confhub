# Kế hoạch & trạng thái task

> "Bộ nhớ chung" giữa các chat. **Task đổi trạng thái → cập nhật mục 3 và nhật ký mục 5.**
> Mẹo: đọc file này ở chế độ xem trước của VS Code (`Ctrl+Shift+V`).

## 0. Đang ở đâu

- **Vừa xong:** T0.1, T0.2, T0.5 (mẫu nền tảng backend) — 2026-09-28
- **Tiếp theo:** T0.4 (RabbitMQ + MassTransit + EF Core/SQL Server) — Claude viết đặc tả
- **Còn lại của G0:** T0.4 (sau T0.5), T0.3 (độc lập)
- **Nhánh đang mở:** không

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

Task thiết kế nhóm (D7–D14) chạy trước code; code của nhóm bắt đầu khi task thiết kế nhóm `done`. *Chưa rõ 4 tài liệu môn yêu cầu gì → khi có đề bài/mẫu thì ánh xạ lại.*

| ID  | Việc                                   | Phụ thuộc  | Trạng thái |
|-----|----------------------------------------|------------|------------|
| D1  | ERD tổng                               | –          | review     |
| D2  | Biểu đồ UC                             | –          | todo       |
| D3  | ~~Đặc tả UC P1–P3 một lượt~~           | –          | dropped    |
| D4  | ~~Biểu đồ hoạt động/tuần tự một lượt~~ | –          | dropped    |
| D5  | Kiến trúc hệ thống                     | D1         | todo       |
| D6  | Slide trình bày                        | D1, D2, D5 | todo       |
| D7  | Thiết kế G1 Tài khoản                  | D1, D2     | todo       |
| D8  | Thiết kế G3 Hội nghị, phiên            | D7         | todo       |
| D9  | Thiết kế G4 Đăng ký                    | D8         | todo       |
| D10 | Thiết kế G5 Xếp lịch                   | D9         | todo       |
| D11 | Thiết kế G6 Dịch vụ                    | D10        | todo       |
| D12 | Thiết kế G7 Vận hành tại chỗ           | D11        | todo       |
| D13 | Thiết kế G8 Hỏi đáp và AI              | D10        | todo       |
| D14 | Thiết kế G9 Hỗ trợ                     | D12, D13   | todo       |

**Chi tiết:**
- **D1** — ERD tổng (bản đồ): bảng + cột (`erd.md`) → vẽ PlantUML `docs/diagrams/erd.puml`.
- **D2** — Biểu đồ UC tổng quát + phân rã theo 7 tác nhân (57 UC).
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
| T0.3 | Khung frontend                          | T0.1      | todo       |
| T0.4 | Hạ tầng sự kiện (RabbitMQ, MassTransit) | T0.5      | todo       |
| T0.5 | Mẫu nền tảng backend                    | T0.2      | done       |

**Chi tiết:**
- **T0.1** — `.gitattributes`, `.gitignore`, repo GitHub. Huy làm tay. `docker-compose.yml` + Seq đã dời sang T0.5 (khung chưa ghi log; máy Huy chưa có Docker).
- **T0.2** — Huy làm tay **phần khung**: `ConfHub.slnx`, 4 lớp Domain/Application/Infrastructure/Api + `ConfHub.Scheduling`, project reference, `global.json` (SDK 10), `Directory.Build.props` (Nullable, TreatWarningsAsErrors, StyleCop + Roslynator), `.editorconfig`, endpoint `/health` + `tests/ConfHub.Api.IntegrationTests`. Các mẫu code tách sang T0.5.
- **T0.3** — Vite React TS, antd v5 token sáng/tối, i18n vi/en, 3 shell A/B/C + router rỗng.
- **T0.4** — Thêm RabbitMQ + smtp4dev vào compose; **EF Core + DbContext + kết nối SQL Server** (nhận từ T0.5); MassTransit 8.x + EF Transactional Outbox/Inbox + consumer gửi email.
- **T0.5** — Đặc tả: `plan/tasks/T0.5.md`. BaseEntity + domain event, MediatR 12.x + ValidationBehavior, exception → ProblemDetails, Serilog → Seq, OpenAPI + Scalar; **Docker Compose + Seq**. Không có CSDL: DbContext sang T0.4, Repository/Specification sang T1.1. Quy trình chuẩn Codex → Huy gõ lại.

#### G1 — Tài khoản (P1: UC01, UC02)

| ID   | Việc                           | Phụ thuộc                | Trạng thái |
|------|--------------------------------|--------------------------|------------|
| T1.1 | BE: tài khoản, JWT, phân quyền | D7, T0.2, T0.4, T0.5, D1 | todo       |
| T1.2 | FE: đăng ký / đăng nhập        | T1.1, T0.3               | todo       |

**Chi tiết:**
- **T1.1** — Đăng ký, xác thực email, đăng nhập, JWT + refresh token; Role + Permissions (seed 5 vai trò), kiểm tra quyền theo permission. Nhận từ T0.5: IRepository/IReadRepository + Specification, SaveChangesInterceptor (audit), sinh Guid tuần tự.
- **T1.2** — Đăng ký / đăng nhập / đăng xuất, chặn route theo quyền.

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
| T3.1 | BE: hội nghị, ngày, phòng, track | D8, T1.1   | todo       |
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
