# PROJECT.md — Hệ thống quản lý hội nghị (Khóa luận tốt nghiệp)

> **Đọc file này trước khi làm bất cứ việc gì trong repo.**
> File ghi lại toàn bộ quyết định đã chốt, kèm lý do. Mục 12 liệt kê những hướng
> đã cân nhắc và loại bỏ — đừng đề xuất lại.

---

## 1. Đề tài và định vị

**Tên:** Phân tích, thiết kế và xây dựng hệ thống quản lý hội nghị tích hợp thuật toán xếp lịch và trợ lý AI.

**Định vị (đã chốt với GVHD):** **sản phẩm chính là hệ thống phần mềm.** Bài toán xếp lịch hội nghị đã có nhiều nhóm nghiên cứu và sản phẩm thương mại giải quyết, nên khóa luận **không đặt mục tiêu đề xuất thuật toán tối ưu mới**. Thay vào đó:

- Khảo sát, lựa chọn, tích hợp các thuật toán đã có vào bài toán quản lý hội nghị.
- So sánh thực nghiệm các phương án thuật toán.
- Giải quyết phần mà nghiên cứu lý thuyết không đề cập: đưa bài toán tối ưu vào hệ thống vận hành thật (thu thập dữ liệu qua luồng nghiệp vụ, chạy nền, xếp lại khi biến động, đồng bộ thời gian thực).

**Bối cảnh trao đổi với GVHD:** thầy chỉ ra đề tài phải chọn dứt khoát giữa "làm hệ thống phần mềm dùng thuật toán đã có" và "cải tiến phần tối ưu". Đã chọn hướng thứ nhất. Thầy yêu cầu thêm: bổ sung module dịch vụ đi kèm (đồ ăn, tea break, thiết bị) cho đủ khối lượng, liệt kê chi tiết use case từng actor, và bổ sung tính năng AI cho phù hợp xu hướng.

**Khảo sát hiện trạng (đã kiểm chứng, phải viết đúng trong báo cáo):**

| Nhóm                         | Đại diện                   | Mức hỗ trợ                                                                                         |
|------------------------------|----------------------------|----------------------------------------------------------------------------------------------------|
| Nền tảng quản lý hội nghị    | Whova, Sched, Sessionboard | Chỉ **cảnh báo** xung đột, người dùng tự sửa                                                       |
| Công cụ xếp lịch chuyên biệt | **PragmaPlanner**          | **Đã có** tự sinh lịch bằng bộ giải ràng buộc, có cả tính năng khóa phiên rồi sắp lại phần còn lại |
| Giám sát sức chứa            | EventPilot, EventHex       | Ước lượng nguy cơ quá tải; tài liệu EventPilot ghi rõ đây là *ước lượng, không phải dự đoán*       |

> **CẢNH BÁO KHI VIẾT BÁO CÁO:** KHÔNG được viết "chưa có nền tảng nào tự động xếp lịch" — sai, PragmaPlanner đã làm. Cách phát biểu đúng: các nền tảng quản lý hội nghị *tích hợp* chỉ dừng ở phát hiện xung đột; việc tự sinh lịch nằm ở công cụ chuyên biệt tách rời. Đóng góp là **tích hợp vào một hệ thống hoàn chỉnh**, không phải phát minh ra việc xếp lịch tự động.

---

## 2. Câu chuyện hệ thống (kịch bản thực tế)

Dùng kịch bản này làm chuẩn khi thiết kế màn hình và viết báo cáo.

**Hội nghị "Công nghệ Xanh 2026", 300 người, 2 ngày, 3 phòng (P101 100 chỗ, P102 60 chỗ, P103 40 chỗ).**

**Giai đoạn chuẩn bị.** Chị Lan (ban tổ chức) tạo hội nghị, khai 3 phòng và sức chứa, nhập 12 phiên trình bày kèm thời lượng (có phiên 60 phút, có keynote 90 phút), mời diễn giả. Anh Long nhận lời, khai chỉ tham gia được 9h–11h, và anh phụ trách 2 phiên. Anh tải slide lên kèm phần tóm tắt nội dung.

**Thu thập nguyện vọng.** Người tham dự đăng ký. Trong form đăng ký, ngoài chọn vé, họ **tick các phiên quan tâm** (lúc này lịch chưa xếp, nên họ chọn thuần theo nội dung) và chọn suất ăn mặn/chay, khai dị ứng.

**Xếp lịch.** Hết hạn đăng ký, chị Lan bấm "Chạy xếp lịch". Thuật toán chạy nền, vài giây sau trả về lưới lịch. Chị thấy con số: *"5 lượt xung đột nguyện vọng, 0 vi phạm ràng buộc, 11/12 phiên đã xếp"*. Phiên còn lại báo rõ lý do "diễn giả kín lịch". Chị kéo thả chỉnh vài chỗ — hệ thống tô đỏ ngay nếu vi phạm — rồi bấm Công bố.

**Tổng hợp hậu cần.** Chị bấm "Tổng hợp nhu cầu dịch vụ". Hệ thống tính từ 287 người đã đăng ký: 234 suất mặn, 12 suất mặn không hải sản, 41 suất chay; và từ chương trình đã xếp: ngày 1 có 3 phòng chạy song song nên cần 3 máy chiếu (phòng đã có sẵn 2, chỉ thuê 1) và 6 micro. Gửi đơn cho chị Mai (nhà hàng) và anh Hùng (thiết bị). Hai người xác nhận trên hệ thống.

**Biến động.** Ba ngày trước, một diễn giả báo ốm. Chị Lan chạy xếp lại — thuật toán giữ nguyên tối đa phần không bị ảnh hưởng, chỉ dời những phiên bắt buộc. Hệ thống tự phát hiện đơn thiết bị bị ảnh hưởng và báo anh Hùng: buổi chiều ngày 2 giảm từ 3 xuống 2 máy chiếu.

**Ngày diễn ra, 7h.** Anh Sơn (nhân viên vận hành) mở app trên điện thoại, thấy nhiệm vụ ca sáng. Nhận thiết bị, đếm thực nhận thiếu 1 micro → ghi nhận ngay, ban tổ chức nhận cảnh báo. Lắp xong từng phòng thì bấm xác nhận bàn giao.

**8h.** Bạn Nam quét QR vé từng người ở quầy đón tiếp. Chị Lan nhìn dashboard thấy số check-in nhảy theo thời gian thực.

**9h15.** Phòng 101 đã 96/100 người, phòng 103 mới 34/40. Hệ thống cảnh báo sắp đầy. Ban tổ chức gửi thông báo điều phối tới nhân viên.

**Trong phiên.** Anh Minh không hiểu một slide, gõ câu hỏi vào trợ lý AI — hệ thống tìm trong slide của đúng phiên này và trả lời. Chị Hương gửi câu hỏi công khai, hiện ngay trên màn hình mọi người trong phòng. 47 câu hỏi gửi lên, hệ thống gom thành 8 nhóm: *"18 người cùng hỏi về chi phí triển khai"*. Diễn giả biết nên trả lời gì trước.

**11h30.** Nam chuyển sang màn hình quét voucher, chọn "Trưa ngày 1", quét từng người. Màn hình hiện "SUẤT CHAY" để phát đúng phần; ai đã lấy rồi quét lại thì báo "đã sử dụng 11:42".

**Sau sự kiện.** 240 nhận xét tự do, hệ thống tóm tắt thành các nhóm ý kiến kèm mức độ phổ biến. Đối chiếu dự toán và chi phí thực tế, xuất báo cáo quyết toán.

---

## 3. Tác nhân và danh sách ca sử dụng (57 UC — chốt 2026-09-25)

**Danh sách đầy đủ, xếp theo ưu tiên: `docs/design/use-cases.md`** (mã UC01–UC57, UC01 làm trước nhất). Thay thế danh sách 74 UC cũ.

- 7 tác nhân: **Người dùng** (chung: đăng ký, đăng nhập, hồ sơ) và 6 tác nhân kế thừa — Người tham dự 12, Ban tổ chức 20, Diễn giả 8, Nhà cung cấp 4, Nhân viên vận hành 5, Quản trị viên 5.
- Mức ưu tiên: **P1** lõi đến công bố lịch (UC01–19) · **P2** dịch vụ + vận hành (UC20–32) · **P3** hỏi đáp + AI (UC33–40) · **P4** hỗ trợ (UC41–47) · **P5** mở rộng, cắt đầu tiên (UC48–57).
- CSDL: 19 bảng (P1) → 24 (P2) → 28 (P3) → 31 (P4) → 37 (P5). ERD nộp môn vẽ P1–P3 = 27 bảng nghiệp vụ (bỏ OutboxMessage kỹ thuật). Nguyên tắc: chỉ tách bảng khi có quan hệ 1–N/N–N thật hoặc vòng đời riêng. Chi tiết: `docs/design/erd.md`.
- Mỗi người dùng 1 vai trò; "Diễn giả" không phải vai trò mà là người được gán vào phiên (bảng SessionSpeaker; một phiên có thể nhiều diễn giả). Mỗi hội nghị 1 BTC (OwnerId). Các giới hạn chấp nhận: `erd.md` mục 10.
- Tài liệu nộp môn Dự án công nghệ: vẽ đủ 57 UC trên biểu đồ tổng quát; đặc tả chi tiết + biểu đồ hoạt động/tuần tự cho P1–P3.

## 4. Phân hệ dịch vụ đi kèm

### Hai loại hàng hóa — quy tắc tính khác nhau (dễ làm sai)

|               | **Hàng tiêu hao** (suất ăn, tiệc trà) | **Hàng cho thuê** (máy chiếu, micro, màn LED)                                   |
|---------------|---------------------------------------|---------------------------------------------------------------------------------|
| Đơn vị        | Suất / người                          | Bộ / ngày                                                                       |
| Gắn với       | Khung giờ ăn của cả hội nghị          | Từng phòng, từng khung giờ                                                      |
| Vòng đời      | Giao → dùng hết, không thu hồi        | Giao → lắp → dùng lại nhiều phiên → **thu hồi**                                 |
| Nguồn nhu cầu | Người tham dự chọn khi đăng ký        | Diễn giả yêu cầu + phòng thiếu sẵn                                              |
| Đặc thù       | Phân loại chay/mặn/dị ứng             | Lắp đặt, bàn giao, kiểm kê khi trả                                              |
| **Cách tính** | **Phép cộng**: 287 người → 287 suất   | **Đỉnh đồng thời**: 3 phòng song song → 3 máy chiếu (KHÔNG phải 3×6 phiên = 18) |

Với hàng cho thuê còn phải **trừ thiết bị sẵn có tại phòng** (khai ở UC ban tổ chức số 2) — chỉ thuê phần thiếu.

### Luồng
Khai danh mục → thu thập nhu cầu (người tham dự + diễn giả) → tổng hợp tự động từ chương trình và số đăng ký → gửi đơn → nhà cung cấp xác nhận → vận hành (quét voucher, bàn giao thiết bị) → quyết toán.

**Liên kết với chương trình:** khi lịch xếp lại do biến động, hệ thống tự phát hiện đơn dịch vụ bị ảnh hưởng và thông báo nhà cung cấp. Đây là điểm bảng tính rời rạc không làm được — nhấn mạnh trong báo cáo.

### Voucher
Vé vào cửa dùng một lần khi check-in. Mỗi suất dịch vụ có **QR riêng** (anh Minh 2 ngày, có tiệc tối → 5 voucher). Lý do tách: cần biết ai đã lấy suất nào, ngày nào. Quét hiện tên + loại suất; quét lại báo "đã sử dụng lúc HH:mm"; người không đăng ký báo "không có suất".

---

## 5. Tính năng AI — đã chốt 3 + 1

Tất cả dùng API sẵn có ở mức suy luận, **không huấn luyện mô hình**.

**Làm (3):**
1. **Hỏi đáp theo nội dung phiên** — RAG giới hạn phạm vi theo `SessionId`. Trích text slide + ghép tóm tắt do diễn giả nhập, chia đoạn, sinh embedding, lưu kèm SessionId. Khi hỏi: embedding câu hỏi → cosine similarity **chỉ trong phạm vi phiên đó** → top 3-5 đoạn → LLM. **Dưới ngưỡng tương đồng thì trả lời "không có trong tài liệu phiên"**, không để mô hình suy diễn.
2. **Gom nhóm câu hỏi trùng lặp** — gom cụm theo tương đồng ngữ nghĩa, hiển thị "18 người cùng hỏi về…", xếp theo số người. Tái sử dụng thành phần embedding của (1) nên chi phí thấp.
3. **Tóm tắt phản hồi sau sự kiện** — gom 240 nhận xét tự do thành các nhóm ý kiến kèm mức độ phổ biến.

**Làm nếu còn thời gian (1):**
4. **Ước lượng khi cold start** — hội nghị lần đầu chưa có dữ liệu quan tâm, dùng độ tương đồng chủ đề giữa các phiên thay cho ma trận `W`. Không phải tính năng người dùng, mà là **phương án dự phòng cho thuật toán** — và là câu trả lời sẵn cho câu hỏi chắc chắn bị hỏi khi bảo vệ.

**Đã loại (đừng đề xuất lại):**
- *Trợ lý hậu cần riêng* — trùng kỹ thuật với (1), chỉ khác nguồn tài liệu. Nếu muốn thì gộp thành 2 chế độ của cùng một trợ lý.
- *Gán chủ đề tự động cho phiên* — giá trị thấp, ban tổ chức thường tự phân track theo ý đồ.
- *Gợi ý lịch trình cá nhân* — chồng lấn với chính thuật toán xếp lịch: lịch đã tối ưu giảm trùng giờ rồi thì gợi ý tổ hợp không trùng mất phần lớn ý nghĩa. Giữ lại phần đơn giản là cảnh báo trùng giờ.

**Giới hạn phải nêu trong báo cáo:** slide chỉ có hình ảnh thì trích text gần như không được gì → bắt buộc diễn giả nhập tóm tắt khi tải lên.

---

## 6. Thuật toán xếp lịch

### 6.1. Cấu trúc dữ liệu

```
Phiên        { Id, Tên, ThờiLượng(phút), DiễnGiảId, Chủđề[] }
Phòng        { Id, Tên, SứcChứa }
DiễnGiảRảnh  { DiễnGiảId, Từ, Đến }
Quan tâm     danh sách cặp (NgườiId, PhiênId)  — lưu thưa
```

**Tập mốc thời gian `T`:** sinh từ khoảng tổ chức với bước nhảy cố định (9h–12h, bước 15 phút → `[9:00, 9:15, ..., 11:45]`).

**Ma trận đồng quan tâm `W`** — tính **một lần** trước khi chạy: `W[i][j]` = số người quan tâm cả phiên *i* và *j*. Then chốt để tính hàm mục tiêu nhanh (tra bảng thay vì duyệt toàn bộ người tham dự). 100 phiên → ma trận 10.000 ô, rất nhẹ.

**Trạng thái lời giải:** `PhiênId → (GiờBắtĐầu, PhòngId)`, ban đầu rỗng, điền dần.

### 6.2. Ràng buộc cứng

- **H1** — Một diễn giả không trình bày hai phiên chồng lấn thời gian.
- **H2** — Số phiên song song tại mọi thời điểm ≤ số phòng khả dụng.
- **H3** — Một phòng phục vụ tối đa một phiên tại một thời điểm.
- **H4** — Sức chứa phòng ≥ quy mô dự kiến của phiên.
- **H5** — Phiên nằm trong thời gian tổ chức và thời gian diễn giả tham gia được.

> **H2 phải kiểm tra NGAY trong lúc gán thời gian**, không tách thành bước riêng sau đó. Nếu tách, thuật toán có thể xếp 5 phiên song song khi chỉ có 3 phòng → lịch bế tắc, phải làm lại từ đầu.

### 6.3. Hàm mục tiêu

> **f = w₁·C + w₂·B + w₃·S** (càng nhỏ càng tốt)

- **C — Số lượt xung đột nguyện vọng.** Tổng `W[i][j]` trên mọi cặp phiên xếp chồng lấn thời gian. *Ví dụ: 40 người quan tâm cả A và B, xếp song song → C tăng 40.* Đây là đại lượng đo trực tiếp thiệt hại người tham dự, và là **khái niệm trung tâm của đề tài**.
- **B — Độ lệch tải.** Độ lệch chuẩn số phiên mỗi khung giờ. *Chia 4–2 lệch hơn chia 3–3; phân bố lệch gây quá tải hạ tầng khung này, bỏ trống phòng khung kia.*
- **S — Sức chứa lãng phí.** Tổng (sức chứa phòng − quy mô phiên). *Phiên 20 người vào hội trường 200 chỗ → S tăng 180.*

**Chuẩn hóa bắt buộc:** ba đại lượng khác đơn vị (lượt người / số phiên / số ghế), thang đo chênh lệch lớn → **chuẩn hóa về 0–1 trước khi nhân trọng số**, với w₁+w₂+w₃ = 1. Không chuẩn hóa thì S lấn át và thuật toán tối ưu sai mục tiêu. Mặc định đề xuất: **w₁=0,7 / w₂=0,2 / w₃=0,1**.

### 6.4. Thuật toán hai giai đoạn

**Giai đoạn 1 — tham lam:**
```
1. Tính độ ràng buộc mỗi phiên:
     (số mốc diễn giả rảnh) − (số phiên diễn giả đó phải trình bày)
   Sắp TĂNG dần (nhỏ = chặt nhất, xếp trước)
2. Với mỗi phiên P:
   a. Duyệt mốc t thuộc T
   b. Loại sớm nếu diễn giả bận hoặc không còn phòng đủ sức chứa
   c. Trong các t hợp lệ, chọn t cho f nhỏ nhất
      (f chỉ tính trên các phiên ĐÃ xếp — bản chất tham lam)
   d. Gán phòng tối ưu bằng thuật toán Hungarian
   e. Không có t hợp lệ → "chưa xếp được" kèm lý do cụ thể
```

**Giai đoạn 2 — cải thiện cục bộ có định hướng.** Thử **dời** một phiên hoặc **hoán đổi** hai phiên; chỉ chấp nhận khi vẫn thỏa ràng buộc cứng **và** giảm `f`. Ba quy tắc thu hẹp:

- **Q1** — Tính mức đóng góp vào `f` của từng phiên (tổng `W[X][Y]` với mọi Y đang chồng giờ với X); chỉ biến đổi phiên đóng góp cao nhất. *Phiên đóng góp 0 thì biến đổi cũng không thể làm f giảm.*
- **Q2** — Khi dời, chỉ xét mốc diễn giả rảnh và còn phòng; mốc khác loại ngay không cần tính `f`.
- **Q3** — Chỉ hoán đổi cặp cùng nằm trong nhóm gây xung đột.

**Thoát cực trị cục bộ.** Q1 có điểm mù: đôi khi phải dời một phiên "vô tội" đi nhường chỗ, bước đó tạm làm `f` tăng nên bị bỏ qua. Hai kỹ thuật khắc phục, cài cả hai để so sánh: **khởi động lại nhiều lần** (chạy lại giai đoạn 1 với thứ tự khác, giữ kết quả tốt nhất) và **mô phỏng luyện kim** (chấp nhận có xác suất biến đổi làm `f` tăng nhẹ, xác suất giảm dần).

**Gán phòng — thuật toán Hungarian.** Gán *k* phiên vào *k* phòng trong một khung giờ là **bài toán phân công**, giải tối ưu bằng Hungarian, O(k³) với *k* = số phòng nên rất rẻ. Ma trận chi phí = sức chứa lãng phí. Nâng từ xấp xỉ lên **tối ưu có chứng minh**.

**Xếp lịch lại hạn chế xáo trộn.** Thêm vào `f` thành phần phạt cho mỗi phiên bị đổi vị trí so với lịch đã công bố. Chỉ số đo: số phiên bị thay đổi vị trí.

**Giới hạn phải nêu rõ:** heuristic, **không đảm bảo** tìm được lời giải khi lời giải tồn tại, không đảm bảo tối ưu toàn cục. Khi bế tắc báo rõ phiên nào không xếp được và vì sao.

**Độ phức tạp:** O(n × m × r), n = số phiên, m = số mốc, r = số phòng.

### 6.5. Ví dụ chạy tay (dùng trong báo cáo)

2 khung giờ (9h, 10h); P101 100 chỗ, P102 40 chỗ; 4 phiên: A và D của Long, B của Hoa, C của Tùng; Long chỉ rảnh 9h–11h. `W[A][C]=35`, `W[A][B]=5`, còn lại 0. Quy mô: A=42, B=39, C=21, D=14 *(A cần ≥42 nên chỉ vừa P101)*. Độ ràng buộc: Long 2 mốc/2 phiên → 0 (chặt nhất). Thứ tự xếp: **A, D, B, C**.

| Bước | Phiên | Xét mốc                                        | Chọn      |
|------|-------|------------------------------------------------|-----------|
| 1    | A     | 9h: f=0 · 10h: f=0 → hòa, ưu tiên cân bằng tải | 9h, P101  |
| 2    | D     | 9h: Long bận (H1) · 10h: f=0                   | 10h, P102 |
| 3    | B     | 9h: chồng A → +5 · 10h: chồng D → +0           | 10h, P101 |
| 4    | C     | 9h: chồng A → **+35** · 10h: hết phòng (H2)    | 9h, P102  |

→ **f = 35.** Điểm yếu tham lam lộ ra: bước 3 chọn 10h vì lúc đó rẻ hơn, nhưng chiếm mất chỗ khiến C buộc đứng cạnh A.

Giai đoạn 2: đóng góp A=35, C=35, B=0, D=0 → chỉ thử A và C. Dời C sang 10h → hết phòng, loại (Q2). **Hoán đổi C↔B** → kiểm tra: B(39)→P102(40) vừa, Hoa rảnh 9h, C(21)→P101 → hợp lệ, **f = 5**, chấp nhận. Hoán đổi A↔D → A(42)→P102(40) vi phạm H4, bác bỏ. → **Kết quả: 9h có A+B, 10h có C+D, f = 5.**

---

## 7. Phương pháp đánh giá

**Dữ liệu:** sinh mô phỏng 3 quy mô (30 / 60 / 100 phiên); ma trận quan tâm sinh theo phân phối **có cụm chủ đề** để phản ánh thực tế. *Nêu rõ hạn chế: dữ liệu mô phỏng, chỉ có giá trị so sánh tương đối.*

|        | Phương pháp                                                         |
|--------|---------------------------------------------------------------------|
| **M1** | Xếp thủ công mô phỏng: theo thứ tự nhập, gán vào mốc trống đầu tiên |
| **M2** | Chỉ đảm bảo ràng buộc cứng, không dùng dữ liệu nguyện vọng          |
| **M3** | Giai đoạn 1 + cải thiện cục bộ                                      |
| **M4** | M3 + mô phỏng luyện kim                                             |

**11 chỉ số:** (1) số lượt xung đột nguyện vọng *(chính)*; (2) tỷ lệ người không mất phiên nào quan tâm; (3) vi phạm ràng buộc cứng còn sót *(kỳ vọng 0)*; (4) tỷ lệ phiên xếp thành công; (5) độ lệch tải; (6) tỷ lệ lấp đầy phòng; (7) thời gian chạy; (8) số phiên xáo trộn khi xếp lại; (9) sai số ước lượng quy mô phiên so với check-in thực tế; (10) lãng phí sức chứa Hungarian vs tham lam; (11) tương quan giữa tương đồng chủ đề và ma trận quan tâm thực tế *(kiểm chứng giả định cold start)*.

**Phân tích độ nhạy:** chạy lại với vài bộ trọng số (w₁,w₂,w₃) khác nhau.

**Giả thuyết:** M3, M4 giảm đáng kể chỉ số (1) so với M1, M2, trong khi giữ (3) = 0.

> Nếu kết quả không như giả thuyết: **trình bày trung thực kèm phân tích nguyên nhân** — kết quả âm tính có phân tích vẫn là kết quả khoa học hợp lệ.

---

## 8. Kiến trúc và công nghệ

### Backend
ASP.NET Core Web API · **Clean Architecture 4 lớp** (Domain / Application / Infrastructure / API) · **CQRS với MediatR** (Command, Query, Validator, Pipeline Behavior) · FluentValidation · EF Core · **SQL Server** (local: `127.0.0.1,1433`).

**Xác thực & phân quyền:** JWT (access + refresh token); **phân quyền theo permission** — mỗi UC gắn một quyền cụ thể (`Conference.Create`, `Schedule.Run`, `Service.Order`…), vai trò là tập hợp quyền, kiểm tra qua Authorization Handler tùy biến. Với 6 tác nhân phạm vi rất khác nhau, cơ chế này cần thiết hơn phân quyền theo vai trò đơn thuần.

| Thành phần         | Vai trò                                                                                                            | Ghi chú                                                                  |
|--------------------|--------------------------------------------------------------------------------------------------------------------|--------------------------------------------------------------------------|
| **SignalR**        | Q&A/poll thời gian thực theo từng phiên (**mỗi phiên là một group riêng**); theo dõi check-in; thông báo điều phối | Group tên theo SessionId — người phòng 101 không thấy câu hỏi phòng 102  |
| **Hangfire**       | Chạy thuật toán xếp lịch nền; tổng hợp nhu cầu dịch vụ; nhắc lịch; xuất báo cáo lớn                                | Việc **theo lịch / tốn thời gian**                                       |
| **RabbitMQ**       | Sự kiện `RegistrationApproved`, `ScheduleChanged` → Email / Notification / Analytics xử lý độc lập                 | Việc **phản ứng theo sự kiện**; email lỗi không làm hỏng giao dịch chính |
| **Redis**          | Cache chương trình đã công bố, danh mục dịch vụ; **backplane cho SignalR** khi chạy nhiều instance                 |                                                                          |
| **MinIO**          | Slide, tài liệu, ảnh, tệp báo cáo                                                                                  | DB chỉ lưu đường dẫn                                                     |
| **Serilog + Seq**  | Log có cấu trúc tập trung từ API, job nền, consumer                                                                |                                                                          |
| **Docker Compose** | Dựng SQL Server, Redis, RabbitMQ, MinIO, Seq bằng một lệnh                                                         | Làm ngay tuần 1                                                          |

> **Câu hỏi chắc chắn bị hỏi khi bảo vệ:** "Vì sao cần cả Hangfire lẫn RabbitMQ?" → Hangfire cho việc **theo lịch/định kỳ**; RabbitMQ cho việc **phản ứng theo sự kiện**, phát tán tới nhiều consumer độc lập. Hai mục đích khác nhau.

**Phong cách thiết kế (chốt 2026-09-25):**
- **DDD nhẹ**: aggregate + phương thức nghiệp vụ cho lõi (`ScheduleVersion`, `Registration`, `ServiceOrder`, `Conference`); value object `TimeRange` (kiểm tra chồng lấn — lõi của H1, H3, H5); domain event cho thay đổi có ý nghĩa nghiệp vụ. Phần CRUD danh mục giữ entity đơn giản. Không tuyên bố DDD đầy đủ (không bounded context).
- **Event-driven cho hiệu ứng phụ**: `RegistrationApproved`, `ScheduleChanged` → MassTransit 8.x + RabbitMQ + **EF Core Transactional Outbox/Inbox của MassTransit** (thay bảng OutboxMessage tự viết). Luồng chính vẫn request/response + CQRS. Không phải event sourcing.
- **Nền tảng**: .NET 10 LTS (.NET 8 hết hỗ trợ 10/11/2026). Mẫu mã tham khảo từ khung TD.Microservice.ServiceBase — giữ/bỏ gì và quy tắc chất lượng: `docs/design/base-reference.md`.

**Tự viết hay dùng thư viện (chốt 2026-09-28):** mục tiêu là hiểu sâu để tự gõ lại và giải thích khi bảo vệ.
- **Tự viết** khi code ngắn và giúp hiểu cơ chế: xử lý lỗi (middleware `try/catch` → ProblemDetails), validation pipeline, phân quyền theo permission, thuật toán xếp lịch.
- **Dùng thư viện** khi tự viết vừa dài vừa dễ sai/thiếu an toàn: ký/xác thực JWT, băm mật khẩu, EF Core, MassTransit/RabbitMQ, SignalR, Serilog.
- Tính năng có sẵn của framework nhưng che mất cơ chế (vd `IExceptionHandler`) → ưu tiên tự viết nếu ngắn tương đương.

**Kiểm thử:** unit test tầng Application (Handler) · **test riêng cho thuật toán xếp lịch** (xác minh không vi phạm ràng buộc cứng trên nhiều bộ dữ liệu) · integration test cho API chính.

### Frontend
React + TypeScript + **Ant Design v5** + TanStack Query + SignalR client + Recharts.

**Không thêm Tailwind/Bootstrap** — antd đã đủ. Tailwind chung với antd phải tắt preflight vì reset CSS làm vỡ style antd, không đáng.

---

## 9. Thiết kế giao diện

### Ba shell — KHÔNG dùng chung một layout

| Shell                             | Cho ai                                   | Hình dạng                                                                                                                  |
|-----------------------------------|------------------------------------------|----------------------------------------------------------------------------------------------------------------------------|
| **A — Bảng điều khiển**           | Ban tổ chức, Quản trị viên, Nhà cung cấp | Sidebar trái (thu gọn được) + header + `<Outlet/>`. Màn nhỏ: sidebar thành Drawer                                          |
| **B — Công khai / người tham dự** | Khách, Người tham dự                     | Header ngang (logo, tìm kiếm, đăng nhập). Điện thoại: thêm tab bar dưới đáy (Chương trình · Lịch của tôi · Vé · Tài khoản) |
| **C — Vận hành tại chỗ**          | Nhân viên vận hành                       | Tối giản, gần như không điều hướng. Camera quét chiếm gần hết màn hình, vài nút to ở dưới                                  |

```
/                  → ShellB   (trang chủ, danh sách hội nghị)
/conferences/:id   → ShellB   (chương trình, đăng ký)
/me/*              → ShellB   (lịch của tôi, vé)
/manage/*          → ShellA   (hội nghị, phiên, xếp lịch, dịch vụ, báo cáo)
/admin/*           → ShellA   (người dùng, duyệt, cấu hình)
/supplier/*        → ShellA   (đơn hàng, danh mục)
/onsite/*          → ShellC   (quét vé, quét voucher, báo sự cố)
```

### Bảng màu

**Màu chính: xanh dầu (petrol) `#0E5A62`.** Lý do: không dùng xanh dương SaaS mặc định; màn hình lưới lịch đã có 5 màu track rực rỡ nên màu thương hiệu phải trầm để không đánh nhau.

| Vai trò      | Sáng      | Tối       |
|--------------|-----------|-----------|
| Màu chính    | `#0E5A62` | `#4FB3BF` |
| Nền          | `#F7F8F7` | `#14181A` |
| Bề mặt       | `#FFFFFF` | `#1D2326` |
| Chữ          | `#17232B` | `#E6EAEB` |
| Chữ phụ      | `#5E6E75` | `#9AA8AD` |
| Đường kẻ     | `#D8DEDF` | `#333C40` |
| Cảnh báo/chờ | `#B26B00` | `#E0A33D` |
| Lỗi/xung đột | `#B3261E` | `#F2887F` |
| Thành công   | `#2E6B45` | `#68B98A` |

**Màu track** (lưới lịch), chọn để phân biệt được cả khi in đen trắng và với người mù màu — sáng/tối: `#1F6F8B`/`#6FB6D0`, `#7A4E9E`/`#B492D8`, `#A25A2A`/`#D69A6B`, `#2F7050`/`#6FB894`, `#8C3B54`/`#D08AA0`.

**Chữ: Be Vietnam Pro** — phông do người Việt thiết kế, dấu đặt chuẩn (nhiều phông quốc tế bị dấu chồng lên chữ hoa, xấu khi in báo cáo). Dùng **số dạng bảng** (`font-variant-numeric: tabular-nums`) để cột giờ trong lưới lịch thẳng hàng.

### Token — không gõ số pixel trực tiếp

Dùng **design token của antd v5**, khai một chỗ:

```tsx
<ConfigProvider
  theme={{
    algorithm: dark ? theme.darkAlgorithm : theme.defaultAlgorithm,
    cssVar: true,
    token: {
      colorPrimary: '#0E5A62',
      fontFamily: "'Be Vietnam Pro', system-ui, sans-serif",
      fontSize: 15,
      borderRadius: 8,
    },
  }}
  locale={lang === 'vi' ? viVN : enUS}
>
```

**Quy tắc bắt buộc:** cỡ chữ lấy từ `fontSize`, `fontSizeSM/LG`, `fontSizeHeading1..5`; khoảng cách lấy từ `padding`, `paddingXS/SM/LG/XL`, `margin*`. **Không bao giờ gõ số pixel trực tiếp trong component.** Màu track và màu trạng thái riêng khai thêm ở `:root` dạng biến CSS, cũng hai bộ sáng/tối.

**Nền tối:** đổi `algorithm` sang `darkAlgorithm` là antd tự sinh lại bảng màu — không viết bộ CSS thứ hai. Mặc định theo cài đặt hệ điều hành, cho người dùng ghi đè, lưu lựa chọn.

### Song ngữ (vi/en)
Hai lớp: **`ConfigProvider locale`** lo chữ có sẵn của antd (OK/Cancel, lịch, "Không có dữ liệu"); **`react-i18next`** lo chữ của mình, tách ra `vi.json` / `en.json`, **không hardcode chữ trong component**.

> **Điểm quan trọng:** dữ liệu người dùng nhập (tên phiên, mô tả) không dịch tự động được → bảng `Sessions` cần `title_vi` và `title_en`. Ảnh hưởng tới thiết kế CSDL, phải nói trong báo cáo.

### Màn hình đặc trưng: lưới xếp lịch
Giờ theo hàng × phòng theo cột, mỗi track một màu, kéo thả chỉnh tay và hệ thống kiểm tra ràng buộc tức thời (viền đỏ nếu vi phạm). Dưới lưới là dải số liệu: lượt xung đột nguyện vọng, vi phạm ràng buộc, phiên đã xếp, tỷ lệ lấp đầy, thời gian chạy — **chính là các chỉ số của chương đánh giá, hiển thị luôn trên giao diện**.

Bản mẫu giao diện đã dựng (có nút đổi nền tối và ngôn ngữ): `docs/ui-prototype.html`

---

## 10. Kế hoạch 15 tuần

> **Đã thay thế (2026-09-25):** 15 tuần hiện tại là môn Dự án công nghệ (nộp tài liệu + slide); code để giai đoạn khóa luận. Kế hoạch hiện hành: `plan/README.md` mục 3. Bảng dưới giữ để tham khảo.

| Tuần | Nội dung                                                                                                               |
|------|------------------------------------------------------------------------------------------------------------------------|
| 1    | Chốt phạm vi, đặc tả UC, **vẽ ERD**, dựng solution + Docker Compose. Vẽ màn hình xương sống. Viết chương 1–2 báo cáo   |
| 2–3  | Auth + phân quyền + quản lý người dùng (đủ 6 actor). **Song song: bản nháp thuật toán chạy console để thăm dò rủi ro** |
| 4–5  | Quản lý hội nghị / phòng / phiên / diễn giả — biểu đồ → giao diện → code. Xong là viết luôn báo cáo phần này           |
| 6–7  | Thuật toán xếp lịch bản chính: hai giai đoạn, Hungarian, chạy nền, lưới lịch kéo thả                                   |
| 8    | Đăng ký, thu thập nguyện vọng, vé QR, check-in, kiến trúc hướng sự kiện                                                |
| 9    | Phân hệ dịch vụ + nhà cung cấp                                                                                         |
| 10   | Phân hệ nhân viên vận hành (Shell C, quét mã)                                                                          |
| 11   | Q&A/poll thời gian thực + gom nhóm câu hỏi (AI-2)                                                                      |
| 12   | Trợ lý hỏi đáp theo phiên (AI-1) + tóm tắt phản hồi (AI-3)                                                             |
| 13   | Xếp lịch lại hạn chế xáo trộn + thống kê, dashboard, báo cáo                                                           |
| 14   | **Thực nghiệm đánh giá** (M1–M4, phân tích độ nhạy) + kiểm thử tổng thể                                                |
| 15   | Hoàn thiện báo cáo, rà biểu đồ, chuẩn bị bảo vệ                                                                        |

**Nguyên tắc:** mỗi cụm chức năng đi qua 3 bước liền nhau — **vẽ biểu đồ → vẽ giao diện → code**. Xong cụm nào thì phần báo cáo cụm đó xong luôn. **Không dồn báo cáo tới cuối** — các biểu đồ chính là tài liệu thiết kế, phải nghĩ ra trước khi code, không phải vẽ lại sau.

**Thứ tự cắt giảm nếu chậm:** phân hệ nhà cung cấp (chuyển thành quản lý đơn giản trong phân hệ ban tổ chức) → một phần tính năng AI → RabbitMQ (Hangfire cáng đáng được phần lớn). **Giữ bằng mọi giá:** hệ thống quản lý chương trình + đăng ký, thuật toán xếp lịch, phân hệ dịch vụ, chương đánh giá.

---

## 10b. Nguyên tắc làm tăng dần (chốt 2026-09-25)

Làm đến nhóm chức năng nào thì tài liệu (đặc tả, biểu đồ), thiết kế giao diện, bảng/cột CSDL (migration theo task) và hạ tầng (đăng ký service, container) **tới đúng nhóm đó**. `erd.md`, `use-cases.md` là bản đồ tổng. Chi tiết: `plan/README.md` mục 3.

## 11. Công cụ đã chốt

| Việc             | Công cụ                                                                                                     | Trạng thái                                    |
|------------------|-------------------------------------------------------------------------------------------------------------|-----------------------------------------------|
| Biểu đồ UML, ERD | **PlantUML** (đã test, tiếng Việt hiển thị đúng)                                                            | Sinh file `.puml` → xuất PNG/SVG chèn báo cáo |
| Báo cáo          | **Overleaf** (LaTeX)                                                                                        | Tài khoản đã kết nối; tạo project mới được    |
| Code             | **Codex MCP**                                                                                               | Giao phần triển khai đáng kể cho Codex        |
| Giao diện        | Thiết kế từng nhóm bằng mockup HTML `docs/ui/<nhóm>.html` (cùng token màu/chữ với antd), rồi mới code React | Figma MCP chỉ đọc được thiết kế, không vẽ hộ  |
| CSDL             | SQL Server local `127.0.0.1,1433`                                                                           |                                               |

**Hình thức báo cáo** (theo 3 khóa luận khóa trước cùng GVHD): Times New Roman, hình đánh số dạng "Hình 3.36", chú thích in nghiêng **dưới** hình, sau mỗi hình có đoạn diễn giải. Độ dài tham chiếu: 82–107 trang.

---

## 12. Những hướng đã cân nhắc và LOẠI (đừng đề xuất lại)

**Đề tài khác đã loại:** dashboard kéo-thả cấu hình (người dùng không được tự cấu hình dashboard — chỉ xem + lọc); phòng khám / đặt lịch khám; phòng gym; đặt sân thể thao; thuê trọ sinh viên; hiến máu; thú y; gây quỹ; homestay; hội nghị networking thuần; phân bổ ký túc xá; xếp ca làm việc; đặt phòng lab; đăng ký học phần; ghép giảng viên hướng dẫn; phát hiện đạo văn; quản lý dự án CPM; ngân hàng máu; công cụ tìm kiếm nội bộ; tối ưu tuyến xe đưa đón; giao hàng; đi chung xe; **xếp phòng thi** (đã kiểm chứng: rất nhiều bài báo + sản phẩm thương mại + SMAS ở VN đã có, là đề tài sinh viên kinh điển).

**Trong phạm vi đề tài hiện tại, đã loại:**
- *Mô hình tô màu đồ thị (graph coloring)* cho xếp lịch — chỉ đúng khi mọi phiên dài bằng nhau và khung giờ rời rạc. Thời lượng khác nhau thì mô hình sụp đổ → đã chuyển sang **interval scheduling**. **Không dùng đồng thời hai mô hình.**
- *Tuyên bố đạt "equitable coloring"* — thuật toán tham lam không đảm bảo tính chất này theo định nghĩa toán học. Chỉ gọi là **heuristic cân bằng tải**.
- *Networking matching / gợi ý kết nối người tham dự* — thị trường (Whova, Brella, Bizzabo) đã làm tốt hơn từ lâu, không tính là đóng góp.
- *Ba tính năng AI* ở mục 5.
- *Vector database riêng* (Qdrant/pgvector) — quy mô vài trăm đoạn/hội nghị, lưu vector trong SQL Server và so sánh bằng vòng lặp C# là đủ; giữ trọng tâm ở web/backend.
- *Figma để thiết kế* — connector chỉ đọc được thiết kế và sinh code, không vẽ hộ được.
- *Tailwind/Bootstrap* — xung đột với antd.

---

## 13. Việc cần làm tiếp

> **Từ 2026-09-25:** danh sách task chi tiết, quy trình Claude → Codex → Huy và trạng thái từng task nằm ở `plan/README.md`. Mục này chỉ giữ các việc tổng.

0. ~~Gửi GVHD danh sách UC~~ — đã gửi 2026-09-25, chờ phản hồi.

1. Chốt phạm vi theo phản hồi của GVHD về 74 UC.
2. **Vẽ ERD** — quyết định mọi thứ phía sau.
3. Dựng `docker-compose.yml` + khung solution Clean Architecture.
4. Bản nháp thuật toán chạy console (thăm dò rủi ro sớm).
5. Hỏi GVHD: trường có mẫu LaTeX cho khóa luận không?

## 14. Câu hỏi còn mở

- Mẫu báo cáo của trường (LaTeX hay Word)?
- Có cần nộp bản thiết kế giao diện riêng, hay ảnh chụp sản phẩm là đủ?
- Hội nghị có tính phí vé thật không, hay chỉ mô phỏng thanh toán? *(hiện giả định: mô phỏng)*
