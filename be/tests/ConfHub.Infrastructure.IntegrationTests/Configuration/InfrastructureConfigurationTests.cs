using ConfHub.Infrastructure.Messaging;
using ConfHub.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ConfHub.Infrastructure.IntegrationTests.Configuration;

public class InfrastructureConfigurationTests
{
    [Fact]
    public void MissingConnectionStringStopsStartup()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>());

        Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddInfrastructure(configuration));
    }

    [Fact]
    public void MissingRabbitMqSettingsFailValidation()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:ConfHub"] = "Server=127.0.0.1,1433;Database=ConfHub_Test",
            ["RabbitMq:Host"] = "localhost",
        });
        using var provider = new ServiceCollection().AddInfrastructure(configuration).BuildServiceProvider();

        var exception = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<RabbitMqOptions>>().Value);

        Assert.Contains(exception.Failures, failure => failure.Contains(nameof(RabbitMqOptions.Username), StringComparison.Ordinal));
    }

    [Fact]
    public void ValidRabbitMqSettingsAreBound()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:ConfHub"] = "Server=127.0.0.1,1433;Database=ConfHub_Test",
            ["RabbitMq:Host"] = "localhost",
            ["RabbitMq:Username"] = "guest",
            ["RabbitMq:Password"] = "guest",
        });
        using var provider = new ServiceCollection().AddInfrastructure(configuration).BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<RabbitMqOptions>>().Value;

        Assert.Equal("localhost", options.Host);
        Assert.Equal("guest", options.Username);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("too-short-key")]
    public void MissingOrShortJwtSigningKeyFailsValidation(string? signingKey)
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:ConfHub"] = "Server=127.0.0.1,1433;Database=ConfHub_Test",
            ["Jwt:Issuer"] = "confhub-api",
            ["Jwt:Audience"] = "confhub-web",
            ["Jwt:SigningKey"] = signingKey,
        });
        using var provider = new ServiceCollection().AddInfrastructure(configuration).BuildServiceProvider();

        var exception = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<JwtOptions>>().Value);

        Assert.Contains(exception.Failures, failure => failure.Contains(nameof(JwtOptions.SigningKey), StringComparison.Ordinal));
    }

    // Cấu hình giả trong bộ nhớ thay cho appsettings.json.
    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }
}
