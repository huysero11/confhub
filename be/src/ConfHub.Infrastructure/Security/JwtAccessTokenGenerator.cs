using System.Security.Claims;
using System.Text;
using ConfHub.Application.Common.Security;
using ConfHub.Domain.Accounts;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ConfHub.Infrastructure.Security;

// Tạo access token dạng JWT: header.payload.chữ-ký. Payload chứa các claim (ai, vai trò, quyền, hạn);
// chữ ký HMAC-SHA256 bảo đảm không ai sửa được payload nếu không có khóa bí mật.
// JWT KHÔNG mã hóa nội dung: ai cầm token cũng đọc được payload → không đưa dữ liệu nhạy cảm vào.
public sealed class JwtAccessTokenGenerator(IOptions<JwtOptions> options, TimeProvider timeProvider) : IAccessTokenGenerator
{
    public AccessToken Generate(User user)
    {
        var jwtOptions = options.Value;
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var lifetime = TimeSpan.FromMinutes(jwtOptions.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(JwtClaimNames.UserId, user.Id.ToString()),
            new(JwtClaimNames.Email, user.Email),
            new(JwtClaimNames.FullName, user.FullName),
            new(JwtClaimNames.Role, user.Role.Code),
            new(JwtClaimNames.TokenId, Guid.NewGuid().ToString()),
        };
        foreach (var permission in user.Role.Permissions)
        {
            claims.Add(new Claim(JwtClaimNames.Permission, permission));
        }

        // Tạo token
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey));
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = jwtOptions.Issuer,
            Audience = jwtOptions.Audience,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = now,
            NotBefore = now,
            Expires = now + lifetime,
            SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256)
        };

        // Việc ký giao cho thư viện (tự viết dễ sai và thiếu an toàn).
        var jwt = new JsonWebTokenHandler().CreateToken(tokenDescriptor);
        return new AccessToken(jwt, (int)lifetime.TotalSeconds);
    }
}
