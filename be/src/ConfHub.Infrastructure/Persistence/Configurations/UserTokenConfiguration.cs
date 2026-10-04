using ConfHub.Domain.Accounts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConfHub.Infrastructure.Persistence.Configurations;

public sealed class UserTokenConfiguration : IEntityTypeConfiguration<UserToken>
{
    public void Configure(EntityTypeBuilder<UserToken> builder)
    {
        builder.ToTable("UserTokens");
        builder.Ignore(token => token.DomainEvents);

        builder.Property(token => token.Purpose).HasConversion<string>().HasMaxLength(30);

        // SHA-256 dạng hex = 64 ký tự. Unique: tra token theo hash.
        builder.Property(token => token.TokenHash).HasMaxLength(64).IsRequired();
        builder.HasIndex(token => token.TokenHash).IsUnique();

        // Truy vấn hay dùng: token của 1 người theo mục đích.
        builder.HasIndex(token => new { token.UserId, token.Purpose });

        // UserToken là aggregate riêng nên không có navigation, chỉ khóa ngoại.
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
