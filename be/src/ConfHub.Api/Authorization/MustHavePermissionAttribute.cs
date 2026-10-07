using Microsoft.AspNetCore.Authorization;

namespace ConfHub.Api.Authorization;

/*
Class này tạo ra một Attribute để viết [MustHavePermission("Conference.Create")] thay vì [Authorize(Policy = "Permission:Conference.Create")]
*/

// Cái này nghĩa là có thể gắn trên class hoặc method, và có thể gắn nhiều cái cùng lúc
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
