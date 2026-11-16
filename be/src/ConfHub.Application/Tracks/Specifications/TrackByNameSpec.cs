using Ardalis.Specification;
using ConfHub.Domain.Conferences;

namespace ConfHub.Application.Tracks.Specifications;

public sealed class TrackByNameSpec : Specification<Track>
{
    public TrackByNameSpec(Guid conferenceId, string nameVi)
    {
        Query.Where(track => track.ConferenceId == conferenceId && track.NameVi == nameVi);
    }
}
