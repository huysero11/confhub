using ConfHub.Application.Common.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ConfHub.Infrastructure.Email;

// Đăng ký DI cho phần gửi email. Được gọi từ DependencyInjection.AddInfrastructure.
internal static class EmailServiceRegistration
{
    public static IServiceCollection AddEmail(this IServiceCollection services, IConfiguration configuration)
    {
        // Cấu hình SMTP + địa chỉ frontend (để dựng link), kiểm tra khi khởi động.
        services.AddOptions<SmtpOptions>()
            .Bind(configuration.GetSection(SmtpOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddOptions<FrontendOptions>()
            .Bind(configuration.GetSection(FrontendOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Handler xin IEmailSender thì nhận được SmtpEmailSender.
        services.AddScoped<IEmailSender, SmtpEmailSender>();

        return services;
    }
}
