using ConfHub.Application.Common.Security;
using Microsoft.AspNetCore.Identity;

namespace ConfHub.Infrastructure.Security;

public sealed class AspNetPasswordHasher : IPasswordHasher
{
    // _userPlaceholder chỉ là một object giả để truyền vào PasswordHasher<TUser>,
    // vì API của ASP.NET Identity bắt buộc phải nhận một user.
    private static readonly object _userPlaceholder = new();
    private readonly PasswordHasher<object> _passwordHasher = new();

    public string Hash(string password)
    {
        return _passwordHasher.HashPassword(_userPlaceholder, password);
    }

    public bool Verify(string passwordHash, string password)
    {
        var result = _passwordHasher.VerifyHashedPassword(_userPlaceholder, passwordHash, password);

        // SuccessRehashNeeded: đúng mật khẩu, chỉ là bản băm theo thuật toán cũ hơn.
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
