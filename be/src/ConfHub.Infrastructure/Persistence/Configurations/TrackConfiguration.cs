using ConfHub.Domain.Conferences;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConfHub.Infrastructure.Persistence.Configurations;

public sealed class TrackConfiguration : IEntityTypeConfiguration<Track>
{
    public void Configure(EntityTypeBuilder<Track> builder)
    {
        builder.ToTable("Tracks");
        builder.Ignore(track => track.DomainEvents);
        builder.Property(track => track.NameVi).HasMaxLength(100).IsRequired();
        builder.Property(track => track.NameEn).HasMaxLength(100).IsRequired();
        builder.HasIndex(track => new { track.ConferenceId, track.NameVi }).IsUnique();
        builder.HasOne<Conference>().WithMany().HasForeignKey(track => track.ConferenceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
