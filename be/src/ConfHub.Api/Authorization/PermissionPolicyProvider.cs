using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace ConfHub.Api.Authorization;

/*
Máy tạo policy tự động cho permission
VD:
    [MustHavePermission("Conference.Create")] sẽ biến thành Permission:Conference.Create
    ASP.NET thấy policy đó và hỏi PermissionPolicyProvider: “Policy tên Permission:Conference.Create là gì?”
        Provider này sẽ tự tạo policy tương ứng.
*/
public sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : IAuthorizationPolicyProvider
{
    // provider mặc định để lấy những cái đã có sẵn, ví dụ như AdminOnly
    private readonly DefaultAuthorizationPolicyProvider _defaultProvider = new(options);

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith(MustHavePermissionAttribute.PolicyPrefix, StringComparison.Ordinal))
        {
            return _defaultProvider.GetPolicyAsync(policyName);
        }

        // Cắt bỏ prefix Permission:
        var permission = policyName[MustHavePermissionAttribute.PolicyPrefix.Length..];
        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(permission))
            .Build();

        return Task.FromResult<AuthorizationPolicy?>(policy);
    }

    // Dùng cho [Authorize], không ghi tên Policy
    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
    {
        return _defaultProvider.GetDefaultPolicyAsync();
    }

    // Là policy mặc định áp dụng cho endpoint khi không ghi [Authorize(...)], nếu ứng dụng có cấu hình fallback policy.
    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
    {
        return _defaultProvider.GetFallbackPolicyAsync();
    }
}
