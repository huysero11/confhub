# ConfHub

Hệ thống quản lý hội nghị tích hợp xếp lịch tự động và trợ lý AI (khóa luận tốt nghiệp).
Quyết định thiết kế: `PROJECT.md` · Kế hoạch và tiến độ: `plan/README.md`.

## Chạy trên máy dev (lệnh cmd)

```cmd
cd /d D:\Code\my-projects\confhub
docker compose up -d
cd be
dotnet ef database update --project src\ConfHub.Infrastructure --startup-project src\ConfHub.Api
dotnet run --project src\ConfHub.Api
```

Frontend (cửa sổ cmd khác):

```cmd
cd /d D:\Code\my-projects\confhub\fe
npm install
npm run dev
```

Kiểm tra frontend: `npm run lint`, `npm run typecheck`, `npm run test`, `npm run build`
(`npm run format` để định dạng lại code).

## Địa chỉ và cổng

**Mở bằng trình duyệt:**

| Địa chỉ                        | Chương trình | Dùng để                                      |
|--------------------------------|--------------|----------------------------------------------|
| `http://localhost:5065/scalar` | API ConfHub  | Tài liệu API + gửi thử request               |
| `http://localhost:5065/health` | API ConfHub  | Kiểm tra API, CSDL, RabbitMQ còn sống        |
| `http://localhost:5000`        | smtp4dev     | Hộp thư giả: xem email API gửi ra            |
| `http://localhost:15672`       | RabbitMQ     | Trang quản lý hàng đợi (guest / guest)       |
| `http://localhost:5341`        | Seq          | Xem log của API                              |
| `http://localhost:5173`        | Frontend     | Giao diện React (từ T1.3)                    |

**Chỉ các chương trình dùng với nhau** (không mở bằng trình duyệt):

| Cổng   | Chương trình | Ai kết nối tới                               |
|--------|--------------|----------------------------------------------|
| `1433` | SQL Server   | API (chuỗi kết nối `ConfHub`)                |
| `5672` | RabbitMQ     | API gửi / nhận message                       |
| `2525` | smtp4dev     | API gửi email (cấu hình `Smtp:Port`)         |

- Container nào có cổng nào: `docker-compose.yml` (dạng `"cổng máy:cổng trong container"`).
- Cấu hình API cho máy dev: `be/src/ConfHub.Api/appsettings.Development.json` — ghi đè
  `appsettings.json` (file gốc để trống / giá trị cho máy thật).
- Cổng của chính API: `be/src/ConfHub.Api/Properties/launchSettings.json`.

## Tài khoản mẫu (chỉ máy dev)

API tự tạo khi khởi động ở môi trường Development (`DevSeed` trong `appsettings.Development.json`),
sau khi đã chạy `dotnet ef database update`. Mật khẩu chung: `Confhub@123`.

| Email                     | Vai trò            |
|---------------------------|--------------------|
| `attendee@confhub.local`  | Người tham dự      |
| `organizer@confhub.local` | Ban tổ chức        |
| `supplier@confhub.local`  | Nhà cung cấp       |
| `staff@confhub.local`     | Nhân viên vận hành |
| `admin@confhub.local`     | Quản trị viên      |

Đăng nhập: `POST /api/auth/login` trong Scalar → dán `accessToken` vào ô Bearer token để gọi các API
cần đăng nhập.
