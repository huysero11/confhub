using MediatR;

namespace ConfHub.Application.Tracks.GetTracks;

public sealed record GetTracksQuery(Guid ConferenceId) : IRequest<List<TrackResponse>>;
