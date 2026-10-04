using ConfHub.Domain.Common;

namespace ConfHub.Domain.Accounts.Events;

public sealed record PasswordResetRequested(User User) : IDomainEvent;
