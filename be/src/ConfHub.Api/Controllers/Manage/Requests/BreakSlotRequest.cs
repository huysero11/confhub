namespace ConfHub.Api.Controllers.Manage.Requests;

public sealed record BreakSlotRequest(
    string Name,
    TimeOnly StartTime,
    TimeOnly EndTime);
