using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConfHub.Api.Json;

public sealed class TimeOnlyHourMinuteJsonConverter : JsonConverter<TimeOnly>
{
    public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("Time must be a string in HH:mm format.");
        }

        if (!TimeOnly.TryParseExact(reader.GetString(), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
        {
            throw new JsonException("Time must use HH:mm format.");
        }

        return time;
    }

    public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("HH:mm", CultureInfo.InvariantCulture));
    }
}
