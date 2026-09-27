# Kịch bản thật → UC → bảng dữ liệu

> Dùng để hiểu ERD và làm ví dụ trong báo cáo. Hội nghị **"Công nghệ Xanh 2026"**: 2 ngày, 3 phòng, 12 phiên.
> Mỗi bước ghi: ai làm gì · UC · bảng **ghi** (✎) / **đọc** (👁) · dữ liệu mẫu.

## Giai đoạn 1 — Chuẩn bị

**B1. Chị Lan tạo tài khoản ban tổ chức** — UC01, UC20
- ✎ `User` (Lan, Role = Organizer, Status = `Unverified`) · ✎ `UserToken` (Purpose = VerifyEmail)
- Lan bấm link email → Status = `PendingApproval` → quản trị viên duyệt → `Active`

**B2. Lan tạo hội nghị và khai ngày** — UC03
- ✎ `Conference` (Công nghệ Xanh 2026, RequireApproval = true, Owner = Lan)
- ✎ `ConferenceDay`: Ngày 1 = 08:00–17:00 · Ngày 2 = 08:00–12:00

**B3. Lan khai danh mục dịch vụ** — UC21 *(bảng dùng từ P1 vì bước 4 cần)*
- ✎ `ServiceItem`:

| Name | Kind | UnitPrice | QtyPerActiveRoom | Supplier |
|---|---|---|---|---|
| Suất ăn trưa | Consumable | 85.000 | – | chị Mai (nhà hàng) |
| Trà bánh | Consumable | 30.000 | – | chị Mai |
| Máy chiếu | Rental | 500.000 | 1 | anh Hùng (thiết bị) |
| Micro không dây | Rental | 100.000 | 2 | anh Hùng |

**B4. Lan khai phòng và thiết bị sẵn có** — UC04
- ✎ `Room`: P101 (100 chỗ) · P102 (60) · P103 (40)
- ✎ `RoomEquipment`: P101 có 1 máy chiếu · P102 có 1 máy chiếu · P103 không có gì

**B5. Lan khai khung nghỉ** — UC03
- ✎ `BreakSlot`: Ngày 1 "Nghỉ trưa" 11:30–13:00 → Suất ăn trưa, IsOptional = false (gồm trong vé) · Ngày 1 "Tea break" 15:00–15:15 → Trà bánh, IsOptional = true (ai muốn thì chọn)

**B6. Lan tạo track, phiên, mời diễn giả** — UC08, UC05
- ✎ `Track`: Công nghệ · Kinh tế · Khởi nghiệp
- ✎ `Session`: A "AI trong nông nghiệp" 60′ · D "Blockchain carbon" 60′ · … (12 phiên)
- ✎ `SessionSpeaker`: (A, Long, `Pending`) · (A, chị Hoa, `Pending`) · (D, Long, `Pending`) — phiên A có 2 diễn giả

**B7. Anh Long nhận lời, khai giờ rảnh** — UC06, UC07
- ✎ `SessionSpeaker`: (A, Long) và (D, Long) → `Accepted`
- ✎ `SpeakerAvailability`: (Ngày 1, Long, 09:00–11:00)

**B8. Lan tạo loại vé** — UC09
- ✎ `TicketType`: Vé thường 300.000, Quota 250 · Vé sinh viên 100.000, Quota 50

## Giai đoạn 2 — Đăng ký

**B9. Anh Minh xem hội nghị, đăng ký** — UC10, UC11, UC12, UC13, UC22
- 👁 `Conference`, `Session`, `Track`
- ✎ `Registration` (Minh, Vé thường, `Pending`, DietType = Vegetarian, AllergyFlags = 0, TicketCode = "K7Q2…")
- ✎ `SessionInterest`: (Minh, A) · (Minh, C) — Minh tick 2 phiên quan tâm
- Minh tick thêm tea break (IsOptional = true) → ✎ `Voucher` (Minh, Tea break N1) ngay lúc chọn; voucher chỉ quét được khi đăng ký `Approved`

**B10. Lan duyệt đăng ký** — UC14
- ✎ `Registration`: Status → `Approved`
- ✎ `Voucher`: (Minh, Nghỉ trưa N1) — tự tạo cho mọi khung gồm trong vé
- ✎ `OutboxMessage` (RegistrationApproved) → gửi email vé QR cho Minh

## Giai đoạn 3 — Xếp lịch

**B11. Hết hạn đăng ký, Lan bấm "Chạy xếp lịch"** — UC15
- 👁 đọc: `Session` (thời lượng, quy mô) · `SessionSpeaker` (diễn giả `Accepted` → H1: A và D cùng có Long nên không được chồng giờ) · `SpeakerAvailability` (H5) · `ConferenceDay` + `BreakSlot` (sinh mốc T, bỏ giờ nghỉ) · `Room` (H3, H4) · `SessionInterest` (tính ma trận **W**, vd 35 người quan tâm cả A và C)
- ✎ `ScheduleVersion` v1 (Status = `Draft`, W = 0,7/0,2/0,1, ConflictCount = 5, …)
- ✎ `ScheduleItem` ×12: (v1, A, P101, N1 09:00) · … · (v1, K, null, null, "Diễn giả kín lịch")
- Giao diện hiện: "5 lượt xung đột, 11/12 phiên đã xếp" (11/12 = đếm ScheduleItem có StartAt)

**B12. Lan kéo thả chỉnh, ghim, công bố** — UC16, UC17
- ✎ `ScheduleItem` (đổi giờ/phòng, IsLocked = true cho phiên đã ghim)
- ✎ `ScheduleVersion` v1 → `Published` · ✎ `Conference` Status → `Scheduled`

**B13. Minh xem lịch cá nhân** — UC18
- 👁 `SessionInterest` (Minh) ⋈ `ScheduleItem` (bản Published) → A 09:00 và C 09:00 trùng giờ → cảnh báo

## Giai đoạn 4 — Dịch vụ

**B14. Anh Long yêu cầu thêm loa, Lan duyệt** — UC23, UC24
- ✎ `SessionEquipmentRequest` (A, Loa, 1, `Pending` → `Approved`)

**B15. Lan bấm "Tổng hợp nhu cầu", gửi đơn** — UC25, UC26
- **Suất ăn** (hàng tiêu hao = phép cộng): 👁 `Voucher` của khung Nghỉ trưa N1 + `Registration` (DietType, AllergyFlags) → 234 mặn · 12 mặn không hải sản · 41 chay
- **Thiết bị** (hàng thuê = đỉnh đồng thời): 👁 `ScheduleItem` → ngày 1 có tối đa **3 phòng chạy song song**
  - Máy chiếu: 3 phòng × 1 − sẵn có 2 (`RoomEquipment`) = **thuê 1**
  - Micro: 3 phòng × 2 − 0 = **thuê 6**
  - Loa: +1 từ yêu cầu đã duyệt
- ✎ `ServiceOrder` ×2 (tách theo `ServiceItem.SupplierId`): đơn chị Mai, đơn anh Hùng — Status = `Sent`
- ✎ `ServiceOrderLine`: (đơn Mai, Suất ăn trưa, N1, Variant "Chay", 41, 85.000) · (đơn Hùng, Máy chiếu, N1, 1, 500.000) · …

**B16. Chị Mai, anh Hùng xác nhận** — UC27, UC28
- ✎ `ServiceOrder` Status → `Confirmed` → `Delivered`

## Giai đoạn 5 — Biến động

**B17. Ba ngày trước, một diễn giả báo ốm; Lan chạy xếp lại** — UC19
- ✎ `ScheduleVersion` v2 (BaseVersionId = v1, phạt xáo trộn) · ✎ `ScheduleItem` ×12 của v2
- Công bố v2: v2 → `Published`, v1 → `Superseded`. Số phiên bị dời = so ScheduleItem v2 với v1
- Tổng hợp lại: ngày 2 chỉ còn 2 phòng song song → ✎ `ServiceOrderLine` máy chiếu N2: Quantity 3 → 2, **PreviousQuantity = 3** · ✎ `ServiceOrder` (Hùng) NeedsReview = true
- ✎ `OutboxMessage` (ScheduleChanged) → email anh Hùng "Ngày 2 giảm 3 → 2 máy chiếu"

## Giai đoạn 6 — Ngày diễn ra

**B18. 8h, Nam quét vé ở cổng** — UC30
- 👁 `Registration` theo TicketCode · ✎ `Registration`.CheckedInAt = 08:05

**B19. 9h15, quét ở cửa phòng, Lan theo dõi** — UC30, UC32
- ✎ `SessionCheckIn` (A, Minh) · 👁 đếm `SessionCheckIn` của A so với `Room`.Capacity → "96/100 — sắp đầy"

**B20. 11h30, Nam quét voucher trưa** — UC31
- 👁 `Voucher` theo Code → hiện "SUẤT CHAY" (lấy DietType từ `Registration`)
- ✎ `Voucher`.RedeemedAt = 11:42 → quét lại báo "đã sử dụng 11:42"

## Giai đoạn 7 — Hỏi đáp và AI

**B21. Anh Long tải slide** — UC33, UC34
- 👁 `SessionSpeaker` của Long · ✎ `SessionSpeaker` (A, Long).SlideKey, SlideSummary
- Job nền trích text, chia đoạn → ✎ `DocumentChunk` ×N (SessionId = A, Content, Embedding)

**B22. Minh hỏi trợ lý AI trong phiên A** — UC38
- 👁 `DocumentChunk` **chỉ của phiên A** → top đoạn giống câu hỏi → LLM trả lời (không lưu câu hỏi AI)

**B23. 47 câu hỏi công khai, gom nhóm, BTC kiểm duyệt** — UC35, UC36, UC37
- ✎ `Question` ×47 · ✎ `QuestionVote` (ai bình chọn câu nào)
- Gom nhóm → ✎ `Question`.ClusterLabel = "Chi phí triển khai" (18 câu) · BTC ẩn câu spam → Status = `Hidden`

**B24. Sau sự kiện** — UC39, UC40
- ✎ `Feedback` ×240 · 👁 `Feedback` → AI tóm tắt thành nhóm ý kiến

## Đối chiếu: mọi bảng P1–P3 đều được dùng

| Bảng | Bước | Bảng | Bước |
|---|---|---|---|
| User, UserToken | B1 | ScheduleVersion, ScheduleItem | B11, B12, B17 |
| Role | B1 (phân quyền mọi bước) | TicketType | B8 |
| Conference, ConferenceDay | B2 | Registration | B9, B10, B18, B20 |
| ServiceItem | B3, B15 | SessionInterest | B9, B11, B13 |
| Room, RoomEquipment | B4, B15 | SessionCheckIn | B19 |
| BreakSlot | B5, B11 | SessionEquipmentRequest | B14 |
| Track, Session | B6 | Voucher | B10, B15, B20 |
| SessionSpeaker | B6, B7, B11, B21 | ServiceOrder, ServiceOrderLine | B15–B17 |
| SpeakerAvailability | B7, B11 | DocumentChunk | B21, B22 |
| OutboxMessage | B10, B17 | Question, QuestionVote, Feedback | B23, B24 |
