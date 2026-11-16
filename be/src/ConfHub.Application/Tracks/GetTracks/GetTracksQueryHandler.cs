using ConfHub.Application.Common.Persistence;
using ConfHub.Application.Common.Security;
using ConfHub.Application.Conferences;
using ConfHub.Application.Conferences.Specifications;
using ConfHub.Application.Tracks.Specifications;
using ConfHub.Domain.Conferences;
using MediatR;

namespace ConfHub.Application.Tracks.GetTracks;

public sealed class GetTracksQueryHandler(
    IReadRepository<Conference> conferenceRepository,
    ICurrentUser currentUser,
    IReadRepository<Track> trackRepository) : IRequestHandler<GetTracksQuery, List<TrackResponse>>
{
    public async Task<List<TrackResponse>> Handle(GetTracksQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var conference = await conferenceRepository.FirstOrDefaultAsync(
            new ConferenceByIdSpec(request.ConferenceId), cancellationToken);
        conference = ConferenceAccess.EnsureOwned(conference, request.ConferenceId, userId);

        return await trackRepository.ListAsync(new TracksByConferenceSpec(conference.Id), cancellationToken);
    }
}
