using System.Net;

namespace ConfHub.Api.IntegrationTests;

public class HealthCheckTests(ConfHubApiFactory factory)
    : IClassFixture<ConfHubApiFactory>
{
    [Fact]
    public async Task HealthEndpointReturnsOk()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/health", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
