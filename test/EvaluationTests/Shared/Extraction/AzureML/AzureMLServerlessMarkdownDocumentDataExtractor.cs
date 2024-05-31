using System.Text;
using EvaluationTests.Shared.Markdown;

namespace EvaluationTests.Shared.Extraction.AzureML;

public class AzureMLServerlessMarkdownDocumentDataExtractor(
    AzureMLServerlessClient client,
    AzureMLServerlessChatCompletionOptions chatCompletionOptions,
    IDocumentMarkdownConverter markdownConverter)
    : AzureMLServerlessDocumentDataExtractor(client, chatCompletionOptions)
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

        return await GetChatCompletionsAsync(
            new AzureMLServerlessChatRequestMessage("user", Encoding.UTF8.GetString(markdownContent)));
    }
}
