using ConfHub.Application.Common;
using ConfHub.Application.Common.Exceptions;
using ConfHub.Application.Common.Persistence;
using ConfHub.Application.Common.Security;
using ConfHub.Application.Conferences;
using ConfHub.Application.Conferences.Specifications;
using ConfHub.Application.Tracks.Specifications;
using ConfHub.Domain.Common;
using ConfHub.Domain.Conferences;
using MediatR;

namespace ConfHub.Application.Tracks.CreateTrack;

public sealed class CreateTrackCommandHandler(
    IRepository<Conference> conferenceRepository,
    ICurrentUser currentUser,
    IRepository<Track> trackRepository) : IRequestHandler<CreateTrackCommand, CreatedIdResponse>
{
    public async Task<CreatedIdResponse> Handle(CreateTrackCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var conference = await conferenceRepository.FirstOrDefaultAsync(
            new ConferenceByIdSpec(request.ConferenceId), cancellationToken);
        conference = ConferenceAccess.EnsureOwned(conference, request.ConferenceId, userId);

        var existingTrack = await trackRepository.FirstOrDefaultAsync(
            new TrackByNameSpec(conference.Id, request.NameVi.Trim()), cancellationToken);
        if (existingTrack is not null)
        {
            throw new ConflictException(ConferenceErrorCodes.TrackNameTaken, "The name is already in use.");
        }

        var trackCount = await trackRepository.CountAsync(new TracksByConferenceSpec(conference.Id), cancellationToken);
        if (trackCount >= Track.MaxPerConference)
        {
            throw new DomainException(ConferenceErrorCodes.TooManyTracks, "A conference can have at most 5 tracks.");
        }

        var track = Track.Create(conference.Id, request.NameVi, request.NameEn);

        await trackRepository.AddAsync(track, cancellationToken);
        return new CreatedIdResponse(track.Id);
    }
}
