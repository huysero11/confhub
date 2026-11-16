using Ardalis.Specification;
using ConfHub.Domain.Conferences;

namespace ConfHub.Application.Conferences.Specifications;

public sealed class ConferencesByOwnerSpec : Specification<Conference, ConferenceListItemResponse>
{
    public ConferencesByOwnerSpec(Guid ownerId)
    {
        Query.Where(conference => conference.OwnerId == ownerId);
        Query.OrderByDescending(conference => conference.CreatedAt);
        Query.Select(conference => new ConferenceListItemResponse(
            conference.Id,
            conference.NameVi,
            conference.NameEn,
            conference.Venue,
            conference.Status.ToString(),
            conference.Days.Min(day => day.Date),
            conference.Days.Max(day => day.Date),
            0));
    }
}
