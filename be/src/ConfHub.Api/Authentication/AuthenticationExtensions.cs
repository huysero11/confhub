using System.Text;
using ConfHub.Api.ErrorHandling;
using ConfHub.Application.Common.Exceptions;
using ConfHub.Application.Common.Security;
using ConfHub.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ConfHub.Api.Authentication;

// Xác thực (authentication) = trả lời "request này của ai". Mỗi request kèm header
// "Authorization: Bearer <access token>"; JwtBearer kiểm chữ ký + hạn rồi đổ các claim vào HttpContext.User.
public static class AuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        // Cấu hình JwtBearer cần JwtOptions và TimeProvider từ DI → dùng AddOptions().Configure<...>
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>, TimeProvider>(ConfigureJwtBearer);

        // ở controller thì có sẵn HttpContext nhưng chỗ khác thì không có sẵn, nên cần context accessor để dùng httpcontext
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();

        return services;
    }

    private static void ConfigureJwtBearer(JwtBearerOptions bearerOptions, IOptions<JwtOptions> jwtOptionsAccessor, TimeProvider timeProvider)
    {
        var jwtOptions = jwtOptionsAccessor.Value;

        // Giữ nguyên tên claim trong token ("sub", "role"...), không đổi sang tên dài kiểu cũ của .NET.
        // Thiếu dòng này thì HttpCurrentUser không tìm thấy claim "sub" → /me luôn 401.
        bearerOptions.MapInboundClaims = false;

        bearerOptions.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
            LifetimeValidator = (notBefore, expires, _, _) => IsWithinLifetime(notBefore, expires, timeProvider),
            NameClaimType = JwtClaimNames.FullName,
            RoleClaimType = JwtClaimNames.Role
        };

        // JwtBearer tự trả 401 / 403 rỗng (không ném exception) → ghi lại theo định dạng lỗi chung.
        bearerOptions.Events = new JwtBearerEvents
        {
            // Thiếu token, token sai chữ ký hoặc hết hạn.
            OnChallenge = context =>
            {
                context.HandleResponse();
                return ProblemResponseWriter.WriteAsync(
                    context.HttpContext,
                    StatusCodes.Status401Unauthorized,
                    UnauthorizedException.DefaultCode,
                    "Authentication is required.",
                    context.HttpContext.RequestAborted);
            },

            // Token hợp lệ nhưng thiếu quyền.
            OnForbidden = context =>
            {
                return ProblemResponseWriter.WriteAsync(
                    context.HttpContext,
                    StatusCodes.Status403Forbidden,
                    ForbiddenException.DefaultCode,
                    "You do not have permission to perform this action.",
                    context.HttpContext.RequestAborted);
            },
        };
    }

    private static bool IsWithinLifetime(DateTime? notBefore, DateTime? expires, TimeProvider timeProvider)
    {
        if (expires is null)
        {
            return false;
        }

        var now = timeProvider.GetUtcNow().UtcDateTime;
        if (notBefore is not null && now < notBefore)
        {
            return false;
        }

        return now < expires;
    }
}
