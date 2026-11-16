using ConfHub.Application.Common.Exceptions;
using ConfHub.Application.Common.Persistence;
using ConfHub.Application.Common.Security;
using ConfHub.Application.Conferences.Specifications;
using ConfHub.Domain.Conferences;
using MediatR;

namespace ConfHub.Application.Conferences.DeleteConferenceDay;

public sealed class DeleteConferenceDayCommandHandler(
    IRepository<Conference> conferenceRepository,
    ICurrentUser currentUser) : IRequestHandler<DeleteConferenceDayCommand>
{
    public async Task Handle(DeleteConferenceDayCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var conference = await conferenceRepository.FirstOrDefaultAsync(
            new ConferenceWithDaysByIdSpec(request.ConferenceId), cancellationToken);
        conference = ConferenceAccess.EnsureOwned(conference, request.ConferenceId, userId);

        var day = conference.FindDay(request.DayId)
            ?? throw new NotFoundException(nameof(ConferenceDay), request.DayId);
        conference.RemoveDay(day.Id);

        await conferenceRepository.SaveChangesAsync(cancellationToken);
    }
}
