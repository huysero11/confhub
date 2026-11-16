using ConfHub.Application.Common;
using MediatR;

namespace ConfHub.Application.Rooms.CreateRoom;

public sealed record CreateRoomCommand(Guid ConferenceId,
    string Name,
    int Capacity) : IRequest<CreatedIdResponse>;
