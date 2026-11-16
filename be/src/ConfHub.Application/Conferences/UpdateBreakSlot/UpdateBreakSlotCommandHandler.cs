using ConfHub.Application.Common.Exceptions;
using ConfHub.Application.Common.Persistence;
using ConfHub.Application.Common.Security;
using ConfHub.Application.Conferences.Specifications;
using ConfHub.Domain.Common;
using ConfHub.Domain.Conferences;
using MediatR;

namespace ConfHub.Application.Conferences.UpdateBreakSlot;

public sealed class UpdateBreakSlotCommandHandler(
    IRepository<Conference> conferenceRepository,
    ICurrentUser currentUser) : IRequestHandler<UpdateBreakSlotCommand>
{
    public async Task Handle(UpdateBreakSlotCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var conference = await conferenceRepository.FirstOrDefaultAsync(new ConferenceWithDaysByIdSpec(request.ConferenceId), cancellationToken);
        conference = ConferenceAccess.EnsureOwned(conference, request.ConferenceId, userId);

        var day = conference.FindDay(request.DayId)
            ?? throw new NotFoundException(nameof(ConferenceDay), request.DayId);
        _ = day.FindBreak(request.BreakId)
            ?? throw new NotFoundException(nameof(BreakSlot), request.BreakId);

        var time = TimeRange.Create(request.StartTime, request.EndTime);
        conference.UpdateBreak(day.Id, request.BreakId, request.Name, time);

        await conferenceRepository.SaveChangesAsync(cancellationToken);
    }
}
