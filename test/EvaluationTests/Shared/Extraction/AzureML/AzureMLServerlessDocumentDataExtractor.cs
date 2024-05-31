namespace EvaluationTests.Shared.Extraction.AzureML;

public abstract class AzureMLServerlessDocumentDataExtractor(
    AzureMLServerlessClient client,
    AzureMLServerlessChatCompletionOptions chatCompletionOptions)
    : IDocumentDataExtractor
{
    public abstract Task<DataExtractionResult> FromDocumentBytesAsync(
        byte[] documentBytes,
        CancellationToken cancellationToken = default);

    public async Task<DataExtractionResult> GetChatCompletionsAsync(params AzureMLServerlessChatRequestMessage[] messages)
    {
        var result = new DataExtractionResult();

        try
        {
            foreach (var message in messages)
            {
                chatCompletionOptions.Messages.Add(message);
            }

            var response = await client.GetChatCompletionsAsync(chatCompletionOptions);

            var usage = response.Usage;
            result.CompletionTokens = usage.CompletionTokens;
            result.PromptTokens = usage.PromptTokens;

            var completion = response.Choices.FirstOrDefault();
            if (completion != null)
            {
                var extractedData = completion.Message.Content;
                if (!string.IsNullOrEmpty(extractedData))
                {
                    result.Content = extractedData;
                }
            }
        }
        catch (Exception ex)
        {
            // Handle exceptions
        }

        return result;
    }
}
