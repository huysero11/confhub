using ConfHub.Application.Common.Persistence;
using ConfHub.Application.Common.Security;
using ConfHub.Application.Conferences;
using ConfHub.Application.Conferences.Specifications;
using ConfHub.Application.Rooms.Specifications;
using ConfHub.Domain.Conferences;
using MediatR;

namespace ConfHub.Application.Rooms.GetRooms;

public sealed class GetRoomsQueryHandler(
    IReadRepository<Conference> conferenceRepository,
    ICurrentUser currentUser,
    IReadRepository<Room> roomRepository) : IRequestHandler<GetRoomsQuery, List<RoomResponse>>
{
    public async Task<List<RoomResponse>> Handle(GetRoomsQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var conference = await conferenceRepository.FirstOrDefaultAsync(
            new ConferenceByIdSpec(request.ConferenceId), cancellationToken);
        conference = ConferenceAccess.EnsureOwned(conference, request.ConferenceId, userId);

        return await roomRepository.ListAsync(new RoomsByConferenceSpec(conference.Id), cancellationToken);
    }
}
