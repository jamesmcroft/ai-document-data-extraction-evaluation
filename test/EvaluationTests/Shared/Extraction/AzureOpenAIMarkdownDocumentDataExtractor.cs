using System.Text;
using Azure.AI.OpenAI;
using EvaluationTests.Shared.Markdown;

namespace EvaluationTests.Shared.Extraction;

public class AzureOpenAIMarkdownDocumentDataExtractor(
    OpenAIClient client,
    ChatCompletionsOptions chatCompletionOptions,
    IDocumentMarkdownConverter markdownConverter)
    : AzureOpenAIDocumentDataExtractor(client, chatCompletionOptions)
{
    public override async Task<DataExtractionResult> FromDocumentBytesAsync(
        byte[] documentBytes,
        CancellationToken cancellationToken = default)
    {
        var result = new DataExtractionResult();

        var markdownContent = await markdownConverter.FromByteArrayAsync(documentBytes, cancellationToken);
        if (markdownContent == null)
        {
            return result;
        }

        return await GetChatCompletionsAsync(new ChatRequestUserMessage(Encoding.UTF8.GetString(markdownContent)));
    }
}
