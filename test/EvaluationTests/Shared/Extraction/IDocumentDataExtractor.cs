namespace EvaluationTests.Shared.Extraction;

public interface IDocumentDataExtractor
{
    Task<DataExtractionResult> FromDocumentBytesAsync(
        byte[] documentBytes,
        CancellationToken cancellationToken = default);
}
