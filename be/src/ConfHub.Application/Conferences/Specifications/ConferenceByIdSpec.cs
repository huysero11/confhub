using Ardalis.Specification;
using ConfHub.Domain.Conferences;

namespace ConfHub.Application.Conferences.Specifications;

public sealed class ConferenceByIdSpec : Specification<Conference>
{
    public ConferenceByIdSpec(Guid conferenceId)
    {
        Query.Where(conference => conference.Id == conferenceId);
    }
}
