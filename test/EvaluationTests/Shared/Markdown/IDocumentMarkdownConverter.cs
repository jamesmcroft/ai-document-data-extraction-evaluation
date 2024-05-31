namespace EvaluationTests.Shared.Markdown;

public interface IDocumentMarkdownConverter
{
    Task<byte[]?> FromUriAsync(string documentUri, CancellationToken cancellationToken = default);

    Task<byte[]?> FromByteArrayAsync(byte[] documentBytes, CancellationToken cancellationToken = default);
}
