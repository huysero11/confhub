using System.Net;

namespace ConfHub.Api.IntegrationTests;

[Collection(ApiCollectionDefinition.Name)]
public class HealthCheckTests(ConfHubApiFactory factory)
{
    [Fact]
    public async Task HealthEndpointReturnsOk()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/health", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
