using System.Text.Json;
using System.Text.Json.Serialization;

namespace EvaluationTests.Shared.Serialization;

/// <summary>
/// Defines a custom JSON converter that serializes and deserializes objects to a specified type when the type is generic or a base implementation (e.g., interface).
/// </summary>
/// <remarks>
/// The converter adds a <c>$type</c> property to the JSON object that contains the fully qualified type name of the object.
/// </remarks>
/// <typeparam name="T">The base type to fall back to when converting objects to and from.</typeparam>
public class ObjectToTypeConverter<T> : JsonConverter<T> where T : class
{
    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var root = JsonDocument.ParseValue(ref reader).RootElement.Clone();

        // Get the type from the $type property
        var typeElement = root.GetProperty("$type");
        var inputType = Type.GetType(typeElement.GetString() ?? string.Empty) ?? typeof(T);

        // Deserialize the JSON object to the specified type
        return JsonSerializer.Deserialize(root.GetRawText(), inputType, options) as T;
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        var inputType = value?.GetType() ?? typeof(T);

        // Create a new JSON object from the value, and add a $type property with the fully qualified type name
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(value, inputType, options));
        var root = document.RootElement.Clone();
        root = root.SetProperty("$type", inputType.AssemblyQualifiedName);

        // Write the JSON object to the writer
        writer.WriteStartObject();
        foreach (var property in root.EnumerateObject())
        {
            property.WriteTo(writer);
        }

        writer.WriteEndObject();
        writer.Flush();
    }
}
