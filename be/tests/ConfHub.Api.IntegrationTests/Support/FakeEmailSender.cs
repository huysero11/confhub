using System.Collections.Concurrent;
using ConfHub.Application.Common.Email;

namespace ConfHub.Api.IntegrationTests.Support;

// Thay SMTP thật: ghi lại email đã "gửi" (kèm token gốc) để test đọc lại.
// FailNext(n): n lần gửi tiếp theo ném lỗi, giả lập máy chủ mail chết.
public sealed class FakeEmailSender : IEmailSender
{
    public const string VerifyEmailKind = "VerifyEmail";
    public const string ResetPasswordKind = "ResetPassword";

    private readonly ConcurrentQueue<SentEmail> _sent = new();
    private int _failuresLeft;

    public void FailNext(int count)
    {
        Interlocked.Exchange(ref _failuresLeft, count);
    }

    public IReadOnlyList<SentEmail> SentTo(string email, string kind)
    {
        var result = new List<SentEmail>();
        foreach (var sent in _sent)
        {
            if (string.Equals(sent.Email, email, StringComparison.OrdinalIgnoreCase) && sent.Kind == kind)
            {
                result.Add(sent);
            }
        }

        return result;
    }

    // Chờ tới khi có đủ `count` email (outbox + consumer chạy nền nên cần chờ), tối đa 10 giây.
    public async Task<IReadOnlyList<SentEmail>> WaitForAsync(string email, string kind, int count = 1)
    {
        var stopAt = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < stopAt)
        {
            var sent = SentTo(email, kind);
            if (sent.Count >= count)
            {
                return sent;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100));
        }

        return SentTo(email, kind);
    }

    public Task SendVerificationEmailAsync(string email, string fullName, string token, CancellationToken cancellationToken)
    {
        return RecordAsync(VerifyEmailKind, email, token);
    }

    public Task SendPasswordResetEmailAsync(string email, string fullName, string token, CancellationToken cancellationToken)
    {
        return RecordAsync(ResetPasswordKind, email, token);
    }

    private Task RecordAsync(string kind, string email, string token)
    {
        if (Interlocked.Decrement(ref _failuresLeft) >= 0)
        {
            throw new InvalidOperationException("Simulated SMTP failure.");
        }

        _sent.Enqueue(new SentEmail(kind, email, token));
        return Task.CompletedTask;
    }
}
