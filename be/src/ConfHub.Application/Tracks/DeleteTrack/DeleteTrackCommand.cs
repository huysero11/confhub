using MediatR;

namespace ConfHub.Application.Tracks.DeleteTrack;

public sealed record DeleteTrackCommand(Guid ConferenceId,
    Guid TrackId) : IRequest;
