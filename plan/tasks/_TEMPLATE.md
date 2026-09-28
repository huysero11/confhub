# <ID> — <Tên task>

- **Nhóm:** G?
- **Nhánh:** `task/<ID>-<slug>` (tạo từ `main`)
- **UC liên quan:** (số UC trong PROJECT.md mục 3)
- **Phụ thuộc:** 

## 1. Mục tiêu
Một đoạn: sau task này hệ thống làm được gì, người dùng thấy gì.

## 2. Phạm vi
**Làm:**
- 

**Không làm (để task khác):**
- 

## 3. Thiết kế
- Tài liệu nhóm tham chiếu: `docs/specs/<nhóm>.md`, `docs/ui/<nhóm>.html`
- **CSDL — chỉ bảng/cột task này cần** (migration `<ID>_<MoTa>`; không sửa migration cũ):
- **Hạ tầng mới đăng ký ở task này** (DI, container compose) — chỉ khi task dùng đến:
- Entity / bảng / cột thay đổi:
- API (method, route, permission, request/response):
- Luồng xử lý / quy tắc nghiệp vụ:
- Màn hình (shell nào, route nào, thành phần antd chính):

## 4. Danh sách file dự kiến
| File | Vai trò |
|------|---------|

## 5. Test phải viết
- 

## 6. Cách kiểm tra (Codex chạy, Claude chạy lại)
```bash
```

## 7. Kiểm thử thủ công cho Huy (trên giao diện)
1. 

## 8. Tiêu chí hoàn thành
- [ ] build + test pass
- [ ] 

## 9. Hướng dẫn gõ lại (Claude điền sau khi review xong, trước khi gắn tag ref/<ID>)

**Luồng tổng:** 2–4 câu mô tả request đi qua những lớp nào (vd: Controller → Command → Handler → Aggregate → DbContext → Outbox).

Gõ theo thứ tự dưới đây (từ trong ra ngoài: Domain → Application → Infrastructure → Api → Test; FE: types → api → hook → component → page → route). Sau mỗi **điểm dừng** chạy lệnh kiểm tra để chắc phần vừa gõ đúng rồi mới đi tiếp.

| #   | File                        | Vai trò | Điểm chính cần hiểu | Gõ / Đọc |
|-----|-----------------------------|---------|---------------------|----------|
| 1   | `be/src/ConfHub.Domain/...` |         |                     | Gõ       |
| …   |                             |         |                     |          |

**Điểm dừng:**
- Sau bước …: `dotnet build` pass
- Sau bước …: `dotnet test --filter ...` pass
- Cuối cùng: `git diff ref/<ID> --stat` chỉ còn khác biệt có chủ ý

Cột **Gõ / Đọc**: `Gõ` = phần cốt lõi, nên tự gõ; `Đọc` = cấu hình/boilerplate lặp lại, đọc hiểu là đủ (có thể copy).
