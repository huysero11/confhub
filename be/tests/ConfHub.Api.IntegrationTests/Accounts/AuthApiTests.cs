using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ConfHub.Api.IntegrationTests.Support;
using ConfHub.Application.Common.Security;
using ConfHub.Domain.Accounts;
using ConfHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ConfHub.Api.IntegrationTests.Accounts;

[Collection(ApiCollectionDefinition.Name)]
public sealed class AuthApiTests(ConfHubApiFactory factory) : IDisposable
{
    private const string Password = "matkhau123";

    private readonly HttpClient _client = factory.CreateClient();

    public void Dispose()
    {
        _client.Dispose();
    }

    [Fact]
    public async Task AttendeeRegistersVerifiesAndBecomesActive()
    {
        var email = UniqueEmail();

        var registerResponse = await RegisterAsync(email, RoleCodes.Attendee, null);
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var token = await WaitForTokenAsync(email, FakeEmailSender.VerifyEmailKind);
        var verifyResponse = await PostAsync("verify-email", new { token });

        Assert.Equal(HttpStatusCode.OK, verifyResponse.StatusCode);
        Assert.Equal("Active", (await ReadJsonAsync(verifyResponse)).GetProperty("status").GetString());
    }

    [Fact]
    public async Task OrganizerBecomesPendingApprovalAfterVerification()
    {
        var email = UniqueEmail();
        await RegisterAsync(email, RoleCodes.Organizer, "Đại học Bách khoa");

        var token = await WaitForTokenAsync(email, FakeEmailSender.VerifyEmailKind);
        var verifyResponse = await PostAsync("verify-email", new { token });

        Assert.Equal("PendingApproval", (await ReadJsonAsync(verifyResponse)).GetProperty("status").GetString());
    }

    [Fact]
    public async Task RegisterWithTakenEmailReturnsConflict()
    {
        var email = UniqueEmail();
        await RegisterAsync(email, RoleCodes.Attendee, null);

        var response = await RegisterAsync(email.ToUpperInvariant(), RoleCodes.Attendee, null);

        await AssertProblemAsync(response, HttpStatusCode.Conflict, AccountErrorCodes.EmailTaken);
    }

    [Fact]
    public async Task RegisterWithInvalidDataReturnsFieldErrors()
    {
        var response = await PostAsync("register", new
        {
            fullName = "M",
            email = "not-an-email",
            password = "short",
            role = RoleCodes.Admin,
        });

        var body = await AssertProblemAsync(response, HttpStatusCode.BadRequest, "Validation");
        var errors = body.GetProperty("errors");
        Assert.True(errors.TryGetProperty("FullName", out _));
        Assert.True(errors.TryGetProperty("Email", out _));
        Assert.True(errors.TryGetProperty("Password", out _));
        Assert.Equal(AccountErrorCodes.RoleNotAllowed, errors.GetProperty("Role")[0].GetString());
    }

    [Fact]
    public async Task VerificationTokenWorksOnlyOnce()
    {
        var email = UniqueEmail();
        await RegisterAsync(email, RoleCodes.Attendee, null);
        var token = await WaitForTokenAsync(email, FakeEmailSender.VerifyEmailKind);

        await PostAsync("verify-email", new { token });
        var secondResponse = await PostAsync("verify-email", new { token });

        await AssertProblemAsync(secondResponse, HttpStatusCode.UnprocessableEntity, AccountErrorCodes.TokenInvalid);
    }

    [Fact]
    public async Task UnknownOrExpiredTokenIsRejected()
    {
        var unknownResponse = await PostAsync("verify-email", new { token = RandomToken.Generate() });
        await AssertProblemAsync(unknownResponse, HttpStatusCode.UnprocessableEntity, AccountErrorCodes.TokenInvalid);

        var email = UniqueEmail();
        await RegisterAsync(email, RoleCodes.Attendee, null);
        var token = await WaitForTokenAsync(email, FakeEmailSender.VerifyEmailKind);

        factory.Clock.Advance(TimeSpan.FromHours(25));
        var expiredResponse = await PostAsync("verify-email", new { token });

        await AssertProblemAsync(expiredResponse, HttpStatusCode.UnprocessableEntity, AccountErrorCodes.TokenInvalid);
    }

    [Fact]
    public async Task ResendToUnknownEmailSendsNothing()
    {
        var email = UniqueEmail();

        var response = await PostAsync("resend-verification", new { email });
        await Task.Delay(TimeSpan.FromSeconds(3));

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Empty(factory.EmailSender.SentTo(email, FakeEmailSender.VerifyEmailKind));
    }

    [Fact]
    public async Task ResendRespectsCooldownAndInvalidatesOldLink()
    {
        var email = UniqueEmail();
        await RegisterAsync(email, RoleCodes.Attendee, null);
        var firstToken = await WaitForTokenAsync(email, FakeEmailSender.VerifyEmailKind);

        // Trong 60 giây: không gửi thêm.
        await PostAsync("resend-verification", new { email });
        await Task.Delay(TimeSpan.FromSeconds(3));
        Assert.Single(factory.EmailSender.SentTo(email, FakeEmailSender.VerifyEmailKind));

        // Sau 60 giây: có email mới, link cũ hết hiệu lực.
        factory.Clock.Advance(TimeSpan.FromSeconds(61));
        await PostAsync("resend-verification", new { email });
        var sent = await factory.EmailSender.WaitForAsync(email, FakeEmailSender.VerifyEmailKind, count: 2);
        Assert.Equal(2, sent.Count);

        var oldLinkResponse = await PostAsync("verify-email", new { token = firstToken });
        await AssertProblemAsync(oldLinkResponse, HttpStatusCode.UnprocessableEntity, AccountErrorCodes.TokenInvalid);

        var newLinkResponse = await PostAsync("verify-email", new { token = sent[1].Token });
        Assert.Equal(HttpStatusCode.OK, newLinkResponse.StatusCode);
    }

    [Fact]
    public async Task ForgotPasswordThenResetPassword()
    {
        var email = await CreateActiveUserAsync();
        var userId = await GetUserIdAsync(email);
        var refreshTokenHash = await AddRefreshTokenAsync(userId);

        var forgotResponse = await PostAsync("forgot-password", new { email });
        Assert.Equal(HttpStatusCode.Accepted, forgotResponse.StatusCode);
        var token = await WaitForTokenAsync(email, FakeEmailSender.ResetPasswordKind);

        var resetResponse = await PostAsync("reset-password", new { token, newPassword = "matkhaumoi456" });
        Assert.Equal(HttpStatusCode.NoContent, resetResponse.StatusCode);

        var reuseResponse = await PostAsync("reset-password", new { token, newPassword = "matkhaumoi789" });
        await AssertProblemAsync(reuseResponse, HttpStatusCode.UnprocessableEntity, AccountErrorCodes.TokenInvalid);

        // BR14: refresh token cũ bị thu hồi.
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ConfHubDbContext>();
        var refreshToken = await dbContext.Set<UserToken>().SingleAsync(item => item.TokenHash == refreshTokenHash);
        Assert.NotNull(refreshToken.UsedAt);
    }

    [Fact]
    public async Task ForgotPasswordForUnverifiedSendsVerificationEmailInstead()
    {
        var email = UniqueEmail();
        await RegisterAsync(email, RoleCodes.Attendee, null);
        await WaitForTokenAsync(email, FakeEmailSender.VerifyEmailKind);
        factory.Clock.Advance(TimeSpan.FromSeconds(61));

        await PostAsync("forgot-password", new { email });

        var sent = await factory.EmailSender.WaitForAsync(email, FakeEmailSender.VerifyEmailKind, count: 2);
        Assert.Equal(2, sent.Count);
        Assert.Empty(factory.EmailSender.SentTo(email, FakeEmailSender.ResetPasswordKind));
    }

    [Fact]
    public async Task ForgotPasswordForLockedOrUnknownSendsNothing()
    {
        var lockedEmail = await CreateActiveUserAsync();
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ConfHubDbContext>();
            await dbContext.Database.ExecuteSqlAsync(
                $"UPDATE Users SET Status = 'Locked' WHERE Email = {lockedEmail}");
        }

        var unknownEmail = UniqueEmail();
        var lockedResponse = await PostAsync("forgot-password", new { email = lockedEmail });
        var unknownResponse = await PostAsync("forgot-password", new { email = unknownEmail });
        await Task.Delay(TimeSpan.FromSeconds(3));

        Assert.Equal(HttpStatusCode.Accepted, lockedResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, unknownResponse.StatusCode);
        Assert.Empty(factory.EmailSender.SentTo(lockedEmail, FakeEmailSender.ResetPasswordKind));
        Assert.Empty(factory.EmailSender.SentTo(unknownEmail, FakeEmailSender.ResetPasswordKind));
    }

    [Fact]
    public async Task FailedEmailIsRetriedAndFailedTokenIsRolledBack()
    {
        var email = UniqueEmail();
        factory.EmailSender.FailNext(1);

        await RegisterAsync(email, RoleCodes.Attendee, null);
        await WaitForTokenAsync(email, FakeEmailSender.VerifyEmailKind);

        // Lần gửi lỗi đã bị rollback → chỉ còn đúng 1 token, của lần gửi thành công.
        var userId = await GetUserIdAsync(email);
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ConfHubDbContext>();
        var tokenCount = await dbContext.Set<UserToken>()
            .CountAsync(token => token.UserId == userId && token.Purpose == TokenPurpose.VerifyEmail);

        Assert.Single(factory.EmailSender.SentTo(email, FakeEmailSender.VerifyEmailKind));
        Assert.Equal(1, tokenCount);
    }

    [Fact]
    public async Task RegisterIsRateLimited()
    {
        using var limitedFactory = factory.WithWebHostBuilder(
            builder => builder.UseSetting("RateLimiting:RegisterPerMinute", "1"));
        using var client = limitedFactory.CreateClient();

        // Body sai để không tạo tài khoản: rate limiter chặn trước cả validation.
        var first = await client.PostAsJsonAsync(new Uri("/api/auth/register", UriKind.Relative), new { });
        var second = await client.PostAsJsonAsync(new Uri("/api/auth/register", UriKind.Relative), new { });

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

    private Task<HttpResponseMessage> PostAsync(string path, object body)
    {
        return _client.PostAsJsonAsync(new Uri($"/api/auth/{path}", UriKind.Relative), body);
    }

    private Task<HttpResponseMessage> RegisterAsync(string email, string role, string? organization)
    {
        return PostAsync("register", new { fullName = "Nguyễn Minh", email, password = Password, role, organization });
    }

    private async Task<string> WaitForTokenAsync(string email, string kind)
    {
        var sent = await factory.EmailSender.WaitForAsync(email, kind);
        Assert.NotEmpty(sent);
        return sent[^1].Token;
    }

    private async Task<string> CreateActiveUserAsync()
    {
        var email = UniqueEmail();
        await RegisterAsync(email, RoleCodes.Attendee, null);
        var token = await WaitForTokenAsync(email, FakeEmailSender.VerifyEmailKind);
        await PostAsync("verify-email", new { token });
        return email;
    }

    private async Task<Guid> GetUserIdAsync(string email)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ConfHubDbContext>();
        var normalizedEmail = User.NormalizeEmail(email);
        return await dbContext.Set<User>()
            .Where(user => user.Email == normalizedEmail)
            .Select(user => user.Id)
            .SingleAsync();
    }

    // Chèn thẳng 1 refresh token còn hạn (T1.4 mới có đăng nhập để sinh ra).
    private async Task<string> AddRefreshTokenAsync(Guid userId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ConfHubDbContext>();
        var tokenHash = RandomToken.Hash(RandomToken.Generate());
        var expiresAt = factory.Clock.GetUtcNow().UtcDateTime.AddDays(7);

        dbContext.Set<UserToken>().Add(UserToken.Issue(userId, TokenPurpose.Refresh, tokenHash, expiresAt));
        await dbContext.SaveChangesAsync();
        return tokenHash;
    }
}
