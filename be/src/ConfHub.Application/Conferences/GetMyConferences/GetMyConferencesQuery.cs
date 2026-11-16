using MediatR;

namespace ConfHub.Application.Conferences.GetMyConferences;

public sealed record GetMyConferencesQuery() : IRequest<List<ConferenceListItemResponse>>;
