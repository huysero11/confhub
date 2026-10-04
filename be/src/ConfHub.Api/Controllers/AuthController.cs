using ConfHub.Api.RateLimiting;
using ConfHub.Application.Accounts.ForgotPassword;
using ConfHub.Application.Accounts.Register;
using ConfHub.Application.Accounts.ResendVerificationEmail;
using ConfHub.Application.Accounts.ResetPassword;
using ConfHub.Application.Accounts.VerifyEmail;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ConfHub.Api.Controllers;

// Tài khoản (UC01, UC02). Controller mỏng: nhận request → gửi Command qua MediatR → trả status.
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
}
