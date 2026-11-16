using ConfHub.Application.Common;
using ConfHub.Application.Common.Exceptions;
using ConfHub.Application.Common.Persistence;
using ConfHub.Application.Common.Security;
using ConfHub.Application.Conferences;
using ConfHub.Application.Conferences.Specifications;
using ConfHub.Application.Rooms.Specifications;
using ConfHub.Domain.Conferences;
using MediatR;

namespace ConfHub.Application.Rooms.CreateRoom;

public sealed class CreateRoomCommandHandler(
    IRepository<Conference> conferenceRepository,
    ICurrentUser currentUser,
    IRepository<Room> roomRepository) : IRequestHandler<CreateRoomCommand, CreatedIdResponse>
{
    public async Task<CreatedIdResponse> Handle(CreateRoomCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var conference = await conferenceRepository.FirstOrDefaultAsync(
            new ConferenceByIdSpec(request.ConferenceId), cancellationToken);
        conference = ConferenceAccess.EnsureOwned(conference, request.ConferenceId, userId);

        var existingRoom = await roomRepository.FirstOrDefaultAsync(
            new RoomByNameSpec(conference.Id, request.Name.Trim()), cancellationToken);
        if (existingRoom is not null)
        {
            throw new ConflictException(ConferenceErrorCodes.RoomNameTaken, "The name is already in use.");
        }

        var room = Room.Create(conference.Id, request.Name, request.Capacity);

        await roomRepository.AddAsync(room, cancellationToken);
        return new CreatedIdResponse(room.Id);
    }
}
