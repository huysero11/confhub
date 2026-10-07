namespace ConfHub.Application.Common.Exceptions;

// Chưa xác định được người gọi: sai email / mật khẩu, phiên hết hạn, thiếu token. Api trả 401.
// (403 ForbiddenException thì khác: biết là ai nhưng không được phép.)
public class UnauthorizedException : Exception
{
    public const string DefaultCode = "Unauthorized";

    public UnauthorizedException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}
