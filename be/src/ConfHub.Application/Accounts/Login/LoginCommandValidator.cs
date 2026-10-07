using FluentValidation;

namespace ConfHub.Application.Accounts.Login;

// Chỉ kiểm tra "có nhập". KHÔNG áp quy tắc mật khẩu mạnh ở đây: quy tắc có thể đổi về sau,
// người đặt mật khẩu theo quy tắc cũ vẫn phải đăng nhập được.
public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(command => command.Password)
            .NotEmpty()
            .MaximumLength(PasswordRules.MaxLength);
    }
}
