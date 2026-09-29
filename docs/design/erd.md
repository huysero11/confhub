# ERD — thiết kế cơ sở dữ liệu (T0.4)

> Trạng thái: **đã chốt 2026-09-25**. Đã vẽ `docs/diagrams/erd.puml` (P1–P3, 27 bảng nghiệp vụ) 2026-09-29.
> Khớp 57 UC trong `use-cases.md`.
> **Nguyên tắc:** chỉ tách bảng khi có quan hệ 1–N / N–N thật hoặc vòng đời riêng; **chỉ giữ cột mà một UC hoặc thuật toán thực sự dùng**. Cột suy ra được từ dữ liệu khác thì không lưu.

## 0. Quy ước chung

| Quy ước | Nội dung |
|---|---|
| Cột mặc định | Mọi bảng có `Id` (Guid) và `CreatedAt` — không liệt kê lại. Bảng nối N–N dùng khóa ghép, không có `Id` |
| Song ngữ | Chỉ nội dung công khai do BTC nhập: tên/mô tả hội nghị, tên track, tiêu đề/tóm tắt phiên có cặp `_Vi` / `_En` |
| Enum | Lưu dạng chuỗi |
| Mã QR | Chuỗi ngẫu nhiên khó đoán, không dùng Id → chống làm giả |
| Bí mật | Mật khẩu, token chỉ lưu bản băm |

Kiểu dữ liệu: **Guid** (khóa), **chuỗi**, **số nguyên**, **số thực**, **tiền** (decimal), **ngày**, **giờ**, **thời điểm** (ngày + giờ), **logic** (true/false), **vector** (mảng số thực, varbinary).

---

## 1. Người dùng (P1 — 4 bảng)

**User** — tài khoản mọi tác nhân
| Cột | Kiểu | Mô tả |
|---|---|---|
| Email | chuỗi, unique | Đăng nhập, nhận email thông báo |
| PasswordHash | chuỗi | Mật khẩu đã băm |
| FullName | chuỗi | Họ tên |
| RoleId | FK → Role | Vai trò |
| Organization | chuỗi, null | Đơn vị công tác; bắt buộc với BTC/NCC (D7); với nhà cung cấp là tên công ty. Hiện cạnh tên diễn giả, trên đơn hàng |
| Bio | chuỗi, null | Giới thiệu ngắn về diễn giả, hiện ở trang chi tiết phiên |
| Status | enum | `Unverified` (chưa xác thực email) · `PendingApproval` (BTC/NCC chờ QT duyệt) · `Active` · `Locked` |

> "Diễn giả" không phải vai trò: là người được gán vào phiên qua bảng SessionSpeaker.

**Role** — vai trò
| Cột | Kiểu | Mô tả |
|---|---|---|
| Code | chuỗi, unique | `Attendee` · `Organizer` · `Supplier` · `Staff` · `Admin` |
| Name | chuỗi | Tên hiển thị |
| Permissions | chuỗi (JSON) | Danh sách mã quyền, vd `["Schedule.Run"]`. Danh mục quyền là hằng số trong code |

**UserToken** — refresh token, token xác thực email
| Cột | Kiểu | Mô tả |
|---|---|---|
| UserId | FK → User | |
| Purpose | enum | `Refresh` · `VerifyEmail` · `ResetPassword` |
| TokenHash | chuỗi | Băm của token |
| ExpiresAt | thời điểm | Hết hạn |
| UsedAt | thời điểm, null | Đã dùng / thu hồi; khác null = hết hiệu lực |

**OutboxMessage** — bảng kỹ thuật, không vẽ vào ERD nghiệp vụ. **Thực tế dùng bảng Outbox/Inbox do MassTransit EF Outbox tự tạo** (xem `base-reference.md`); mô tả dưới đây là ý nghĩa khái niệm
| Cột | Kiểu | Mô tả |
|---|---|---|
| Type | chuỗi | Tên sự kiện, vd `RegistrationApproved` |
| Payload | chuỗi (JSON) | Dữ liệu sự kiện, ghi cùng giao dịch với thay đổi nghiệp vụ |
| ProcessedAt | thời điểm, null | Đã đẩy lên RabbitMQ; null = chưa |

---

## 2. Hội nghị, phòng, dịch vụ (P1 — 6 bảng)

**Conference**
| Cột | Kiểu | Mô tả |
|---|---|---|
| Name_Vi/En, Description_Vi/En | chuỗi | Tên, mô tả |
| Venue | chuỗi | Địa điểm |
| RegistrationCloseAt | thời điểm | Hạn đăng ký; hết hạn mới chạy xếp lịch |
| RequireApproval | logic | Đăng ký cần BTC duyệt hay tự động duyệt |
| Status | enum | `Draft` · `RegistrationOpen` · `RegistrationClosed` · `Scheduled` (đã công bố lịch) · `Finished` |
| OwnerId | FK → User | BTC phụ trách |

**ConferenceDay** — từng ngày tổ chức
| Cột | Kiểu | Mô tả |
|---|---|---|
| ConferenceId | FK | |
| Date | ngày | Unique (ConferenceId, Date) |
| StartTime, EndTime | giờ | Khung giờ riêng của ngày đó → sinh tập mốc T (bước 15 phút), ràng buộc **H5** |

**BreakSlot** — khung nghỉ
| Cột | Kiểu | Mô tả |
|---|---|---|
| ConferenceDayId | FK | |
| Name | chuỗi | vd "Nghỉ trưa", "Tea break chiều" |
| StartTime, EndTime | giờ | Không xếp phiên vào khoảng này |
| ServiceItemId | FK, null | Suất phục vụ trong khung; null = nghỉ không kèm dịch vụ |
| IsOptional | logic | `false`: suất gồm trong vé, tự tạo voucher. `true`: người tham dự tự chọn (UC22) |

**Room**
| Cột | Kiểu | Mô tả |
|---|---|---|
| ConferenceId | FK | |
| Name | chuỗi | vd "P101" |
| Capacity | số nguyên | Số chỗ → ràng buộc **H4**, chi phí Hungarian |

**RoomEquipment** — thiết bị có sẵn tại phòng
| Cột | Kiểu | Mô tả |
|---|---|---|
| RoomId, ServiceItemId | FK, khóa ghép | |
| Quantity | số nguyên | Số lượng sẵn có → trừ đi khi tính số cần thuê |

**ServiceItem** — danh mục dịch vụ của hội nghị
| Cột | Kiểu | Mô tả |
|---|---|---|
| ConferenceId | FK | |
| Name | chuỗi | vd "Suất ăn trưa", "Máy chiếu" |
| Kind | enum | `Consumable` (tính theo người) · `Rental` (tính theo số phòng chạy đồng thời) |
| UnitPrice | tiền | Đơn giá dự toán |
| QtyPerActiveRoom | số nguyên, null | Chỉ hàng thuê: mỗi phòng đang có phiên cần bao nhiêu (vd micro = 2) |
| SupplierId | FK → User | Nhà cung cấp mặt hàng này → dùng để tách đơn theo NCC |

---

## 3. Phiên, diễn giả (P1 — 4 bảng)

**Track** — nhóm chủ đề
| Cột | Kiểu | Mô tả |
|---|---|---|
| ConferenceId | FK | |
| Name_Vi/En | chuỗi | vd "Công nghệ". Màu trên lưới lịch gán theo thứ tự track, không lưu |

**Session** — phiên: một khối thời gian liền trong một phòng (60–90 phút), gồm một hoặc nhiều diễn giả trình bày cùng chủ đề
| Cột | Kiểu | Mô tả |
|---|---|---|
| ConferenceId, TrackId | FK | |
| Title_Vi/En, Abstract_Vi/En | chuỗi | Tiêu đề, tóm tắt công khai |
| DurationMinutes | số nguyên | Thời lượng (60, 90…) |
| ExpectedSize | số nguyên, null | Quy mô BTC nhập tay. Null → thuật toán tự ước lượng từ số người quan tâm (**H4**) |

**SessionSpeaker** — diễn giả của phiên (Session N–N User)
| Cột | Kiểu | Mô tả |
|---|---|---|
| SessionId, SpeakerId | FK, khóa ghép | SpeakerId → User. Người được mời phải có tài khoản |
| InvitationStatus | enum | `Pending` · `Accepted` · `Declined`. Ràng buộc **H1** xét mọi diễn giả `Accepted` của phiên: không ai được trình bày 2 phiên chồng giờ |
| SlideKey | chuỗi, null | Khóa tệp slide **của diễn giả này** trên MinIO |
| SlideSummary | chuỗi, null | Tóm tắt do diễn giả nhập, bắt buộc khi tải slide — nguồn cho trợ lý AI |

**SpeakerAvailability** — thời gian diễn giả tham gia được
| Cột | Kiểu | Mô tả |
|---|---|---|
| ConferenceDayId, SpeakerId | FK | |
| StartTime, EndTime | giờ | Một khoảng rảnh; mỗi người nhiều khoảng → ràng buộc **H5** |

---

## 4. Xếp lịch (P1 — 2 bảng)

**ScheduleVersion** — một phiên bản lịch (mỗi lần chạy)
| Cột | Kiểu | Mô tả |
|---|---|---|
| ConferenceId | FK | |
| BaseVersionId | FK → ScheduleVersion, null | Khác null = lần **xếp lại**: bản đã công bố làm gốc để phạt xáo trộn |
| W1, W2, W3 | số thực | Trọng số dùng cho lần chạy này (phục vụ phân tích độ nhạy) |
| Status | enum | `Running` · `Failed` · `Draft` · `Published` · `Superseded`. Mỗi hội nghị chỉ 1 bản `Published` |
| DurationMs | số nguyên | Thời gian chạy |
| ConflictCount | số nguyên | **C** — lượt xung đột nguyện vọng |
| LoadStdDev | số thực | **B** — độ lệch tải |
| WastedSeats | số nguyên | **S** — sức chứa lãng phí |
| Objective | số thực | **f** sau chuẩn hóa |

> Số phiên đã xếp, số phiên bị xáo trộn: đếm từ ScheduleItem, không lưu. Ma trận W: tính từ SessionInterest mỗi lần chạy.

**ScheduleItem** — vị trí một phiên trong phiên bản
| Cột | Kiểu | Mô tả |
|---|---|---|
| VersionId, SessionId | FK | Unique (VersionId, SessionId) |
| RoomId | FK, null | Phòng được gán |
| StartAt | thời điểm, null | Giờ bắt đầu (giờ kết thúc = StartAt + thời lượng). **Null = chưa xếp được** |
| UnscheduledReason | chuỗi, null | vd "Diễn giả kín lịch" |
| IsLocked | logic | BTC ghim; thuật toán không được dời |

---

## 5. Đăng ký, check-in (P1: 3 bảng · P2: 1 bảng)

**TicketType** — loại vé (P1)
| Cột | Kiểu | Mô tả |
|---|---|---|
| ConferenceId | FK | |
| Name | chuỗi | vd "Vé thường", "Vé sinh viên" |
| Price | tiền | Giá (thanh toán nằm ngoài phạm vi) |
| Quota | số nguyên | Số vé tối đa; hết thì đăng ký vào danh sách chờ |

**Registration** — đăng ký tham dự (P1)
| Cột | Kiểu | Mô tả |
|---|---|---|
| ConferenceId, UserId, TicketTypeId | FK | Unique (ConferenceId, UserId) |
| Status | enum | `Pending` · `Approved` · `Rejected` · `Waitlisted` · `Cancelled`. Thứ tự chờ theo CreatedAt |
| DietType | enum | `Regular` (mặn) · `Vegetarian` (chay) |
| AllergyFlags | số nguyên | Dị ứng dạng cờ bit: 1 hải sản, 2 đậu phộng, 4 sữa, 8 gluten |
| TicketCode | chuỗi, unique | Chuỗi trên QR vé |
| CheckedInAt | thời điểm, null | Check-in cổng; null = chưa đến |

**SessionInterest** — phiên quan tâm (P1)
| Cột | Kiểu | Mô tả |
|---|---|---|
| UserId, SessionId | FK, khóa ghép | Lưu thưa. Đầu vào ma trận **W** và ước lượng quy mô |

**SessionCheckIn** — vào phòng từng phiên (P2)
| Cột | Kiểu | Mô tả |
|---|---|---|
| SessionId, RegistrationId | FK, khóa ghép | Đếm "96/100", cảnh báo sắp đầy; so với quy mô ước lượng → chỉ số (9). Thời điểm = CreatedAt |

---

## 6. Dịch vụ đi kèm (P2 — 4 bảng)

**SessionEquipmentRequest** — diễn giả yêu cầu thiết bị
| Cột | Kiểu | Mô tả |
|---|---|---|
| SessionId, ServiceItemId | FK | |
| Quantity | số nguyên | Số lượng cần thêm |
| Status | enum | `Pending` · `Approved` · `Rejected`; chỉ `Approved` được cộng vào tổng hợp |

**Voucher** — suất dịch vụ của từng người
| Cột | Kiểu | Mô tả |
|---|---|---|
| RegistrationId, BreakSlotId | FK | Unique (RegistrationId, BreakSlotId) |
| Code | chuỗi, unique | Chuỗi trên QR voucher |
| RedeemedAt | thời điểm, null | Đã đổi lúc nào; quét lại báo "đã sử dụng lúc 11:42". Loại suất (chay/mặn) lấy từ Registration khi quét |

**ServiceOrder** — đơn gửi nhà cung cấp
| Cột | Kiểu | Mô tả |
|---|---|---|
| ConferenceId, SupplierId | FK | SupplierId → User |
| Status | enum | `Draft` · `Sent` · `Confirmed` · `Delivered` · `Cancelled` |
| NeedsReview | logic | Bật khi lịch đổi làm số lượng thay đổi → NCC xác nhận lại |

**ServiceOrderLine** — dòng chi tiết đơn
| Cột | Kiểu | Mô tả |
|---|---|---|
| OrderId, ServiceItemId | FK | |
| ConferenceDayId | FK | Ngày sử dụng |
| BreakSlotId | FK, null | Với suất ăn: khung nghỉ nào |
| Variant | chuỗi, null | Biến thể suất ăn: "Chay", "Mặn – không hải sản"… |
| Quantity | số nguyên | Số lượng tổng hợp tự động |
| PreviousQuantity | số nguyên, null | Số lượng trước lần tính lại → hiện "3 → 2" cho NCC |
| UnitPrice | tiền | Đơn giá chốt lúc gửi (giá danh mục có thể đổi sau) |

> Tổng tiền đơn = tổng dòng, không lưu. Nhu cầu tổng hợp là phép tính, không lưu bảng.

---

## 7. Hỏi đáp, AI, đánh giá (P3 — 4 bảng)

**DocumentChunk** — đoạn tài liệu cho trợ lý AI
| Cột | Kiểu | Mô tả |
|---|---|---|
| SessionId | FK | Truy vấn luôn lọc theo phiên (gồm slide của mọi diễn giả trong phiên) |
| Content | chuỗi | Nội dung đoạn (từ slide hoặc tóm tắt) |
| Embedding | vector | So cosine với câu hỏi |

**Question**
| Cột | Kiểu | Mô tả |
|---|---|---|
| SessionId, AuthorId | FK | |
| Content | chuỗi | Nội dung |
| Status | enum | `Visible` · `Hidden` (BTC ẩn) · `Answered` |
| ClusterLabel | chuỗi, null | Nhãn nhóm (AI-2), vd "Chi phí triển khai" |
| Embedding | vector | Dùng để gom nhóm |

**QuestionVote**
| Cột | Kiểu | Mô tả |
|---|---|---|
| QuestionId, UserId | FK, khóa ghép | Mỗi người bình chọn 1 câu tối đa 1 lần; số phiếu = đếm dòng |

**Feedback**
| Cột | Kiểu | Mô tả |
|---|---|---|
| ConferenceId, UserId | FK | |
| SessionId | FK, null | Null = đánh giá cả hội nghị |
| Rating | số nguyên (1–5) | Số sao |
| Comment | chuỗi, null | Nhận xét → AI-3 tóm tắt (kết quả cache, không lưu) |

---

## 8. Hỗ trợ (P4 — 3 bảng)

| Bảng | Cột | Mô tả |
|---|---|---|
| Notification | UserId, Title, Body, ReadAt | Thông báo trong app (UC43). ReadAt null = chưa đọc |
| SystemSetting | Key, Value | Tham số QT cấu hình (UC46), vd trọng số mặc định |
| EquipmentHandover | RoomId, ServiceItemId, ExpectedQty, ActualQty, StaffId | Bàn giao thiết bị vào phòng (UC47). Lệch số → cảnh báo BTC |

## 9. Mở rộng (P5 — 6 bảng)

| Bảng | Cột | UC |
|---|---|---|
| Poll | SessionId, Question, Options (JSON), Status | UC48, UC49 |
| PollVote | PollId, UserId, OptionIndex | UC48 |
| SupplierProduct | SupplierId, Name, UnitPrice | UC52 |
| Incident | ConferenceId, RoomId, ReportedById, Description, Status | UC54 |
| StaffTask | ConferenceDayId, AssigneeId, Title, StartTime, EndTime, Status | UC55 |
| Complaint | ReporterId, Content, Status, Resolution | UC57 |

Các UC P5 khác cần thêm cột khi làm tới: UC51 thêm `ActualQuantity` vào ServiceOrderLine; UC53 thêm `Rating` vào ServiceOrder.

---

## 10. Tổng hợp

| Mức | Bảng cộng dồn |
|---|---|
| P1 | 19 |
| P1–P2 | 24 |
| P1–P3 | 28 (**27 bảng nghiệp vụ** vẽ ERD nộp môn, bỏ OutboxMessage) |
| P1–P4 | 31 |
| Đủ 57 UC | 37 |

### Giới hạn chấp nhận (mục "Hạn chế và hướng phát triển" của báo cáo)
| Giới hạn | Hướng mở rộng |
|---|---|
| Mỗi hội nghị 1 BTC; nhân viên vận hành không gắn theo hội nghị | Thêm bảng ConferenceMember |
| Mỗi khung nghỉ phục vụ 1 loại suất | Tạo 2 khung nghỉ trùng giờ, hoặc tách bảng ServiceOffering |
| Thanh toán vé nằm ngoài phạm vi | Tích hợp cổng thanh toán |
| Diễn giả phải có tài khoản trước khi được mời | Mời qua email kèm link đăng ký |

## 11. Quyết định đã chốt (2026-09-25)

| # | Nội dung |
|---|---|
| Q2 | Diễn giả không khai lịch rảnh → coi như rảnh toàn bộ, giao diện nhắc khai |
| Q3 | Một phiên có nhiều diễn giả (bảng SessionSpeaker); mỗi diễn giả tải slide riêng |
| Q5 | Phòng khai theo từng hội nghị |
| Q6 | Quan tâm gắn với User; tính W chỉ lấy đăng ký `Approved` |
| Q7 | Giữ Track (màu + chú thích trên lưới lịch) |
| Q8 | Chấp nhận 4 giới hạn ở mục 10 |
