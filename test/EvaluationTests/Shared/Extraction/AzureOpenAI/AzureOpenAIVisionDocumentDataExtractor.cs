using System.Globalization;
using Azure.AI.OpenAI;
using EvaluationTests.Shared.Storage;
using SkiaSharp;

namespace EvaluationTests.Shared.Extraction.AzureOpenAI;

public class AzureOpenAIVisionDocumentDataExtractor(
    OpenAIClient client,
    ChatCompletionsOptions chatCompletionOptions,
    TestOutputStorage? outputStorage = null) :
    AzureOpenAIDocumentDataExtractor(client, chatCompletionOptions, outputStorage)
{
    public override async Task<DataExtractionResult> FromDocumentBytesAsync(byte[] documentBytes,
        CancellationToken cancellationToken = default)
    {
        var images = await ToProcessedImages(documentBytes);

        var imagePromptItems = new List<ChatMessageContentItem>();
        imagePromptItems.AddRange(images.Select(image =>
            new ChatMessageImageContentItem(BinaryData.FromBytes(image), "image/jpeg")));

        return await GetChatCompletionsAsync(new ChatRequestUserMessage(imagePromptItems.ToArray()));
    }

    private async Task<IEnumerable<byte[]>> ToProcessedImages(byte[] documentBytes)
    {
        var pageImages = PDFtoImage.Conversion.ToImages(documentBytes);

        var totalPageCount = pageImages.Count();

        // Group images if the total page count is too large.
        var maxSize = (int)Math.Ceiling(totalPageCount / 25.0);

        var pageImageGroups = new List<List<SKBitmap>>();

        for (var i = 0; i < totalPageCount; i += maxSize)
        {
            var pageImageGroup = pageImages.Skip(i).Take(maxSize).ToList();
            pageImageGroups.Add(pageImageGroup);
        }

        var pdfImageFiles = new List<byte[]>();

        // Stitch images together if they have been grouped.
        foreach (var pageImageGroup in pageImageGroups)
        {
            var totalHeight = pageImageGroup.Sum(image => image.Height);
            var width = pageImageGroup.Max(image => image.Width);

            var stitchedImage = new SKBitmap(width, totalHeight);
            var canvas = new SKCanvas(stitchedImage);
            var currentHeight = 0;
            foreach (var pageImage in pageImageGroup)
            {
                canvas.DrawBitmap(pageImage, 0, currentHeight);
                currentHeight += pageImage.Height;
            }

            var stitchedImageStream = new MemoryStream();
            stitchedImage.Encode(stitchedImageStream, SKEncodedImageFormat.Jpeg, 100);
            pdfImageFiles.Add(stitchedImageStream.ToArray());
        }

        if (OutputStorage == null)
        {
            return pdfImageFiles;
        }

        for (var i = 0; i < pdfImageFiles.Count; i++)
        {
            await OutputStorage.SaveBytesAsync(pdfImageFiles[i],
                $"{DateTime.UtcNow.ToString("yy-MM-dd", CultureInfo.InvariantCulture)}.Page-{i}.jpg");
        }

        return pdfImageFiles;
    }
}
