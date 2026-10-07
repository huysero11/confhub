namespace ConfHub.Application.Common.Exceptions;

// Biết là ai nhưng không được phép làm việc này (vd sửa hội nghị của BTC khác,
// đăng nhập khi tài khoản đang bị khóa). Api trả 403.
public class ForbiddenException : Exception
{
    public const string DefaultCode = "Forbidden";

    public ForbiddenException(string message)
        : this(DefaultCode, message)
    {
    }

    // code riêng để giao diện hiện đúng thông báo (vd AccountLocked).
    public ForbiddenException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}
