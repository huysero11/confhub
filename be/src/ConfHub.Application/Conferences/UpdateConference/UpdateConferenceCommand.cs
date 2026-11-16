using MediatR;

namespace ConfHub.Application.Conferences.UpdateConference;

public sealed record UpdateConferenceCommand(Guid ConferenceId,
    string NameVi,
    string NameEn,
    string? DescriptionVi,
    string? DescriptionEn,
    string Venue,
    DateTimeOffset RegistrationCloseAt,
    bool RequireApproval) : IRequest;
