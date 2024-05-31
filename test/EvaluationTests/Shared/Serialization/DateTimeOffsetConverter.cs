using System.Text.Json;
using System.Text.Json.Serialization;

namespace EvaluationTests.Shared.Serialization;

/// <summary>
/// Defines a JSON converter for serializing and deserializing <see cref="DateTime"/> values as UTC in a specified format.
/// </summary>
public class DateTimeOffsetConverter : JsonConverter<DateTimeOffset?>
{
    public override DateTimeOffset? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var ticks = reader.GetInt64();
        if (ticks < 0)
        {
            return null;
        }

        return new DateTimeOffset(ticks, TimeSpan.Zero);
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        var dateTimeOffset = value.Value;
        writer.WriteNumberValue(dateTimeOffset.Ticks);
    }
}
