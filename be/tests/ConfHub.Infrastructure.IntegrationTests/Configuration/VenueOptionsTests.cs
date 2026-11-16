using ConfHub.Application.Common.Time;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ConfHub.Infrastructure.IntegrationTests.Configuration;

public class VenueOptionsTests
{
    // ValidateOnStart phải chặn múi giờ rỗng hoặc không có trong hệ điều hành.
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("Not/A_Time_Zone")]
    public void InvalidTimeZoneStopsStartup(string timeZoneId)
    {
        using var provider = BuildServices(timeZoneId);
        var exception = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IStartupValidator>().Validate());
        Assert.Contains(exception.Failures, failure => failure.Contains("TimeZoneId", StringComparison.Ordinal));
    }

    // Múi giờ hợp lệ được bind và VenueClock lấy đúng ngày địa phương từ UTC.
    [Fact]
    public void VietnamTimeZonePassesStartupAndBuildsSingletonClock()
    {
        using var provider = BuildServices("Asia/Ho_Chi_Minh");
        provider.GetRequiredService<IStartupValidator>().Validate();
        var clock = provider.GetRequiredService<VenueClock>();
        Assert.Same(clock, provider.GetRequiredService<VenueClock>());
        Assert.Equal(TimeSpan.FromHours(7), clock.TimeZone.BaseUtcOffset);
        Assert.Equal(DateTimeKind.Utc, clock.UtcNow.Kind);
    }

    // Helper của các test cấu hình (cùng lớp): Các mục khác hợp lệ để chỉ kiểm Venue.
    private static ServiceProvider BuildServices(string timeZoneId)
    {
        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:ConfHub"] = "Server=127.0.0.1,1433;Database=ConfHub_Test",
            ["RabbitMq:Host"] = "localhost",
            ["RabbitMq:Username"] = "test",
            ["RabbitMq:Password"] = "test",
            ["Smtp:Host"] = "localhost",
            ["Smtp:Port"] = "25",
            ["Smtp:FromAddress"] = "test@example.com",
            ["Smtp:FromName"] = "Test",
            ["Frontend:BaseUrl"] = "https://example.com",
            ["Jwt:Issuer"] = "test",
            ["Jwt:Audience"] = "test",
            ["Jwt:SigningKey"] = "test-only-key-with-at-least-32-characters",
            ["Venue:TimeZoneId"] = timeZoneId
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        return new ServiceCollection().AddInfrastructure(configuration).BuildServiceProvider();
    }
}
