using ConfHub.Application.Common.Exceptions;
using ConfHub.Application.Conferences;

namespace ConfHub.Application.Tests.Conferences;

public class ConferenceAccessTests
{
    // Không tiết lộ hội nghị của người khác; trả cùng 404 như id không tồn tại.
    [Fact]
    public void MissingOrForeignConferenceIsNotFound()
    {
        var conference = ConferenceTestData.Create();
        Assert.Throws<NotFoundException>(() => ConferenceAccess.EnsureOwned(null, conference.Id, conference.OwnerId));
        Assert.Throws<NotFoundException>(() => ConferenceAccess.EnsureOwned(conference, conference.Id, Guid.NewGuid()));
        Assert.Same(conference, ConferenceAccess.EnsureOwned(conference, conference.Id, conference.OwnerId));
    }
}
