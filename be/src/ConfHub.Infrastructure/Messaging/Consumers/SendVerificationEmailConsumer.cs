using ConfHub.Application.Accounts.Messages;
using ConfHub.Application.Accounts.SendVerificationEmail;
using MassTransit;
using MediatR;

namespace ConfHub.Infrastructure.Messaging.Consumers;

// Consumer chỉ nhận message rồi chuyển cho handler ở Application (logic nằm ở đó).
// Inbox + transaction + thử lại 1s/5s/15s: cấu hình chung trong DependencyInjection.
public sealed class SendVerificationEmailConsumer(ISender sender) : IConsumer<SendVerificationEmailMessage>
{
    public Task Consume(ConsumeContext<SendVerificationEmailMessage> context)
    {
        return sender.Send(new SendVerificationEmailCommand(context.Message.UserId), context.CancellationToken);
    }
}
