using ConfHub.Api.Authorization;
using ConfHub.Application.Common.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConfHub.Api.IntegrationTests.Support;

// Controller chỉ có trong test (nạp bằng AddApplicationPart) để thử cơ chế bảo vệ API:
// G1 chưa có endpoint nghiệp vụ nào cần quyền.
[ApiController]
[Route("api/test")]
public sealed class TestProbeController(ICurrentUser currentUser) : ControllerBase
{
    public const string Permission = "Test.Read";

    [HttpGet("permission")]
    [MustHavePermission(Permission)]
    public IActionResult RequiresPermission()
    {
        return Ok();
    }

    // Không gắn attribute nào → FallbackPolicy phải chặn khi chưa đăng nhập.
    [HttpGet("default")]
    public IActionResult NoAttribute()
    {
        return Ok();
    }

    [HttpGet("current-user")]
    [Authorize]
    public IActionResult CurrentUser()
    {
        return Ok(new { userId = currentUser.UserId, hasPermission = currentUser.HasPermission(Permission) });
    }
}
