using ConfHub.Application.Accounts.Register;
using ConfHub.Domain.Accounts;

namespace ConfHub.Application.Tests.Accounts;

public class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _validator = new();

    [Fact]
    public void ValidAttendeePasses()
    {
        var result = _validator.Validate(Valid());

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("short1", "LengthValidator")]
    [InlineData("onlyletters", AccountErrorCodes.PasswordNeedsLetterAndDigit)]
    [InlineData("12345678", AccountErrorCodes.PasswordNeedsLetterAndDigit)]
    public void WeakPasswordFails(string password, string expectedCode)
    {
        var result = _validator.Validate(Valid() with { Password = password });

        Assert.Contains(result.Errors, error => error.PropertyName == "Password" && error.ErrorCode == expectedCode);
    }

    [Fact]
    public void InvalidEmailFails()
    {
        var result = _validator.Validate(Valid() with { Email = "not-an-email" });

        Assert.Contains(result.Errors, error => error.PropertyName == "Email" && error.ErrorCode == "EmailValidator");
    }

    [Theory]
    [InlineData("M")]
    [InlineData("   ")]
    public void TooShortFullNameFails(string fullName)
    {
        var result = _validator.Validate(Valid() with { FullName = fullName });

        Assert.Contains(result.Errors, error => error.PropertyName == "FullName");
    }

    [Theory]
    [InlineData(RoleCodes.Admin)]
    [InlineData(RoleCodes.Staff)]
    [InlineData("Hacker")]
    public void RoleOutsideSelfRegisterListFails(string role)
    {
        var result = _validator.Validate(Valid() with { Role = role });

        Assert.Contains(result.Errors, error => error.ErrorCode == AccountErrorCodes.RoleNotAllowed);
    }

    [Theory]
    [InlineData(RoleCodes.Organizer)]
    [InlineData(RoleCodes.Supplier)]
    public void OrganizerAndSupplierNeedOrganization(string role)
    {
        var result = _validator.Validate(Valid() with { Role = role, Organization = null });

        Assert.Contains(result.Errors, error => error.ErrorCode == AccountErrorCodes.OrganizationRequired);
    }

    private static RegisterCommand Valid()
    {
        return new RegisterCommand("Nguyễn Minh", "minh@gmail.com", "matkhau123", RoleCodes.Attendee, null);
    }
}
