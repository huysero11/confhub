using ConfHub.Application.Common.Persistence;
using ConfHub.Application.Common.Security;
using ConfHub.Application.Common.Time;
using ConfHub.Application.Conferences.Specifications;
using ConfHub.Domain.Conferences;
using MediatR;

namespace ConfHub.Application.Conferences.UpdateConference;

public sealed class UpdateConferenceCommandHandler(
    IRepository<Conference> conferenceRepository,
    ICurrentUser currentUser,
    VenueClock venueClock) : IRequestHandler<UpdateConferenceCommand>
{
    public async Task Handle(UpdateConferenceCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var conference = await conferenceRepository.FirstOrDefaultAsync(
            new ConferenceWithDaysByIdSpec(request.ConferenceId), cancellationToken);
        conference = ConferenceAccess.EnsureOwned(conference, request.ConferenceId, userId);

        var details = new ConferenceDetails(
            request.NameVi,
            request.NameEn,
            request.DescriptionVi,
            request.DescriptionEn,
            request.Venue,
            request.RegistrationCloseAt.UtcDateTime,
            request.RequireApproval);

        conference.UpdateDetails(details, venueClock.UtcNow, venueClock.TimeZone);

        await conferenceRepository.SaveChangesAsync(cancellationToken);
    }
}
