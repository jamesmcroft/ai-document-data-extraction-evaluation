using System.Text.Json;
using System.Text.Json.Nodes;

namespace EvaluationTests.Shared.Serialization;

/// <summary>
/// Defines a set of extension methods for working with JSON objects.
/// </summary>
public static class JsonExtensions
{
    /// <summary>
    /// Sets a property on the JSON node.
    /// </summary>
    /// <typeparam name="T">The type of the property value.</typeparam>
    /// <param name="node">The JSON node to set the property on.</param>
    /// <param name="name">The name of the property to set.</param>
    /// <param name="value">The value of the property to set.</param>
    /// <returns>The updated JSON node.</returns>
    public static JsonNode SetProperty<T>(this JsonNode node, string name, T value)
    {
        switch (node)
        {
            case JsonObject obj:
                obj[name] = JsonSerializer.SerializeToNode(value);
                break;
        }

        return node;
    }

    /// <summary>
    /// Sets a property on the JSON element.
    /// </summary>
    /// <typeparam name="T">The type of the property value.</typeparam>
    /// <param name="element">The JSON element to set the property on.</param>
    /// <param name="name">The name of the property to set.</param>
    /// <param name="value">The value of the property to set.</param>
    /// <returns>The updated JSON element.</returns>
    public static JsonElement SetProperty<T>(this JsonElement element, string name, T value)
    {
        return element.ValueKind is JsonValueKind.Object
            ? JsonSerializer.SerializeToElement(element.Deserialize<JsonNode>()!.SetProperty(name, value))
            : element;
    }

    /// <summary>
    /// Removes a property from the JSON node.
    /// </summary>
    /// <param name="node">The JSON node to remove the property from.</param>
    /// <param name="name">The name of the property to remove.</param>
    /// <returns>The updated JSON node.</returns>
    public static JsonNode RemoveProperty(this JsonNode node, string name)
    {
        switch (node)
        {
            case JsonObject obj:
                obj.Remove(name);
                break;
        }

        return node;
    }

    /// <summary>
    /// Removes a property from the JSON element.
    /// </summary>
    /// <param name="element">The JSON element to remove the property from.</param>
    /// <param name="name">The name of the property to remove.</param>
    /// <returns>The updated JSON element.</returns>
    public static JsonElement RemoveProperty(this JsonElement element, string name)
    {
        return element.ValueKind is JsonValueKind.Object
            ? JsonSerializer.SerializeToElement(element.Deserialize<JsonNode>()!.RemoveProperty(name))
            : element;
    }
}
