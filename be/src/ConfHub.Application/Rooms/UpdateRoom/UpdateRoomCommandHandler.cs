using ConfHub.Application.Common.Exceptions;
using ConfHub.Application.Common.Persistence;
using ConfHub.Application.Common.Security;
using ConfHub.Application.Conferences;
using ConfHub.Application.Conferences.Specifications;
using ConfHub.Application.Rooms.Specifications;
using ConfHub.Domain.Conferences;
using MediatR;

namespace ConfHub.Application.Rooms.UpdateRoom;

public sealed class UpdateRoomCommandHandler(
    IRepository<Conference> conferenceRepository,
    ICurrentUser currentUser,
    IRepository<Room> roomRepository) : IRequestHandler<UpdateRoomCommand>
{
    public async Task Handle(UpdateRoomCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var conference = await conferenceRepository.FirstOrDefaultAsync(
            new ConferenceByIdSpec(request.ConferenceId), cancellationToken);
        conference = ConferenceAccess.EnsureOwned(conference, request.ConferenceId, userId);

        var room = await roomRepository.GetByIdAsync(request.RoomId, cancellationToken);
        if (room is null || room.ConferenceId != conference.Id)
        {
            throw new NotFoundException(nameof(Room), request.RoomId);
        }

        var existingRoom = await roomRepository.FirstOrDefaultAsync(
            new RoomByNameSpec(conference.Id, request.Name.Trim()), cancellationToken);
        if (existingRoom is not null && existingRoom.Id != room.Id)
        {
            throw new ConflictException(ConferenceErrorCodes.RoomNameTaken, "The name is already in use.");
        }

        room.Update(request.Name, request.Capacity);

        await roomRepository.SaveChangesAsync(cancellationToken);
    }
}
