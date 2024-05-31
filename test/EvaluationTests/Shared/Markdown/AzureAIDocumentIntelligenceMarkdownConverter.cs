using System.Text;
using Azure;
using Azure.AI.DocumentIntelligence;

namespace EvaluationTests.Shared.Markdown;

public class AzureAIDocumentIntelligenceMarkdownConverter(DocumentIntelligenceClient client)
    : IDocumentMarkdownConverter
{
    public Task<byte[]?> FromUriAsync(string documentUri, CancellationToken cancellationToken = default)
    {
        return ToMarkdownAsync(
            new AnalyzeDocumentContent { UrlSource = new Uri(documentUri) },
            cancellationToken);
    }

    public Task<byte[]?> FromByteArrayAsync(byte[] documentBytes, CancellationToken cancellationToken = default)
    {
        return ToMarkdownAsync(
            new AnalyzeDocumentContent { Base64Source = BinaryData.FromBytes(documentBytes) },
            cancellationToken);
    }

    private async Task<byte[]?> ToMarkdownAsync(
        AnalyzeDocumentContent content,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var operation = await client.AnalyzeDocumentAsync(
                WaitUntil.Completed,
                "prebuilt-layout",
                content,
                outputContentFormat: ContentFormat.Markdown, cancellationToken: cancellationToken);

            return operation is { HasValue: true } ? Encoding.UTF8.GetBytes(operation.Value.Content) : default;
        }
        catch (Exception)
        {
            return default;
        }
    }
}
