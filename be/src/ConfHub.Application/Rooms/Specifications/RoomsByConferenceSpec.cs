using Ardalis.Specification;
using ConfHub.Domain.Conferences;

namespace ConfHub.Application.Rooms.Specifications;

public sealed class RoomsByConferenceSpec : Specification<Room, RoomResponse>
{
    public RoomsByConferenceSpec(Guid conferenceId)
    {
        Query.Where(room => room.ConferenceId == conferenceId);
        Query.OrderByDescending(room => room.Capacity).ThenBy(room => room.Name);
        Query.Select(room => new RoomResponse(room.Id, room.Name, room.Capacity));
    }
}
