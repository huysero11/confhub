using ConfHub.Application.Common;
using MediatR;

namespace ConfHub.Application.Conferences.AddConferenceDay;

public sealed record AddConferenceDayCommand(Guid ConferenceId,
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime) : IRequest<CreatedIdResponse>;
