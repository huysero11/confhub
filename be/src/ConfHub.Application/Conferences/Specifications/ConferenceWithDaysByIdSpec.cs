using Ardalis.Specification;
using ConfHub.Domain.Conferences;

namespace ConfHub.Application.Conferences.Specifications;

public sealed class ConferenceWithDaysByIdSpec : Specification<Conference>
{
    public ConferenceWithDaysByIdSpec(Guid conferenceId)
    {
        Query.Where(conference => conference.Id == conferenceId);
        Query.Include(conference => conference.Days).ThenInclude(day => day.Breaks);
    }
}
