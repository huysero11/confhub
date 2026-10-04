using ConfHub.Infrastructure.IntegrationTests.Messaging;

namespace ConfHub.Infrastructure.IntegrationTests;

// Các lớp test dùng chung 1 CSDL ConfHub_Test → gom vào 1 collection để xUnit không chạy song song
// (tránh 2 fixture cùng xóa / tạo lại CSDL một lúc).
[CollectionDefinition(Name)]
public sealed class DatabaseCollectionDefinition : ICollectionFixture<OutboxFixture>
{
    public const string Name = "Database";
}
