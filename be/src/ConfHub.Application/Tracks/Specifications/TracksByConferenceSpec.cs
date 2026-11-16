using Ardalis.Specification;
using ConfHub.Domain.Conferences;

namespace ConfHub.Application.Tracks.Specifications;

public sealed class TracksByConferenceSpec : Specification<Track, TrackResponse>
{
    public TracksByConferenceSpec(Guid conferenceId)
    {
        Query.Where(track => track.ConferenceId == conferenceId);
        Query.OrderBy(track => track.CreatedAt).ThenBy(track => track.Id);
        Query.Select(track => new TrackResponse(track.Id, track.NameVi, track.NameEn));
    }
}
