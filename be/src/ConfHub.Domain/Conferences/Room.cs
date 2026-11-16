using ConfHub.Domain.Common;

namespace ConfHub.Domain.Conferences;

public sealed class Room : BaseEntity, IAggregateRoot
{
    private Room()
    {
    }

    public Guid ConferenceId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int Capacity { get; private set; }

    public static Room Create(Guid conferenceId, string name, int capacity)
    {
        var room = new Room { ConferenceId = conferenceId };
        room.Update(name, capacity);
        return room;
    }

    public void Update(string name, int capacity)
    {
        Name = name.Trim();
        Capacity = capacity;
    }
}
