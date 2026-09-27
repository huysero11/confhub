# Kế hoạch & trạng thái task

> File này là "bộ nhớ chung" giữa các chat. **Mỗi khi task đổi trạng thái, cập nhật bảng ở mục 3 và nhật ký ở mục 4.**

## 1. Quy trình một task

```
Claude viết plan/tasks/<ID>.md
   → Codex code + test trên nhánh task/<ID>-<slug>
   → Claude review diff + chạy lại test (lặp tới khi đạt)
   → Claude viết mục 9 "Hướng dẫn gõ lại" (thứ tự file + vai trò + điểm dừng)
   → tag ref/<ID>  (bản tham khảo đã kiểm)
   → Huy test trên giao diện
   → Huy tự gõ lại từng file theo mục 9 (cùng nhánh)
   → Claude so bản gõ lại với ref/<ID> + chạy test, giải thích khi được hỏi
   → merge vào main, cập nhật bảng trạng thái
```

Trạng thái: `todo` · `spec` (đang viết file task) · `codex` (Codex đang làm) · `review` (Claude kiểm) · `ui-test` (Huy test giao diện) · `retype` (Huy gõ lại) · `done` (đã merge main) · `dropped` (bỏ, ghi lý do)

## 2. Chiến lược git

**Một nhánh cho mỗi task, không chia nhánh theo cả nhóm chức năng.** Task nhỏ thì nhánh sống ngắn, merge sớm, ít xung đột; nhánh theo nhóm sẽ sống vài tuần và rất khó review.

| Thành phần | Quy ước |
|---|---|
| `main` | Chỉ chứa code **Huy đã gõ lại** và test pass. Luôn chạy được. |
| `task/<ID>-<slug>` | Ví dụ `task/T1.1-jwt-auth`. Tạo từ `main` mới nhất. |
| Commit của Codex | `codex(<ID>): ...` |
| Commit của Huy | `<ID>: ...` (tự do) |
| Tag `ref/<ID>` | Gắn vào commit Codex cuối cùng đã được Claude duyệt = **bản tham khảo vĩnh viễn**. |

**Gõ lại mà không cần copy file ra chỗ khác:** tag `ref/<ID>` đã giữ bản tham khảo, nên có thể xóa/viết lại file thoải mái rồi xem lại bản gốc bằng:

```bash
git show ref/T1.1:be/src/ConfHub.Api/Program.cs     # xem 1 file bản tham khảo
git diff ref/T1.1 -- be/src/ConfHub.Api/Program.cs  # bản mình gõ khác bản tham khảo chỗ nào
git diff ref/T1.1 --stat                             # tổng quan khác biệt cả task
```

(VS Code: GitLens → "Compare with…" chọn tag `ref/T1.1`.) Vẫn thích copy ra chỗ khác cũng được, nhưng đặt ngoài repo hoặc trong `scratch/` (đã gitignore).

**Vì sao vẫn chia nhánh dù task sau sẽ chứa code task trước?** Nhánh không dùng để tách chức năng vĩnh viễn, mà để cách ly *việc đang dở*:
- `main` luôn là bản chạy được. Code Codex vừa sinh hoặc bản đang gõ dở nằm ở nhánh task, lỗi thì bỏ nhánh, `main` không bị ảnh hưởng.
- Mỗi nhánh/PR là diff của đúng 1 task → review, giải thích, chụp ảnh cho báo cáo theo từng task.
- Hai việc chạy chồng thời gian được: Huy đang gõ lại T1.1 thì Codex sinh T2.1 (thuật toán, độc lập) ở nhánh khác, không trộn vào nhau.

Task sau **luôn tách từ `main`**, nghĩa là xây trên code của Huy chứ không phải code Codex → hai bên không lệch nhau.

## 3. Bảng task

> Tái cấu trúc 2026-09-25 theo 57 UC (P1–P5) và ERD đã chốt. Chưa task nào bắt đầu nên đánh lại mã một lần; **từ đây áp dụng quy tắc 3b**.
> Hai giai đoạn: **A — Dự án công nghệ** (15 tuần hiện tại: nộp tài liệu + slide) và **B — Code** (chủ yếu ở giai đoạn khóa luận; G0 và G2 có thể làm sớm trong giai đoạn A để thăm dò rủi ro).
> Cột UC tham chiếu `docs/design/use-cases.md`; bảng tham chiếu `docs/design/erd.md`.

### Nguyên tắc: làm tăng dần theo từng nhóm chức năng (lát cắt dọc)

Mỗi nhóm chức năng đi đủ 4 lớp **cùng lúc**, chỉ trong phạm vi nhóm đó — không làm trước cho nhóm sau:

| Lớp | Làm đến đâu có đến đó |
|---|---|
| **Tài liệu** | Đặc tả UC + biểu đồ hoạt động / tuần tự của **đúng các UC trong nhóm** → cũng là phần báo cáo của nhóm |
| **Giao diện** | Bản thiết kế **đúng các màn hình của nhóm** (`docs/ui/<nhóm>.html`) trước khi code FE |
| **CSDL** | Migration chỉ tạo **bảng/cột nhóm đó cần**. Nhóm sau cần thêm cột → migration mới `<ID>_<MoTa>` (không sửa migration cũ) |
| **Hạ tầng** | Chỉ đăng ký service (DI) và thêm container vào `docker-compose.yml` ở **task đầu tiên dùng đến** (vd RabbitMQ ở T0.4, Hangfire ở T5.1, Redis ở T5.2, MinIO ở T8.1) |

`erd.md` và `use-cases.md` là **bản đồ tổng** để không thiết kế lệch; chi tiết từng nhóm chốt trong task thiết kế của nhóm đó.

### Giai đoạn A — Tài liệu (Claude + Huy, không giao Codex)

> Task thiết kế nhóm (D7–D14) có thể **chạy trước code** trong 15 tuần môn Dự án công nghệ; code của nhóm bắt đầu khi task thiết kế nhóm đã `done`.
> **Chưa rõ 4 tài liệu môn yêu cầu là gì** → khi Huy gửi đề bài/mẫu, ánh xạ lại.

| ID | Nội dung | Phụ thuộc | Trạng thái |
|---|---|---|---|
| D1 | ERD tổng (bản đồ): bảng + cột (`erd.md`) → vẽ PlantUML `docs/diagrams/erd.puml` | – | review |
| D2 | Biểu đồ UC tổng quát + phân rã theo 7 tác nhân (57 UC) | – | todo |
| D3 | ~~Đặc tả UC P1–P3 một lượt~~ | – | dropped — thay bằng thiết kế theo nhóm D7–D14 |
| D4 | ~~Biểu đồ hoạt động/tuần tự một lượt~~ | – | dropped — thay bằng D7–D14 |
| D5 | Kiến trúc hệ thống (sơ đồ thành phần, luồng sự kiện, triển khai) + khung 3 shell giao diện | D1 | todo |
| D6 | Slide trình bày | D1, D2, D5 + các task thiết kế nhóm đã làm | todo |
| D7 | Thiết kế nhóm G1 Tài khoản (UC01–02): đặc tả, biểu đồ, giao diện, bảng/cột | D1, D2 | todo |
| D8 | Thiết kế nhóm G3 Hội nghị, phiên (UC03–08) | D7 | todo |
| D9 | Thiết kế nhóm G4 Đăng ký (UC09–14) | D8 | todo |
| D10 | Thiết kế nhóm G5 Xếp lịch (UC15–19) | D9 | todo |
| D11 | Thiết kế nhóm G6 Dịch vụ (UC20–28) | D10 | todo |
| D12 | Thiết kế nhóm G7 Vận hành tại chỗ (UC29–32) | D11 | todo |
| D13 | Thiết kế nhóm G8 Hỏi đáp và AI (UC33–40) | D10 | todo |
| D14 | Thiết kế nhóm G9 Hỗ trợ (UC41–47) | D12, D13 | todo |

Mỗi task thiết kế nhóm tạo: `docs/specs/<nhóm>.md` (đặc tả UC) · `docs/diagrams/<nhóm>-*.puml` (hoạt động, tuần tự) · `docs/ui/<nhóm>.html` (giao diện) · cập nhật `erd.md` nếu chi tiết cột thay đổi.

### Giai đoạn B — Code

**G0 — Nền tảng**
| ID | Nội dung | Phụ thuộc | Trạng thái |
|---|---|---|---|
| T0.1 | Git + .gitignore + .gitattributes. ~~`docker-compose.yml` + Seq~~ → dời sang task đầu tiên dùng Serilog→Seq (khung chưa ghi log ra Seq; máy Huy chưa có Docker) | – | done |
| T0.2 | Khung .NET 10 Clean Architecture 4 lớp + `ConfHub.Scheduling` + các mẫu giữ lại từ khung base (BaseEntity, Specification/Repository, MediatR + ValidationBehavior, ExceptionMiddleware→ProblemDetails, Serilog→Seq, analyzer) + project test | T0.1 | retype (Huy làm tay **phần khung**: solution, project, reference, analyzer, `/health` + integration test; các mẫu code chờ quyết định tách task) |
| T0.3 | Khung FE: Vite React TS, antd v5 token sáng/tối, i18n vi/en, 3 shell A/B/C + router rỗng | T0.1 | todo |
| T0.4 | Hạ tầng sự kiện: thêm RabbitMQ + smtp4dev vào compose; MassTransit 8.x + EF Transactional Outbox/Inbox + consumer gửi email | T0.2 | todo |

**G1 — Tài khoản (P1: UC01, UC02)**
| ID | Nội dung | Phụ thuộc | Trạng thái |
|---|---|---|---|
| T1.1 | BE: đăng ký, xác thực email, đăng nhập, JWT + refresh token; Role + Permissions (seed 5 vai trò), kiểm tra quyền theo permission | D7, T0.2, T0.4, D1 | todo |
| T1.2 | FE: đăng ký / đăng nhập / đăng xuất, chặn route theo quyền | T1.1, T0.3 | todo |

**G2 — Thuật toán bản console (làm sớm, độc lập)**
| ID | Nội dung | Phụ thuộc | Trạng thái |
|---|---|---|---|
| T2.1 | Mô hình dữ liệu, tập mốc T, ma trận W, kiểm tra H1–H5, hàm f chuẩn hóa, bộ sinh dữ liệu có cụm chủ đề | T0.2 | todo |
| T2.2 | Giai đoạn 1 tham lam + Hungarian + M1, M2. Test ví dụ chạy tay f = 35 | T2.1 | todo |
| T2.3 | Giai đoạn 2 (Q1–Q3), khởi động lại, mô phỏng luyện kim, phạt xáo trộn. Test ví dụ f = 5 | T2.2 | todo |
| T2.4 | Chương trình thực nghiệm M1–M4 × 3 quy mô → CSV chỉ số, phân tích độ nhạy | T2.3 | todo |

**G3 — Hội nghị, phòng, phiên, diễn giả (P1: UC03–UC08)**
| ID | Nội dung | Phụ thuộc | Trạng thái |
|---|---|---|---|
| T3.1 | BE: Conference, ConferenceDay, BreakSlot, Room, RoomEquipment, ServiceItem (cơ bản), Track | D8, T1.1 | todo |
| T3.2 | BE: Session, SessionSpeaker (mời nhiều diễn giả / phản hồi), SpeakerAvailability | T3.1 | todo |
| T3.3 | FE Shell A (BTC): hội nghị, ngày, khung nghỉ, phòng, phiên, mời diễn giả | T3.2, T1.2 | todo |
| T3.4 | FE diễn giả: lời mời, khai giờ rảnh | T3.2, T1.2 | todo |

**G4 — Đăng ký (P1: UC09–UC14)**
| ID | Nội dung | Phụ thuộc | Trạng thái |
|---|---|---|---|
| T4.1 | BE: TicketType, xem hội nghị công khai, Registration (chế độ ăn), SessionInterest, duyệt / danh sách chờ, sự kiện RegistrationApproved | D9, T3.2 | todo |
| T4.2 | FE Shell B: danh sách + tìm kiếm hội nghị, chương trình, form đăng ký + tick phiên quan tâm | T4.1 | todo |
| T4.3 | FE Shell A: cấu hình loại vé, duyệt đăng ký | T4.1 | todo |

**G5 — Xếp lịch trong hệ thống (P1: UC15–UC19)**
| ID | Nội dung | Phụ thuộc | Trạng thái |
|---|---|---|---|
| T5.1 | Job Hangfire chạy thuật toán, lưu ScheduleVersion + ScheduleItem, phiên chưa xếp kèm lý do | D10, T2.3, T4.1 | todo |
| T5.2 | Lưới lịch kéo thả + kiểm ràng buộc tức thời + ghim + công bố + cache Redis | T5.1 | todo |
| T5.3 | Lịch cá nhân + cảnh báo trùng giờ (UC18) | T5.2 | todo |
| T5.4 | Xếp lại hạn chế xáo trộn + sự kiện ScheduleChanged (UC19) | T5.2 | todo |

— **Hết P1** —

**G6 — Dịch vụ đi kèm (P2: UC20–UC28)**
| ID | Nội dung | Phụ thuộc | Trạng thái |
|---|---|---|---|
| T6.1 | QT quản lý tài khoản: duyệt BTC/NCC, khóa/mở (UC20) | D11, T1.2 | todo |
| T6.2 | Danh mục dịch vụ, khung nghỉ có suất, chọn suất → Voucher, yêu cầu thiết bị + duyệt (UC21–UC24) | T4.1, T6.1 | todo |
| T6.3 | Tổng hợp nhu cầu (cộng / đỉnh đồng thời − sẵn có) + tạo và gửi đơn theo NCC (UC25–UC26) | T6.2, T5.2 | todo |
| T6.4 | NCC xử lý đơn, cập nhật giao hàng; tính lại khi ScheduleChanged → PreviousQuantity, NeedsReview (UC27–UC28) | T6.3, T5.4 | todo |

**G7 — Vận hành tại chỗ (P2: UC29–UC32)**
| ID | Nội dung | Phụ thuộc | Trạng thái |
|---|---|---|---|
| T7.1 | Xem vé + voucher QR (UC29) | D12, T6.2 | todo |
| T7.2 | Shell C: quét check-in cổng, quét voucher chống dùng lại (UC30–UC31) | T7.1 | todo |
| T7.3 | Check-in cửa phòng + dashboard thời gian thực SignalR, cảnh báo sắp đầy (UC32) | T7.2 | todo |

— **Hết P2** —

**G8 — Hỏi đáp và AI (P3: UC33–UC40)**
| ID | Nội dung | Phụ thuộc | Trạng thái |
|---|---|---|---|
| T8.1 | Diễn giả xem phiên được giao, tải slide + tóm tắt lên MinIO (UC33–UC34) | D13, T5.2 | todo |
| T8.2 | Hỏi đáp thời gian thực (SignalR group theo phiên), bình chọn, kiểm duyệt (UC35, UC37) | T5.2 | todo |
| T8.3 | Lớp trừu tượng AI (embedding + LLM) + gom nhóm câu hỏi (UC36) | T8.2 | todo |
| T8.4 | Trợ lý RAG theo phiên: trích text, chia đoạn, DocumentChunk, ngưỡng tương đồng (UC38) | T8.1, T8.3 | todo |
| T8.5 | Đánh giá + tóm tắt phản hồi bằng AI (UC39–UC40) | T8.3 | todo |

— **Hết P3** —

**G9 — Hỗ trợ (P4: UC41–UC47)**
| ID | Nội dung | Phụ thuộc | Trạng thái |
|---|---|---|---|
| T9.1 | Hồ sơ cá nhân, hủy đăng ký (UC41–UC42) | D14, T4.2 | todo |
| T9.2 | Thông báo hàng loạt (Notification), dashboard thống kê (UC43–UC44) | T7.3 | todo |
| T9.3 | Quản lý vai trò / quyền, cấu hình tham số (UC45–UC46) | T6.1 | todo |
| T9.4 | Nhận hàng và bàn giao thiết bị (UC47) | T7.2 | todo |

**G10 — Mở rộng (P5: UC48–UC57)** — chỉ chia task khi P1–P4 xong.

**G11 — Thực nghiệm chính thức**
| ID | Nội dung | Phụ thuộc | Trạng thái |
|---|---|---|---|
| T11.1 | Chạy thực nghiệm M1–M4, 11 chỉ số, phân tích độ nhạy; bảng + biểu đồ cho chương đánh giá | T2.4, T5.4 | todo |

**Thứ tự cắt giảm nếu chậm:** G10 → G9 → T8.5 → T6.4 (NCC chỉ còn xem đơn). **Giữ bằng mọi giá:** P1 (G1–G5), T6.3, G7, T8.4, T11.1.

## 3b. Quy tắc giữ plan không rối

1. **Không đổi/tái sử dụng mã task.** Task bị bỏ → trạng thái `dropped` kèm lý do. Task phát sinh → thêm mã mới cuối nhóm (vd `T3.5`), không chèn số.
2. **Mỗi lúc chỉ 1 task ở trạng thái `codex`/`review`**, trừ khi 2 task không phụ thuộc nhau (vd G1 và G2).
3. **Chỉ bắt đầu task khi mọi task trong cột Phụ thuộc đã `done`** (đã merge `main`).
4. Đặc tả `plan/tasks/<ID>.md` được Huy duyệt xong mới giao Codex. Đổi phạm vi giữa chừng → sửa file task trước, rồi mới sửa code.
5. Mọi thay đổi bảng task hoặc quyết định thiết kế → ghi 1 dòng vào Nhật ký; quyết định lâu dài → cập nhật `PROJECT.md`.
6. Cuối mỗi chat: bảng trạng thái phải phản ánh đúng thực tế (nhánh nào tồn tại, task nào dở).

## 4. Nhật ký

| Ngày | Sự kiện |
|---|---|
| 2026-09-25 | Dựng khung điều phối (CLAUDE.md, AGENTS.md, plan/). Đã gửi GVHD danh sách UC. |
| 2026-09-25 | T0.4: bản nháp ERD mức tổng thể `docs/design/erd.md` (66 bảng, 9 nhóm), chờ Huy chốt Q1–Q7. |
| 2026-09-25 | Chốt 57 UC theo ưu tiên P1–P5 (`docs/design/use-cases.md`), rút ERD còn 48 bảng (39 bảng cho P1–P3). 15 tuần hiện tại là môn Dự án công nghệ: nộp 4 tài liệu + slide, code để giai đoạn khóa luận. Bảng task cần sắp lại theo P1→P5. |
| 2026-09-25 | ERD gộp thêm 3 bảng (RefreshToken→UserToken, Supplier→cột User, RegistrationService→Voucher): 45 bảng tổng, 35 bảng nghiệp vụ cho P1–P3. Đối chiếu 3 khóa luận khóa trước: 11–26 bảng. |
| 2026-09-25 | ERD rút còn 36 bảng (26 bảng nghiệp vụ cho P1–P3): gộp ConferenceDay, ConferenceSpeaker, SessionMaterial, ServiceOffering, Permission, RolePermission, UserRole, ConferenceMember, PollOption. |
| 2026-09-25 | ERD: thêm lại ConferenceDay (mỗi ngày khung giờ riêng); bỏ CompanyName/TaxCode/Address/Interests khỏi User (dùng Organization); viết mô tả từng cột. 37 bảng, ERD nộp môn 27 bảng. |
| 2026-09-25 | ERD rút cột: chỉ giữ cột có UC/thuật toán dùng, bỏ cột suy ra được (tổng tiền, số phiên đã xếp, EndAt, VoteCount…). Thanh toán ra ngoài phạm vi; diễn giả phải có tài khoản trước khi được mời. |
| 2026-09-25 | Chốt ERD (1 diễn giả/phiên → bỏ SessionSpeaker; 36 bảng, ERD nộp môn 26). Tái cấu trúc bảng task: giai đoạn A (tài liệu D1–D6) + giai đoạn B (G0–G11 theo P1→P5). |
| 2026-09-25 | ERD: quay lại cho phép nhiều diễn giả/phiên (SessionSpeaker, slide theo từng diễn giả). 37 bảng, ERD nộp môn 27. |
| 2026-09-25 | Rà khung TD.Microservice.ServiceBase → `docs/design/base-reference.md` (giữ/bỏ/quy tắc chất lượng). Chốt DDD nhẹ + event-driven cho hiệu ứng phụ, .NET 10, MassTransit EF Outbox. |
| 2026-09-25 | Chốt dùng MassTransit EF Outbox. Thêm bước bắt buộc: mỗi task có mục 9 "Hướng dẫn gõ lại" (thứ tự file, vai trò, điểm dừng) trước khi tag ref. |
| 2026-09-25 | Nguyên tắc làm tăng dần theo nhóm: tài liệu + giao diện + bảng/cột + hạ tầng chỉ làm tới nhóm đang làm. Thêm D7–D14 (thiết kế từng nhóm), D3/D4 dropped. Task code đầu mỗi nhóm phụ thuộc task thiết kế nhóm. |
| 2026-09-27 | Bắt đầu G0 phần backend: Huy làm tay T0.1 + khung T0.2 (không Codex, không tag ref; Claude review diff trước khi merge). Khung đã chạy thử với SDK 10.0.112. T0.3 để sau. |
| 2026-09-27 | Dời `docker-compose.yml` + Seq khỏi T0.1 sang task đầu tiên dùng Serilog→Seq (đúng nguyên tắc hạ tầng thêm ở task dùng đến). Máy Huy cần cài .NET 10 SDK (đang có 8, 9). |
| 2026-09-27 | T0.1 done: merge `task/T0.1-git` vào main (--no-ff). Bắt đầu khung T0.2 trên `task/T0.2-be-skeleton`. |
