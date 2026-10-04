using ConfHub.Application.Accounts.Messages;
using ConfHub.Application.Accounts.SendPasswordResetEmail;
using MassTransit;
using MediatR;

namespace ConfHub.Infrastructure.Messaging.Consumers;

public sealed class SendPasswordResetEmailConsumer(ISender sender) : IConsumer<SendPasswordResetEmailMessage>
{
    public Task Consume(ConsumeContext<SendPasswordResetEmailMessage> context)
    {
        return sender.Send(new SendPasswordResetEmailCommand(context.Message.UserId), context.CancellationToken);
    }
}
