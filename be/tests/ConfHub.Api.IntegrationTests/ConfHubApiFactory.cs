using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;

namespace ConfHub.Api.IntegrationTests;

// Dựng API trong bộ nhớ cho test. Thay RabbitMQ thật bằng test harness của MassTransit
// (hàng đợi trong bộ nhớ) → chạy test không cần bật Docker.
public sealed class ConfHubApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services => services.AddMassTransitTestHarness());
    }
}
