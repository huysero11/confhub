using ConfHub.Domain.Conferences;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConfHub.Infrastructure.Persistence.Configurations;

public sealed class ConferenceDayConfiguration : IEntityTypeConfiguration<ConferenceDay>
{
    public void Configure(EntityTypeBuilder<ConferenceDay> builder)
    {
        builder.ToTable("ConferenceDays");
        builder.Ignore(conferenceDay => conferenceDay.DomainEvents);

        // Id do aggregate tự cấp (Guid v7) → báo EF không tự sinh; nếu không, EF thấy Id đã có giá trị
        // sẽ tưởng là bản ghi cũ và chạy UPDATE thay vì INSERT khi thêm vào aggregate đã lưu.
        builder.Property(day => day.Id).ValueGeneratedNever();
        builder.Property(day => day.Date).HasColumnType("date");
        builder.HasIndex(day => new { day.ConferenceId, day.Date }).IsUnique();
        builder.ComplexProperty(day => day.Hours, hours =>
        {
            hours.Property(time => time.Start).HasColumnName("StartTime").HasColumnType("time(0)");
            hours.Property(time => time.End).HasColumnName("EndTime").HasColumnType("time(0)");
            hours.Ignore(time => time.DurationMinutes);
        });
        builder.HasMany(day => day.Breaks).WithOne().HasForeignKey(breakSlot => breakSlot.ConferenceDayId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(day => day.Breaks).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
