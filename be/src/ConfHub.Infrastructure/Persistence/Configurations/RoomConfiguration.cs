using ConfHub.Domain.Conferences;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConfHub.Infrastructure.Persistence.Configurations;

public sealed class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    // Bảng Rooms: unique (ConferenceId, Name) — collation CI nên "P101" trùng "p101"; xóa hội nghị xóa theo phòng.
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.ToTable("Rooms");
        builder.Ignore(room => room.DomainEvents);
        builder.Property(room => room.Name).HasMaxLength(50).IsRequired();
        builder.HasIndex(room => new { room.ConferenceId, room.Name }).IsUnique();
        builder.HasOne<Conference>().WithMany().HasForeignKey(room => room.ConferenceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
