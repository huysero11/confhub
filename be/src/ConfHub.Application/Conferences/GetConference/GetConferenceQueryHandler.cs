using ConfHub.Application.Common.Persistence;
using ConfHub.Application.Common.Security;
using ConfHub.Application.Conferences.Specifications;
using ConfHub.Domain.Conferences;
using MediatR;

namespace ConfHub.Application.Conferences.GetConference;

public sealed class GetConferenceQueryHandler(
    IReadRepository<Conference> conferenceRepository,
    ICurrentUser currentUser) : IRequestHandler<GetConferenceQuery, ConferenceDetailResponse>
{
    public async Task<ConferenceDetailResponse> Handle(GetConferenceQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var conference = await conferenceRepository.FirstOrDefaultAsync(
            new ConferenceWithDaysByIdSpec(request.ConferenceId), cancellationToken);

        // biến conference ở trên lấy từ id trong url, nên có thể là của người khác nếu gõ id của người khác trên url
        // nên phải kiểm tra ownerId của conference đấy có giống userId từ currentUser không
        conference = ConferenceAccess.EnsureOwned(conference, request.ConferenceId, userId);

        return ConferenceDetailResponse.FromConference(conference);
    }
}
