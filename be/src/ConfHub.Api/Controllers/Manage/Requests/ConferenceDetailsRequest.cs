namespace ConfHub.Api.Controllers.Manage.Requests;

public sealed record ConferenceDetailsRequest(
    string NameVi,
    string NameEn,
    string? DescriptionVi,
    string? DescriptionEn,
    string Venue,
    DateTimeOffset RegistrationCloseAt,
    bool RequireApproval);
