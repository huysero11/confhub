using ConfHub.Domain.Accounts;

namespace ConfHub.Application.Common.Security;

// Tạo access token (JWT) cho người dùng. Cài đặt: JwtAccessTokenGenerator ở Infrastructure.
public interface IAccessTokenGenerator
{
    // Cần user.Role đã được nạp: vai trò và quyền được ghi vào token.
    AccessToken Generate(User user);
}
