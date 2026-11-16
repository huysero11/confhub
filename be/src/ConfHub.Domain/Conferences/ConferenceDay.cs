using ConfHub.Domain.Common;

namespace ConfHub.Domain.Conferences;

public sealed class ConferenceDay : BaseEntity
{
    private readonly List<BreakSlot> _breaks = [];

    internal ConferenceDay(Guid conferenceId, DateOnly date, TimeRange hours)
    {
        Id = Guid.CreateVersion7();
        ConferenceId = conferenceId;
        Date = date;
        Hours = hours;
    }

    private ConferenceDay()
    {
    }

    public Guid ConferenceId { get; private set; }
    public DateOnly Date { get; private set; }
    public TimeRange Hours { get; private set; } = null!;
    public IReadOnlyCollection<BreakSlot> Breaks => _breaks.AsReadOnly();

    public BreakSlot? FindBreak(Guid breakId)
    {
        return _breaks.Find(breakSlot => breakSlot.Id == breakId);
    }

    internal void Update(DateOnly date, TimeRange hours)
    {
        Date = date;
        Hours = hours;
    }

    internal BreakSlot AddBreak(string name, TimeRange time)
    {
        var breakSlot = new BreakSlot(Id, name, time);
        _breaks.Add(breakSlot);
        return breakSlot;
    }

    internal void RemoveBreak(BreakSlot breakSlot)
    {
        _breaks.Remove(breakSlot);
    }
}
