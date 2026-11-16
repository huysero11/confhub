using Ardalis.Specification;
using ConfHub.Domain.Conferences;

namespace ConfHub.Application.Rooms.Specifications;

public sealed class RoomByNameSpec : Specification<Room>
{
    public RoomByNameSpec(Guid conferenceId, string name)
    {
        Query.Where(room => room.ConferenceId == conferenceId && room.Name == name);
    }
}
