using System.Net.Http.Headers;
using System.Text.Json;

namespace EvaluationTests.Shared.Extraction.AzureML;

public class AzureMLServerlessClient(Uri endpoint, string apiKey)
{
    private readonly HttpClientHandler _handler = new()
    {
        ClientCertificateOptions = ClientCertificateOption.Manual,
        ServerCertificateCustomValidationCallback = (_, _, _, _) => true
    };

    public virtual async Task<AzureMLServerlessChatCompletions> GetChatCompletionsAsync(
        AzureMLServerlessChatCompletionOptions chatCompletionOptions,
        CancellationToken cancellationToken = default)
    {
        using var client = new HttpClient(_handler);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        client.BaseAddress = endpoint;

        var context = new StringContent(JsonSerializer.Serialize(chatCompletionOptions));
        context.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        var response = await client.PostAsync("v1/chat/completions", context, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<AzureMLServerlessChatCompletions>(result)!;
        }

        var errorResult = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new InvalidOperationException($"Failed to get chat completions. {errorResult}");
    }
}
