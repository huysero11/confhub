using ConfHub.Domain.Accounts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConfHub.Infrastructure.Persistence.Configurations;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    // Seed cố định: Id và thời điểm không đổi giữa các lần chạy migration.
    private static readonly DateTime _seedCreatedAt = new(2026, 9, 29, 0, 0, 0, DateTimeKind.Utc);

    // Ánh xạ vai trò và seed quyền cố định qua migration.
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");
        builder.Ignore(role => role.DomainEvents);

        builder.Property(role => role.Code).HasMaxLength(30).IsRequired();
        builder.HasIndex(role => role.Code).IsUnique();
        builder.Property(role => role.Name).HasMaxLength(100).IsRequired();

        // List<string> → EF Core tự lưu thành 1 cột JSON, vd ["Schedule.Run"].
        builder.PrimitiveCollection(role => role.Permissions).IsRequired();

        // Mỗi nhóm nghiệp vụ bổ sung quyền vào seed khi triển khai.
        builder.HasData(
            SeedRole("0199e0a0-0000-7000-8000-000000000001", RoleCodes.Attendee, "Người tham dự"),
            SeedRole("0199e0a0-0000-7000-8000-000000000002", RoleCodes.Organizer, "Ban tổ chức"),
            SeedRole("0199e0a0-0000-7000-8000-000000000003", RoleCodes.Supplier, "Nhà cung cấp"),
            SeedRole("0199e0a0-0000-7000-8000-000000000004", RoleCodes.Staff, "Nhân viên vận hành"),
            SeedRole("0199e0a0-0000-7000-8000-000000000005", RoleCodes.Admin, "Quản trị viên"));
    }

    // Helper của Configure (cùng lớp): Tạo dữ liệu seed, cấp quyền G3 cho Organizer.
    private static object SeedRole(string id, string code, string name)
    {
        var permissions = new List<string>();
        if (code == RoleCodes.Organizer)
        {
            permissions.Add(Permissions.ConferenceManage);
            permissions.Add(Permissions.RoomManage);
            permissions.Add(Permissions.SessionManage);
        }

        return new
        {
            Id = Guid.Parse(id),
            Code = code,
            Name = name,
            Permissions = permissions,
            CreatedAt = _seedCreatedAt,
        };
    }
}
