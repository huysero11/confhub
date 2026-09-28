namespace ConfHub.Application.Common.Exceptions;

// Đã đăng nhập nhưng không có quyền trên dữ liệu này (vd sửa hội nghị của BTC khác). Api trả 403.
public class ForbiddenException : Exception
{
    public ForbiddenException(string message)
        : base(message)
    {
    }
}
