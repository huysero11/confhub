using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace ConfHub.Api.Authorization;

// ASP.NET Core hỏi provider này mỗi khi gặp [Authorize(Policy = "...")].
// Bình thường mỗi policy phải đăng ký trước bằng tay (AddPolicy) — mỗi quyền 1 dòng.
// Provider này dựng policy ngay lúc được hỏi, dựa vào tên: "Permission:X" → policy "phải có quyền X",
// nên thêm quyền mới không phải đăng ký gì thêm.
public sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : IAuthorizationPolicyProvider
{
    // Provider mặc định của ASP.NET Core: lo các policy không phải của mình.
    private readonly DefaultAuthorizationPolicyProvider _defaultProvider = new(options);

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith(MustHavePermissionAttribute.PolicyPrefix, StringComparison.Ordinal))
        {
            return _defaultProvider.GetPolicyAsync(policyName);
        }

        var permission = policyName[MustHavePermissionAttribute.PolicyPrefix.Length..];
        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(permission))
            .Build();

        return Task.FromResult<AuthorizationPolicy?>(policy);
    }

    // [Authorize] không ghi policy.
    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
    {
        return _defaultProvider.GetDefaultPolicyAsync();
    }

    // Endpoint không gắn attribute nào (xem AuthorizationExtensions).
    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
    {
        return _defaultProvider.GetFallbackPolicyAsync();
    }
}
