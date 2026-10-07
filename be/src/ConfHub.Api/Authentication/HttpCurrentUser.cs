using ConfHub.Application.Common.Exceptions;
using ConfHub.Application.Common.Security;
using ConfHub.Infrastructure.Security;

namespace ConfHub.Api.Authentication;

// ICurrentUser đọc từ HttpContext.User — danh sách claim mà JwtBearer đã lấy ra từ access token
// sau khi kiểm chữ ký và hạn. IHttpContextAccessor cho phép lấy HttpContext của request hiện tại
// ở ngoài controller.
public sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid UserId
    {
        get
        {
            var userIdClaim = httpContextAccessor.HttpContext?.User.FindFirst(JwtClaimNames.UserId)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedException(UnauthorizedException.DefaultCode, "Authentication is required.");
            }

            return userId;
        }
    }

    public bool HasPermission(string permission)
    {
        var user = httpContextAccessor.HttpContext?.User;
        return user?.HasClaim(JwtClaimNames.Permission, permission) == true;
    }
}
