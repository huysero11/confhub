using ConfHub.Domain.Accounts.Events;
using ConfHub.Domain.Common;

namespace ConfHub.Domain.Accounts;

public sealed class User : BaseEntity, IAggregateRoot
{
    private User()
    {
    }

    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;

    public Guid RoleId { get; private set; }
    public Role Role { get; private set; } = null!;

    public UserStatus Status { get; private set; }

    public string? Organization { get; private set; }
    public string? Bio { get; private set; }

    // Chỉ tài khoản đã xác thực email và chưa bị khóa mới được đặt lại mật khẩu.
    public bool CanResetPassword
        => Status is UserStatus.Active or UserStatus.PendingApproval;

    public static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }

    public static User Register(
        string email, string passwordHash, string fullName, Role role, string? organization)
    {
        ArgumentNullException.ThrowIfNull(role);

        if (!role.CanSelfRegister)
        {
            throw new DomainException(
                AccountErrorCodes.RoleNotAllowed,
                $"The role '{role.Code}' is not allowed for self-registration.");
        }

        var trimmedOrganization = TrimOrNull(organization);

        if (role.RequiresApproval && trimmedOrganization is null)
        {
            throw new DomainException(
                AccountErrorCodes.OrganizationRequired,
                $"The role '{role.Code}' requires an organization to be provided.");
        }

        var user = new User
        {
            Email = NormalizeEmail(email),
            PasswordHash = passwordHash,
            FullName = fullName.Trim(),
            RoleId = role.Id,
            Role = role,
            Organization = trimmedOrganization,
            Status = UserStatus.Unverified
        };
        user.AddDomainEvent(new UserRegistered(user));
        return user;
    }

    // Tạo tài khoản dùng được ngay: không qua xác thực email, không ghi sự kiện (không gửi mail),
    // vai trò nào cũng tạo được. Dùng cho tài khoản mẫu (dev); sau này cho quản trị viên tạo tài khoản.
    public static User CreateActive(
        string email, string passwordHash, string fullName, Role role, string? organization)
    {
        ArgumentNullException.ThrowIfNull(role);

        return new User
        {
            Email = NormalizeEmail(email),
            PasswordHash = passwordHash,
            FullName = fullName.Trim(),
            RoleId = role.Id,
            Role = role,
            Organization = TrimOrNull(organization),
            Status = UserStatus.Active
        };
    }

    // Yêu cầu gửi (lại) email xác thực: chỉ ghi sự kiện, consumer mới tạo token và gửi mail.
    public void RequestEmailVerification()
    {
        EnsureStatus(Status == UserStatus.Unverified);
        AddDomainEvent(new EmailVerificationRequested(this));
    }

    // Cần Role đã được nạp: vai trò quyết định trạng thái tiếp theo.
    public void VerifyEmail()
    {
        EnsureStatus(Status == UserStatus.Unverified);

        if (Role.RequiresApproval)
        {
            Status = UserStatus.PendingApproval;
        }
        else
        {
            Status = UserStatus.Active;
        }
    }

    public void RequestPasswordReset()
    {
        EnsureStatus(CanResetPassword);
        AddDomainEvent(new PasswordResetRequested(this));
    }

    public void ResetPassword(string newPasswordHash)
    {
        EnsureStatus(CanResetPassword);
        PasswordHash = newPasswordHash;
    }

    // Chuỗi rỗng / toàn khoảng trắng coi như không nhập.
    private static string? TrimOrNull(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }

    private void EnsureStatus(bool isAllowed)
    {
        if (!isAllowed)
        {
            throw new DomainException(
                AccountErrorCodes.InvalidAccountStatus,
                $"This action is not allowed for the current account status: {Status}");
        }
    }
}
