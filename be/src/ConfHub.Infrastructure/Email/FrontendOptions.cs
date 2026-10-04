using System.ComponentModel.DataAnnotations;

namespace ConfHub.Infrastructure.Email;

// Options pattern: địa chỉ giao diện web, dùng để dựng link trong email.
public sealed class FrontendOptions
{
    public const string SectionName = "Frontend";

    [Required]
    [Url]
    public string BaseUrl { get; init; } = string.Empty;
}
