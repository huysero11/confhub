using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ConfHub.Api.IntegrationTests.Support;
using ConfHub.Application.Common.Security;
using ConfHub.Domain.Accounts;
using ConfHub.Infrastructure.Persistence;
using ConfHub.Infrastructure.Security;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ConfHub.Api.IntegrationTests.Accounts;

// Đăng nhập, làm mới phiên, đăng xuất, /me và cơ chế phân quyền (T1.4).
[Collection(ApiCollectionDefinition.Name)]
public sealed class SessionApiTests(ConfHubApiFactory factory) : IDisposable
{
    private const string Password = "matkhau123";

    // Tắt tự giữ cookie: test tự đọc Set-Cookie và tự gửi lại để điều khiển được cookie nào đi kèm.
    private readonly HttpClient _client = factory.CreateClient(
        new WebApplicationFactoryClientOptions { HandleCookies = false });

    public void Dispose()
    {
        _client.Dispose();
    }

    [Fact]
    public async Task LoginReturnsAccessTokenAndPutsRefreshTokenInHttpOnlyCookie()
    {
        var email = await CreateUserAsync(RoleCodes.Attendee, UserStatus.Active);

        var response = await LoginAsync(email.ToUpperInvariant(), Password);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await ReadJsonAsync(response);
        Assert.False(string.IsNullOrEmpty(body.GetProperty("accessToken").GetString()));
        Assert.Equal(900, body.GetProperty("expiresIn").GetInt32());
        Assert.Equal(email, body.GetProperty("user").GetProperty("email").GetString());
        Assert.Equal(RoleCodes.Attendee, body.GetProperty("user").GetProperty("role").GetString());

        // Refresh token chỉ nằm trong cookie, không nằm trong body.
        Assert.False(body.TryGetProperty("refreshToken", out _));
        var cookieHeader = RefreshCookie.ReadHeader(response).ToLowerInvariant();
        Assert.Contains("httponly", cookieHeader, StringComparison.Ordinal);
        Assert.Contains("secure", cookieHeader, StringComparison.Ordinal);
        Assert.Contains("samesite=strict", cookieHeader, StringComparison.Ordinal);
        Assert.Contains("path=/api/auth", cookieHeader, StringComparison.Ordinal);

        // CSDL chỉ có bản băm của refresh token.
        var refreshToken = RefreshCookie.ReadValue(response);
        var hashes = await GetRefreshTokenHashesAsync(email);
        Assert.Equal([RandomToken.Hash(refreshToken)], hashes);
    }

    [Fact]
    public async Task WrongPasswordAndUnknownEmailLookTheSame()
    {
        var email = await CreateUserAsync(RoleCodes.Attendee, UserStatus.Active);

        var wrongPasswordResponse = await LoginAsync(email, "saimatkhau999");
        var unknownEmailResponse = await LoginAsync(UniqueEmail(), Password);

        var wrongPasswordBody = await AssertProblemAsync(
            wrongPasswordResponse, HttpStatusCode.Unauthorized, AccountErrorCodes.InvalidCredentials);
        var unknownEmailBody = await AssertProblemAsync(
            unknownEmailResponse, HttpStatusCode.Unauthorized, AccountErrorCodes.InvalidCredentials);
        Assert.Equal(
            wrongPasswordBody.GetProperty("detail").GetString(),
            unknownEmailBody.GetProperty("detail").GetString());
    }

    [Theory]
    [InlineData(UserStatus.Unverified, AccountErrorCodes.AccountUnverified)]
    [InlineData(UserStatus.PendingApproval, AccountErrorCodes.AccountPending)]
    [InlineData(UserStatus.Locked, AccountErrorCodes.AccountLocked)]
    public async Task InactiveAccountIsRejectedOnlyAfterCorrectPassword(UserStatus status, string expectedCode)
    {
        var email = await CreateUserAsync(RoleCodes.Organizer, status);

        var correctPasswordResponse = await LoginAsync(email, Password);
        var wrongPasswordResponse = await LoginAsync(email, "saimatkhau999");

        await AssertProblemAsync(correctPasswordResponse, HttpStatusCode.Forbidden, expectedCode);

        // Sai mật khẩu thì không lộ trạng thái tài khoản.
        await AssertProblemAsync(wrongPasswordResponse, HttpStatusCode.Unauthorized, AccountErrorCodes.InvalidCredentials);
    }

    [Fact]
    public async Task RefreshRotatesTokenAndReusingOldTokenRevokesAllSessions()
    {
        var email = await CreateUserAsync(RoleCodes.Attendee, UserStatus.Active);
        var oldRefreshToken = RefreshCookie.ReadValue(await LoginAsync(email, Password));

        var refreshResponse = await RefreshAsync(oldRefreshToken);

        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        var newRefreshToken = RefreshCookie.ReadValue(refreshResponse);
        Assert.NotEqual(oldRefreshToken, newRefreshToken);
        Assert.False(string.IsNullOrEmpty((await ReadJsonAsync(refreshResponse)).GetProperty("accessToken").GetString()));

        // Gửi lại token cũ (đã dùng) = dấu hiệu bị đánh cắp → thu hồi cả token mới.
        var reuseResponse = await RefreshAsync(oldRefreshToken);
        await AssertProblemAsync(reuseResponse, HttpStatusCode.Unauthorized, AccountErrorCodes.SessionExpired);

        var newTokenResponse = await RefreshAsync(newRefreshToken);
        await AssertProblemAsync(newTokenResponse, HttpStatusCode.Unauthorized, AccountErrorCodes.SessionExpired);
    }

    [Fact]
    public async Task RefreshFailsWithoutCookieOrWithUnknownToken()
    {
        var noCookieResponse = await RefreshAsync(null);
        var unknownTokenResponse = await RefreshAsync(RandomToken.Generate());

        await AssertProblemAsync(noCookieResponse, HttpStatusCode.Unauthorized, AccountErrorCodes.SessionExpired);
        await AssertProblemAsync(unknownTokenResponse, HttpStatusCode.Unauthorized, AccountErrorCodes.SessionExpired);
    }

    [Fact]
    public async Task RefreshFailsAfterSevenDays()
    {
        var email = await CreateUserAsync(RoleCodes.Attendee, UserStatus.Active);
        var refreshToken = RefreshCookie.ReadValue(await LoginAsync(email, Password));

        factory.Clock.Advance(TimeSpan.FromDays(7) + TimeSpan.FromMinutes(1));
        var response = await RefreshAsync(refreshToken);

        await AssertProblemAsync(response, HttpStatusCode.Unauthorized, AccountErrorCodes.SessionExpired);
    }

    [Fact]
    public async Task RefreshFailsWhenAccountGetsLocked()
    {
        var email = await CreateUserAsync(RoleCodes.Attendee, UserStatus.Active);
        var refreshToken = RefreshCookie.ReadValue(await LoginAsync(email, Password));

        await SetStatusAsync(email, UserStatus.Locked);
        var response = await RefreshAsync(refreshToken);

        await AssertProblemAsync(response, HttpStatusCode.Unauthorized, AccountErrorCodes.SessionExpired);
    }

    [Fact]
    public async Task LogoutRevokesRefreshTokenAndClearsCookie()
    {
        var email = await CreateUserAsync(RoleCodes.Attendee, UserStatus.Active);
        var refreshToken = RefreshCookie.ReadValue(await LoginAsync(email, Password));

        using var logoutRequest = RefreshCookie.Post("/api/auth/logout", refreshToken);
        var logoutResponse = await _client.SendAsync(logoutRequest);

        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);
        Assert.Equal(string.Empty, RefreshCookie.ReadValue(logoutResponse));

        var refreshResponse = await RefreshAsync(refreshToken);
        await AssertProblemAsync(refreshResponse, HttpStatusCode.Unauthorized, AccountErrorCodes.SessionExpired);
    }

    [Fact]
    public async Task LogoutWithoutCookieStillSucceeds()
    {
        using var logoutRequest = RefreshCookie.Post("/api/auth/logout", null);
        var response = await _client.SendAsync(logoutRequest);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task MeRequiresValidAccessToken()
    {
        var email = await CreateUserAsync(RoleCodes.Supplier, UserStatus.Active);
        var accessToken = await LoginForAccessTokenAsync(email);

        var noTokenResponse = await GetAsync("/api/auth/me", null);
        var validResponse = await GetAsync("/api/auth/me", accessToken);

        var noTokenBody = await AssertProblemAsync(noTokenResponse, HttpStatusCode.Unauthorized, "Unauthorized");
        Assert.True(noTokenBody.TryGetProperty("traceId", out _));
        Assert.Equal("application/problem+json", noTokenResponse.Content.Headers.ContentType?.MediaType);

        Assert.Equal(HttpStatusCode.OK, validResponse.StatusCode);
        var body = await ReadJsonAsync(validResponse);
        Assert.Equal(email, body.GetProperty("email").GetString());
        Assert.Equal(RoleCodes.Supplier, body.GetProperty("role").GetString());
        Assert.Equal("Đơn vị thử", body.GetProperty("organization").GetString());
        Assert.Equal(JsonValueKind.Array, body.GetProperty("permissions").ValueKind);
    }

    [Fact]
    public async Task AccessTokenExpiresAfterFifteenMinutes()
    {
        var email = await CreateUserAsync(RoleCodes.Attendee, UserStatus.Active);
        var accessToken = await LoginForAccessTokenAsync(email);

        factory.Clock.Advance(TimeSpan.FromMinutes(16));
        var response = await GetAsync("/api/auth/me", accessToken);

        await AssertProblemAsync(response, HttpStatusCode.Unauthorized, "Unauthorized");
    }

    [Fact]
    public async Task AccessTokenSignedWithAnotherKeyIsRejected()
    {
        var email = await CreateUserAsync(RoleCodes.Attendee, UserStatus.Active);
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ConfHubDbContext>();
        var user = await dbContext.Set<User>().Include(item => item.Role).SingleAsync(item => item.Email == email);

        // Cùng issuer / audience, chỉ khác khóa ký → chữ ký không khớp.
        var realOptions = scope.ServiceProvider.GetRequiredService<IOptions<JwtOptions>>().Value;
        var forgedOptions = new JwtOptions
        {
            Issuer = realOptions.Issuer,
            Audience = realOptions.Audience,
            SigningKey = "another-signing-key-0123456789-abcdefghij",
            AccessTokenMinutes = realOptions.AccessTokenMinutes,
        };
        var forgedToken = new JwtAccessTokenGenerator(Options.Create(forgedOptions), factory.Clock).Generate(user);

        var response = await GetAsync("/api/auth/me", forgedToken.Value);

        await AssertProblemAsync(response, HttpStatusCode.Unauthorized, "Unauthorized");
    }

    [Fact]
    public async Task EndpointWithoutAttributeRequiresSignIn()
    {
        var email = await CreateUserAsync(RoleCodes.Attendee, UserStatus.Active);
        var accessToken = await LoginForAccessTokenAsync(email);

        var anonymousResponse = await GetAsync("/api/test/default", null);
        var signedInResponse = await GetAsync("/api/test/default", accessToken);

        await AssertProblemAsync(anonymousResponse, HttpStatusCode.Unauthorized, "Unauthorized");
        Assert.Equal(HttpStatusCode.OK, signedInResponse.StatusCode);
    }

    [Fact]
    public async Task PermissionComesFromRoleAtSignIn()
    {
        // Vai trò Staff không dùng ở test nào khác nên đổi quyền của nó không ảnh hưởng test khác.
        var email = await CreateUserAsync(RoleCodes.Staff, UserStatus.Active);
        var tokenWithoutPermission = await LoginForAccessTokenAsync(email);

        var forbiddenResponse = await GetAsync("/api/test/permission", tokenWithoutPermission);
        await AssertProblemAsync(forbiddenResponse, HttpStatusCode.Forbidden, "Forbidden");

        try
        {
            await SetStaffPermissionsAsync($"[\"{TestProbeController.Permission}\"]");

            // Token cũ vẫn chưa có quyền; phải đăng nhập lại để nhận token mới.
            var oldTokenResponse = await GetAsync("/api/test/permission", tokenWithoutPermission);
            Assert.Equal(HttpStatusCode.Forbidden, oldTokenResponse.StatusCode);

            var tokenWithPermission = await LoginForAccessTokenAsync(email);
            var allowedResponse = await GetAsync("/api/test/permission", tokenWithPermission);
            Assert.Equal(HttpStatusCode.OK, allowedResponse.StatusCode);

            var currentUserResponse = await GetAsync("/api/test/current-user", tokenWithPermission);
            Assert.True((await ReadJsonAsync(currentUserResponse)).GetProperty("hasPermission").GetBoolean());
        }
        finally
        {
            await SetStaffPermissionsAsync("[]");
        }
    }

    [Fact]
    public async Task CurrentUserMatchesSignedInUser()
    {
        var email = await CreateUserAsync(RoleCodes.Attendee, UserStatus.Active);
        var loginBody = await ReadJsonAsync(await LoginAsync(email, Password));
        var accessToken = loginBody.GetProperty("accessToken").GetString();

        var response = await GetAsync("/api/test/current-user", accessToken);

        var body = await ReadJsonAsync(response);
        Assert.Equal(loginBody.GetProperty("user").GetProperty("id").GetGuid(), body.GetProperty("userId").GetGuid());
        Assert.False(body.GetProperty("hasPermission").GetBoolean());
    }

    [Fact]
    public async Task LoginIsRateLimited()
    {
        using var limitedFactory = factory.WithWebHostBuilder(
            builder => builder.UseSetting("RateLimiting:LoginPerMinute", "1"));
        using var client = limitedFactory.CreateClient();

        // Body sai để không phải tạo tài khoản: rate limiter chặn trước cả validation.
        var first = await client.PostAsJsonAsync(new Uri("/api/auth/login", UriKind.Relative), new { });
        var second = await client.PostAsJsonAsync(new Uri("/api/auth/login", UriKind.Relative), new { });

        Assert.Equal(HttpStatusCode.BadRequest, first.StatusCode);
        await AssertProblemAsync(second, HttpStatusCode.TooManyRequests, "TooManyRequests");
    }

    private static string UniqueEmail()
    {
        return $"user-{Guid.NewGuid():N}@test.local";
    }

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task<JsonElement> AssertProblemAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus,
        string expectedCode)
    {
        Assert.Equal(expectedStatus, response.StatusCode);
        var body = await ReadJsonAsync(response);
        Assert.Equal(expectedCode, body.GetProperty("code").GetString());
        return body;
    }

    private Task<HttpResponseMessage> LoginAsync(string email, string password)
    {
        return _client.PostAsJsonAsync(new Uri("/api/auth/login", UriKind.Relative), new { email, password });
    }

    private async Task<string> LoginForAccessTokenAsync(string email)
    {
        var response = await LoginAsync(email, Password);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var accessToken = (await ReadJsonAsync(response)).GetProperty("accessToken").GetString();
        Assert.NotNull(accessToken);
        return accessToken;
    }

    private async Task<HttpResponseMessage> RefreshAsync(string? refreshToken)
    {
        using var request = RefreshCookie.Post("/api/auth/refresh", refreshToken);
        return await _client.SendAsync(request);
    }

    private async Task<HttpResponseMessage> GetAsync(string path, string? accessToken)
    {
        using var request = RefreshCookie.Get(path, accessToken);
        return await _client.SendAsync(request);
    }

    // Tạo thẳng tài khoản trong CSDL (không qua đăng ký + email) rồi đặt trạng thái cần thử.
    private async Task<string> CreateUserAsync(string roleCode, UserStatus status)
    {
        var email = UniqueEmail();

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ConfHubDbContext>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            var role = await dbContext.Set<Role>().SingleAsync(item => item.Code == roleCode);

            var user = User.CreateActive(email, passwordHasher.Hash(Password), "Trần Lan", role, "Đơn vị thử");
            dbContext.Set<User>().Add(user);
            await dbContext.SaveChangesAsync();
        }

        if (status != UserStatus.Active)
        {
            await SetStatusAsync(email, status);
        }

        return email;
    }

    private async Task SetStatusAsync(string email, UserStatus status)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ConfHubDbContext>();
        var statusName = status.ToString();
        await dbContext.Database.ExecuteSqlAsync($"UPDATE Users SET Status = {statusName} WHERE Email = {email}");
    }

    private async Task SetStaffPermissionsAsync(string permissionsJson)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ConfHubDbContext>();
        await dbContext.Database.ExecuteSqlAsync(
            $"UPDATE Roles SET Permissions = {permissionsJson} WHERE Code = {RoleCodes.Staff}");
    }

    private async Task<List<string>> GetRefreshTokenHashesAsync(string email)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ConfHubDbContext>();
        var userId = await dbContext.Set<User>().Where(user => user.Email == email).Select(user => user.Id).SingleAsync();
        return await dbContext.Set<UserToken>()
            .Where(token => token.UserId == userId && token.Purpose == TokenPurpose.Refresh)
            .Select(token => token.TokenHash)
            .ToListAsync();
    }
}
