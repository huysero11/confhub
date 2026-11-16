using ConfHub.Application.Common.Exceptions;
using ConfHub.Application.Common.Persistence;
using ConfHub.Application.Common.Security;
using ConfHub.Application.Common.Time;
using ConfHub.Application.Conferences.Specifications;
using ConfHub.Domain.Common;
using ConfHub.Domain.Conferences;
using MediatR;

namespace ConfHub.Application.Conferences.UpdateConferenceDay;

public sealed class UpdateConferenceDayCommandHandler(
    IRepository<Conference> conferenceRepository,
    ICurrentUser currentUser,
    VenueClock venueClock) : IRequestHandler<UpdateConferenceDayCommand>
{
    public async Task Handle(UpdateConferenceDayCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var conference = await conferenceRepository.FirstOrDefaultAsync(
            new ConferenceWithDaysByIdSpec(request.ConferenceId), cancellationToken);
        conference = ConferenceAccess.EnsureOwned(conference, request.ConferenceId, userId);

        var day = conference.FindDay(request.DayId)
            ?? throw new NotFoundException(nameof(ConferenceDay), request.DayId);
        var hours = TimeRange.Create(request.StartTime, request.EndTime);
        conference.UpdateDay(day.Id, request.Date, hours, venueClock.Today, venueClock.TimeZone);

        await conferenceRepository.SaveChangesAsync(cancellationToken);
    }
}
