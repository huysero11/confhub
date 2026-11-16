using ConfHub.Application.Common.Persistence;
using ConfHub.Application.Common.Security;
using ConfHub.Application.Conferences.Specifications;
using ConfHub.Application.Rooms.Specifications;
using ConfHub.Domain.Conferences;
using MediatR;

namespace ConfHub.Application.Conferences.GetMyConferences;

public sealed class GetMyConferencesQueryHandler(
    ICurrentUser currentUser,
    IReadRepository<Conference> conferenceRepository,
    IReadRepository<Room> roomRepository) : IRequestHandler<GetMyConferencesQuery, List<ConferenceListItemResponse>>
{
    public async Task<List<ConferenceListItemResponse>> Handle(GetMyConferencesQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var conferences = await conferenceRepository.ListAsync(new ConferencesByOwnerSpec(userId), cancellationToken);

        // Với mỗi conference cần đếm số phòng: đếm các phòng có room.ConferenceId = conference.Id
        // conferenceIds: id các conference của người này, VD {A, B, C}
        var conferenceIds = new List<Guid>();
        foreach (var conference in conferences)
        {
            conferenceIds.Add(conference.Id);
        }

        // Mỗi phòng cho ra ConferenceId của nó, VD {A, A, A, B}: A có 3 phòng, B có 1 phòng, C không có phòng
        var roomConferenceIds = await roomRepository.ListAsync(new RoomConferenceIdsSpec(conferenceIds), cancellationToken);

        var roomCounts = new Dictionary<Guid, int>();
        foreach (var conferenceId in roomConferenceIds)
        {
            roomCounts.TryGetValue(conferenceId, out var count);
            roomCounts[conferenceId] = count + 1;
        }

        for (var i = 0; i < conferences.Count; i++)
        {
            roomCounts.TryGetValue(conferences[i].Id, out var roomCount);

            conferences[i] = conferences[i] with { RoomCount = roomCount };
        }

        return conferences;
    }
}
