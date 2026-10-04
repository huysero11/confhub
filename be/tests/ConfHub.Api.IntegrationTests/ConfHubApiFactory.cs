using ConfHub.Api.IntegrationTests.Support;
using ConfHub.Application.Common.Email;
using ConfHub.Infrastructure.Persistence;
using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ConfHub.Api.IntegrationTests;

// Dựng API trong bộ nhớ cho test:
// - CSDL riêng ConfHub_ApiTest trên SQL Server của máy dev, xóa và tạo lại mỗi lần chạy test.
// - RabbitMQ thay bằng test harness của MassTransit (hàng đợi trong bộ nhớ) → không cần Docker.
// - Email thay bằng FakeEmailSender (ghi lại thay vì gửi), đồng hồ thay bằng TestClock (tua được).
public sealed class ConfHubApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string ConnectionString =
        "Server=127.0.0.1,1433;Database=ConfHub_ApiTest;Trusted_Connection=True;TrustServerCertificate=True";

    public FakeEmailSender EmailSender { get; } = new();

    public TestClock Clock { get; } = new();

    public async Task InitializeAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<ConfHubDbContext>().Database;

        await database.EnsureDeletedAsync();
        await database.MigrateAsync();
    }

    Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:ConfHub", ConnectionString);

        // Test gọi API rất nhiều lần từ cùng 1 "IP" → nới giới hạn; test rate limit tự đặt lại thấp.
        builder.UseSetting("RateLimiting:RegisterPerMinute", "10000");
        builder.UseSetting("RateLimiting:EmailPerMinute", "10000");
        builder.UseSetting("RateLimiting:ResetPasswordPerMinute", "10000");

        builder.ConfigureTestServices(services =>
        {
            services.AddMassTransitTestHarness();
            services.AddSingleton<IEmailSender>(EmailSender);
            services.AddSingleton<TimeProvider>(Clock);
        });
    }
}
