# Tham khảo khung TD.Microservice.ServiceBase → ConfHub

> Rà ngày 2026-09-25 trên bản zip Huy gửi (3.181 tệp). Khung gốc dựa trên **fullstackhero .NET WebAPI** (Clean Architecture + CQRS), đã thêm nhiều thứ cho hệ microservice đa tenant.
> **Cách dùng:** KHÔNG copy nguyên khung. Dựng repo ConfHub sạch, **viết lại gọn** những mẫu được giữ dưới đây. Codex đọc file này khi làm T0.2–T0.4, T1.1.

## 1. Giữ lại (viết lại gọn theo mẫu của khung)

| Thành phần | Trong khung | Dùng trong ConfHub | Ghi chú |
|---|---|---|---|
| Cấu trúc solution | Domain / Application / Infrastructure / Host / Shared | Giữ nguyên 4 lớp + project `ConfHub.Scheduling` riêng | |
| `Directory.Build.props` + `.editorconfig` | Nullable, ImplicitUsings, StyleCop + Roslynator, file-scoped namespace | Giữ, **bật `TreatWarningsAsErrors=true`**, thống nhất 1 phiên bản analyzer | Khung đang để false và lệch phiên bản Roslynator |
| `BaseEntity` | Id Guid tuần tự (`NewId`) + danh sách `DomainEvents` | Giữ | Guid tuần tự giảm phân mảnh index SQL Server |
| `IAggregateRoot` + `IRepository<T>` / `IReadRepository<T>` chỉ cho aggregate | Ardalis.Specification | Giữ — khớp **DDD nhẹ** | Bỏ `IRepositoryWithEvents` (sự kiện CRUD chung chung không có nghĩa nghiệp vụ) |
| Specification + phân trang | `EntitiesByPaginationFilterSpec`, `PaginationFilter`, `PaginationResponse` | Giữ | |
| CQRS | MediatR + `ValidationBehavior` + FluentValidation (`CustomValidator`) | Giữ, **ghim MediatR 12.x** | Bản mới của MediatR chuyển sang giấy phép thương mại |
| Phân quyền theo permission | `MustHavePermission(action, resource)` + PolicyProvider + AuthorizationHandler + hằng số `TDAction`/`TDResource` | Giữ mẫu, đổi tên `Permissions.Schedule.Run`… | Quyền đưa vào claim JWT hoặc cache, không truy vấn DB mỗi request |
| Xử lý lỗi | `ExceptionMiddleware` + `NotFoundException`, `ConflictException`, `ForbiddenException` | Giữ, trả về **ProblemDetails** (chuẩn RFC 9457 của ASP.NET) | Bỏ lớp bọc `IApiResult`/`Result` — xem mục 3 |
| Event bus | **MassTransit 8.x + RabbitMQ + EF Core Transactional Outbox/Inbox**, retry + delayed redelivery | Giữ — **thay bảng OutboxMessage tự viết** | MassTransit tự tạo bảng Outbox/Inbox (bảng kỹ thuật). Ghim 8.x (bản 9 có giấy phép thương mại) |
| Checklist consumer | `.cursor/skills/event-consumer` (idempotent, retry, stateless) | Đưa vào AGENTS.md | Bỏ phần multitenancy |
| Job nền | Hangfire + `JobActivator` scoped + filter log | Giữ, **chỉ storage SQL Server** | |
| Thời gian thực | SignalR `NotificationHub` + `NotificationSender` + Redis backplane | Giữ mẫu; thêm hub Q&A theo group phiên | |
| Log | Serilog + enricher + Seq + request logging middleware | Giữ, chỉ sink Console + Seq | |
| Lưu tệp | `IMinioService` | Giữ (slide) | |
| Cache | `ICacheService` (Redis / memory) + `CacheKeyService` | Giữ | |
| Seeder | `ICustomSeeder` + `SeederOrderAttribute` | Giữ — seed vai trò + dữ liệu mẫu "Công nghệ Xanh 2026" | |
| Audit tự động | SaveChanges tự gán `CreatedOn`/`CreatedBy` | Giữ dạng **SaveChangesInterceptor** | Bỏ bảng AuditTrail |
| Mapping | Mapster (`ProjectToType` trong spec) | Bỏ (2026-10-05) | Map tay (`FromXxx` trên DTO); danh sách dùng `Query.Select` của Specification |
| Email | MailKit + template Razor | Giữ MailKit; template đơn giản | |
| OpenAPI | NSwag + FluentValidation schema | Giữ (hoặc Swashbuckle/Scalar) | Bỏ API versioning |

## 2. Bỏ

| Bỏ | Lý do |
|---|---|
| **Multitenancy** (Finbuckle, TenantId, TenantDbContext, `[TenantIdHeader]`) | ConfHub một tenant; phức tạp nhất trong khung |
| Đa CSDL (Oracle, MySQL, PostgreSQL, SQLite, migrator từng loại, SqlKata dialect) | Chỉ SQL Server |
| **ASP.NET Core Identity đầy đủ** (7 bảng AspNet*) | ERD chỉ có User, Role, UserToken. Dùng `PasswordHasher<T>` + JWT tự phát |
| Dapper + SqlKata + TD.SqlPartial.Tool | EF Core đủ cho quy mô này |
| gRPC, Firebase, LDAP, Azure AD / Microsoft.Identity.Web, SSO | Không có UC nào dùng |
| UserType / Behavior mapping, OrganizationUnit, JobTitle, Position, Menu, AppConfig, ImportFile + các integration event đồng bộ danh mục | Nghiệp vụ của hệ khác |
| `IRepositoryWithEvents` (EntityCreated/Updated/Deleted) | Thay bằng domain event có nghĩa nghiệp vụ (`RegistrationApproved`, `ScheduleChanged`) |
| Bảng AuditTrail, soft delete toàn cục | Không có UC dùng; ERD không có cột DeletedOn |
| OrchardCore localization (.po) | BE trả mã lỗi, FE dịch bằng react-i18next |
| ClosedXML, RestSharp, HtmlAgilityPack, UAParser, Figgle, FlexLabs Upsert, System.Linq.Dynamic, FluentStorage, Newtonsoft.Json, SpaServices, sink Elasticsearch/MSSql/AWS | Không dùng / trùng chức năng |
| **Client Metronic** (Bootstrap + Redux-saga + Formik + react-query v3 + nhiều thư viện biểu đồ) | Theo PROJECT.md: Vite + antd v5 + TanStack Query v5 + react-i18next. Chỉ tham khảo ý tưởng interceptor refresh token trong `setup/axios`, danh sách endpoint tập trung `services/endpoints.tsx` |

## 3. Quy tắc chất lượng code (rút ra từ lỗi thấy trong khung)

| Thấy trong khung | ConfHub làm thế nào |
|---|---|
| `JobPosting` constructor 26 tham số, mọi property `private set` nhưng không có hành vi (entity thiếu máu) | Aggregate có **factory method + phương thức nghiệp vụ** (`Registration.Approve()`, `ScheduleVersion.Publish()`), kiểm tra bất biến bên trong. Nhóm tham số thành **value object** (`TimeRange`, `Money`) |
| `Status` là chuỗi `"Draft"` | Enum, lưu dạng chuỗi qua EF `HasConversion<string>()` |
| Controller `Create`/`Delete` gắn `[AllowAnonymous]` | Mọi endpoint ghi dữ liệu có `[MustHavePermission]`; chỉ trang công khai mới `AllowAnonymous` |
| Vừa ném exception vừa trả `Result.Fail` → 2 đường báo lỗi | **Một đường**: lỗi nghiệp vụ = exception → middleware → ProblemDetails. Handler trả DTO trực tiếp |
| Domain event gửi **sau** khi commit qua publisher trong tiến trình → mất sự kiện nếu sập giữa chừng | Domain event → publish qua MassTransit **trước** `SaveChanges` → nằm trong outbox cùng transaction |
| `DbContext.SaveChangesAsync` tự resolve `IUserService` (service locator) | Dùng `SaveChangesInterceptor` + `ICurrentUser` inject qua constructor |
| `EnableSensitiveDataLogging()` luôn bật (TODO) | Chỉ bật ở môi trường Development |
| Nhiều khối code bị comment, comment lỗi mã hóa UTF-8 | Không để code chết; tệp UTF-8; comment tiếng Việt ngắn chỉ ở chỗ cần |
| `Microsoft.AspNetCore.Http.Features` 5.0 trong tầng Application | Application không tham chiếu gói web; mọi gói cùng dòng phiên bản với runtime |
| **Bí mật nằm trong repo**: khóa riêng Firebase, mật khẩu ứng dụng email, access/secret key MinIO trong `Configurations/*.json`; kèm `hangfire.db`, thư mục `Logs` | Bí mật chỉ ở **User Secrets / biến môi trường**; `appsettings.json` chỉ có giá trị giả; `.gitignore` chặn `*.db`, `Logs/` |

## 4. Phiên bản nền tảng

- **.NET 10 (LTS)** thay .NET 8 của khung: .NET 8 và 9 hết hỗ trợ ngày 10/11/2026, trước khi khóa luận kết thúc.
- MediatR **12.x**, MassTransit **8.x** (giữ bản mã nguồn mở, ghim phiên bản).
