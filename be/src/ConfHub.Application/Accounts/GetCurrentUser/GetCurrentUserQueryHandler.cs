using ConfHub.Application.Accounts.Specifications;
using ConfHub.Application.Common.Exceptions;
using ConfHub.Application.Common.Persistence;
using ConfHub.Application.Common.Security;
using ConfHub.Domain.Accounts;
using MediatR;

namespace ConfHub.Application.Accounts.GetCurrentUser;

// Mẫu dùng ICurrentUser: lấy UserId từ token, rồi đọc dữ liệu mới nhất của User qua repository.
public sealed class GetCurrentUserQueryHandler(
    ICurrentUser currentUser,
    IReadRepository<User> userRepository) : IRequestHandler<GetCurrentUserQuery, CurrentUserResponse>
{
    public async Task<CurrentUserResponse> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var user = await userRepository.FirstOrDefaultAsync(
            new UserWithRoleByIdSpec(currentUser.UserId),
            cancellationToken);

        // Token còn hạn nhưng tài khoản đã bị xóa → coi như phiên hết hạn.
        if (user is null)
        {
            throw new UnauthorizedException(AccountErrorCodes.SessionExpired, "The session has expired. Please sign in again.");
        }

        return CurrentUserResponse.FromUser(user);
    }
}
