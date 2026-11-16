using MediatR;

namespace ConfHub.Application.Conferences.DeleteConferenceDay;

public sealed record DeleteConferenceDayCommand(Guid ConferenceId,
    Guid DayId) : IRequest;
