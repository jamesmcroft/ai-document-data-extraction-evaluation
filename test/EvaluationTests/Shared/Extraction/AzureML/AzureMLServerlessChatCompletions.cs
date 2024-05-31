using System.Text.Json.Serialization;
using EvaluationTests.Shared.Serialization;

namespace EvaluationTests.Shared.Extraction.AzureML;

public class AzureMLServerlessChatCompletions
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("choices")]
    public IEnumerable<AzureMLServerlessChatChoice> Choices { get; set; }

    [JsonPropertyName("created")]
    [JsonConverter(typeof(DateTimeOffsetConverter))]
    public DateTimeOffset? Created { get; set; }

    [JsonPropertyName("model")]
    public string Model { get; set; }

    [JsonPropertyName("usage")]
    public AzureMLServerlessCompletionsUsage Usage { get; set; }
}
