using MediatR;

namespace ConfHub.Application.Conferences.UpdateBreakSlot;

public sealed record UpdateBreakSlotCommand(
    Guid ConferenceId,
    Guid DayId,
    Guid BreakId,
    string Name,
    TimeOnly StartTime,
    TimeOnly EndTime) : IRequest;
