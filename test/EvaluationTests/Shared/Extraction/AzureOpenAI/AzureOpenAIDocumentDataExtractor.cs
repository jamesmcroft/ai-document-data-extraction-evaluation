using Azure.AI.OpenAI;
using EvaluationTests.Shared.Storage;

namespace EvaluationTests.Shared.Extraction.AzureOpenAI;

public abstract class AzureOpenAIDocumentDataExtractor(
    OpenAIClient client,
    ChatCompletionsOptions chatCompletionOptions,
    TestOutputStorage? outputStorage = null)
    : IDocumentDataExtractor
{
    protected TestOutputStorage? OutputStorage => outputStorage;

    public abstract Task<DataExtractionResult> FromDocumentBytesAsync(
        byte[] documentBytes,
        CancellationToken cancellationToken = default);

    public async Task<DataExtractionResult> GetChatCompletionsAsync(params ChatRequestUserMessage[] messages)
    {
        var result = new DataExtractionResult();

        try
        {
            foreach (var message in messages)
            {
                chatCompletionOptions.Messages.Add(message);
            }

            var response = await client.GetChatCompletionsAsync(chatCompletionOptions);

            var usage = response.Value.Usage;
            result.CompletionTokens = usage.CompletionTokens;
            result.PromptTokens = usage.PromptTokens;

            var completion = response.Value.Choices[0];
            if (completion != null)
            {
                var extractedData = completion.Message.Content;
                if (!string.IsNullOrEmpty(extractedData))
                {
                    result.Content = extractedData;
                }
            }
        }
        catch (Exception)
        {
            // Handle exceptions
        }

        return result;
    }
}
