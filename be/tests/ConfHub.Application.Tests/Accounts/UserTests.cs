using ConfHub.Domain.Accounts;
using ConfHub.Domain.Accounts.Events;
using ConfHub.Domain.Common;

namespace ConfHub.Application.Tests.Accounts;

public class UserTests
{
    [Fact]
    public void RegisterNormalizesEmailAndRaisesEvent()
    {
        var user = User.Register("  Minh.Nguyen@Gmail.COM ", "hash", "  Nguyễn Minh ", TestRoles.Create(RoleCodes.Attendee), null);

        Assert.Equal("minh.nguyen@gmail.com", user.Email);
        Assert.Equal("Nguyễn Minh", user.FullName);
        Assert.Equal(UserStatus.Unverified, user.Status);
        var domainEvent = Assert.IsType<UserRegistered>(Assert.Single(user.DomainEvents));
        Assert.Same(user, domainEvent.User);
    }

    [Theory]
    [InlineData(RoleCodes.Staff)]
    [InlineData(RoleCodes.Admin)]
    public void RegisterRejectsRolesThatCannotSelfRegister(string roleCode)
    {
        var exception = Assert.Throws<DomainException>(
            () => User.Register("a@b.com", "hash", "Minh", TestRoles.Create(roleCode), "Org"));

        Assert.Equal(AccountErrorCodes.RoleNotAllowed, exception.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void RegisterRequiresOrganizationForOrganizer(string? organization)
    {
        var exception = Assert.Throws<DomainException>(
            () => User.Register("a@b.com", "hash", "Lan", TestRoles.Create(RoleCodes.Organizer), organization));

        Assert.Equal(AccountErrorCodes.OrganizationRequired, exception.Code);
    }

    [Theory]
    [InlineData(RoleCodes.Attendee, UserStatus.Active)]
    [InlineData(RoleCodes.Organizer, UserStatus.PendingApproval)]
    [InlineData(RoleCodes.Supplier, UserStatus.PendingApproval)]
    public void VerifyEmailMovesToStatusByRole(string roleCode, UserStatus expectedStatus)
    {
        var user = User.Register("a@b.com", "hash", "Minh", TestRoles.Create(roleCode), "Org");

        user.VerifyEmail();

        Assert.Equal(expectedStatus, user.Status);
    }

    [Fact]
    public void VerifyEmailTwiceThrows()
    {
        var user = User.Register("a@b.com", "hash", "Minh", TestRoles.Create(RoleCodes.Attendee), null);
        user.VerifyEmail();

        Assert.Throws<DomainException>(user.VerifyEmail);
    }

    [Fact]
    public void RequestPasswordResetNeedsVerifiedAccount()
    {
        var user = User.Register("a@b.com", "hash", "Minh", TestRoles.Create(RoleCodes.Attendee), null);
        user.ClearDomainEvents();

        Assert.Throws<DomainException>(user.RequestPasswordReset);

        user.VerifyEmail();
        user.RequestPasswordReset();

        Assert.IsType<PasswordResetRequested>(Assert.Single(user.DomainEvents));
    }

    [Fact]
    public void RequestEmailVerificationOnlyWhenUnverified()
    {
        var user = User.Register("a@b.com", "hash", "Minh", TestRoles.Create(RoleCodes.Attendee), null);
        user.ClearDomainEvents();

        user.RequestEmailVerification();
        Assert.IsType<EmailVerificationRequested>(Assert.Single(user.DomainEvents));

        user.VerifyEmail();
        Assert.Throws<DomainException>(user.RequestEmailVerification);
    }

    [Fact]
    public void ResetPasswordChangesHash()
    {
        var user = User.Register("a@b.com", "old-hash", "Minh", TestRoles.Create(RoleCodes.Attendee), null);
        user.VerifyEmail();

        user.ResetPassword("new-hash");

        Assert.Equal("new-hash", user.PasswordHash);
    }

    [Theory]
    [InlineData(RoleCodes.Admin)]
    [InlineData(RoleCodes.Staff)]
    [InlineData(RoleCodes.Organizer)]
    public void CreateActiveWorksForAnyRoleAndRaisesNoEvent(string roleCode)
    {
        var user = User.CreateActive("  Admin@ConfHub.Local ", "hash", "  Quản trị ", TestRoles.Create(roleCode), "  ");

        Assert.Equal("admin@confhub.local", user.Email);
        Assert.Equal("Quản trị", user.FullName);
        Assert.Null(user.Organization);
        Assert.Equal(UserStatus.Active, user.Status);
        Assert.Equal(roleCode, user.Role.Code);

        // Không có sự kiện → không gửi email xác thực.
        Assert.Empty(user.DomainEvents);
    }
}
