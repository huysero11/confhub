using ConfHub.Domain.Accounts;
using FluentValidation;

namespace ConfHub.Application.Accounts;

public static class PasswordRules
{
    public const int MinLength = 8;
    public const int MaxLength = 128;

    /*
        Extension method cho FluentValidation: viết RuleFor(x => x.Password).StrongPassword()
        thay vì lặp lại 3 quy tắc ở mỗi validator (đăng ký, đặt lại mật khẩu).
        - IRuleBuilder<T, string>: rule đang viết cho 1 thuộc tính kiểu string của class T.
        - IRuleBuilderOptions<T, string>: rule đã gắn validator, còn nối tiếp được
          (vd .WithMessage(...)).
    */
    public static IRuleBuilderOptions<T, string> StrongPassword<T>(
        this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty()
            .Length(MinLength, MaxLength)
            .Must(HasLetterAndDigit)
            .WithErrorCode(AccountErrorCodes.PasswordNeedsLetterAndDigit)
            .WithMessage("Password must contain at least one letter and one digit.");
    }

    private static bool HasLetterAndDigit(string? password)
    {
        if (password is null)
        {
            return false;
        }

        var hasLetter = false;
        var hasDigit = false;

        foreach (var character in password)
        {
            if (char.IsLetter(character))
            {
                hasLetter = true;
            }
            else if (char.IsDigit(character))
            {
                hasDigit = true;
            }
        }

        return hasLetter && hasDigit;
    }
}
