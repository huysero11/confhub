using ConfHub.Application.Common;
using ConfHub.Application.Common.Persistence;
using ConfHub.Application.Common.Security;
using ConfHub.Application.Common.Time;
using ConfHub.Domain.Common;
using ConfHub.Domain.Conferences;
using MediatR;

namespace ConfHub.Application.Conferences.CreateConference;

public sealed class CreateConferenceCommandHandler(
    IRepository<Conference> conferenceRepository,
    ICurrentUser currentUser,
    VenueClock venueClock) : IRequestHandler<CreateConferenceCommand, CreatedIdResponse>
{
    public async Task<CreatedIdResponse> Handle(CreateConferenceCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        var details = new ConferenceDetails(
            request.NameVi,
            request.NameEn,
            request.DescriptionVi,
            request.DescriptionEn,
            request.Venue,
            request.RegistrationCloseAt.UtcDateTime,
            request.RequireApproval);
        var hours = TimeRange.Create(request.DayStartTime, request.DayEndTime);
        var conference = Conference.Create(
            userId,
            details,
            request.StartDate,
            request.EndDate,
            hours,
            venueClock.Today,
            venueClock.UtcNow,
            venueClock.TimeZone);

        await conferenceRepository.AddAsync(conference, cancellationToken);
        return new CreatedIdResponse(conference.Id);
    }
}
