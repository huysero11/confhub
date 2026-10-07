using ConfHub.Application.Accounts.Login;

namespace ConfHub.Application.Tests.Accounts;

public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    [Fact]
    public void WeakPasswordIsStillAccepted()
    {
        // Đăng nhập không áp quy tắc mật khẩu mạnh: mật khẩu đặt theo quy tắc cũ vẫn phải vào được.
        var result = _validator.Validate(new LoginCommand("minh@gmail.com", "abc"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void EmptyEmailAndPasswordFail()
    {
        var result = _validator.Validate(new LoginCommand(string.Empty, string.Empty));

        Assert.Contains(result.Errors, error => error.PropertyName == "Email");
        Assert.Contains(result.Errors, error => error.PropertyName == "Password");
    }
}
