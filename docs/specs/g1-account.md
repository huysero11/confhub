# G1 — Tài khoản: đặc tả UC01, UC02 (D7)

> Chốt thiết kế nhóm G1 trước khi code T1.1, T1.4, T1.2, T1.3.
> Nguồn: `docs/design/use-cases.md`, `docs/design/erd.md` mục 1, `PROJECT.md` mục 8 (quy tắc tài khoản).
> Biểu đồ: `docs/diagrams/g1-*.puml` · Giao diện: `docs/ui/g1-account.html`.

## 1. Phạm vi

| UC   | Tên                   | Luồng con                                 | Task       |
|------|-----------------------|-------------------------------------------|------------|
| UC01 | Đăng ký tài khoản     | Đăng ký · Xác thực email · Gửi lại email  | T1.1, T1.2 |
| UC02 | Đăng nhập / đăng xuất | Đăng nhập · Làm mới · Đăng xuất · Quên MK | T1.4, T1.2 |

- "Làm mới phiên" không phải thao tác người dùng: giao diện tự gọi khi access token hết hạn hoặc khi mở lại trang.
- **Ngoài G1:** hồ sơ cá nhân, đổi mật khẩu khi đang đăng nhập (UC41) · QT duyệt / khóa tài khoản (UC20, T6.1) · QT quản lý vai trò và quyền (UC45).

## 2. Dữ liệu

Dùng 3 bảng của `erd.md` mục 1, không thêm bảng. Thay đổi duy nhất: `User.Organization` cho phép null.

| Bảng      | Cột dùng ở G1                                               | Ghi chú     |
|-----------|-------------------------------------------------------------|-------------|
| User      | Email, PasswordHash, FullName, RoleId, Organization, Status | Bio: UC41   |
| Role      | Code, Name, Permissions                                     | Seed 5 dòng |
| UserToken | UserId, Purpose, TokenHash, ExpiresAt, UsedAt               | 3 mục đích  |

**Vai trò (seed):**

| Code        | Tên                | Tự đăng ký | Sau xác thực      | Trang đầu   |
|-------------|--------------------|------------|-------------------|-------------|
| `Attendee`  | Người tham dự      | Có         | `Active`          | `/`         |
| `Organizer` | Ban tổ chức        | Có         | `PendingApproval` | `/manage`   |
| `Supplier`  | Nhà cung cấp       | Có         | `PendingApproval` | `/supplier` |
| `Staff`     | Nhân viên vận hành | Không      | –                 | `/onsite`   |
| `Admin`     | Quản trị viên      | Không      | –                 | `/admin`    |

- `Staff`, `Admin` do QT tạo (UC20). Tới khi có UC20, môi trường dev seed sẵn 1 tài khoản `Active` cho mỗi vai trò (T1.4).
- Diễn giả không phải vai trò: người tham dự (hoặc vai trò khác) được BTC mời vào phiên.
- Danh mục quyền là hằng số trong code; mỗi nhóm chức năng thêm quyền của mình. G1 chưa có quyền nghiệp vụ nào — T1.4 chỉ dựng cơ chế và test bằng quyền mẫu.

**Trạng thái tài khoản** (`g1-trang-thai-tai-khoan.puml`):

| Từ                | Sự kiện                               | Sang              |
|-------------------|---------------------------------------|-------------------|
| (mới)             | Đăng ký (UC01)                        | `Unverified`      |
| `Unverified`      | Xác thực email, vai trò Người tham dự | `Active`          |
| `Unverified`      | Xác thực email, vai trò BTC / NCC     | `PendingApproval` |
| `PendingApproval` | QT duyệt (UC20)                       | `Active`          |
| `PendingApproval` | QT từ chối (UC20)                     | `Locked`          |
| `Active`          | QT khóa (UC20)                        | `Locked`          |
| `Locked`          | QT mở khóa (UC20)                     | `Active`          |

**Mục đích token (`UserToken.Purpose`):**

| Purpose         | Hạn     | Tạo khi                | Hết hiệu lực khi                 |
|-----------------|---------|------------------------|----------------------------------|
| `VerifyEmail`   | 24 giờ  | Đăng ký, gửi lại email | Dùng xong · có token mới hơn     |
| `ResetPassword` | 30 phút | Quên mật khẩu          | Dùng xong · có token mới hơn     |
| `Refresh`       | 7 ngày  | Đăng nhập, làm mới     | Làm mới · đăng xuất · đặt lại MK |

## 3. Quy tắc nghiệp vụ

- **BR01 — Email:** bắt buộc, đúng định dạng, ≤ 256 ký tự; cắt khoảng trắng và đổi về chữ thường trước khi lưu/so sánh; duy nhất trong hệ thống.
- **BR02 — Mật khẩu:** 8–128 ký tự, có ít nhất 1 chữ cái và 1 chữ số. Chỉ lưu bản băm (`PasswordHasher<T>`, PBKDF2).
- **BR03 — Họ tên:** bắt buộc, 2–100 ký tự sau khi cắt khoảng trắng.
- **BR04 — Đơn vị (Organization):** ≤ 200 ký tự; **bắt buộc** với Ban tổ chức và Nhà cung cấp (tên cơ quan / công ty), tùy chọn với Người tham dự.
- **BR05 — Vai trò khi đăng ký:** chỉ nhận `Attendee`, `Organizer`, `Supplier`. Gửi vai trò khác → lỗi 400 (không tin giao diện).
- **BR06 — Token một lần:** token trong link email là chuỗi ngẫu nhiên 32 byte (Base64Url); DB chỉ lưu SHA-256 của nó. Token đã dùng, hết hạn hoặc bị thay bằng token mới → không hợp lệ.
- **BR07 — Token không đi qua outbox:** sự kiện chỉ mang `UserId`; consumer gửi mail mới sinh token, lưu băm rồi gửi. Chuỗi token gốc chỉ nằm trong email, không nằm trong CSDL, bảng outbox hay hàng đợi RabbitMQ (lý do và cách xử lý lỗi: mục 7).
- **BR08 — Gửi lại email:** cách lần gửi trước (cùng mục đích) ít nhất 60 giây. Sớm hơn thì consumer bỏ qua, không gửi; API vẫn trả cùng thông báo như BR09 (báo "vui lòng chờ" sẽ lộ email có tài khoản). Giao diện khóa nút 60 giây.
- **BR09 — Không lộ email tồn tại** ở chức năng gửi lại email xác thực và quên mật khẩu: luôn trả cùng một thông báo dù email có hay không. (Riêng đăng ký phải báo "email đã được sử dụng" — chấp nhận, đã có rate limit.)
- **BR10 — Đăng nhập:** chỉ tài khoản `Active` được cấp token. Sai email hoặc sai mật khẩu → cùng một thông báo. Kiểm tra trạng thái **sau** khi mật khẩu đúng (người lạ không dò được trạng thái).
- **BR11 — Token phiên:** access token JWT 15 phút (claim: `sub`, `email`, `name`, `role`, `permission` × n), giao diện giữ trong bộ nhớ. Refresh token 7 ngày trong cookie `confhub_refresh` (`HttpOnly`, `Secure`, `SameSite=Strict`, `Path=/api/auth`).
- **BR12 — Xoay vòng refresh token:** mỗi lần làm mới, token cũ bị đánh dấu dùng và cấp token mới. Token **đã dùng** mà bị gửi lại (dấu hiệu bị đánh cắp) → thu hồi mọi refresh token của người đó, bắt đăng nhập lại.
- **BR13 — Đăng xuất:** thu hồi refresh token hiện tại và xóa cookie. Access token đã cấp còn dùng được tới khi hết hạn (tối đa 15 phút) — giới hạn chấp nhận.
- **BR14 — Đặt lại mật khẩu** thành công → thu hồi mọi refresh token (đăng xuất khỏi mọi thiết bị).
- **BR15 — Rate limit** theo IP (bộ giới hạn có sẵn của ASP.NET Core): đăng nhập 5 lần/phút; đăng ký 5 lần/phút; gửi lại email xác thực và quên mật khẩu 3 lần/phút. Vượt → 429. **Không khóa tài khoản** khi nhập sai nhiều lần.

## 4. UC01 — Đăng ký tài khoản

| Mục            | Nội dung                                                   |
|----------------|------------------------------------------------------------|
| Tác nhân       | Người dùng (chưa có tài khoản)                             |
| Mục tiêu       | Tạo tài khoản Người tham dự, Ban tổ chức hoặc Nhà cung cấp |
| Tiền điều kiện | Chưa đăng nhập                                             |
| Hậu điều kiện  | Tài khoản `Unverified`; email xác thực đã được gửi         |
| Kích hoạt      | Bấm "Đăng ký" trên header hoặc trang đăng nhập             |

**Luồng chính — Đăng ký:**
1. Người dùng mở trang `/register`.
2. Chọn vai trò (mặc định Người tham dự); chọn BTC / NCC thì hiện ô "Đơn vị" bắt buộc và ghi chú "cần quản trị viên duyệt".
3. Nhập họ tên, email, mật khẩu, nhập lại mật khẩu, (đơn vị) rồi bấm "Đăng ký".
4. Giao diện kiểm tra BR01–BR04 và mật khẩu nhập lại khớp; lỗi hiện ngay dưới ô.
5. Hệ thống kiểm tra lại BR01–BR05, kiểm tra email chưa tồn tại.
6. Hệ thống băm mật khẩu, tạo User `Unverified`, ghi sự kiện gửi email xác thực vào outbox **cùng giao dịch**.
7. Giao diện chuyển sang trang "Kiểm tra email" (`/register/check-email`) hiển thị email vừa đăng ký.
8. Phía sau: consumer sinh token `VerifyEmail`, lưu bản băm, gửi email chứa link `/<frontend>/verify-email?token=...`.

**Luồng con — Xác thực email:**
1. Người dùng bấm link trong email → giao diện mở `/verify-email?token=...` và gửi token lên hệ thống.
2. Hệ thống tìm token theo bản băm, kiểm tra đúng mục đích, chưa dùng, chưa hết hạn (BR06).
3. Hệ thống đánh dấu token đã dùng; đổi trạng thái theo vai trò (bảng mục 2).
4. Giao diện báo kết quả: Người tham dự → "Xác thực thành công" + nút Đăng nhập; BTC / NCC → "Đã xác thực, tài khoản đang chờ quản trị viên duyệt".

**Luồng con — Gửi lại email xác thực:**
1. Ở trang "Kiểm tra email" (hoặc khi đăng nhập bị báo chưa xác thực), người dùng bấm "Gửi lại email".
2. Nếu tài khoản tồn tại và `Unverified` thì hệ thống ghi sự kiện gửi email xác thực; consumer áp BR08 và làm token cũ hết hiệu lực khi tạo token mới.
3. Luôn trả "Nếu email chưa xác thực, chúng tôi đã gửi lại liên kết" (BR09). Nút bị khóa 60 giây, có đếm ngược.

**Ngoại lệ:**
- **E1** — Dữ liệu sai quy tắc → 400 `Validation`, kèm mã lỗi theo từng ô.
- **E2** — Email đã tồn tại → 409 `EmailTaken` "Email đã được sử dụng", gợi ý Đăng nhập / Quên mật khẩu.
- **E3** — Token xác thực không hợp lệ / hết hạn / đã dùng → 422 `TokenInvalid`; giao diện hiện "Liên kết không còn hiệu lực" + ô nhập email để gửi lại.
- **E4** — Tài khoản đã xác thực trước đó mà bấm lại link → token đã dùng → như E3 (không lộ trạng thái).
- **E5** — Gửi mail lỗi (SMTP chết) → consumer thử lại theo chính sách retry của MassTransit; đăng ký vẫn thành công, người dùng có thể bấm "Gửi lại".
- **E6** — Vượt rate limit → 429 "Thao tác quá nhanh, thử lại sau".

## 5. UC02 — Đăng nhập / đăng xuất

| Mục            | Nội dung                                                |
|----------------|---------------------------------------------------------|
| Tác nhân       | Người dùng (cả 6 tác nhân kế thừa)                      |
| Mục tiêu       | Vào hệ thống đúng quyền của vai trò; thoát an toàn      |
| Tiền điều kiện | Đã có tài khoản                                         |
| Hậu điều kiện  | Có cặp token (đăng nhập) / token bị thu hồi (đăng xuất) |
| Kích hoạt      | Bấm Đăng nhập / Đăng xuất, hoặc mở trang cần đăng nhập  |

**Luồng chính — Đăng nhập:**
1. Người dùng mở `/login` (hoặc bị chuyển tới kèm `returnUrl` khi vào trang cần đăng nhập).
2. Nhập email, mật khẩu, bấm "Đăng nhập".
3. Hệ thống tìm User theo email, so mật khẩu với bản băm (BR10).
4. Hệ thống kiểm tra `Status = Active`.
5. Hệ thống tạo access token (kèm danh sách quyền của vai trò) và refresh token; lưu băm refresh token; trả access token + thông tin người dùng trong body, refresh token trong cookie (BR11).
6. Giao diện lưu access token trong bộ nhớ, chuyển tới `returnUrl` nếu có quyền vào, ngược lại tới trang mặc định của vai trò (bảng mục 2).

**Luồng con — Làm mới phiên (hệ thống tự làm):**
1. Khi mở lại trang, hoặc khi API trả 401 vì access token hết hạn, giao diện gọi làm mới (trình duyệt tự gửi cookie).
2. Hệ thống kiểm tra refresh token: tồn tại, chưa dùng, chưa hết hạn, User còn `Active`.
3. Hệ thống đánh dấu token cũ đã dùng, cấp cặp token mới (BR12); giao diện gửi lại request vừa bị 401.
4. Làm mới thất bại → giao diện xóa phiên, chuyển về `/login?returnUrl=...`.

**Luồng con — Đăng xuất:**
1. Người dùng bấm "Đăng xuất" ở menu tài khoản.
2. Hệ thống thu hồi refresh token trong cookie, trả lệnh xóa cookie (BR13).
3. Giao diện xóa access token, xóa cache dữ liệu, về trang chủ.

**Luồng con — Quên mật khẩu:**
1. Ở `/login` bấm "Quên mật khẩu?" → `/forgot-password`, nhập email.
2. Hệ thống (BR08, BR09): `Active` / `PendingApproval` → ghi sự kiện gửi email đặt lại mật khẩu; `Unverified` → gửi lại email xác thực thay thế; `Locked` hoặc không tồn tại → không gửi gì.
3. Luôn trả "Nếu email tồn tại, chúng tôi đã gửi hướng dẫn".
4. Người dùng bấm link → `/reset-password?token=...`, nhập mật khẩu mới (BR02) hai lần.
5. Hệ thống kiểm tra token (BR06), đổi mật khẩu, đánh dấu token đã dùng, thu hồi mọi refresh token (BR14).
6. Giao diện báo thành công, chuyển tới `/login`.

**Ngoại lệ:**
- **E1** — Sai email hoặc mật khẩu → 401 "Email hoặc mật khẩu không đúng".
- **E2** — `Unverified` → 403 mã `AccountUnverified`: "Email chưa được xác thực" + nút "Gửi lại email xác thực".
- **E3** — `PendingApproval` → 403 mã `AccountPending`: "Tài khoản đang chờ quản trị viên duyệt".
- **E4** — `Locked` → 403 mã `AccountLocked`: "Tài khoản đã bị khóa, liên hệ quản trị viên".
- **E5** — Refresh token đã dùng bị gửi lại → 401 + thu hồi mọi refresh token (BR12).
- **E6** — Token đặt lại mật khẩu không hợp lệ → 422 `TokenInvalid` "Liên kết không còn hiệu lực" + nút gửi lại.
- **E7** — Vượt rate limit → 429.

## 6. API

Mọi endpoint dưới `/api/auth`. Lỗi trả ProblemDetails (middleware của T0.5) có trường `code` (PascalCase, theo quy ước T0.5) để giao diện dịch. Token không hợp lệ dùng 422 vì middleware T0.5 đổi `DomainException` → 422.

| Method | Route                  | Cần đăng nhập | Rate limit | Task |
|--------|------------------------|---------------|------------|------|
| POST   | `/register`            | Không         | 5/phút     | T1.1 |
| POST   | `/verify-email`        | Không         | –          | T1.1 |
| POST   | `/resend-verification` | Không         | 3/phút     | T1.1 |
| POST   | `/forgot-password`     | Không         | 3/phút     | T1.1 |
| POST   | `/reset-password`      | Không         | 5/phút     | T1.1 |
| POST   | `/login`               | Không         | 5/phút     | T1.4 |
| POST   | `/refresh`             | Cookie        | –          | T1.4 |
| POST   | `/logout`              | Cookie        | –          | T1.4 |
| GET    | `/me`                  | Có            | –          | T1.4 |

**Request / response chính:**
- `register`: `{ fullName, email, password, role, organization? }` → `201` `{ email }`.
- `verify-email`: `{ token }` → `200` `{ status }` (`Active` hoặc `PendingApproval`).
- `resend-verification`, `forgot-password`: `{ email }` → luôn `202`.
- `reset-password`: `{ token, newPassword }` → `204`.
- `login`: `{ email, password }` → `200` `{ accessToken, expiresIn, user }` + `Set-Cookie`.
- `refresh`: (cookie) → `200` như `login`. `logout`: → `204` + xóa cookie.
- `me`: → `{ id, email, fullName, role, organization, permissions[] }`.

**Mã lỗi nghiệp vụ (`code`):**

| code                 | HTTP | Khi nào                              |
|----------------------|------|--------------------------------------|
| `Validation`         | 400  | Sai quy tắc dữ liệu (kèm lỗi từng ô) |
| `EmailTaken`         | 409  | Email đã tồn tại                     |
| `TokenInvalid`       | 422  | Token email không hợp lệ / hết hạn   |
| `InvalidCredentials` | 401  | Sai email hoặc mật khẩu              |
| `AccountUnverified`  | 403  | Chưa xác thực email                  |
| `AccountPending`     | 403  | Chờ QT duyệt                         |
| `AccountLocked`      | 403  | Bị khóa                              |
| `SessionExpired`     | 401  | Refresh token không hợp lệ           |
| `TooManyRequests`    | 429  | Vượt rate limit                      |

## 7. Sự kiện và email

| Message (RabbitMQ)              | Phát khi                  | Consumer                 |
|---------------------------------|---------------------------|--------------------------|
| `SendVerificationEmailMessage`  | Đăng ký, gửi lại, quên MK | Sinh token, lưu băm, gửi |
| `SendPasswordResetEmailMessage` | Quên MK                   | Sinh token, lưu băm, gửi |

- Aggregate `User` phát domain event (`UserRegistered`, `VerificationEmailRequested`, `PasswordResetRequested`); repository đổi chúng thành 2 message trên và publish qua outbox ngay trước `SaveChanges`.
- Quên mật khẩu: tài khoản `Unverified` nhận mail xác thực, `Active` / `PendingApproval` nhận mail đặt lại mật khẩu.
- Phát qua MassTransit EF Outbox (T0.4) cùng giao dịch với thay đổi dữ liệu; consumer có Inbox nên mỗi message chỉ xử lý một lần.
- Gửi mail bằng MailKit tới smtp4dev (môi trường dev; xem thư trên giao diện web của smtp4dev, cổng chốt ở T1.1); mỗi email có cả tiếng Việt và tiếng Anh (chưa lưu ngôn ngữ của người dùng).
- Link trong mail lấy gốc từ cấu hình `Frontend:BaseUrl`.

**Vì sao consumer sinh token (BR07)** — so với cách handler sinh token rồi gửi kèm trong message:
- Message nằm ở nhiều chỗ ngoài tầm kiểm soát: bảng `OutboxMessage` (tới khi gửi xong), hàng đợi RabbitMQ, và hàng đợi lỗi `_error` (giữ mãi nếu consumer lỗi hết số lần thử). Ai đọc được các chỗ đó sẽ có link đặt lại mật khẩu còn hạn = chiếm được tài khoản.
- Message chỉ mang `UserId` thì không còn bí mật nào phải bảo vệ ngoài email của người dùng; CSDL chỉ có bản băm (giống mật khẩu).
- Consumer đọc dữ liệu mới nhất lúc xử lý nên tự bỏ qua trường hợp đã lỗi thời (vd người dùng đã xác thực xong trước khi mail kịp gửi).

**Các bước của consumer** (cả hai message giống nhau, chỉ khác Purpose và mẫu email):
1. Đọc User theo `UserId`. Không còn tồn tại, hoặc trạng thái không còn phù hợp (VerifyEmail: khác `Unverified`; ResetPassword: khác `Active` / `PendingApproval`) → bỏ qua, không gửi.
2. Token cùng mục đích mới nhất được tạo trong vòng 60 giây → bỏ qua (BR08).
3. Đánh dấu `UsedAt` cho các token cùng mục đích còn hiệu lực (token cũ hết hiệu lực).
4. Sinh token ngẫu nhiên, lưu bản băm SHA-256 + `ExpiresAt`.
5. Gửi email chứa token gốc.

**Khi có lỗi** (consumer chạy trong transaction chung với Inbox — T1.1 phải có test chứng minh):
- Gửi mail lỗi → ném exception → transaction rollback (token bước 4 không được lưu) → MassTransit thử lại 1s/5s/15s. Hết lượt thử → message vào `_error`; người dùng bấm "Gửi lại email".
- Mail đã gửi nhưng lưu lỗi (hiếm) → message được xử lý lại → người dùng nhận 2 email, **chỉ email sau dùng được**; bấm link email trước thì gặp E3 và gửi lại được. Chấp nhận.

## 8. Biểu đồ

Thư mục: `docs/diagrams/`

| File                           | Loại       | Nội dung                             |
|--------------------------------|------------|--------------------------------------|
| `g1-trang-thai-tai-khoan.puml` | Trạng thái | Vòng đời `User.Status`               |
| `g1-hd-dang-ky.puml`           | Hoạt động  | Đăng ký, xác thực, gửi lại           |
| `g1-hd-dang-nhap.puml`         | Hoạt động  | Đăng nhập, chuyển trang theo vai trò |
| `g1-hd-quen-mat-khau.puml`     | Hoạt động  | Quên mật khẩu + đặt lại              |
| `g1-td-dang-ky.puml`           | Tuần tự    | Đăng ký → outbox → mail → xác thực   |
| `g1-td-dang-nhap.puml`         | Tuần tự    | Đăng nhập, làm mới, đăng xuất        |

## 9. Giao diện

Mockup: `docs/ui/g1-account.html` (có nút đổi sáng/tối và vi/en). Mọi màn hình thuộc **Shell B**, dạng thẻ giữa trang.

| Route                   | Màn hình         | Thành phần antd chính                   |
|-------------------------|------------------|-----------------------------------------|
| `/register`             | Đăng ký          | `Form`, `Radio.Group`, `Input.Password` |
| `/register/check-email` | Kiểm tra email   | `Result`, `Button` đếm ngược            |
| `/verify-email`         | Kết quả xác thực | `Result` (3 trạng thái)                 |
| `/login`                | Đăng nhập        | `Form`, `Alert` theo mã lỗi             |
| `/forgot-password`      | Quên mật khẩu    | `Form`, `Result`                        |
| `/reset-password`       | Đặt lại mật khẩu | `Form`, `Input.Password`                |

- Header Shell B: đã đăng nhập thì thay nút "Đăng nhập / Đăng ký" bằng avatar + menu (tên, vai trò, "Trang quản lý" nếu có, "Đăng xuất").
- Chặn route: trang cần đăng nhập → `/login?returnUrl=...`; đăng nhập rồi mà thiếu quyền → trang 403.

## 10. Giới hạn chấp nhận

- Access token đã cấp còn hiệu lực tới 15 phút sau khi đăng xuất / bị khóa (không có danh sách đen token).
- Quyền nằm trong JWT: QT đổi quyền của vai trò thì người dùng nhận quyền mới ở lần làm mới phiên kế tiếp (≤ 15 phút).
- Đăng ký báo "email đã được sử dụng" nên lộ việc email có tài khoản — đánh đổi lấy trải nghiệm, giảm bằng rate limit.
- Rate limit theo IP: nhiều người chung một mạng (NAT) có thể chạm giới hạn cùng lúc.
