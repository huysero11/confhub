using ConfHub.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ConfHub.Infrastructure.IntegrationTests.Messaging;

[Collection(DatabaseCollectionDefinition.Name)]
public class OutboxTests(OutboxFixture fixture)
{
    [Fact]
    public async Task MigrationCreatesOutboxTables()
    {
        await using var scope = fixture.Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<ConfHubDbContext>().Database;

        var appliedMigrations = await database.GetAppliedMigrationsAsync();
        var tableCount = await database
            .SqlQuery<int>($"SELECT COUNT(*) AS [Value] FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME IN ('InboxState', 'OutboxMessage', 'OutboxState')")
            .SingleAsync();

        Assert.Contains(appliedMigrations, name => name.EndsWith("_T0_4_MassTransitOutbox", StringComparison.Ordinal));
        Assert.Equal(3, tableCount);
    }

    [Fact]
    public async Task MessageIsDeliveredAfterSaveChanges()
    {
        var messageId = Guid.NewGuid();

        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
            var dbContext = scope.ServiceProvider.GetRequiredService<ConfHubDbContext>();

            // Publish chỉ ghi vào bảng OutboxMessage; SaveChanges mới lưu thật (cùng transaction).
            await publishEndpoint.Publish(new PingMessage(messageId));
            await dbContext.SaveChangesAsync();
        }

        var consumed = await WaitUntilConsumedAsync(messageId, TimeSpan.FromSeconds(10));

        Assert.True(consumed);
    }

    [Fact]
    public async Task MessageIsNotDeliveredWithoutSaveChanges()
    {
        var messageId = Guid.NewGuid();

        await using (var scope = fixture.Services.CreateAsyncScope())
        {
            var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

            // Publish nhưng KHÔNG SaveChanges (giống thao tác nghiệp vụ bị lỗi giữa chừng):
            // tin nhắn không được lưu nên không bao giờ được gửi đi.
            await publishEndpoint.Publish(new PingMessage(messageId));
        }

        // Chờ 2 giây: nếu tin nhắn được lưu thì outbox (quét mỗi 100ms) đã kịp gửi đi.
        await Task.Delay(TimeSpan.FromSeconds(2));

        Assert.False(IsConsumed(messageId));
    }

    // Kiểm tra mỗi 100ms xem consumer đã nhận tin nhắn chưa, tối đa `timeout`.
    // Không dùng harness.Consumed.Any: nó trả false ngay nếu harness đã "rảnh" từ test chạy trước.
    private async Task<bool> WaitUntilConsumedAsync(Guid messageId, TimeSpan timeout)
    {
        var stopAt = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < stopAt)
        {
            if (IsConsumed(messageId))
            {
                return true;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100));
        }

        return IsConsumed(messageId);
    }

    private bool IsConsumed(Guid messageId)
    {
        return fixture.Harness.Consumed
            .Select<PingMessage>(message => message.Context.Message.Id == messageId)
            .Any();
    }
}
