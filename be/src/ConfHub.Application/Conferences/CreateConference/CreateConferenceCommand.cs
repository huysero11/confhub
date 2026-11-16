using ConfHub.Application.Common;
using MediatR;

namespace ConfHub.Application.Conferences.CreateConference;

public sealed record CreateConferenceCommand(string NameVi,
    string NameEn,
    string? DescriptionVi,
    string? DescriptionEn,
    string Venue,
    DateOnly StartDate,
    DateOnly EndDate,
    TimeOnly DayStartTime,
    TimeOnly DayEndTime,
    DateTimeOffset RegistrationCloseAt,
    bool RequireApproval = true) : IRequest<CreatedIdResponse>;
