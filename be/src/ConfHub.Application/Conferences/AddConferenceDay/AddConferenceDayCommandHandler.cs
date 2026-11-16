using ConfHub.Application.Common;
using ConfHub.Application.Common.Persistence;
using ConfHub.Application.Common.Security;
using ConfHub.Application.Common.Time;
using ConfHub.Application.Conferences.Specifications;
using ConfHub.Domain.Common;
using ConfHub.Domain.Conferences;
using MediatR;

namespace ConfHub.Application.Conferences.AddConferenceDay;

public sealed class AddConferenceDayCommandHandler(
    IRepository<Conference> conferenceRepository,
    ICurrentUser currentUser,
    VenueClock venueClock) : IRequestHandler<AddConferenceDayCommand, CreatedIdResponse>
{
    public async Task<CreatedIdResponse> Handle(AddConferenceDayCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var conference = await conferenceRepository.FirstOrDefaultAsync(
            new ConferenceWithDaysByIdSpec(request.ConferenceId), cancellationToken);
        conference = ConferenceAccess.EnsureOwned(conference, request.ConferenceId, userId);

        var hours = TimeRange.Create(request.StartTime, request.EndTime);
        var day = conference.AddDay(request.Date, hours, venueClock.Today, venueClock.TimeZone);

        await conferenceRepository.SaveChangesAsync(cancellationToken);
        return new CreatedIdResponse(day.Id);
    }
}
