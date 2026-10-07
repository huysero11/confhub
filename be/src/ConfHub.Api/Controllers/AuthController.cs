using ConfHub.Api.Authentication;
using ConfHub.Api.RateLimiting;
using ConfHub.Application.Accounts;
using ConfHub.Application.Accounts.ForgotPassword;
using ConfHub.Application.Accounts.Login;
using ConfHub.Application.Accounts.Logout;
using ConfHub.Application.Accounts.RefreshSession;
using ConfHub.Application.Accounts.Register;
using ConfHub.Application.Accounts.ResendVerificationEmail;
using ConfHub.Application.Accounts.ResetPassword;
using ConfHub.Application.Accounts.VerifyEmail;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ConfHub.Api.Controllers;

// Tài khoản (UC01, UC02): các endpoint KHÔNG cần access token. Controller mỏng: nhận request → gửi Command qua MediatR → trả status.
// Lỗi (validation, trùng email, token sai) do handler ném exception, middleware đổi thành ProblemDetails.
[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public sealed class AuthController(ISender sender) : ControllerBase
{
    [HttpPost("register")]
    [EnableRateLimiting(RateLimitPolicies.Register)]
    [ProducesResponseType<RegisterResponse>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Register(RegisterCommand command, CancellationToken cancellationToken)
    {
        var response = await sender.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPost("verify-email")]
    [ProducesResponseType<VerifyEmailResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> VerifyEmail(VerifyEmailCommand command, CancellationToken cancellationToken)
    {
        var response = await sender.Send(command, cancellationToken);
        return Ok(response);
    }

    // Luôn trả 202 dù email có hay không (BR09).
    [HttpPost("resend-verification")]
    [EnableRateLimiting(RateLimitPolicies.Email)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> ResendVerification(
        ResendVerificationEmailCommand command,
        CancellationToken cancellationToken)
    {
        await sender.Send(command, cancellationToken);
        return Accepted();
    }

    // Luôn trả 202 dù email có hay không (BR09).
    [HttpPost("forgot-password")]
    [EnableRateLimiting(RateLimitPolicies.Email)]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordCommand command, CancellationToken cancellationToken)
    {
        await sender.Send(command, cancellationToken);
        return Accepted();
    }

    [HttpPost("reset-password")]
    [EnableRateLimiting(RateLimitPolicies.ResetPassword)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ResetPassword(ResetPasswordCommand command, CancellationToken cancellationToken)
    {
        await sender.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPost("login")]
    [EnableRateLimiting(RateLimitPolicies.Login)]
    [ProducesResponseType<SessionResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Login(LoginCommand command, CancellationToken cancellationToken)
    {
        var session = await sender.Send(command, cancellationToken);
        return SessionOk(session);
    }

    // Không có body: refresh token nằm trong cookie, trình duyệt tự gửi kèm.
    [HttpPost("refresh")]
    [ProducesResponseType<SessionResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var command = new RefreshSessionCommand(RefreshTokenCookie.Read(Request));
        var session = await sender.Send(command, cancellationToken);
        return SessionOk(session);
    }

    // Không cần access token: phiên hết hạn rồi vẫn đăng xuất được. Luôn trả 204 và xóa cookie.
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var command = new LogoutCommand(RefreshTokenCookie.Read(Request));
        await sender.Send(command, cancellationToken);

        RefreshTokenCookie.Delete(Response);
        return NoContent();
    }

    // Refresh token vào cookie HttpOnly; body chỉ có access token + thông tin người dùng.
    private OkObjectResult SessionOk(SessionResult session)
    {
        RefreshTokenCookie.Append(Response, session.RefreshToken, session.RefreshTokenExpiresAt);
        return Ok(new SessionResponse(session.AccessToken, session.AccessTokenExpiresInSeconds, session.User));
    }
}
