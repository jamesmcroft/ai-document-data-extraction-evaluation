using System.Text.Json;
using System.Text.Json.Serialization;

namespace EvaluationTests.Shared.Extraction;

public class DataExtractionResult
{
    [JsonIgnore]
    public string? Content { get; set; }

    public int? PromptTokens { get; set; }

    public int? CompletionTokens { get; set; }

    public object? Data { get; set; }

    public DataExtractionResult Deserialize<T>()
    {
        if (Content is null)
        {
            return this;
        }

        Data = JsonSerializer.Deserialize<T>(Content);
        return this;
    }
}
