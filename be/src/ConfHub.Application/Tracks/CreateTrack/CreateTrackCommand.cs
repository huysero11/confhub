using ConfHub.Application.Common;
using MediatR;

namespace ConfHub.Application.Tracks.CreateTrack;

public sealed record CreateTrackCommand(Guid ConferenceId,
    string NameVi,
    string NameEn) : IRequest<CreatedIdResponse>;
