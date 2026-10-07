namespace ConfHub.Application.Common.Security;

// Người đang gọi request hiện tại, đọc từ access token (cài đặt: HttpCurrentUser ở Api).
// Chỉ trả lời "ai đang gọi" — không truy vấn CSDL. Cần dữ liệu của User thì đọc qua repository
// bằng UserId. Use case chưa đăng nhập và consumer chạy nền không có người gọi → không dùng.
public interface ICurrentUser
{
    // Chưa đăng nhập mà đọc → UnauthorizedException (401).
    Guid UserId { get; }

    // Cho handler cần rẽ nhánh theo quyền. Chặn cả endpoint thì dùng [MustHavePermission].
    bool HasPermission(string permission);
}
