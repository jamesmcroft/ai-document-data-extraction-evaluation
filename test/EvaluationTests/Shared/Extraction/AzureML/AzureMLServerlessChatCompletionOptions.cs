using System.Text.Json.Serialization;

namespace EvaluationTests.Shared.Extraction.AzureML;

public class AzureMLServerlessChatCompletionOptions
{
    [JsonPropertyName("messages")]
    public List<AzureMLServerlessChatRequestMessage> Messages { get; set; } = new();

    [JsonPropertyName("temperature")] public float? Temperature { get; set; } = 1.0f;

    [JsonPropertyName("top_p")] public float? NucleusSamplingFactor { get; set; } = 1.0f;

    [JsonPropertyName("max_tokens")] public int? MaxTokens { get; set; } = 1024;
}
