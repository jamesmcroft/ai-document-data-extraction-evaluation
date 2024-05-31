using Azure.AI.OpenAI;
using SkiaSharp;

namespace EvaluationTests.Shared.Extraction.AzureOpenAI;

public class AzureOpenAIVisionDocumentDataExtractor(
    OpenAIClient client,
    ChatCompletionsOptions chatCompletionOptions) :
    AzureOpenAIDocumentDataExtractor(client, chatCompletionOptions)
{
    public override async Task<DataExtractionResult> FromDocumentBytesAsync(byte[] documentBytes, CancellationToken cancellationToken = default)
    {
        var images = ToProcessedImages(documentBytes);

        var imagePromptItems = new List<ChatMessageContentItem>();
        imagePromptItems.AddRange(images.Select(image =>
            new ChatMessageImageContentItem(BinaryData.FromBytes(image), "image/jpeg")));

        return await GetChatCompletionsAsync(new ChatRequestUserMessage(imagePromptItems.ToArray()));
    }

    private static IEnumerable<byte[]> ToProcessedImages(byte[] documentBytes)
    {
        var pageImages = PDFtoImage.Conversion.ToImages(documentBytes);

        var totalPageCount = pageImages.Count();

        // If there are more than 10 pages, we need to stitch images together so that the total number of pages is less than or equal to 10 for the OpenAI API.
        var maxSize = (int)Math.Ceiling(totalPageCount / 10.0);

        var pageImageGroups = new List<List<SKBitmap>>();

        for (var i = 0; i < totalPageCount; i += maxSize)
        {
            var pageImageGroup = pageImages.Skip(i).Take(maxSize).ToList();
            pageImageGroups.Add(pageImageGroup);
        }

        var pdfImageFiles = new List<byte[]>();

        // Stitch images together if they have been grouped. This should result in a total of 10 or fewer images in the list.
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

        return pdfImageFiles;
    }
}
