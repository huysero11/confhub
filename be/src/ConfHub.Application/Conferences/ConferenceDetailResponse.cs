using ConfHub.Domain.Conferences;

namespace ConfHub.Application.Conferences;

public sealed record ConferenceDetailResponse(
    Guid Id,
    string NameVi,
    string NameEn,
    string? DescriptionVi,
    string? DescriptionEn,
    string Venue,
    DateTime RegistrationCloseAt,
    bool RequireApproval,
    string Status,
    List<ConferenceDayResponse> Days)
{
    public static ConferenceDetailResponse FromConference(Conference conference)
    {
        var days = new List<ConferenceDayResponse>();
        foreach (var day in conference.Days.OrderBy(day => day.Date))
        {
            days.Add(ConferenceDayResponse.FromConferenceDay(day));
        }

        return new ConferenceDetailResponse(
            conference.Id,
            conference.NameVi,
            conference.NameEn,
            conference.DescriptionVi,
            conference.DescriptionEn,
            conference.Venue,
            conference.RegistrationCloseAt,
            conference.RequireApproval,
            conference.Status.ToString(),
            days);
    }
}
