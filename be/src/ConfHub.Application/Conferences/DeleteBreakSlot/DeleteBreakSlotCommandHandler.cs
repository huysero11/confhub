using ConfHub.Application.Common.Exceptions;
using ConfHub.Application.Common.Persistence;
using ConfHub.Application.Common.Security;
using ConfHub.Application.Conferences.Specifications;
using ConfHub.Domain.Conferences;
using MediatR;

namespace ConfHub.Application.Conferences.DeleteBreakSlot;

public sealed class DeleteBreakSlotCommandHandler(
    IRepository<Conference> conferenceRepository,
    ICurrentUser currentUser) : IRequestHandler<DeleteBreakSlotCommand>
{
    public async Task Handle(DeleteBreakSlotCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var conference = await conferenceRepository.FirstOrDefaultAsync(
            new ConferenceWithDaysByIdSpec(request.ConferenceId), cancellationToken);
        conference = ConferenceAccess.EnsureOwned(conference, request.ConferenceId, userId);

        var day = conference.FindDay(request.DayId)
            ?? throw new NotFoundException(nameof(ConferenceDay), request.DayId);
        _ = day.FindBreak(request.BreakId)
            ?? throw new NotFoundException(nameof(BreakSlot), request.BreakId);
        conference.RemoveBreak(day.Id, request.BreakId);

        await conferenceRepository.SaveChangesAsync(cancellationToken);
    }
}
