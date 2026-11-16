using ConfHub.Application.Common;
using MediatR;

namespace ConfHub.Application.Conferences.AddBreakSlot;

public sealed record AddBreakSlotCommand(Guid ConferenceId,
    Guid DayId,
    string Name,
    TimeOnly StartTime,
    TimeOnly EndTime) : IRequest<CreatedIdResponse>;
