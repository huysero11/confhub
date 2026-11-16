using ConfHub.Domain.Common;

namespace ConfHub.Domain.Conferences;

public sealed class BreakSlot : BaseEntity
{
    internal BreakSlot(Guid conferenceDayId, string name, TimeRange time)
    {
        Id = Guid.CreateVersion7();
        ConferenceDayId = conferenceDayId;
        Update(name, time);
    }

    private BreakSlot()
    {
    }

    public Guid ConferenceDayId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public TimeRange Time { get; private set; } = null!;

    internal void Update(string name, TimeRange time)
    {
        Name = name.Trim();
        Time = time;
    }
}
