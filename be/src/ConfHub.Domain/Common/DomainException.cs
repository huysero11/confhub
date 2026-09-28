namespace ConfHub.Domain.Common;

// Ném khi aggregate phát hiện vi phạm quy tắc nghiệp vụ (bất biến), vd diễn giả trùng giờ.
public class DomainException : Exception
{
    public DomainException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}
