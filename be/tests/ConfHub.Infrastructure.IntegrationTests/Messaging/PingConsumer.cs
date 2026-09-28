using MassTransit;

namespace ConfHub.Infrastructure.IntegrationTests.Messaging;

// Consumer mẫu: không làm gì, chỉ cần "nhận được" để test harness ghi nhận.
public sealed class PingConsumer : IConsumer<PingMessage>
{
    public Task Consume(ConsumeContext<PingMessage> context) => Task.CompletedTask;
}
