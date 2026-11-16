using ConfHub.Application.Common.Persistence;
using ConfHub.Application.Common.Security;
using ConfHub.Application.Conferences.Specifications;
using ConfHub.Domain.Conferences;
using MediatR;

namespace ConfHub.Application.Conferences.DeleteConference;

public sealed class DeleteConferenceCommandHandler(
    IRepository<Conference> conferenceRepository,
    ICurrentUser currentUser) : IRequestHandler<DeleteConferenceCommand>
{
    public async Task Handle(DeleteConferenceCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var conference = await conferenceRepository.FirstOrDefaultAsync(
            new ConferenceByIdSpec(request.ConferenceId), cancellationToken);
        conference = ConferenceAccess.EnsureOwned(conference, request.ConferenceId, userId);

        await conferenceRepository.DeleteAsync(conference, cancellationToken);
    }
}
