namespace ConfHub.Infrastructure.Security;

// Tên các claim trong access token. Nơi ghi (JwtAccessTokenGenerator) và nơi đọc
// (HttpCurrentUser, PermissionAuthorizationHandler ở Api) dùng chung để không lệch tên.
public static class JwtClaimNames
{
    public const string UserId = "sub";
    public const string Email = "email";
    public const string FullName = "name";
    public const string Role = "role";

    // Mỗi quyền của vai trò là 1 claim "permission".
    public const string Permission = "permission";

    // Mã riêng của từng token.
    public const string TokenId = "jti";
}
