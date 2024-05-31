using System.Text.Json.Serialization;

namespace EvaluationTests.Shared.Extraction.AzureML;

public class AzureMLServerlessChatChoice
{
    [JsonPropertyName("message")]
    public AzureMLServerlessChatResponseMessage Message { get; set; }

    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }

    [JsonPropertyName("stop_reason")]
    public int? StopReason { get; set; }
}
