namespace ConfHub.Application.Common.Email;

// Cổng gửi email. Infrastructure cài đặt bằng MailKit; test thay bằng bản giả.
public interface IEmailSender
{
    Task SendVerificationEmailAsync(string email, string fullName, string token, CancellationToken cancellationToken);
    Task SendPasswordResetEmailAsync(string email, string fullName, string token, CancellationToken cancellationToken);
}
