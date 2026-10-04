using ConfHub.Domain.Common;

namespace ConfHub.Domain.Accounts.Events;

public sealed record EmailVerificationRequested(User User) : IDomainEvent;
