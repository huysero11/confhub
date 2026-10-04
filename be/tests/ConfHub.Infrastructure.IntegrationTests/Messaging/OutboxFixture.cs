using ConfHub.Application.Common.Persistence;
using ConfHub.Infrastructure.Persistence;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ConfHub.Infrastructure.IntegrationTests.Messaging;

// Dựng một "mini host" giống API nhưng chỉ có CSDL + repository + MassTransit:
// - CSDL riêng ConfHub_Test trên SQL Server thật của máy dev, xóa và tạo lại mỗi lần chạy test.
// - RabbitMQ được thay bằng test harness (hàng đợi trong bộ nhớ) → không cần bật Docker.
public sealed class OutboxFixture : IAsyncLifetime
{
    // SQL Server trên máy dev, đăng nhập bằng tài khoản Windows (xem CLAUDE.md mục Môi trường).
    private const string ConnectionString =
        "Server=127.0.0.1,1433;Database=ConfHub_Test;Trusted_Connection=True;TrustServerCertificate=True";

    private IHost? _host;

    public IServiceProvider Services => _host?.Services
        ?? throw new InvalidOperationException("Fixture is not initialized.");

    public ITestHarness Harness => Services.GetRequiredService<ITestHarness>();

    public async Task InitializeAsync()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<AuditInterceptor>();
        builder.Services.AddDbContext<ConfHubDbContext>((serviceProvider, options) => options
            .UseSqlServer(ConnectionString)
            .AddInterceptors(serviceProvider.GetRequiredService<AuditInterceptor>()));
        builder.Services.AddScoped<DomainEventPublisher>();
        builder.Services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        builder.Services.AddScoped(typeof(IReadRepository<>), typeof(EfRepository<>));
        builder.Services.AddMassTransitTestHarness(bus =>
        {
            // Outbox cấu hình giống AddInfrastructure; chỉ khác transport là bộ nhớ thay cho RabbitMQ.
            bus.AddEntityFrameworkOutbox<ConfHubDbContext>(outbox =>
            {
                outbox.UseSqlServer();
                outbox.UseBusOutbox();

                // Quét bảng OutboxMessage mỗi 100ms (mặc định lâu hơn) để test chạy nhanh.
                outbox.QueryDelay = TimeSpan.FromMilliseconds(100);
            });
            bus.AddConsumer<PingConsumer>();
            bus.AddConsumer<VerificationMessageConsumer>();
        });
        _host = builder.Build();

        await RecreateDatabaseAsync(_host.Services);
        await _host.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
    }

    // Xóa CSDL test cũ rồi chạy toàn bộ migration → mỗi lần chạy test bắt đầu từ CSDL sạch.
    private static async Task RecreateDatabaseAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<ConfHubDbContext>().Database;

        await database.EnsureDeletedAsync();
        await database.MigrateAsync();
    }
}
