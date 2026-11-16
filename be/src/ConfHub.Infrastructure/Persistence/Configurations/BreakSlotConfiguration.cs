using ConfHub.Domain.Conferences;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConfHub.Infrastructure.Persistence.Configurations;

public sealed class BreakSlotConfiguration : IEntityTypeConfiguration<BreakSlot>
{
    public void Configure(EntityTypeBuilder<BreakSlot> builder)
    {
        builder.ToTable("BreakSlots");
        builder.Ignore(breakSlot => breakSlot.DomainEvents);

        // Id do aggregate tự cấp (Guid v7) → báo EF không tự sinh; nếu không, EF thấy Id đã có giá trị
        // sẽ tưởng là bản ghi cũ và chạy UPDATE thay vì INSERT khi thêm vào aggregate đã lưu.
        builder.Property(breakSlot => breakSlot.Id).ValueGeneratedNever();
        builder.Property(breakSlot => breakSlot.Name).HasMaxLength(100).IsRequired();
        builder.ComplexProperty(breakSlot => breakSlot.Time, time =>
        {
            time.Property(range => range.Start).HasColumnName("StartTime").HasColumnType("time(0)");
            time.Property(range => range.End).HasColumnName("EndTime").HasColumnType("time(0)");
            time.Ignore(range => range.DurationMinutes);
        });
    }
}
