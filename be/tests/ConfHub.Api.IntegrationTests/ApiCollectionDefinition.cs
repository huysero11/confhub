namespace ConfHub.Api.IntegrationTests;

// Mọi lớp test API dùng chung 1 factory (1 CSDL test) và chạy lần lượt, không song song.
[CollectionDefinition(Name)]
public sealed class ApiCollectionDefinition : ICollectionFixture<ConfHubApiFactory>
{
    public const string Name = "Api";
}
