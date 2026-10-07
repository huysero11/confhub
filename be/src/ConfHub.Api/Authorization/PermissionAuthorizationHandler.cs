using ConfHub.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;

namespace ConfHub.Api.Authorization;

// Kiểm tra 1 PermissionRequirement: access token có claim "permission" đúng tên quyền thì đạt.
// Quyền được ghi vào token lúc đăng nhập (lấy từ vai trò) → không phải truy vấn CSDL ở mỗi request.
// Đổi quyền của vai trò thì có hiệu lực khi token được làm mới (tối đa 15 phút).
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.HasClaim(JwtClaimNames.Permission, requirement.Permission))
        {
            context.Succeed(requirement);
        }

        // Không gọi Succeed = không đạt → 403.
        return Task.CompletedTask;
    }
}
