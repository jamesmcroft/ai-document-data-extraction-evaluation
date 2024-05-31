using System.Text.Json.Serialization;

namespace EvaluationTests.Shared.Extraction.AzureML;

public class AzureMLServerlessChatRequestMessage(string role, string content)
{
    [JsonPropertyName("role")] public string Role { get; set; } = role;

    [JsonPropertyName("content")] public string Content { get; set; } = content;
}
