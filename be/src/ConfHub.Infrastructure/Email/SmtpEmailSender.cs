using System.Net;
using ConfHub.Application.Common.Email;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;

namespace ConfHub.Infrastructure.Email;

// Gửi email qua SMTP bằng MailKit. Mỗi email có cả tiếng Việt và tiếng Anh. Không ghi token ra log.
public sealed class SmtpEmailSender(
    IOptions<SmtpOptions> smtpOptions,
    IOptions<FrontendOptions> frontendOptions
) : IEmailSender
{
    public Task SendVerificationEmailAsync(string email, string fullName, string token, CancellationToken cancellationToken)
    {
        var link = BuildLink("verify-email", token);

        /*
            - HtmlEncode: tên do người dùng nhập, không để chèn được thẻ HTML vào email.
            - HtmlEncode() sẽ biến các ký tự đặc biệt thành dạng an toàn để chúng chỉ được hiển thị như text.
        */
        var name = WebUtility.HtmlEncode(fullName);

        // Hạn 24 giờ: khớp TokenLifetimes.VerifyEmail.
        var body = $"""
            <p>Xin chào {name},</p>
            <p>Bấm vào liên kết dưới đây để xác thực email tài khoản ConfHub. Liên kết có hiệu lực trong 24 giờ.</p>
            <p><a href="{link}">Xác thực email</a></p>
            <hr>
            <p>Hello {name},</p>
            <p>Click the link below to verify your ConfHub account email. The link is valid for 24 hours.</p>
            <p><a href="{link}">Verify email</a></p>
            """;
        return SendAsync(email, fullName, "Xác thực email ConfHub / Verify your ConfHub email", body, cancellationToken);
    }

    public Task SendPasswordResetEmailAsync(string email, string fullName, string token, CancellationToken cancellationToken)
    {
        var link = BuildLink("reset-password", token);
        var name = WebUtility.HtmlEncode(fullName);

        // Hạn 30 phút: khớp TokenLifetimes.ResetPassword.
        var body = $"""
            <p>Xin chào {name},</p>
            <p>Bấm vào liên kết dưới đây để đặt lại mật khẩu ConfHub. Liên kết có hiệu lực trong 30 phút.
            Nếu bạn không yêu cầu, hãy bỏ qua email này.</p>
            <p><a href="{link}">Đặt lại mật khẩu</a></p>
            <hr>
            <p>Hello {name},</p>
            <p>Click the link below to reset your ConfHub password. The link is valid for 30 minutes.
            If you did not request this, please ignore this email.</p>
            <p><a href="{link}">Reset password</a></p>
            """;

        return SendAsync(email, fullName, "Đặt lại mật khẩu ConfHub / Reset your ConfHub password", body, cancellationToken);
    }

    // VD: http://localhost:5173/verify-email?token=abc
    private string BuildLink(string path, string token)
    {
        var baseUrl = frontendOptions.Value.BaseUrl.TrimEnd('/');
        return $"{baseUrl}/{path}?token={Uri.EscapeDataString(token)}";
    }

    private async Task SendAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        var smtp = smtpOptions.Value;

        // Bắt đầu tạo mail
        // MimeMessage: đại diện cho một email
        using var message = new MimeMessage();
        message.From.Add(new MailboxAddress(smtp.FromName, smtp.FromAddress));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = subject;
        message.Body = new TextPart(TextFormat.Html) { Text = htmlBody };

        // Kết nối đến smtp server
        using var client = new SmtpClient();
        var socketOptions = smtp.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
        await client.ConnectAsync(smtp.Host, smtp.Port, socketOptions, cancellationToken);

        if (!string.IsNullOrEmpty(smtp.Username))
        {
            await client.AuthenticateAsync(smtp.Username, smtp.Password ?? string.Empty, cancellationToken);
        }

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
    }
}
