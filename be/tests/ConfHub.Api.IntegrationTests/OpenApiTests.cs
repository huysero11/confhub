using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ConfHub.Api.IntegrationTests;

public class OpenApiTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task OpenApiDocumentIsAvailableInDevelopment()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
