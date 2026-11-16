using Ardalis.Specification;
using ConfHub.Domain.Conferences;

namespace ConfHub.Application.Rooms.Specifications;

public sealed class RoomConferenceIdsSpec : Specification<Room, Guid>
{
    public RoomConferenceIdsSpec(List<Guid> conferenceIds)
    {
        // conferenceIds là id các conference của 1 người trong ban tổ chức. Chỉ lấy phòng thuộc các conference đó,
        // và chỉ lấy cột ConferenceId vì handler chỉ cần đếm
        Query.Where(room => conferenceIds.Contains(room.ConferenceId));
        Query.Select(room => room.ConferenceId);
    }
}
