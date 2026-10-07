using ConfHub.Application.Accounts;
using ConfHub.Application.Accounts.GetCurrentUser;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConfHub.Api.Controllers;

// Thông tin người đang đăng nhập. Tách khỏi AuthController vì controller đó gắn [AllowAnonymous]
// ở mức class, mà [AllowAnonymous] luôn thắng [Authorize] đặt ở action.
[ApiController]
[Route("api/auth/me")]
[Authorize]
public sealed class CurrentUserController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<CurrentUserResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var response = await sender.Send(new GetCurrentUserQuery(), cancellationToken);
        return Ok(response);
    }
}
