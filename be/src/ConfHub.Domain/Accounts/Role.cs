using ConfHub.Domain.Common;

namespace ConfHub.Domain.Accounts;

// Vai trò = tập hợp quyền. 5 vai trò được seed sẵn trong CSDL (xem RoleCodes).
public sealed class Role : BaseEntity, IAggregateRoot
{
    private Role()
    {
    }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;

    // Lưu dạng json
    public List<string> Permissions { get; private set; } = [];
    public bool CanSelfRegister =>
        Code is RoleCodes.Attendee or RoleCodes.Organizer or RoleCodes.Supplier;
    public bool RequiresApproval => Code is RoleCodes.Organizer or RoleCodes.Supplier;

    public static Role Create(Guid id, string code, string name)
    {
        return new Role
        {
            Id = id,
            Code = code,
            Name = name,
        };
    }
}
