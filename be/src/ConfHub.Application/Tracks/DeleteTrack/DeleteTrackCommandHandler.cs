using ConfHub.Application.Common.Exceptions;
using ConfHub.Application.Common.Persistence;
using ConfHub.Application.Common.Security;
using ConfHub.Application.Conferences;
using ConfHub.Application.Conferences.Specifications;
using ConfHub.Domain.Conferences;
using MediatR;

namespace ConfHub.Application.Tracks.DeleteTrack;

public sealed class DeleteTrackCommandHandler(
    IRepository<Conference> conferenceRepository,
    ICurrentUser currentUser,
    IRepository<Track> trackRepository) : IRequestHandler<DeleteTrackCommand>
{
    public async Task Handle(DeleteTrackCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var conference = await conferenceRepository.FirstOrDefaultAsync(
            new ConferenceByIdSpec(request.ConferenceId), cancellationToken);
        conference = ConferenceAccess.EnsureOwned(conference, request.ConferenceId, userId);

        var track = await trackRepository.GetByIdAsync(request.TrackId, cancellationToken);
        if (track is null || track.ConferenceId != conference.Id)
        {
            throw new NotFoundException(nameof(Track), request.TrackId);
        }

        await trackRepository.DeleteAsync(track, cancellationToken);
    }
}
