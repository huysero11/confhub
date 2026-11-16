namespace ConfHub.Api.Controllers.Manage.Requests;

public sealed record ConferenceDayRequest(
    DateOnly Date,
    TimeOnly StartTime,
    TimeOnly EndTime);
