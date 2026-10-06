using ConfHub.Domain.Accounts;
using ConfHub.Infrastructure.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;

namespace ConfHub.Infrastructure.IntegrationTests.Security;

public class JwtAccessTokenGeneratorTests
{
    private static readonly JwtOptions _jwtOptions = new()
    {
        Issuer = "confhub-api",
        Audience = "confhub-web",
        SigningKey = "test-signing-key-0123456789-abcdefghijkl",
        AccessTokenMinutes = 15,
    };

    [Fact]
    public void TokenCarriesUserRoleAndOneClaimPerPermission()
    {
        var role = Role.Create(Guid.NewGuid(), RoleCodes.Organizer, "Ban tổ chức");
        role.Permissions.Add("Conference.Create");
        role.Permissions.Add("Schedule.Run");
        var user = User.CreateActive("lan@gmail.com", "hash", "Trần Lan", role, "Đại học A");
        var generator = new JwtAccessTokenGenerator(Options.Create(_jwtOptions), TimeProvider.System);

        var accessToken = generator.Generate(user);

        // Đọc lại payload (không kiểm chữ ký) để xem các claim.
        var jwt = new JsonWebToken(accessToken.Value);
        Assert.Equal(900, accessToken.ExpiresInSeconds);
        Assert.Equal("confhub-api", jwt.Issuer);
        Assert.Equal(user.Id.ToString(), jwt.GetClaim(JwtClaimNames.UserId).Value);
        Assert.Equal("lan@gmail.com", jwt.GetClaim(JwtClaimNames.Email).Value);
        Assert.Equal("Trần Lan", jwt.GetClaim(JwtClaimNames.FullName).Value);
        Assert.Equal(RoleCodes.Organizer, jwt.GetClaim(JwtClaimNames.Role).Value);

        var permissions = new List<string>();
        foreach (var claim in jwt.Claims)
        {
            if (claim.Type == JwtClaimNames.Permission)
            {
                permissions.Add(claim.Value);
            }
        }

        Assert.Equal(["Conference.Create", "Schedule.Run"], permissions);
        Assert.Equal(TimeSpan.FromMinutes(15), jwt.ValidTo - jwt.ValidFrom);
    }

    [Fact]
    public void EachTokenHasItsOwnId()
    {
        var role = Role.Create(Guid.NewGuid(), RoleCodes.Attendee, "Người tham dự");
        var user = User.CreateActive("minh@gmail.com", "hash", "Nguyễn Minh", role, null);
        var generator = new JwtAccessTokenGenerator(Options.Create(_jwtOptions), TimeProvider.System);

        var first = new JsonWebToken(generator.Generate(user).Value);
        var second = new JsonWebToken(generator.Generate(user).Value);

        Assert.NotEqual(first.Id, second.Id);
    }
}
