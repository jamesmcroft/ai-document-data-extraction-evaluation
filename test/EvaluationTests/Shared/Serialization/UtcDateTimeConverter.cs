using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EvaluationTests.Shared.Serialization;

/// <summary>
/// Defines a JSON converter for serializing and deserializing <see cref="DateTime"/> values as UTC in a specified format.
/// </summary>
public class UtcDateTimeConverter : JsonConverter<DateTime?>
{
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var parsed = DateTime.TryParse(reader.GetString(), out var dateTime);

        if (!parsed)
        {
            return null;
        }

        if (dateTime.Kind == DateTimeKind.Unspecified)
        {
            return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
        }

        return dateTime.Kind == DateTimeKind.Local ? dateTime.ToUniversalTime() : dateTime;
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        var dateTime = value.Value;

        dateTime = dateTime.Kind switch
        {
            DateTimeKind.Unspecified => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc),
            DateTimeKind.Local => dateTime.ToUniversalTime(),
            _ => dateTime
        };

        writer.WriteStringValue(dateTime.ToString("o", CultureInfo.InvariantCulture));
    }
}
