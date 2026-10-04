using ConfHub.Application.Common.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ConfHub.Infrastructure.Persistence;

// Đăng ký DI cho phần lưu trữ: DbContext, interceptor, repository, publisher.
// Được gọi từ DependencyInjection.AddInfrastructure.
internal static class PersistenceServiceRegistration
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ConfHub");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'ConfHub' is not configured.");
        }

        // DbContext + SQL Server; AuditInterceptor tự gán CreatedAt khi SaveChanges.
        services.AddSingleton<AuditInterceptor>();
        services.AddDbContext<ConfHubDbContext>((serviceProvider, options) => options
            .UseSqlServer(connectionString)
            .AddInterceptors(serviceProvider.GetRequiredService<AuditInterceptor>()));

        // Repository chung cho mọi aggregate (open generic: IRepository<User> → EfRepository<User>).
        services.AddScoped<DomainEventPublisher>();
        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped(typeof(IReadRepository<>), typeof(EfRepository<>));

        return services;
    }
}
