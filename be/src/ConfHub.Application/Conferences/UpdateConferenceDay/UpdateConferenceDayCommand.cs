using MediatR;

namespace ConfHub.Application.Conferences.UpdateConferenceDay;

public sealed record UpdateConferenceDayCommand(Guid ConferenceId,
    Guid DayId,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime) : IRequest;
