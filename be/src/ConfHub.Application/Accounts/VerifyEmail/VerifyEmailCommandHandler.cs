using ConfHub.Application.Accounts.Specifications;
using ConfHub.Application.Common.Persistence;
using ConfHub.Application.Common.Security;
using ConfHub.Domain.Accounts;
using ConfHub.Domain.Common;
using MediatR;

namespace ConfHub.Application.Accounts.VerifyEmail;

public sealed class VerifyEmailCommandHandler(
    IRepository<UserToken> userTokenRepository,
    IRepository<User> userRepository,
    TimeProvider timeProvider) : IRequestHandler<VerifyEmailCommand, VerifyEmailResponse>
{
    public async Task<VerifyEmailResponse> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        // Lấy token từ DB theo hash
        var tokenHash = RandomToken.Hash(request.Token);
        var userToken = await userTokenRepository.FirstOrDefaultAsync(new TokenByHashSpec(tokenHash), cancellationToken);
        if (userToken is null || userToken.Purpose != TokenPurpose.VerifyEmail || !userToken.IsActive(now))
        {
            throw TokenInvalid();
        }

        // Lấy user kèm role
        var user = await userRepository.FirstOrDefaultAsync(new UserWithRoleByIdSpec(userToken.UserId), cancellationToken);
        if (user is null || user.Status != UserStatus.Unverified)
        {
            throw TokenInvalid();
        }

        // Xác thực email
        userToken.MarkUsed(now);
        user.VerifyEmail();

        // Lưu thật xuống CSDL. Token và User cùng nằm trong 1 DbContext
        // → 1 lần SaveChanges = 1 transaction. Thiếu dòng này thì không có gì được lưu.
        await userRepository.SaveChangesAsync(cancellationToken);

        return new VerifyEmailResponse(user.Status.ToString());
    }

    // Mọi trường hợp token sai (không có, hết hạn, đã dùng, sai mục đích) trả cùng 1 lỗi.
    private static DomainException TokenInvalid()
    {
        return new DomainException(AccountErrorCodes.TokenInvalid, "The link is invalid or has expired.");
    }
}
