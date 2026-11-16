using MediatR;

namespace ConfHub.Application.Conferences.DeleteBreakSlot;

public sealed record DeleteBreakSlotCommand(Guid ConferenceId,
    Guid DayId,
    Guid BreakId) : IRequest;
