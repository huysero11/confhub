namespace ConfHub.Application.Common.Exceptions;

// Không tìm thấy dữ liệu theo khóa. Api trả 404.
public class NotFoundException : Exception
{
    public NotFoundException(string entityName, object key)
        : base($"{entityName} '{key}' was not found.")
    {
    }
}
