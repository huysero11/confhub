namespace ConfHub.Application.Tests.Domain;

public class BaseEntityTests
{
    [Fact]
    public void RaisedEventIsKeptUntilCleared()
    {
        var entity = new TestEntity();

        entity.RaiseTestEvent();

        Assert.IsType<TestEvent>(Assert.Single(entity.DomainEvents));

        entity.ClearDomainEvents();

        Assert.Empty(entity.DomainEvents);
    }
}
