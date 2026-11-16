using MediatR;

namespace ConfHub.Application.Rooms.UpdateRoom;

public sealed record UpdateRoomCommand(Guid ConferenceId,
    Guid RoomId,
    string Name,
    int Capacity) : IRequest;
