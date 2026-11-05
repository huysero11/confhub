# G3 — Hội nghị, phòng, phiên, diễn giả: đặc tả UC03–UC08 (D8)

> Chốt thiết kế nhóm G3 trước khi code T3.1–T3.4.
> Nguồn: `docs/design/use-cases.md`, `docs/design/erd.md` mục 2–3, `docs/design/scenario.md` B2–B7,
> `PROJECT.md` mục 6.6 (mô hình xếp lịch dùng dữ liệu của nhóm này).
> Biểu đồ: `docs/diagrams/g3-*.puml` · Giao diện: `docs/ui/g3-conference.html`.

## 1. Phạm vi

| UC   | Tên                                | Tác nhân    | Task       |
|------|------------------------------------|-------------|------------|
| UC03 | Tạo và cấu hình hội nghị           | Ban tổ chức | T3.1, T3.3 |
| UC04 | Quản lý phòng                      | Ban tổ chức | T3.1, T3.3 |
| UC05 | Mời và quản lý diễn giả            | Ban tổ chức | T3.2, T3.3 |
| UC06 | Phản hồi lời mời trình bày         | Diễn giả    | T3.2, T3.4 |
| UC07 | Khai báo thời gian có thể tham gia | Diễn giả    | T3.2, T3.4 |
| UC08 | Quản lý phiên                      | Ban tổ chức | T3.1, T3.2 |

**Luồng con:**
- UC03: tạo · sửa thông tin · xóa (chỉ `Draft`) · quản lý ngày · quản lý khung nghỉ.
- UC04: thêm · sửa · xóa phòng.
- UC05: mời vào phiên · mời lại · gỡ khỏi phiên · xem diễn giả của hội nghị · sửa giờ rảnh thay diễn giả.
- UC06: nhận lời · từ chối · rút lui.
- UC07: khai giờ rảnh theo từng ngày.
- UC08: quản lý track · thêm · sửa · xóa phiên.

- Track (nhóm chủ đề) thuộc UC08 vì chỉ dùng để phân loại phiên, nhưng làm ở T3.1 (CRUD đơn giản, phiên cần có trước).
- "Diễn giả" không phải vai trò: bất kỳ tài khoản `Active` nào được BTC mời vào phiên (bảng SessionSpeaker).
  Trang của diễn giả nằm ở Shell B (`/me/speaking`), ai đăng nhập cũng vào được.
- **Ngoài G3:**
  - Mở / đóng đăng ký, loại vé, hội nghị công khai (UC09–UC14, G4).
  - Ràng buộc sửa / xóa khi đã có đăng ký hoặc lịch (G4, G5 bổ sung — mục 14).
  - Thiết bị sẵn có tại phòng (RoomEquipment), danh mục dịch vụ (ServiceItem), suất phục vụ trong khung nghỉ
    (`BreakSlot.ServiceItemId`, `IsOptional`) → **G6 (T6.2)**. G3–G5 chỉ cần tên phòng + sức chứa.
  - Slide và tóm tắt của diễn giả (`SessionSpeaker.SlideKey`, `SlideSummary`) → G8 (T8.1).

## 2. Dữ liệu

8 bảng (`erd.md` mục 2–3), tạo ở T3.1 / T3.2. Mọi bảng có `Id` (Guid) và `CreatedAt`.

| Bảng                | Cột dùng ở G3                                              | Task |
|---------------------|------------------------------------------------------------|------|
| Conference          | Name_Vi/En, Description_Vi/En, Venue, RegistrationCloseAt, | T3.1 |
|                     | RequireApproval, Status, OwnerId                           |      |
| ConferenceDay       | ConferenceId, Date, StartTime, EndTime                     | T3.1 |
| BreakSlot           | ConferenceDayId, Name, StartTime, EndTime                  | T3.1 |
| Room                | ConferenceId, Name, Capacity                               | T3.1 |
| Track               | ConferenceId, Name_Vi/En                                   | T3.1 |
| Session             | ConferenceId, TrackId (null), Title_Vi/En, Abstract_Vi/En, | T3.2 |
|                     | DurationMinutes, ExpectedSize (null)                       |      |
| SessionSpeaker      | SessionId, SpeakerId, InvitationStatus                     | T3.2 |
| SpeakerAvailability | ConferenceDayId, SpeakerId, StartTime, EndTime             | T3.2 |

**Thay đổi so với `erd.md` (Huy chốt 2026-11-05):**
- Cột `_En` cho phép null: bắt buộc tiếng Việt, tiếng Anh tùy chọn; giao diện EN để trống thì hiện bản tiếng Việt.
- `Session.TrackId` cho phép null: phiên không track hiện màu xám "Chưa phân track".
- ServiceItem, RoomEquipment và 2 cột dịch vụ của BreakSlot dời sang G6 → P1 còn 17 bảng.

**Giờ và múi giờ:**
- `ConferenceDay.Date` + `StartTime` / `EndTime`, `BreakSlot`, `SpeakerAvailability` lưu **giờ địa phương**
  của nơi tổ chức (kiểu `date` / `time`), không đổi múi giờ.
- `RegistrationCloseAt` là thời điểm tuyệt đối, lưu **UTC**, giao diện hiện theo giờ địa phương.
- Cả hệ thống dùng **một múi giờ** cấu hình (`Asia/Ho_Chi_Minh`) để đổi giữa hai loại — giới hạn chấp nhận (mục 15).

**Trạng thái hội nghị** (`g3-trang-thai-hoi-nghi.puml`) — G3 chỉ tạo `Draft`; chuyển tiếp do nhóm sau làm:

| Từ                   | Sự kiện                        | Sang                 | Nhóm |
|----------------------|--------------------------------|----------------------|------|
| (mới)                | Tạo hội nghị (UC03)            | `Draft`              | G3   |
| `Draft`              | Mở đăng ký (cần ≥ 1 loại vé)   | `RegistrationOpen`   | G4   |
| `RegistrationOpen`   | Tới hạn đăng ký / BTC đóng sớm | `RegistrationClosed` | G4   |
| `RegistrationClosed` | Công bố chương trình (UC17)    | `Scheduled`          | G5   |
| `Scheduled`          | Qua ngày cuối của hội nghị     | `Finished`           | G5   |
| `Draft`              | Xóa (UC03)                     | (xóa hẳn)            | G3   |

**Trạng thái lời mời** (`SessionSpeaker.InvitationStatus`, `g3-trang-thai-loi-moi.puml`):

| Từ         | Sự kiện              | Sang       | Ai làm   |
|------------|----------------------|------------|----------|
| (mới)      | Mời vào phiên (UC05) | `Pending`  | BTC      |
| `Pending`  | Nhận lời (UC06)      | `Accepted` | Diễn giả |
| `Pending`  | Từ chối (UC06)       | `Declined` | Diễn giả |
| `Accepted` | Rút lui (UC06)       | `Declined` | Diễn giả |
| `Declined` | Mời lại (UC05)       | `Pending`  | BTC      |
| bất kỳ     | Gỡ khỏi phiên (UC05) | (xóa dòng) | BTC      |

- Đã từ chối thì diễn giả **không tự nhận lại** được (BTC có thể đã mời người khác) — phải chờ BTC mời lại.
- Thuật toán (H1, H5) chỉ xét diễn giả `Accepted` (PROJECT.md 6.6).

## 3. Quy tắc nghiệp vụ

**Chung**
- **BR16 — Quyền và chủ sở hữu:** mọi thao tác quản lý cần quyền của UC (mục 11) **và** hội nghị phải do người
  đang đăng nhập làm BTC (`OwnerId`). Hội nghị của BTC khác → 404 `NotFound` (không lộ là có tồn tại).
- **BR17 — Song ngữ:** trường `_Vi` bắt buộc, `_En` tùy chọn, cùng giới hạn độ dài; chuỗi rỗng / toàn khoảng trắng
  coi như không nhập. Mọi chuỗi được cắt khoảng trắng hai đầu trước khi lưu.
- **BR18 — Bước 15 phút:** mọi giờ (ngày, khung nghỉ, giờ rảnh) và thời lượng phiên là bội số của 15 phút
  — khớp tập mốc T của thuật toán (PROJECT.md 6.1).
- **BR19 — Hội nghị đã kết thúc** (`Finished`) chỉ còn xem, mọi thao tác sửa → 422 `ConferenceFinished`.

**Hội nghị, ngày, khung nghỉ (UC03)**
- **BR20 — Thông tin hội nghị:** tên ≤ 200 ký tự; mô tả ≤ 4000; địa điểm bắt buộc ≤ 300; "cần duyệt đăng ký"
  mặc định bật.
- **BR21 — Ngày tổ chức:** mỗi hội nghị có **ít nhất 1 ngày** (không xóa được ngày cuối cùng còn lại), tối đa 14
  ngày (`TooManyDays`); không trùng ngày (`DayDateTaken`); ngày thêm / đổi không ở quá khứ theo giờ nơi tổ chức
  (`DayInPast`). Giờ bắt đầu < giờ kết thúc, dài ít nhất 60 phút.
  Các ngày không cần liên tiếp.
- **BR22 — Hạn đăng ký:** bắt buộc; khi đặt / đổi phải ở tương lai (`RegistrationCloseInPast`);
  luôn **≤ giờ bắt đầu của ngày đầu tiên**.
  Thêm / sửa ngày làm vi phạm điều này cũng bị chặn (`RegistrationCloseAfterStart`).
- **BR23 — Khung nghỉ:** tên bắt buộc ≤ 100; nằm trọn trong giờ của ngày; dài ≥ 15 phút; không chồng khung nghỉ
  khác cùng ngày (chạm đầu mút được). Đổi giờ của ngày mà khung nghỉ bị lọt ra ngoài → chặn (`BreakOutsideDay`),
  BTC sửa khung nghỉ trước.
- **BR24 — Xóa hội nghị:** chỉ khi `Draft`. Xóa kèm mọi ngày, khung nghỉ, phòng, track, phiên, lời mời, giờ rảnh.
- **BR25 — Xóa ngày:** xóa kèm khung nghỉ và giờ rảnh của ngày đó; giao diện báo trước số dòng giờ rảnh bị mất.
- **BR26 — Đổi giờ của ngày:** giờ rảnh đang trùng đúng giờ cũ của cả ngày (nghĩa là "rảnh cả ngày") được đổi theo
  giờ mới; các khoảng khác giữ nguyên (thuật toán chỉ xét phần nằm trong giờ của ngày).

**Phòng (UC04)**
- **BR27 — Phòng:** tên bắt buộc ≤ 50, không trùng trong hội nghị (không phân biệt hoa thường); sức chứa 1–10 000.
  G3 xóa phòng tự do (chưa có lịch dùng tới).

**Track, phiên (UC08)**
- **BR28 — Track:** tên tiếng Việt không trùng trong hội nghị, ≤ 100; tối đa **5 track** (5 màu track đã chọn để
  phân biệt được khi in đen trắng và với người mù màu — PROJECT.md mục 9). Màu theo thứ tự tạo. Đang có phiên dùng
  thì không xóa được (`TrackInUse`).
- **BR29 — Phiên:** tiêu đề bắt buộc ≤ 200; tóm tắt ≤ 4000; track tùy chọn, phải thuộc cùng hội nghị; thời lượng
  15–240 phút (BR18); quy mô dự kiến tùy chọn 1–10 000 (để trống → thuật toán ước lượng từ số người quan tâm).
- **BR30 — Cảnh báo phiên** (hiện trong danh sách, **không chặn** lưu — BTC có thể thêm phòng / sửa ngày sau):
  - `NoRoomBigEnough` — quy mô dự kiến lớn hơn phòng lớn nhất (vi phạm H4 chắc chắn).
  - `NoSlotLongEnough` — không ngày nào có khoảng liền (giữa các khung nghỉ) đủ dài cho thời lượng.
  - `NoAcceptedSpeaker` — chưa có diễn giả nào nhận lời.
- **BR31 — Xóa phiên:** xóa kèm lời mời của phiên.

**Diễn giả, lời mời (UC05, UC06)**
- **BR32 — Mời:** theo email của tài khoản đã có và đang `Active` (ERD mục 10: diễn giả phải có tài khoản trước).
  Không có → 422 `SpeakerAccountNotFound`. Một người chỉ có 1 dòng mỗi phiên (`SpeakerAlreadyInvited`);
  tối đa 5 diễn giả mỗi phiên (`TooManySpeakers`). BTC tự mời mình được.
- **BR33 — Email mời:** mời / mời lại thì gửi email cho diễn giả qua outbox (mục 11). Message chỉ mang
  `SessionId` + `SpeakerId`; consumer đọc dữ liệu mới nhất, lời mời không còn `Pending` thì không gửi.
- **BR34 — Phản hồi:** chỉ chính diễn giả được phản hồi lời mời của mình; chuyển trạng thái theo bảng mục 2.
  Rút lui (Accepted → Declined) giao diện hỏi xác nhận.
- **BR35 — Gỡ diễn giả:** BTC gỡ được ở mọi trạng thái (gỡ người đã nhận lời thì hỏi xác nhận). Giờ rảnh của người
  đó trong hội nghị **giữ nguyên** (có thể còn phiên khác, hoặc được mời lại).

**Giờ rảnh (UC05, UC07)**
- **BR36 — Ai được khai:** diễn giả có ít nhất 1 lời mời `Pending` / `Accepted` trong hội nghị (khai được trước khi
  nhận lời); BTC của hội nghị sửa thay cho mọi diễn giả có lời mời trong hội nghị.
- **BR37 — Ý nghĩa** (PROJECT.md 6.6 ý 3): **chưa khai dòng nào = rảnh mọi lúc**; đã khai thì chỉ rảnh trong các
  khoảng đã khai, **ngày không khai = không tham gia**. Giao diện khai theo từng ngày với 3 lựa chọn:
  - *Cả ngày* → lưu 1 khoảng = giờ của ngày.
  - *Không tham gia* → không lưu dòng nào cho ngày đó.
  - *Một số khung giờ* → lưu từng khoảng.
  - Mọi ngày đều *Cả ngày* → xóa hết dòng (trở về "chưa khai = rảnh mọi lúc", tránh dữ liệu thừa).
- **BR38 — Khoảng rảnh:** nằm trong giờ của ngày, giờ bắt đầu < giờ kết thúc (BR18); các khoảng trong cùng ngày không
  chồng nhau (chạm đầu mút được). Không được để mọi ngày *Không tham gia* (`NoAvailability`) — vì như vậy sẽ thành
  "chưa khai = rảnh mọi lúc"; không tham gia được thì từ chối lời mời.
- **BR39 — Lưu cả bộ:** mỗi lần lưu thay toàn bộ giờ rảnh của người đó trong hội nghị (một giao dịch).
- **BR40 — Cảnh báo giờ rảnh** (không chặn): khoảng rảnh dài nhất ngắn hơn một phiên người đó đã nhận lời
  (`NoRangeLongEnough`) — thuật toán chắc chắn không xếp được phiên đó.

## 4. UC03 — Tạo và cấu hình hội nghị

| Mục            | Nội dung                                                               |
|----------------|------------------------------------------------------------------------|
| Tác nhân       | Ban tổ chức                                                            |
| Mục tiêu       | Có hội nghị với thông tin, các ngày tổ chức và khung nghỉ              |
| Tiền điều kiện | Đăng nhập vai trò Ban tổ chức (`Active`, có quyền `Conference.Manage`) |
| Hậu điều kiện  | Hội nghị `Draft` có ≥ 1 ngày; BTC đang đăng nhập là chủ sở hữu         |
| Kích hoạt      | Bấm "Tạo hội nghị" ở trang danh sách hội nghị (`/manage/conferences`)  |

**Luồng chính — Tạo hội nghị:**
1. BTC mở `/manage/conferences/new`.
2. Nhập tên (vi, en), mô tả (vi, en), địa điểm; chọn **khoảng ngày** (từ – đến) và **giờ mặc định** mỗi ngày
   (mặc định 08:00–17:00); chọn hạn đăng ký; bật / tắt "Cần duyệt đăng ký".
3. Giao diện kiểm tra BR17, BR18, BR20–BR22; lỗi hiện dưới từng ô.
4. Hệ thống kiểm tra lại, tạo Conference `Draft` (`OwnerId` = người gọi) và một ConferenceDay cho mỗi ngày trong
   khoảng với giờ mặc định.
5. Giao diện chuyển sang trang hội nghị, tab "Ngày và khung nghỉ", để BTC chỉnh giờ từng ngày và thêm khung nghỉ.

**Luồng con — Sửa thông tin:** ở tab "Tổng quan" sửa các trường của bước 2 (trừ khoảng ngày — sửa ở tab Ngày);
hệ thống kiểm tra BR17, BR20, BR22.

**Luồng con — Quản lý ngày:**
1. Thêm ngày: chọn ngày + giờ bắt đầu / kết thúc (BR18, BR21, BR22).
2. Sửa ngày: đổi ngày hoặc giờ (BR21–BR23, BR26).
3. Xóa ngày: giao diện hỏi xác nhận, nêu số khung nghỉ và số khoảng giờ rảnh bị xóa theo (BR21, BR25).

**Luồng con — Quản lý khung nghỉ:** trong từng ngày, thêm / sửa / xóa khung nghỉ (tên, giờ bắt đầu, giờ kết thúc —
BR18, BR23). Danh sách khung nghỉ sắp theo giờ bắt đầu.

**Luồng con — Xóa hội nghị:** chỉ hiện nút khi `Draft`; hỏi xác nhận bằng cách gõ lại tên hội nghị (BR24).

**Ngoại lệ:**
- **E1** — Dữ liệu sai quy tắc → 400 `Validation` kèm lỗi từng ô.
- **E2** — Trùng ngày → 422 `DayDateTaken`; quá 14 ngày → 422 `TooManyDays`; ngày ở quá khứ → 422 `DayInPast`.
- **E3** — Xóa ngày cuối cùng → 422 `LastDayRequired`.
- **E4** — Hạn đăng ký sau giờ bắt đầu ngày đầu tiên → 422 `RegistrationCloseAfterStart`; hạn mới ở quá khứ → 422
  `RegistrationCloseInPast`.
- **E5** — Khung nghỉ chồng khung khác → 422 `BreakOverlap`; khung nghỉ ngoài giờ của ngày → 422 `BreakOutsideDay`.
- **E6** — Xóa hội nghị không ở `Draft` → 422 `ConferenceNotDraft`.
- **E7** — Hội nghị của BTC khác / không tồn tại → 404 `NotFound` (BR16); hội nghị `Finished` → 422 (BR19).

## 5. UC04 — Quản lý phòng

| Mục            | Nội dung                                                        |
|----------------|-----------------------------------------------------------------|
| Tác nhân       | Ban tổ chức                                                     |
| Mục tiêu       | Khai các phòng và sức chứa của hội nghị (đầu vào H2, H3, H4, S) |
| Tiền điều kiện | BTC là chủ hội nghị, có quyền `Room.Manage`                     |
| Hậu điều kiện  | Danh sách phòng được cập nhật                                   |
| Kích hoạt      | Mở tab "Phòng" của hội nghị                                     |

**Luồng chính — Thêm phòng:**
1. BTC bấm "Thêm phòng", nhập tên và sức chứa (BR27).
2. Hệ thống kiểm tra, lưu; bảng phòng (sắp theo sức chứa giảm dần) hiện dòng mới.

**Luồng con:** Sửa phòng (cùng quy tắc) · Xóa phòng (hỏi xác nhận).

**Ngoại lệ:** **E1** dữ liệu sai → 400 · **E2** trùng tên → 409 `RoomNameTaken` · **E3** như UC03 E7.

- Tab Phòng hiện thêm tổng sức chứa và phòng lớn nhất (giúp BTC thấy ngay phiên nào vượt — BR30).
- Thiết bị sẵn có tại phòng: luồng con "Khai thiết bị" thêm ở G6 (T6.2).

## 6. UC08 — Quản lý phiên (kèm track)

| Mục            | Nội dung                                                         |
|----------------|------------------------------------------------------------------|
| Tác nhân       | Ban tổ chức                                                      |
| Mục tiêu       | Khai các phiên trình bày (đầu vào chính của thuật toán xếp lịch) |
| Tiền điều kiện | BTC là chủ hội nghị, có quyền `Session.Manage`                   |
| Hậu điều kiện  | Phiên được lưu; danh sách hiện cảnh báo nếu phiên khó xếp (BR30) |
| Kích hoạt      | Mở tab "Phiên" của hội nghị                                      |

**Luồng con — Quản lý track:** ở đầu tab Phiên, BTC thêm / đổi tên / xóa track (BR28); mỗi track hiện chip màu.

**Luồng chính — Thêm phiên:**
1. BTC bấm "Thêm phiên" → ngăn kéo (drawer) bên phải.
2. Nhập tiêu đề (vi, en), tóm tắt (vi, en), chọn track (tùy chọn), thời lượng (chọn sẵn 30 / 45 / 60 / 90 / 120 hoặc
   nhập bội số 15), quy mô dự kiến (tùy chọn).
3. Hệ thống kiểm tra BR17, BR18, BR29, lưu phiên.
4. Drawer chuyển sang phần "Diễn giả" của phiên vừa tạo để mời ngay (UC05).

**Luồng con:** Sửa phiên (cùng drawer) · Xóa phiên (hỏi xác nhận, nêu số lời mời bị xóa — BR31) ·
Lọc theo track, tìm theo tiêu đề.

**Danh sách phiên** hiện: màu track, tiêu đề, thời lượng, quy mô (hoặc "tự ước lượng"), diễn giả kèm trạng thái lời
mời, cảnh báo BR30.

**Ngoại lệ:** **E1** dữ liệu sai → 400 · **E2** track trùng tên → 409 `TrackNameTaken` · **E3** quá 5 track → 422
`TooManyTracks` · **E4** xóa track đang dùng → 409 `TrackInUse` · **E5** track không thuộc hội nghị → 400
`Validation` · **E6** như UC03 E7.

## 7. UC05 — Mời và quản lý diễn giả

| Mục            | Nội dung                                                             |
|----------------|----------------------------------------------------------------------|
| Tác nhân       | Ban tổ chức                                                          |
| Mục tiêu       | Mỗi phiên có diễn giả nhận lời; biết giờ rảnh của từng người         |
| Tiền điều kiện | Phiên đã tạo; BTC là chủ hội nghị, có quyền `Speaker.Manage`         |
| Hậu điều kiện  | Lời mời `Pending` được tạo và email mời được gửi                     |
| Kích hoạt      | Phần "Diễn giả" trong drawer phiên, hoặc tab "Diễn giả" của hội nghị |

**Luồng chính — Mời diễn giả vào phiên:**
1. Trong drawer phiên, BTC nhập email diễn giả, bấm "Mời".
2. Hệ thống tìm tài khoản `Active` theo email (BR32), kiểm tra chưa có trong phiên, chưa đủ 5 người.
3. Hệ thống tạo SessionSpeaker `Pending`, ghi sự kiện gửi email mời vào outbox **cùng giao dịch** (BR33).
4. Danh sách diễn giả của phiên hiện người mới với nhãn "Chờ phản hồi".
5. Phía sau: consumer gửi email (tên hội nghị, tiêu đề phiên, thời lượng, tên BTC, link `/me/speaking`).

**Luồng con — Mời lại:** với lời mời `Declined`, BTC bấm "Mời lại" → `Pending` + gửi lại email.

**Luồng con — Gỡ khỏi phiên:** BTC bấm "Gỡ" (người đã nhận lời thì hỏi xác nhận) → xóa dòng SessionSpeaker (BR35).

**Luồng con — Xem danh sách diễn giả của hội nghị:** tab "Diễn giả" là bảng mỗi dòng một cặp (diễn giả, phiên),
cột: Diễn giả (họ tên, email, đơn vị) · Phiên · Lời mời (trạng thái + "Mời lại" khi `Declined`) · Giờ rảnh (nhãn
"Đã khai" / "Chưa khai — coi là rảnh mọi lúc", từng ngày một dòng, cảnh báo BR40) · nút "Giờ rảnh". Ô Diễn giả,
Giờ rảnh và nút gộp dọc (`rowSpan`) qua các phiên của cùng một người.

**Luồng con — Sửa giờ rảnh thay diễn giả:** ở tab "Diễn giả" bấm "Giờ rảnh" → cùng màn khai như UC07 (BR36–BR40).
Dùng khi diễn giả báo qua điện thoại / email, nhất là lúc biến động trước hội nghị (rồi chạy xếp lại UC19).

**Ngoại lệ:** **E1** email sai định dạng → 400 · **E2** không có tài khoản `Active` → 422 `SpeakerAccountNotFound`
· **E3** đã có trong phiên → 409 `SpeakerAlreadyInvited` · **E4** đủ 5 người → 422 `TooManySpeakers` · **E5** mời
lại lời mời không ở `Declined` → 422 `InvalidInvitationStatus` · **E6** gửi mail lỗi → MassTransit thử lại, lời mời
vẫn được tạo (như G1 E5) · **E7** như UC03 E7.

## 8. UC06 — Phản hồi lời mời trình bày

| Mục            | Nội dung                                                                |
|----------------|-------------------------------------------------------------------------|
| Tác nhân       | Diễn giả (người dùng đăng nhập bất kỳ được mời)                         |
| Mục tiêu       | Nhận lời / từ chối trình bày một phiên                                  |
| Tiền điều kiện | Có lời mời; hội nghị chưa `Finished`                                    |
| Hậu điều kiện  | `InvitationStatus` đổi theo bảng mục 2                                  |
| Kích hoạt      | Bấm link trong email mời, hoặc mục "Lời mời trình bày" ở menu tài khoản |

**Luồng chính:**
1. Diễn giả mở `/me/speaking` (chưa đăng nhập thì qua `/login?returnUrl=/me/speaking`).
2. Giao diện liệt kê lời mời, gom theo hội nghị: tên, ngày, địa điểm hội nghị; tiêu đề, thời lượng, track của phiên;
   các đồng diễn giả và trạng thái của họ; trạng thái của mình.
3. Với lời mời `Pending`, diễn giả bấm "Nhận lời" hoặc "Từ chối".
4. Hệ thống kiểm tra đúng người, chuyển trạng thái hợp lệ (BR34), lưu.
5. Nhận lời → giao diện nhắc "Hãy khai thời gian có thể tham gia" kèm nút sang UC07 (nếu chưa khai).

**Luồng con — Rút lui:** lời mời `Accepted` có nút "Rút lui"; hỏi xác nhận → `Declined`.

**Ngoại lệ:** **E1** lời mời không phải của mình / không tồn tại → 404 `NotFound` · **E2** chuyển trạng thái không
hợp lệ (vd tự nhận lại sau khi từ chối) → 422 `InvalidInvitationStatus` · **E3** hội nghị `Finished` → 422.

## 9. UC07 — Khai báo thời gian có thể tham gia

| Mục            | Nội dung                                                                |
|----------------|-------------------------------------------------------------------------|
| Tác nhân       | Diễn giả                                                                |
| Mục tiêu       | Cho BTC và thuật toán biết các khoảng mình tham gia được (ràng buộc H5) |
| Tiền điều kiện | Có lời mời `Pending` / `Accepted` trong hội nghị (BR36)                 |
| Hậu điều kiện  | Giờ rảnh của diễn giả trong hội nghị được thay bằng bộ mới (BR39)       |
| Kích hoạt      | Nút "Khai giờ rảnh" ở thẻ hội nghị trong `/me/speaking`                 |

**Luồng chính:**
1. Diễn giả mở `/me/speaking/:conferenceId/availability`.
2. Chưa khai: giao diện báo "Bạn chưa khai — hệ thống coi là rảnh mọi lúc", mọi ngày ở *Cả ngày*.
3. Với từng ngày (hiện ngày, giờ của ngày, các khung nghỉ để tham khảo), chọn *Cả ngày* / *Không tham gia* /
   *Một số khung giờ*; với lựa chọn thứ ba thêm các khoảng giờ (bước 15 phút).
4. Bấm "Lưu"; giao diện kiểm tra BR38.
5. Hệ thống kiểm tra lại, thay toàn bộ giờ rảnh (BR37, BR39); trả về kèm cảnh báo BR40 nếu có.

**Ngoại lệ:** **E1** khoảng sai (ngoài giờ ngày, không bội số 15, bắt đầu ≥ kết thúc) → 400 `Validation` ·
**E2** khoảng chồng nhau → 422 `AvailabilityOverlap` · **E3** mọi ngày *Không tham gia* → 422 `NoAvailability` ·
**E4** không có lời mời phù hợp → 404 `NotFound` · **E5** hội nghị `Finished` → 422.

## 10. API

Lỗi trả ProblemDetails có trường `code` (như G1). Quy ước: vi phạm quy tắc nghiệp vụ (bất biến của aggregate, kể cả
trùng ngày trong cùng hội nghị) → 422 (`DomainException`); trùng với bản ghi khác ở bảng riêng hoặc đang được dùng
(handler kiểm + unique index) → 409 (`ConflictException`); không tồn tại hoặc không phải của mình → 404.

**Ban tổ chức** — cần đăng nhập + quyền ghi ở tiêu đề mỗi bảng + là chủ hội nghị (BR16).
Gốc: `/api/manage/conferences` (viết tắt `…`).

`Conference.Manage` — hội nghị, ngày, khung nghỉ:

| Method | Route                                  | Việc                         |
|--------|----------------------------------------|------------------------------|
| GET    | `…`                                    | Hội nghị của tôi             |
| POST   | `…`                                    | Tạo (kèm khoảng ngày)        |
| GET    | `…/{id}`                               | Chi tiết + ngày + khung nghỉ |
| PUT    | `…/{id}`                               | Sửa thông tin                |
| DELETE | `…/{id}`                               | Xóa (chỉ `Draft`)            |
| POST   | `…/{id}/days`                          | Thêm ngày                    |
| PUT    | `…/{id}/days/{dayId}`                  | Sửa ngày                     |
| DELETE | `…/{id}/days/{dayId}`                  | Xóa ngày                     |
| POST   | `…/{id}/days/{dayId}/breaks`           | Thêm khung nghỉ              |
| PUT    | `…/{id}/days/{dayId}/breaks/{breakId}` | Sửa khung nghỉ               |
| DELETE | `…/{id}/days/{dayId}/breaks/{breakId}` | Xóa khung nghỉ               |

`Room.Manage` — phòng; `Session.Manage` — track, phiên:

| Method | Route                         | Việc                       | Quyền            |
|--------|-------------------------------|----------------------------|------------------|
| GET    | `…/{id}/rooms`                | Danh sách phòng            | `Room.Manage`    |
| POST   | `…/{id}/rooms`                | Thêm phòng                 | `Room.Manage`    |
| PUT    | `…/{id}/rooms/{roomId}`       | Sửa phòng                  | `Room.Manage`    |
| DELETE | `…/{id}/rooms/{roomId}`       | Xóa phòng                  | `Room.Manage`    |
| GET    | `…/{id}/tracks`               | Danh sách track            | `Session.Manage` |
| POST   | `…/{id}/tracks`               | Thêm track                 | `Session.Manage` |
| PUT    | `…/{id}/tracks/{trackId}`     | Đổi tên track              | `Session.Manage` |
| DELETE | `…/{id}/tracks/{trackId}`     | Xóa track                  | `Session.Manage` |
| GET    | `…/{id}/sessions`             | Danh sách phiên + cảnh báo | `Session.Manage` |
| POST   | `…/{id}/sessions`             | Thêm phiên                 | `Session.Manage` |
| GET    | `…/{id}/sessions/{sessionId}` | Chi tiết phiên + diễn giả  | `Session.Manage` |
| PUT    | `…/{id}/sessions/{sessionId}` | Sửa phiên                  | `Session.Manage` |
| DELETE | `…/{id}/sessions/{sessionId}` | Xóa phiên                  | `Session.Manage` |

`Speaker.Manage` — gốc `/api/manage/conferences/{id}` (viết tắt `…`):

| Method | Route                                                  | Việc                   |
|--------|--------------------------------------------------------|------------------------|
| POST   | `…/sessions/{sessionId}/speakers`                      | Mời `{ email }`        |
| POST   | `…/sessions/{sessionId}/speakers/{speakerId}/reinvite` | Mời lại                |
| DELETE | `…/sessions/{sessionId}/speakers/{speakerId}`          | Gỡ khỏi phiên          |
| GET    | `…/speakers`                                           | Diễn giả của hội nghị  |
| GET    | `…/speakers/{speakerId}/availability`                  | Giờ rảnh của một người |
| PUT    | `…/speakers/{speakerId}/availability`                  | Sửa thay giờ rảnh      |

**Diễn giả** — chỉ cần đăng nhập; handler kiểm tra là người được mời (BR34, BR36). Gốc: `/api/me/speaking` (viết tắt `…`).

| Method | Route                                       | Việc                                |
|--------|---------------------------------------------|-------------------------------------|
| GET    | `…`                                         | Lời mời của tôi, gom theo hội nghị  |
| PUT    | `…/sessions/{sessionId}`                    | Phản hồi `{ response }`             |
| GET    | `…/conferences/{conferenceId}/availability` | Giờ rảnh của tôi + ngày, khung nghỉ |
| PUT    | `…/conferences/{conferenceId}/availability` | Lưu cả bộ giờ rảnh                  |

- Gốc `/api/manage/...` tách khỏi `/api/conferences/...` để G4 dùng cho trang công khai (UC10, UC11).
- `response` nhận `Accepted` hoặc `Declined`.

**Request / response chính:**
- Tạo hội nghị: `{ nameVi, nameEn?, descriptionVi?, descriptionEn?, venue, startDate, endDate, dayStartTime,
  dayEndTime, registrationCloseAt, requireApproval }` → `201` `{ id }`.
- Danh sách hội nghị của tôi: `[{ id, nameVi, nameEn, venue, status, firstDate, lastDate, roomCount, sessionCount,
  acceptedSpeakerCount, invitedSpeakerCount }]`.
- Chi tiết: `{ ...thông tin, days: [{ id, date, startTime, endTime, breaks: [{ id, name, startTime, endTime }] }] }`.
- Phiên trong danh sách: `{ id, titleVi, titleEn, trackId, durationMinutes, expectedSize, speakers: [{ speakerId,
  fullName, invitationStatus }], warnings: ["NoRoomBigEnough", ...] }`.
- Giờ rảnh (GET / PUT): `{ declared, days: [{ dayId, date, dayStartTime, dayEndTime, breaks, mode, ranges:
  [{ startTime, endTime }] }], warnings }`; `mode` ∈ `AllDay` · `Unavailable` · `Ranges`. PUT gửi `days: [{ dayId,
  mode, ranges }]` cho **mọi** ngày của hội nghị.
- Giờ dạng `HH:mm`, ngày dạng `yyyy-MM-dd`, `registrationCloseAt` dạng ISO 8601 có múi giờ.

**Mã lỗi nghiệp vụ mới (`code`):**

| code                          | HTTP | Khi nào                                       |
|-------------------------------|------|-----------------------------------------------|
| `NotFound`                    | 404  | Không tồn tại / không phải của mình (BR16)    |
| `ConferenceFinished`          | 422  | Sửa hội nghị đã kết thúc (BR19)               |
| `ConferenceNotDraft`          | 422  | Xóa hội nghị không ở `Draft` (BR24)           |
| `DayDateTaken`                | 422  | Trùng ngày (BR21)                             |
| `TooManyDays`                 | 422  | Quá 14 ngày (BR21)                            |
| `DayInPast`                   | 422  | Ngày thêm / đổi ở quá khứ (BR21)              |
| `LastDayRequired`             | 422  | Xóa ngày cuối cùng (BR21)                     |
| `RegistrationCloseAfterStart` | 422  | Hạn đăng ký sau giờ bắt đầu ngày đầu (BR22)   |
| `RegistrationCloseInPast`     | 422  | Đặt / đổi hạn đăng ký về quá khứ (BR22)       |
| `BreakOutsideDay`             | 422  | Khung nghỉ ngoài giờ của ngày (BR23)          |
| `BreakOverlap`                | 422  | Khung nghỉ chồng nhau (BR23)                  |
| `RoomNameTaken`               | 409  | Trùng tên phòng (BR27)                        |
| `TrackNameTaken`              | 409  | Trùng tên track (BR28)                        |
| `TooManyTracks`               | 422  | Quá 5 track (BR28)                            |
| `TrackInUse`                  | 409  | Xóa track đang có phiên (BR28)                |
| `SpeakerAccountNotFound`      | 422  | Không có tài khoản `Active` với email (BR32)  |
| `SpeakerAlreadyInvited`       | 409  | Đã có trong phiên (BR32)                      |
| `TooManySpeakers`             | 422  | Phiên đã đủ 5 diễn giả (BR32)                 |
| `InvalidInvitationStatus`     | 422  | Chuyển trạng thái lời mời không hợp lệ (BR34) |
| `AvailabilityOverlap`         | 422  | Khoảng rảnh chồng nhau (BR38)                 |
| `NoAvailability`              | 422  | Mọi ngày "Không tham gia" (BR38)              |

- Lỗi vượt giới hạn độ dài, sai bước 15 phút, giờ ngoài ngày… đi qua FluentValidation → 400 `Validation`.
- Mã cảnh báo (không phải lỗi): `NoRoomBigEnough`, `NoSlotLongEnough`, `NoAcceptedSpeaker` (BR30), `NoRangeLongEnough`
  (BR40).

## 11. Quyền, sự kiện và email

**Quyền mới** (hằng số trong code, seed cho vai trò `Organizer`; migration cập nhật `Role.Permissions` ở T3.1):

| Quyền               | UC   | Gồm                                         |
|---------------------|------|---------------------------------------------|
| `Conference.Manage` | UC03 | Hội nghị, ngày, khung nghỉ                  |
| `Room.Manage`       | UC04 | Phòng                                       |
| `Session.Manage`    | UC08 | Track, phiên                                |
| `Speaker.Manage`    | UC05 | Mời / mời lại / gỡ, danh sách, sửa giờ rảnh |

- UC06, UC07 không cần quyền riêng: diễn giả có thể mang vai trò bất kỳ; kiểm tra "là người được mời" trong handler.
- Quyền nằm trong JWT nên tài khoản BTC đang đăng nhập nhận quyền mới sau lần làm mới phiên (G1 mục 10).

**Sự kiện:**

| Message (RabbitMQ)                  | Phát khi     | Consumer                    |
|-------------------------------------|--------------|-----------------------------|
| `SendSpeakerInvitationEmailMessage` | Mời, mời lại | Còn `Pending` thì gửi email |

- Aggregate phiên phát domain event `SpeakerInvited`; repository đổi thành message trên và publish qua outbox ngay trước
  `SaveChanges` (cơ chế của T1.1).
- Message chỉ có `SessionId`, `SpeakerId` (không có bí mật — khác G1 không cần sinh token).
- Email song ngữ vi + en: "Bạn được mời trình bày tại {hội nghị}" — tên phiên, thời lượng, ngày tổ chức, tên BTC,
  nút "Xem lời mời" → `{Frontend:BaseUrl}/me/speaking`.
- Không gửi email cho BTC khi diễn giả phản hồi: BTC xem trạng thái ở tab Phiên / Diễn giả; thông báo trong ứng dụng
  làm ở G9 (UC43).

## 12. Biểu đồ

Thư mục: `docs/diagrams/`

| File                          | Loại       | Nội dung                                   |
|-------------------------------|------------|--------------------------------------------|
| `g3-erd-hoi-nghi.puml`        | ERD        | 8 bảng của G3 + User                       |
| `g3-trang-thai-hoi-nghi.puml` | Trạng thái | Vòng đời `Conference.Status`               |
| `g3-trang-thai-loi-moi.puml`  | Trạng thái | Vòng đời `SessionSpeaker.InvitationStatus` |
| `g3-hd-tao-hoi-nghi.puml`     | Hoạt động  | UC03: tạo, chỉnh ngày, khung nghỉ          |
| `g3-hd-quan-ly-phong.puml`    | Hoạt động  | UC04: thêm / sửa / xóa phòng               |
| `g3-hd-quan-ly-phien.puml`    | Hoạt động  | UC08: track, thêm / sửa phiên, cảnh báo    |
| `g3-hd-moi-dien-gia.puml`     | Hoạt động  | UC05 + UC06: mời → email → phản hồi        |
| `g3-hd-khai-gio-ranh.puml`    | Hoạt động  | UC07: khai theo từng ngày                  |
| `g3-td-tao-hoi-nghi.puml`     | Tuần tự    | UC03: quyền + tạo hội nghị kèm ngày        |
| `g3-td-quan-ly-phien.puml`    | Tuần tự    | UC08: kiểm chủ sở hữu, lưu, tính cảnh báo  |
| `g3-td-moi-dien-gia.puml`     | Tuần tự    | UC05 + UC06: outbox → email → nhận lời     |
| `g3-td-khai-gio-ranh.puml`    | Tuần tự    | UC07: lưu cả bộ giờ rảnh                   |

## 13. Giao diện

Mockup: `docs/ui/g3-conference.html` (đổi sáng / tối, vi / en). Màn BTC thuộc **Shell A** (dựng lần đầu ở T3.3);
màn diễn giả thuộc **Shell B**.

| Route                                     | Màn hình                  | Shell |
|-------------------------------------------|---------------------------|-------|
| `/manage/conferences`                     | Hội nghị của tôi          | A     |
| `/manage/conferences/new`                 | Tạo hội nghị              | A     |
| `/manage/conferences/:id`                 | Tổng quan (sửa thông tin) | A     |
| `/manage/conferences/:id/days`            | Ngày và khung nghỉ        | A     |
| `/manage/conferences/:id/rooms`           | Phòng                     | A     |
| `/manage/conferences/:id/sessions`        | Track và phiên            | A     |
| `/manage/conferences/:id/speakers`        | Diễn giả                  | A     |
| `/me/speaking`                            | Lời mời trình bày         | B     |
| `/me/speaking/:conferenceId/availability` | Khai giờ rảnh             | B     |

- Thành phần antd chính: `Card` (lưới hội nghị của tôi), `Table` (phòng, phiên, diễn giả),
  `Form` + `Modal` (thêm / sửa ngày, khung nghỉ, phòng, track), `Drawer` (phiên + diễn giả của phiên),
  `DatePicker.RangePicker` + `TimePicker` (tạo hội nghị), `Segmented` + `TimePicker.RangePicker` (giờ rảnh),
  `Tag` (trạng thái, track), `Popconfirm` (xóa, gỡ, rút lui).

- **Shell A:** sidebar trái. Ở danh sách: mục "Hội nghị". Trong một hội nghị: tên hội nghị + nhãn trạng thái ở đầu
  sidebar, rồi các mục Tổng quan · Ngày và khung nghỉ · Phòng · Phiên · Diễn giả (nhóm sau thêm Vé, Đăng ký,
  Xếp lịch…). Màn nhỏ: sidebar thành `Drawer`.
- **Nút và link:** thao tác gắn với một trạng thái (Nhận lời, Từ chối, Rút lui, Mời lại) là **nút nhỏ** (`size="small"`;
  Rút lui `danger`); thao tác lặp ở mỗi dòng bảng / danh sách (Sửa, Xóa, Gỡ) là nút `type="link"` như mặc định antd.
- **Tag:** trạng thái và track dùng `Tag` của antd với bo góc mặc định (`borderRadiusSM`), không tự đặt bo tròn riêng;
  bo góc chia 3 mức theo token: nhỏ (tag, mục `Segmented`), vừa (nút, ô nhập), lớn (card, modal).
- Màn khai giờ rảnh dùng chung component cho diễn giả (UC07) và BTC sửa thay (UC05, mở trong `Modal`).
- Menu tài khoản (Shell B và Shell A) thêm mục "Lời mời trình bày".
- Chặn route `/manage/*` cần đăng nhập + quyền, trang 403 — làm ở T3.3 (dời từ T1.2).

## 14. Để dành cho nhóm sau

- **Chặn theo trạng thái:** G3 chỉ có `Draft` nên BR19 (`ConferenceFinished`) cài ở task tạo chuyển sang `Finished`
  (G5), kiểm tra `ConferenceNotDraft` của BR24 cài ở task tạo `RegistrationOpen` (G4) — lúc đó mới test được.
- **G4 (D9):** mở / đóng đăng ký và điều kiện chuyển trạng thái; hội nghị `Draft` không hiện công khai; sửa / xóa phiên
  đã có người đánh dấu quan tâm (UC13).
- **G5 (D10):** sửa phòng, ngày, phiên, lời mời, giờ rảnh khi đã có lịch / lịch đã công bố → đánh dấu lịch cần xếp lại
  (UC19); xóa phòng đang được xếp phiên.
- **G6 (D11):** thiết bị sẵn có tại phòng, danh mục dịch vụ, suất phục vụ trong khung nghỉ.
- **G8 (D13):** slide + tóm tắt của từng diễn giả (UC33, UC34).

## 15. Giới hạn chấp nhận

- Một múi giờ cho cả hệ thống (`Asia/Ho_Chi_Minh`): hội nghị tổ chức ở múi giờ khác hiện sai hạn đăng ký.
- Diễn giả phải có tài khoản trước khi được mời (ERD mục 10); BTC nhập email chưa có tài khoản thì biết được email đó
  chưa đăng ký — chấp nhận vì BTC là người đã được QT duyệt.
- Tối đa 5 track (theo 5 màu track); hội nghị cần nhiều nhóm chủ đề hơn phải gộp.
- Khai "cả ngày" lưu thành một khoảng bằng giờ của ngày; BR26 đổi theo khi sửa giờ ngày, nhưng khoảng "một số khung giờ"
  không tự giãn khi ngày dài ra.
- Hai người cùng sửa một mục thì người lưu sau ghi đè (không kiểm tra phiên bản dữ liệu).
- BTC không nhận email khi diễn giả từ chối / rút lui (thông báo trong ứng dụng ở G9).
