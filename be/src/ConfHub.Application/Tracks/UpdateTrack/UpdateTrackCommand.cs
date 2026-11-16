using MediatR;

namespace ConfHub.Application.Tracks.UpdateTrack;

public sealed record UpdateTrackCommand(Guid ConferenceId,
    Guid TrackId,
    string NameVi,
    string NameEn) : IRequest;
