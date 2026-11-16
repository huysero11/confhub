using System.ComponentModel.DataAnnotations;

namespace ConfHub.Infrastructure.Venue;

public sealed class VenueOptions
{
    public const string SectionName = "Venue";

    [Required]
    public string TimeZoneId { get; init; } = string.Empty;
}
