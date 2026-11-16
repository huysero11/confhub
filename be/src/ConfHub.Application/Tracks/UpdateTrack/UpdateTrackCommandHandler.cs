using ConfHub.Application.Common.Exceptions;
using ConfHub.Application.Common.Persistence;
using ConfHub.Application.Common.Security;
using ConfHub.Application.Conferences;
using ConfHub.Application.Conferences.Specifications;
using ConfHub.Application.Tracks.Specifications;
using ConfHub.Domain.Conferences;
using MediatR;

namespace ConfHub.Application.Tracks.UpdateTrack;

public sealed class UpdateTrackCommandHandler(
    IRepository<Conference> conferenceRepository,
    ICurrentUser currentUser,
    IRepository<Track> trackRepository) : IRequestHandler<UpdateTrackCommand>
{
    public async Task Handle(UpdateTrackCommand request, CancellationToken cancellationToken)
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

        var existingTrack = await trackRepository.FirstOrDefaultAsync(
            new TrackByNameSpec(conference.Id, request.NameVi.Trim()), cancellationToken);
        if (existingTrack is not null && existingTrack.Id != track.Id)
        {
            throw new ConflictException(ConferenceErrorCodes.TrackNameTaken, "The name is already in use.");
        }

        track.Rename(request.NameVi, request.NameEn);

        await trackRepository.SaveChangesAsync(cancellationToken);
    }
}
