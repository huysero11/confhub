using ConfHub.Application.Common.Time;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ConfHub.Infrastructure.Venue;

internal static class VenueServiceRegistration
{
    // Options pattern + ValidateOnStart: phát hiện múi giờ sai ngay khi khởi động.
    public static IServiceCollection AddVenue(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<VenueOptions>()
            .Bind(configuration.GetSection(VenueOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(options => IsValidTimeZone(options.TimeZoneId), "Venue:TimeZoneId must identify an installed time zone.")
            .ValidateOnStart();
        services.AddSingleton(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<VenueOptions>>().Value;
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(options.TimeZoneId);
            return new VenueClock(serviceProvider.GetRequiredService<TimeProvider>(), timeZone);
        });
        return services;
    }

    private static bool IsValidTimeZone(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return false;
        }

        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }
    }
}
