using ConfHub.Domain.Accounts;

namespace ConfHub.Application.Accounts;

// Là một phần trong kết quả trả về khi login / refresh, là thông tin của user
public sealed record CurrentUserResponse(
    Guid Id,
    string Email,
    string FullName,
    string Role,
    string? Organization,
    IReadOnlyList<string> Permissions)
{
    // Ở login handler có đoạn lấy user từ email, thì sẽ truyền nó vào hàm này
    // Cần user.Role đã được nạp (spec có Include).
    public static CurrentUserResponse FromUser(User user)
    {
        return new CurrentUserResponse(
            user.Id,
            user.Email,
            user.FullName,
            user.Role.Code,
            user.Organization,
            user.Role.Permissions);
    }
}
