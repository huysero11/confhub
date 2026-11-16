using ConfHub.Application.Common.Exceptions;
using ConfHub.Domain.Conferences;

namespace ConfHub.Application.Conferences;

public static class ConferenceAccess
{
    // Trả cùng lỗi 404 cho dữ liệu không tồn tại và hội nghị của người khác.
    public static Conference EnsureOwned(Conference? conference, Guid conferenceId, Guid userId)
    {
        if (conference is null || !conference.IsOwnedBy(userId))
        {
            throw new NotFoundException(nameof(Conference), conferenceId);
        }

        return conference;
    }
}
