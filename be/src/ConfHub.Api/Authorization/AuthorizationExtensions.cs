using Microsoft.AspNetCore.Authorization;

namespace ConfHub.Api.Authorization;

// Phân quyền (authorization) = trả lời "người này có được làm việc này không" (sau khi đã biết là ai).
public static class AuthorizationExtensions
{
    public static IServiceCollection AddPermissionAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // Mặc định an toàn: endpoint không gắn gì cũng phải đăng nhập.
            // Endpoint công khai phải ghi rõ [AllowAnonymous] → quên gắn thì bị chặn chứ không bị hở.
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

        return services;
    }
}
