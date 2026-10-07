namespace ConfHub.Domain.Accounts;

// Mã lỗi nghiệp vụ của nhóm Tài khoản, trả về trong trường "code" của response lỗi để FE dịch.
public static class AccountErrorCodes
{
    public const string EmailTaken = "EmailTaken";
    public const string TokenInvalid = "TokenInvalid";
    public const string RoleNotAllowed = "RoleNotAllowed";
    public const string OrganizationRequired = "OrganizationRequired";
    public const string PasswordNeedsLetterAndDigit = "PasswordNeedsLetterAndDigit";
    public const string InvalidAccountStatus = "InvalidAccountStatus";
    public const string TokenAlreadyUsed = "TokenAlreadyUsed";

    // Đăng nhập / phiên (T1.4)
    public const string InvalidCredentials = "InvalidCredentials";
    public const string AccountUnverified = "AccountUnverified";
    public const string AccountPending = "AccountPending";
    public const string AccountLocked = "AccountLocked";
    public const string SessionExpired = "SessionExpired";
}
