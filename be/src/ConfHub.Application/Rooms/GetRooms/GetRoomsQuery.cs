using MediatR;

namespace ConfHub.Application.Rooms.GetRooms;

public sealed record GetRoomsQuery(Guid ConferenceId) : IRequest<List<RoomResponse>>;
