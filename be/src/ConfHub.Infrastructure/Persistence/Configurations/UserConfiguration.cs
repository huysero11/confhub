using ConfHub.Domain.Accounts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConfHub.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.Ignore(user => user.DomainEvents);

        builder.Property(user => user.Email).HasMaxLength(256).IsRequired();
        builder.HasIndex(user => user.Email).IsUnique();
        builder.Property(user => user.PasswordHash).HasMaxLength(256).IsRequired();
        builder.Property(user => user.FullName).HasMaxLength(100).IsRequired();
        builder.Property(user => user.Organization).HasMaxLength(200);
        builder.Property(user => user.Bio).HasMaxLength(1000);

        // Enum lưu dạng chuỗi ("Active"...) → đọc thẳng trong CSDL được.
        builder.Property(user => user.Status).HasConversion<string>().HasMaxLength(30);

        // Không cho xóa Role khi còn User dùng nó.
        builder.HasOne(user => user.Role)
            .WithMany()
            .HasForeignKey(user => user.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
