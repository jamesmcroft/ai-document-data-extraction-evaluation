using System.Text.Json.Serialization;

namespace EvaluationTests.Shared.Extraction.AzureML;

public class AzureMLServerlessChatResponseMessage
{
    [JsonPropertyName("role")] public string Role { get; set; }

    [JsonPropertyName("content")] public string Content { get; set; }
}
