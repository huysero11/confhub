using ConfHub.Infrastructure.Email;
using ConfHub.Infrastructure.Messaging;
using ConfHub.Infrastructure.Persistence;
using ConfHub.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ConfHub.Infrastructure;

// "Mục lục" đăng ký DI của tầng Infrastructure: mỗi mục có file đăng ký riêng trong thư mục của nó
// (<Tên mục>ServiceRegistration.cs). Mục chỉ có 1 dòng thì viết thẳng ở đây.
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Đồng hồ hệ thống (UTC). Test thay bằng đồng hồ giả để tua thời gian.
        services.AddSingleton(TimeProvider.System);

        services.AddPersistence(configuration);
        services.AddMessaging(configuration);
        services.AddEmail(configuration);
        services.AddSecurity(configuration);

        return services;
    }
}
