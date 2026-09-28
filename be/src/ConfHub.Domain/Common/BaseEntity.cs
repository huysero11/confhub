namespace ConfHub.Domain.Common;

// Lớp gốc của Entity: Id và list domain events
public abstract class BaseEntity
{
    private readonly List<IDomainEvent> _domainEvents = [];
    public Guid Id { get; protected set; }
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void ClearDomainEvents() => _domainEvents.Clear();
    protected void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
}
