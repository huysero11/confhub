using Microsoft.AspNetCore.Authorization;

namespace ConfHub.Api.Authorization;

// [MustHavePermission("Conference.Create")] trên controller / action = phải đăng nhập VÀ vai trò có quyền đó.
// Thực chất chỉ là [Authorize(Policy = "Permission:Conference.Create")]: tên policy mang theo tên quyền,
// PermissionPolicyProvider sẽ đọc lại tên này để dựng policy.
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class MustHavePermissionAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "Permission:";

    public MustHavePermissionAttribute(string permission)
        : base(PolicyPrefix + permission)
    {
        Permission = permission;
    }

    public string Permission { get; }
}
