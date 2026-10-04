using ConfHub.Application.Accounts.ResetPassword;

namespace ConfHub.Application.Tests.Accounts;

public class ResetPasswordCommandValidatorTests
{
    private readonly ResetPasswordCommandValidator _validator = new();

    [Fact]
    public void ValidCommandPasses()
    {
        var result = _validator.Validate(new ResetPasswordCommand("token", "matkhau123"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void EmptyTokenAndWeakPasswordFail()
    {
        var result = _validator.Validate(new ResetPasswordCommand(string.Empty, "short"));

        Assert.Contains(result.Errors, error => error.PropertyName == "Token");
        Assert.Contains(result.Errors, error => error.PropertyName == "NewPassword");
    }
}
