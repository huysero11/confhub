using MediatR;

namespace ConfHub.Application.Conferences.GetConference;

public sealed record GetConferenceQuery(Guid ConferenceId) : IRequest<ConferenceDetailResponse>;
