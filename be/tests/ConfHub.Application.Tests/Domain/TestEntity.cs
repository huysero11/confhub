using ConfHub.Domain.Common;

namespace ConfHub.Application.Tests.Domain;

// Entity mẫu: AddDomainEvent là protected nên phải gọi qua một phương thức nghiệp vụ.
public sealed class TestEntity : BaseEntity
{
    public void RaiseTestEvent() => AddDomainEvent(new TestEvent());
}
