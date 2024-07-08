using System.Globalization;
using System.Text;
using EvaluationTests.Shared.Markdown;
using EvaluationTests.Shared.Storage;

namespace EvaluationTests.Shared.Extraction.AzureML;

public class AzureMLServerlessMarkdownDocumentDataExtractor(
    AzureMLServerlessClient client,
    AzureMLServerlessChatCompletionOptions chatCompletionOptions,
    IDocumentMarkdownConverter markdownConverter,
    TestOutputStorage? outputStorage = null)
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

        if (outputStorage != null)
        {
            await outputStorage.SaveBytesAsync(markdownContent,
                $"{DateTime.UtcNow.ToString("yy-MM-dd", CultureInfo.InvariantCulture)}.Markdown.md");
        }

        return await GetChatCompletionsAsync(
            new AzureMLServerlessChatRequestMessage("user", Encoding.UTF8.GetString(markdownContent)));
    }
}
