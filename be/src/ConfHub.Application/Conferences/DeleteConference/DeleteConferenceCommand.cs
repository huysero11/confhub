using MediatR;

namespace ConfHub.Application.Conferences.DeleteConference;

public sealed record DeleteConferenceCommand(Guid ConferenceId) : IRequest;
