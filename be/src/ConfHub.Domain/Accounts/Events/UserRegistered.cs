using ConfHub.Domain.Common;

namespace ConfHub.Domain.Accounts.Events;

// Giữ tham chiếu tới User (không phải Id): lúc sự kiện được ghi, EF Core chưa gán Id cho User.
public sealed record UserRegistered(User User) : IDomainEvent;
