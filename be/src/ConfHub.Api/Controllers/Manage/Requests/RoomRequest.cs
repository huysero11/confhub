namespace ConfHub.Api.Controllers.Manage.Requests;

public sealed record RoomRequest(
    string Name,
    int Capacity);
