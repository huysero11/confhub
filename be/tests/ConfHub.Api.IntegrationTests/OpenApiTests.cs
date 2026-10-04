using System.Net;

namespace ConfHub.Api.IntegrationTests;

[Collection(ApiCollectionDefinition.Name)]
public class OpenApiTests(ConfHubApiFactory factory)
{
    [Fact]
    public async Task OpenApiDocumentIsAvailableInDevelopment()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
