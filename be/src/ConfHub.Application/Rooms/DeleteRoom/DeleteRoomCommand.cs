using MediatR;

namespace ConfHub.Application.Rooms.DeleteRoom;

public sealed record DeleteRoomCommand(Guid ConferenceId,
    Guid RoomId) : IRequest;
