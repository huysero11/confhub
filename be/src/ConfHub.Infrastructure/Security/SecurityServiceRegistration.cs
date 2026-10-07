using ConfHub.Application.Common.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ConfHub.Infrastructure.Security;

// Đăng ký DI cho phần bảo mật: băm mật khẩu, tạo access token.
// Được gọi từ DependencyInjection.AddInfrastructure.
internal static class SecurityServiceRegistration
{
    public static IServiceCollection AddSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        // Cấu hình JWT, kiểm tra khi khởi động (thiếu khóa ký thì API dừng ngay).
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Cả hai không giữ trạng thái theo request → singleton.
        services.AddSingleton<IPasswordHasher, AspNetPasswordHasher>();
        services.AddSingleton<IAccessTokenGenerator, JwtAccessTokenGenerator>();

        return services;
    }
}
