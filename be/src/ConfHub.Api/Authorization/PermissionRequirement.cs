using Microsoft.AspNetCore.Authorization;

namespace ConfHub.Api.Authorization;

// Một "yêu cầu" trong policy: người gọi phải có quyền này. Chỉ mang dữ liệu;
// việc kiểm tra nằm ở PermissionAuthorizationHandler.
public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}
