using ConfHub.Domain.Accounts;
using ConfHub.Domain.Conferences;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConfHub.Infrastructure.Persistence.Configurations;

public sealed class ConferenceConfiguration : IEntityTypeConfiguration<Conference>
{
    public void Configure(EntityTypeBuilder<Conference> builder)
    {
        builder.ToTable("Conferences");
        builder.Ignore(conference => conference.DomainEvents);
        builder.Property(conference => conference.NameVi).HasMaxLength(200).IsRequired();
        builder.Property(conference => conference.NameEn).HasMaxLength(200).IsRequired();
        builder.Property(conference => conference.DescriptionVi).HasMaxLength(4000);
        builder.Property(conference => conference.DescriptionEn).HasMaxLength(4000);
        builder.Property(conference => conference.Venue).HasMaxLength(300).IsRequired();
        builder.Property(conference => conference.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(conference => conference.RegistrationCloseAt)
            .HasColumnType("datetime2")
            .HasConversion(utc => utc, stored => DateTime.SpecifyKind(stored, DateTimeKind.Utc));
        builder.HasIndex(conference => conference.OwnerId);
        builder.HasOne<User>().WithMany().HasForeignKey(conference => conference.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(conference => conference.Days).WithOne().HasForeignKey(day => day.ConferenceId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(conference => conference.Days).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
