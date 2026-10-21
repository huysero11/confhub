# Thuật toán xếp lịch — giải thích theo từng giai đoạn

> Tài liệu để Huy học và bảo vệ. Nguồn: đề cương v5 mục 5, PROJECT.md mục 6.
> Phần nào là lựa chọn của Claude (chưa có trong đề cương) thì có ghi "chọn cho T2.1".
> Ánh xạ task: GĐ0–2 → T2.1 · GĐ3 → T2.2 · GĐ4–5 → T2.3 · so sánh M1–M4 → T2.4 ·
> GĐ6 → T2.3 + T5.4 · GĐ7 → làm sau.

## Ví dụ xuyên suốt (đề cương 5.5)

- 2 khung giờ (9h, 10h); 2 phòng: P101 (100 chỗ), P102 (40 chỗ).
- 4 phiên: A và D của Long (Long chỉ rảnh 9h–11h), B của Hoa, C của Tùng.
- Quy mô phiên: A = 42, B = 39, C = 21, D = 14.
- Người quan tâm: 35 người quan tâm cả A và C, 5 người quan tâm cả A và B.

## Giai đoạn 0 — Chuẩn bị dữ liệu

**Tập mốc thời gian T.** Các mốc có thể bắt đầu phiên, sinh từ giờ tổ chức theo bước cố định
(9h–12h, bước 15 phút → 9:00, 9:15, …, 11:45). Phiên dài `d` phút đặt ở mốc `t` thì chiếm
khoảng `[t, t + d)`.

**Ma trận đồng quan tâm W.** `W[i][j]` = số người quan tâm cả phiên i lẫn phiên j.

    W[i][j] = |{ người p : p quan tâm i và p quan tâm j }|

Tính một lần từ danh sách (người, phiên). Nhờ đó tính f chỉ cần tra bảng, không duyệt lại
toàn bộ người tham dự. Ví dụ: `W[A][C] = 35`, `W[A][B] = 5`, các cặp còn lại bằng 0.

**Quy mô phiên** (dùng cho H4 và S). Đề cương: quy mô = số người quan tâm × hệ số tham dự thực
tế (ví dụ 60 × 0,7 = 42). Chọn cho T2.1: lấy `ExpectedSize` nếu BTC nhập, không thì lấy số
người quan tâm (hệ số 1). Hệ số hiệu chỉnh làm sau, khi có check-in để đo sai số.

**Độ ràng buộc của phiên** (quyết định phiên nào xếp trước):

    độ ràng buộc = (số mốc diễn giả rảnh) − (số phiên diễn giả đó phải trình bày)

Số nhỏ = chặt = xếp trước. Ví dụ: Long có 2 mốc rảnh và 2 phiên → 0; Hoa và Tùng có 2 mốc
và 1 phiên → 1. Thứ tự xếp: A, D, rồi B, C.

## Giai đoạn 1 — Ràng buộc cứng H1–H5 (bộ lọc, kiểm trước khi tính f)

| Mã | Nội dung                                          | Ví dụ                               |
|----|---------------------------------------------------|-------------------------------------|
| H1 | Một diễn giả không có hai phiên chồng giờ         | D xếp 9h bị loại: Long đang dạy A   |
| H2 | Số phiên song song tại mọi lúc ≤ số phòng         | C xếp 10h bị loại: 2 phòng đã kín   |
| H3 | Mỗi phòng tối đa một phiên tại một thời điểm      | Hai phiên cùng P101 lúc 9h là sai   |
| H4 | Sức chứa phòng ≥ quy mô phiên                     | A (42) vào P102 (40) bị bác         |
| H5 | Phiên trong giờ tổ chức và giờ diễn giả rảnh      | Long rảnh 9–11h, không xếp lúc 14h  |

H2 phải kiểm ngay lúc chọn giờ, không để sau: nếu kiểm sau, có thể xếp 5 phiên song song khi
chỉ có 3 phòng, rồi phải làm lại từ đầu.

## Giai đoạn 2 — Hàm mục tiêu f (càng nhỏ, lịch càng tốt)

    f = w₁·C + w₂·B + w₃·S      với w₁ + w₂ + w₃ = 1, mặc định 0,7 / 0,2 / 0,1

C, B, S đều **tính được chính xác** từ lịch, `W` và sức chứa phòng; không phải ước lượng.
Lịch dùng để tính (lịch tham lam của ví dụ):

| Phòng \ Giờ | 9h | 10h |
|-------------|----|-----|
| P101        | A  | B   |
| P102        | C  | D   |

### C — xung đột nguyện vọng

    C = Σ W[i][j]    cộng trên mọi cặp phiên (i, j) chồng giờ

| Cặp  | Chồng giờ?        | W  | Cộng vào C |
|------|-------------------|----|------------|
| A–B  | Không (9h và 10h) | 5  | 0          |
| A–C  | Có (cùng 9h)      | 35 | 35         |
| A–D  | Không             | 0  | 0          |
| B–C  | Không             | 0  | 0          |
| B–D  | Có (cùng 10h)     | 0  | 0          |
| C–D  | Không             | 0  | 0          |

C = 35: có 35 người muốn xem cả A lẫn C nhưng chỉ xem được một trong hai.

### B — độ lệch tải

B đo số phiên có đều giữa các khung giờ không: đếm số phiên mỗi khung, tính trung bình `m`,
rồi lấy độ lệch chuẩn (chia cho số khung, không chia cho số khung − 1):

    B = √( Σ (số phiên mỗi khung − m)² / số khung )

- Lịch trên: 9h có 2 phiên, 10h có 2 phiên → dãy (2, 2), m = 2, mỗi khung lệch 0
  → **B = 0**. B bằng 0 vì lịch đã chia đều 2–2, không phải do chuẩn hóa.
- 6 phiên, 2 khung, 4 phòng: chia 3–3 → dãy (3, 3) → B = 0; chia 4–2 → dãy (4, 2),
  m = 3, lệch +1 và −1 → B = √((1 + 1) / 2) = **1**.
- Với dữ liệu thật, mốc 15 phút và phiên dài 60–90 phút nên B hiếm khi bằng đúng 0.

### S — sức chứa lãng phí

    S = Σ (sức chứa phòng − quy mô phiên)    trên các phiên đã gán phòng

| Phiên | Phòng | Tính      | Kết quả |
|-------|-------|-----------|---------|
| A     | P101  | 100 − 42  | 58      |
| B     | P101  | 100 − 39  | 61      |
| C     | P102  | 40 − 21   | 19      |
| D     | P102  | 40 − 14   | 26      |

S = 58 + 61 + 19 + 26 = 164 ghế trống.

### Chuẩn hóa về thang 0–1

C = 35 (lượt người), B = 0 (số phiên), S = 164 (ghế) khác đơn vị và độ lớn. Cộng thẳng thì S
lấn át: `f = 0,7·35 + 0,2·0 + 0,1·164 = 24,5 + 16,4 = 40,9`, S chiếm gần 40 % dù trọng số
chỉ 0,1. Cách xử lý: chia mỗi đại lượng cho **giá trị lớn nhất nó có thể đạt** (chọn cho T2.1):

- **C:** `Cmax = Σ_{i<j} W[i][j]` (xấu nhất: mọi cặp đều trùng giờ). Ví dụ 35 + 5 = 40
  → `Cn = 35 / 40 = 0,875`.
- **B:** `Bmax = số phòng / 2`. Ví dụ 2 / 2 = 1 → `Bn = 0 / 1 = 0`.
- **S:** `Smax = Σ_i (sức chứa lớn nhất − quy mô_i)` (xấu nhất: mọi phiên vào phòng lớn nhất).
  Ví dụ 58 + 61 + 79 + 86 = 284 → `Sn = 164 / 284 ≈ 0,577`.

- Cận của B: xấu nhất là lúc cả r phòng chạy, lúc 0 phòng chạy → dãy (r, 0), trung bình r/2,
  mỗi bên lệch r/2 → độ lệch chuẩn r/2.
- B = 0 thì Bn = 0 / Bmax = 0, vẫn là 0.
- Cận của S: xấu nhất là mọi phiên đều vào phòng lớn nhất (100 chỗ).

### Ghép thành f (trọng số mặc định)

| Lịch              | Cn            | Bn | Sn    | f                                     |
|-------------------|---------------|----|-------|---------------------------------------|
| Tham lam          | 0,875         | 0  | 0,577 | 0,7·0,875 + 0,1·0,577 ≈ 0,670         |
| Sau hoán đổi C↔B  | 5/40 = 0,125  | 0  | 0,577 | 0,7·0,125 + 0,1·0,577 ≈ 0,145         |

Sau hoán đổi S không đổi (58 + 1 + 79 + 26 = 164) vì cả hai khung vẫn dùng đủ hai phòng; chỉ
C giảm từ 35 xuống 5. Đề cương chạy tay ví dụ chỉ với C (đặt w₂ = w₃ = 0) nên con số 35 và 5
là C thô.

## Giai đoạn 3 — Tham lam + gán phòng Hungarian

Với từng phiên theo thứ tự độ ràng buộc: duyệt mọi mốc t → loại mốc vi phạm H1–H5 → trong các
mốc còn lại chọn mốc cho f nhỏ nhất (f chỉ tính trên phiên đã xếp) → không còn mốc thì đánh dấu
"chưa xếp được" kèm lý do.

| Phiên | Các mốc xét                           | Chọn      |
|-------|---------------------------------------|-----------|
| A     | 9h: f = 0 · 10h: f = 0 → hòa          | 9h, P101  |
| D     | 9h loại (H1) · 10h: f = 0             | 10h, P102 |
| B     | 9h chồng A → +5 · 10h chồng D → +0    | 10h, P101 |
| C     | 9h chồng A → +35 · 10h hết phòng (H2) | 9h, P102  |

Kết quả C = 35. Điểm yếu của tham lam: B chọn 10h vì lúc đó rẻ hơn, nhưng chiếm mất chỗ khiến
C buộc phải đứng cạnh A.

**Hungarian** gán k phiên cùng khung giờ vào các phòng sao cho tổng chi phí nhỏ nhất (bài toán
phân công), độ phức tạp O(k³) với k là số phòng:

    cost(phiên i, phòng j) = sức chứa_j − quy mô_i       (∞ nếu sức chứa_j < quy mô_i)

Ví dụ khung 9h có A (42) và C (21):

|   | P101 | P102 |
|---|------|------|
| A | 58   | ∞    |
| C | 79   | 19   |

Cách gán khả thi duy nhất: A→P101, C→P102, tổng 77.

Nhận xét của Claude (ngoài đề cương): vì chi phí có dạng `sức chứa − quy mô`, tổng chi phí =
(tổng sức chứa các phòng được chọn) − (tổng quy mô). Hungarian thực chất chọn tập phòng nhỏ
nhất vẫn thỏa H4, và quan trọng hơn là phát hiện được khi không có cách gán nào khả thi.

## Giai đoạn 4 — Cải thiện cục bộ có định hướng

Thử **dời** một phiên sang mốc khác hoặc **hoán đổi** hai phiên; chỉ nhận khi vẫn thỏa H1–H5
và f giảm. Ba quy tắc thu hẹp:

- **Q1:** `đóng góp(X) = Σ W[X][Y]` với mọi Y đang chồng giờ với X. Chỉ biến đổi phiên đóng góp
  cao nhất (đóng góp 0 thì đổi cũng không làm f giảm).
- **Q2:** khi dời, chỉ xét mốc diễn giả rảnh và còn phòng; mốc khác loại ngay, không tính f.
- **Q3:** chỉ hoán đổi các cặp nằm trong nhóm đang xung đột.

Ví dụ: đóng góp A = 35, C = 35, B = 0, D = 0 → chỉ thử A và C.

| Phép thử       | Kiểm tra ràng buộc                          | C mới | Quyết định         |
|----------------|---------------------------------------------|-------|--------------------|
| Dời C sang 10h | Hết phòng, loại ngay (Q2)                   | –     | Bỏ qua             |
| Hoán đổi C ↔ B | B→P102 (39 ≤ 40) ✓, Hoa rảnh 9h ✓, C→P101 ✓ | 5     | Chấp nhận (35 → 5) |
| Hoán đổi A ↔ D | A (42) → P102 (40) vi phạm H4               | –     | Bác bỏ             |

Kết quả: 9h có A, B; 10h có C, D; C = 5.

## Giai đoạn 5 — Thoát cực trị cục bộ

Q1 có điểm mù: đôi khi phải dời một phiên "vô tội" đi nhường chỗ, bước đó làm f tăng tạm thời
nên bị bỏ qua. Cài cả hai cách để so sánh:

- **Khởi động lại nhiều lần:** chạy lại giai đoạn 1 với thứ tự phiên khác, giữ kết quả tốt nhất.
- **Mô phỏng luyện kim:** nhận bước làm f tăng Δ với xác suất `P = e^(−Δ / Θ)`, nhiệt độ Θ giảm
  dần (`Θ ← α·Θ`, α khoảng 0,95). Ví dụ Δ = 0,02: Θ = 0,05 → P = e^(−0,4) ≈ 0,67; Θ = 0,01 →
  P = e^(−2) ≈ 0,14. Giá trị Θ, α chốt ở T2.3.

## Giai đoạn 6 — Xếp lại hạn chế xáo trộn

Thêm phạt cho phiên bị đổi vị trí so với lịch đã công bố: `f' = f + w₄·D`, D = số phiên đổi vị
trí (có thể chuẩn hóa `D / n`; ví dụ 12 phiên đổi 2 → 0,167). Dạng cụ thể chốt ở T2.3 / T5.4.

## Giai đoạn 7 — Mở rộng dùng dữ liệu (làm sau, ngoài G2)

- **Ước lượng quy mô:** như GĐ0, đối chiếu check-in thật để đo sai số.
- **Cold start (chưa có dữ liệu quan tâm):** thay `W` bằng độ tương đồng chủ đề giữa các phiên:
  `cos(u, v) = u·v / (|u|·|v|)`. Ví dụ u = (1, 2, 0), v = (2, 1, 1) → 4 / (√5·√6) ≈ 0,73.

---

## Luồng chạy ví dụ 6.5 trong code (T2.1)

> Mục đích: thấy **tham số của mỗi hàm từ đâu ra**. Chưa có thuật toán (T2.2), nên ở đây "người gọi" là
> thuật toán tham lam chạy tay theo bảng PROJECT.md 6.5 (thứ tự A, D, B, C). Test đóng vai này:
> `ConstraintCheckerTests`, `ObjectiveCalculatorTests.GreedyScheduleOfTheExampleHasConflict35`.
> Đường dẫn tính từ `be/src/ConfHub.Scheduling/`. Mã: A = 1, D = 2, B = 3, C = 4; P101 = 1, P102 = 2.

### Bước 1 — Dựng bài toán: `new SchedulingProblem(sessions, rooms, days, availabilities, interests, 60)`

Dữ liệu thô (do tầng gọi đưa vào, sau này T5.1 đọc từ CSDL):
- 4 phiên 60 phút: A (Long, 42 chỗ), D (Long, 14), B (Hoa, 39), C (Tùng, 21).
- 2 phòng: P101 = 100 chỗ, P102 = 40 chỗ. 1 ngày 9:00–11:00 = `[540, 660)`, không khung nghỉ.
- 80 dòng nguyện vọng: người 1–35 quan tâm A, C; người 36–40 quan tâm A, B. Không ai khai giờ rảnh.

Constructor chạy lần lượt (`Problem/SchedulingProblem.cs`):
1. `ValidateDays / Rooms / Sessions / SpeakerAvailabilities / Interests` → dữ liệu hợp lệ, không ném lỗi.
2. `new Timeline(Days, 60)` → `AddDaySlot(ngày 0, 0)`:
   - mốc 540: `[540, 600)` không chạm khung nghỉ → `TimeSlot(Index 0, Day 0, 540)`
   - mốc 600: `[600, 660)` → `TimeSlot(Index 1, Day 0, 600)`; mốc 660 vượt cuối ngày → dừng.
3. `new InterestMatrix(Sessions, interests)`:
   - `_indexBySessionId = {1→0, 2→1, 3→2, 4→3}`
   - `GroupSessionsByAttendee` → người 1–35: `[0, 3]`, người 36–40: `[0, 2]`
   - `CalculateMatrix` → `W[A][C] = 35`, `W[A][B] = 5`, `W[A][A] = 40`, `TotalCoInterest = 40`.
4. `BuildAvailabilityBySpeaker` → rỗng (không ai khai) ⇒ mọi diễn giả rảnh mọi lúc.
5. `BuildValidStartSlots` → với từng phiên, từng mốc:
   `Timeline.CanHost(mốc, 60)` (nằm trọn trong mốc dùng được?) **và** `AreAllSpeakersFree(...)`
   ⇒ mọi phiên có mốc hợp lệ `[0, 1]`. Kết quả để sẵn cho `IsValidStartSlot` (H5).

⇒ Từ đây **mọi số liệu đã tính sẵn**; các lớp sau chỉ tra, không tính lại.

### Bước 2 — Thuật toán tạo công cụ

```
var checker = new ConstraintChecker(problem);
var calculator = new ObjectiveCalculator(problem, ObjectiveWeights.Default);
var schedule = new Schedule();        // lịch rỗng
```

`ObjectiveCalculator` tính sẵn `Smax` trong `CalculateMaxWastedSeats`: phòng lớn nhất 100 →
(100−42) + (100−14) + (100−39) + (100−21) = **284**.

### Bước 3 — Đặt A lúc 9h, P101: `checker.CheckPlacement(schedule, 1, 0, 1)`

Tham số: `1` = phiên A, `0` = mốc 9h (Index của `TimeSlot`), `1` = P101 — do thuật toán chọn.

1. H5: `problem.IsValidStartSlot(1, 0)` → `true`.
2. `IsSlotInRange(0)` → có mốc 0.
3. `BuildPlacedSession(1, 0, 1)` → gọi `problem.GetSessionInterval(1, 0)` → `(Day 0, [540, 600))`
   ⇒ `PlacedSession(A, 0, [540, 600), P101)`. Đây là lý do có `GetSessionInterval`: đổi
   **mốc** thành **khoảng thời gian** để so chồng giờ.
4. `BuildPlacedSessions(schedule, 1)` → lịch rỗng ⇒ không có phiên khác.
5. H2: `AddCandidateConcurrencyViolations` → tại 540 có 1 phiên ≤ 2 phòng.
6. H4: `AddCapacityViolation(…, 1, 1)` → `GetRoom(1).Capacity` 100 ≥ `GetSessionSize(1)` 42.

Kết quả rỗng ⇒ thuật toán gọi `schedule.Place(1, 0, 1)`.

### Bước 4 — Đặt D: thử 9h rồi 10h

`CheckPlacement(schedule, 2, 0, null)` (thử 9h, chưa chọn phòng):
- `BuildPlacedSessions` → `[A (Day 0, [540, 600))]`
- `IsTimeOverlapping(D, A)` → cùng ngày và `[540, 600)` giao `[540, 600)` ⇒ đi tiếp:
  - `AddSpeakerViolation(…, 2, 1)` → `GetSession(2).SpeakerIds` và `GetSession(1).SpeakerIds` chung Long ⇒ **H1**
  - `AddRoomClashViolation` → D chưa có phòng ⇒ bỏ qua H3.

⇒ có vi phạm, thuật toán bỏ 9h. Thử `CheckPlacement(schedule, 2, 1, 2)` (10h, P102): D `[600, 660)` chỉ
**chạm đầu mút** với A (`Overlaps` = false) ⇒ rỗng ⇒ `schedule.Place(2, 1, 2)`.

### Bước 5 — Đặt B lúc 10h, P101 và C lúc 9h, P102

- B 10h: chồng D nhưng khác diễn giả, khác phòng; tại 600 có 2 phiên ≤ 2 ⇒ hợp lệ ⇒ `Place(3, 1, 1)`.
  (Thuật toán chọn 10h vì `InterestMatrix.Get(B, D) = 0` < `Get(B, A) = 5` — phần chọn mốc làm ở T2.2.)
- C 10h: `FindRunningSessionIds` tại 600 = `[D, B, C]` = 3 > 2 phòng ⇒ **H2**.
- C 9h, P102: chồng A, khác diễn giả và phòng; 40 ≥ 21 ⇒ `Place(4, 0, 2)`.

Lưu ý: T2.2 thật sự chọn **giờ trước** (`Place` không phòng), rồi Hungarian gán phòng bằng `SetRoom`.

### Bước 6 — Chấm điểm: `calculator.Evaluate(schedule)`

1. `BuildPlacedSessions(schedule)`: duyệt `schedule.PlacedSessionIds` = `[1, 2, 3, 4]`,
   `TryGetPlacement` lấy (mốc, phòng), `GetSessionInterval` đổi sang khoảng thời gian.
2. `CalculateConflictCount`: cặp chồng giờ (A, C) → `Get(1, 4) = 35`; (D, B) → `Get(2, 3) = 0` ⇒ **C = 35**.
3. `CalculateLoadStdDev`: mốc 540 có 2 phiên, mốc 600 có 2 phiên ⇒ **B = 0**.
4. `CalculateWastedSeats`: 58 + 26 + 61 + 19 = **S = 164**.
5. Chuẩn hóa: Cn = 35 / 40 (`TotalCoInterest`), Bn = 0 / (2 / 2), Sn = 164 / 284 (`Smax` bước 2)
   ⇒ **f = 0,7 × 0,875 + 0,1 × 0,577 = 0,670**.

`checker.Validate(schedule)` → rỗng: trọng tài xác nhận lịch hợp lệ.

### Bước 7 — Xem trước T2.3

Hoán đổi B ↔ C (`Clone` → `Place` lại → `CheckPlacement` → `Evaluate`): A + B lúc 9h, C + D lúc 10h
⇒ C = `Get(A, B)` = 5 ⇒ **f = 0,145**, tốt hơn nên giữ.

### Ai gọi ai (tóm tắt)

```
Thuật toán (T2.2, T2.3) / test
 ├─ new SchedulingProblem ── Validate*, Timeline, InterestMatrix, BuildValidStartSlots
 ├─ ConstraintChecker.CheckPlacement / Validate
 │    └─ problem.IsValidStartSlot, GetSessionInterval, GetSession, GetRoom, GetSessionSize
 ├─ Schedule.Place / SetRoom / Remove / Clone
 └─ ObjectiveCalculator.Evaluate
      └─ schedule.TryGetPlacement, problem.GetSessionInterval, InterestMatrix.Get, GetRoom
```
