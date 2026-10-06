using ConfHub.Application.Accounts.Messages;
using ConfHub.Domain.Accounts.Events;
using ConfHub.Domain.Common;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace ConfHub.Infrastructure.Persistence;

/*
- Đổi domain event của các aggregate đang được DbContext theo dõi
    thành message RabbitMQ và publish qua outbox.
    Được gọi NGAY TRƯỚC SaveChanges → message nằm cùng transaction với dữ liệu.
- IPublishEndpoint là interface của MassTransit, được DI inject vào. Vì đã bật outbox,
    Publish không gửi thẳng lên RabbitMQ mà chỉ thêm bản ghi OutboxMessage vào DbContext.
- Class này không gắn vào EF Core: EfRepository gọi nó trong SaveChangesAsync (đã override),
    ngay trước base.SaveChangesAsync.
*/
public sealed class DomainEventPublisher(IPublishEndpoint publishEndpoint)
{
    public async Task PublishAsync(DbContext dbContext, CancellationToken cancellationToken)
    {
        /*
            Gom entity có sự kiện ra danh sách riêng trước: publish sẽ thêm bản ghi OutboxMessage
            vào DbContext, không được vừa duyệt ChangeTracker vừa làm nó thay đổi.
        */
        var entitiesWithEvents = new List<BaseEntity>();
        foreach (var entry in dbContext.ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.Entity.DomainEvents.Count > 0)
            {
                entitiesWithEvents.Add(entry.Entity);
            }
        }

        foreach (var entity in entitiesWithEvents)
        {
            var domainEvents = entity.DomainEvents.ToList();
            foreach (var domainEvent in domainEvents)
            {
                await publishEndpoint.Publish(MapToMessage(domainEvent), cancellationToken);
            }

            entity.ClearDomainEvents();
        }
    }

    private static object MapToMessage(IDomainEvent domainEvent)
    {
        switch (domainEvent)
        {
            case UserRegistered userRegistered:
                return new SendVerificationEmailMessage(userRegistered.User.Id);
            case EmailVerificationRequested verificationRequested:
                return new SendVerificationEmailMessage(verificationRequested.User.Id);
            case PasswordResetRequested passwordResetRequested:
                return new SendPasswordResetEmailMessage(passwordResetRequested.User.Id);
            default:
                throw new InvalidOperationException(
                    $"No mapping defined for domain event type {domainEvent.GetType().Name}");
        }
    }
}
