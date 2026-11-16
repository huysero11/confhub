using ConfHub.Application.Common;
using ConfHub.Application.Common.Exceptions;
using ConfHub.Application.Common.Persistence;
using ConfHub.Application.Common.Security;
using ConfHub.Application.Conferences.Specifications;
using ConfHub.Domain.Common;
using ConfHub.Domain.Conferences;
using MediatR;

namespace ConfHub.Application.Conferences.AddBreakSlot;

public sealed class AddBreakSlotCommandHandler(
    IRepository<Conference> conferenceRepository,
    ICurrentUser currentUser) : IRequestHandler<AddBreakSlotCommand, CreatedIdResponse>
{
    public async Task<CreatedIdResponse> Handle(AddBreakSlotCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var conference = await conferenceRepository.FirstOrDefaultAsync(
            new ConferenceWithDaysByIdSpec(request.ConferenceId), cancellationToken);
        conference = ConferenceAccess.EnsureOwned(conference, request.ConferenceId, userId);

        var day = conference.FindDay(request.DayId)
            ?? throw new NotFoundException(nameof(ConferenceDay), request.DayId);
        var time = TimeRange.Create(request.StartTime, request.EndTime);
        var breakSlot = conference.AddBreak(day.Id, request.Name, time);

        await conferenceRepository.SaveChangesAsync(cancellationToken);
        return new CreatedIdResponse(breakSlot.Id);
    }
}
