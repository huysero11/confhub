namespace ConfHub.Domain.Conferences;

public sealed record ConferenceDetails(
    string NameVi,
    string NameEn,
    string? DescriptionVi,
    string? DescriptionEn,
    string Venue,
    DateTime RegistrationCloseAt,
    bool RequireApproval);
