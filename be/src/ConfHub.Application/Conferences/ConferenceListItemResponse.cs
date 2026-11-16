namespace ConfHub.Application.Conferences;

public sealed record ConferenceListItemResponse(
    Guid Id,
    string NameVi,
    string NameEn,
    string Venue,
    string Status,
    DateOnly FirstDate,
    DateOnly LastDate,
    int RoomCount);
