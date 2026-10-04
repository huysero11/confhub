using ConfHub.Application.Accounts.Messages;
using MassTransit;

namespace ConfHub.Infrastructure.IntegrationTests.Messaging;

// Consumer rỗng chỉ để test harness ghi nhận message đã tới.
public sealed class VerificationMessageConsumer : IConsumer<SendVerificationEmailMessage>
{
    public Task Consume(ConsumeContext<SendVerificationEmailMessage> context) => Task.CompletedTask;
}
