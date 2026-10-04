# AGENTS.md — hướng dẫn cho Codex

## Trước khi code
1. Đọc `PROJECT.md` (quyết định kiến trúc, thuật toán, UI) và file task được giao `plan/tasks/<ID>.md`.
2. **Chỉ làm đúng phạm vi file task.** Thấy cần thay đổi ngoài phạm vi → ghi vào mục "Ghi chú cho Claude" trong báo cáo, không tự làm.

## Git
- Làm trên nhánh `task/<ID>-<slug>` tạo từ `main` (file task ghi tên nhánh). Không commit thẳng vào `main`.
- **Không commit.** Để mọi thay đổi trong working tree (Huy cần thấy file màu xanh/vàng). Claude tự gắn tag bản tham khảo sau khi review.
- Không force-push, không xóa nhánh/tag.

## Code phải đơn giản, dễ đọc để người dùng gõ lại và học
- **Đơn giản nhưng không kém chất lượng**: đơn giản ở cách viết, KHÔNG cắt bớt logic, xử lý lỗi/trường hợp biên, best practice (Clean Architecture, DI, async/await đúng, validate, bảo mật) hay test.
- Cách viết thẳng, dễ hiểu hơn cách viết ngắn/"hay". `if`/`foreach` rõ ràng thay vì chuỗi LINQ dài; không lồng quá sâu.
- Không thêm lớp trừu tượng, generic, reflection, source generator, pattern khi file task không yêu cầu.
- Dùng pattern/cơ chế chuẩn của .NET khi nó làm code chắc hơn (Options pattern cho nhóm cấu hình + `ValidateOnStart`, `GetConnectionString` cho chuỗi kết nối); comment ghi **tên pattern + mục đích**.
- Analyzer ép cách viết phức tạp (chủ yếu luật tối ưu hiệu năng nhỏ) → **dừng lại, ghi vào báo cáo** cho Claude quyết định; không tự làm code rối để né luật.
- File nhỏ, một trách nhiệm. Tên rõ nghĩa, tiếng Anh; tên hàm nói đúng việc nó làm (`MapExceptionToProblem`, `BuildProblem`). Không dùng overload (cùng tên, khác tham số) cho hai việc khác nhau.
- Đặt tên tiếng Anh đúng ngữ pháp: cụm danh từ thì danh từ chính đứng cuối (`VerificationEmail` = email để xác thực; `EmailVerification` = việc xác thực email). Domain event thì quá khứ theo nghiệp vụ (`UserRegistered`, `EmailVerificationRequested`); Command/Message là động từ mệnh lệnh (`SendVerificationEmailMessage`); bool dùng `Is/Can/Has/Requires`; biến lambda viết đủ tên (`user =>`, không `u =>`). Chi tiết: `plan/tasks/T1.1.md` mục 9.
- Comment tiếng Việt ngắn ở chỗ có logic không hiển nhiên (thuật toán, ràng buộc, lý do thiết kế). Không comment thừa.
- Không thêm thư viện ngoài danh sách trong PROJECT.md mục 8 trừ khi file task cho phép.

## Backend (.NET)
- Clean Architecture: `Domain` ← `Application` ← `Infrastructure`, `Api`. Domain không tham chiếu gì.
- CQRS bằng MediatR: mỗi use case = 1 thư mục chứa Command/Query + Handler + Validator (FluentValidation).
- Quyền: mỗi endpoint gắn một permission (vd `Schedule.Run`), không kiểm tra theo tên vai trò.
- Thuật toán xếp lịch nằm trong project riêng `ConfHub.Scheduling` (thuần C#, không phụ thuộc EF/ASP.NET).
- Chuỗi hiển thị nội dung người dùng nhập có 2 cột `_vi` / `_en` khi file task yêu cầu.

### Quy tắc chất lượng backend (chi tiết: `docs/design/base-reference.md`)
- .NET 10, `Nullable` bật, `TreatWarningsAsErrors=true`, StyleCop + Roslynator, file-scoped namespace.
- Aggregate: constructor private + factory method + phương thức nghiệp vụ kiểm tra bất biến; không public setter. Trạng thái là enum (lưu chuỗi). Value object cho khái niệm dùng lại (`TimeRange`).
- Repository chỉ cho aggregate root (Ardalis.Specification). Truy vấn danh sách dùng Specification + Mapster `ProjectToType`.
- Lỗi: ném exception nghiệp vụ (`NotFoundException`, `ConflictException`…) → middleware → ProblemDetails. Handler trả DTO, không bọc Result.
- Đăng ký DI: `DependencyInjection.cs` của mỗi tầng là "mục lục"; mục có từ 2 dòng đăng ký trở lên tách thành `<Thư mục>/<Tên mục>ServiceRegistration.cs` (`internal static`, hàm `Add<Tên mục>(configuration)`), mục 1 dòng viết thẳng ở file gốc. Route controller ghi tường minh, chữ thường, gạch nối (`[Route("api/auth")]`), không dùng `[controller]`.
- Mọi endpoint ghi dữ liệu có `[MustHavePermission]`; `AllowAnonymous` chỉ cho trang công khai.
- Sự kiện ra ngoài: publish qua MassTransit trong cùng transaction (EF Outbox). Consumer phải idempotent, không giữ trạng thái trong bộ nhớ, có retry.
- Không code bị comment, không bí mật trong repo (dùng User Secrets / biến môi trường), `EnableSensitiveDataLogging` chỉ ở Development.
- Ghim MediatR 12.x, MassTransit 8.x.

## Frontend (React + TS)
- Ant Design v5, **không Tailwind/Bootstrap**. Không gõ số pixel/màu trực tiếp: dùng token antd (`theme.useToken()`) hoặc biến CSS đã khai.
- Mọi chữ hiển thị qua `react-i18next` (`vi.json`/`en.json`), không hardcode.
- Gọi API qua TanStack Query.

## Làm tăng dần — không làm trước cho task sau
- Chỉ tạo bảng/cột mà file task liệt kê. Thêm bảng/cột = migration mới tên `<ID>_<MoTa>`, dấu `.` trong mã task đổi thành `_` (vd `T1_1_Users`); không sửa migration đã có.
- Chỉ đăng ký service DI / cấu hình / container docker-compose khi task này dùng đến. Không thêm package, cấu hình, thư mục "để sẵn".
- FE chỉ tạo route/màn hình của task; không dựng khung màn hình rỗng cho nhóm sau.

## Kiểm tra bắt buộc trước khi báo xong
- Backend: `dotnet build` và `dotnet test` pass.
- Frontend: `npm run lint`, `npm run typecheck` (hoặc `tsc --noEmit`), `npm run build` pass; có test thì `npm test` pass.
- Chạy đúng các lệnh ghi trong mục "Cách kiểm tra" của file task.

## Báo cáo khi xong (trả lời cuối)
1. Danh sách file tạo/sửa.
2. Lệnh đã chạy + kết quả (dán phần tóm tắt pass/fail).
3. Cách chạy và test thủ công trên giao diện/API.
4. Ghi chú cho Claude: giả định đã đặt, việc chưa làm, rủi ro.
5. **Thứ tự gõ lại (bản nháp), chia nhóm theo use case:** liệt kê MỌI file đã tạo/sửa; mỗi nhóm là 1 use case đi trọn luồng qua các lớp (Application → Infrastructure → Api; FE: types → api → hook → component → page → route), trong nhóm xếp **từ chỗ gọi xuống chỗ được gọi** (điểm vào → Command → Handler → thứ handler dùng → cài đặt Infrastructure; file trên tạm báo đỏ là chấp nhận được), file dùng chung đặt ở use case đầu tiên cần nó; Domain có thể là nhóm đầu; test ở cuối. Mỗi file 1 dòng: đường dẫn · vai trò · điểm chính cần hiểu. Đánh dấu điểm dừng có thể build/test giữa chừng.
