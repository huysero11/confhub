namespace ConfHub.Application.Common.Exceptions;

// Trạng thái hiện tại không cho phép thao tác (vd email đã tồn tại). Api trả 409.
public class ConflictException : Exception
{
    public ConflictException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}
