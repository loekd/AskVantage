using Azure.AI.Vision.ImageAnalysis;

namespace ImageApi.Services;

public interface IImageOcrService
{
    Task<string> GetTextAsync(byte[] image, CancellationToken cancellationToken);
}

public class AzureImageOcrService(ImageAnalysisClient imageAnalysisClient, ILogger<AzureImageOcrService> logger) : IImageOcrService
{
    public async Task<string> GetTextAsync(byte[] image, CancellationToken cancellationToken)
    {
        var options = new ImageAnalysisOptions
        {
            GenderNeutralCaption = true,
        };
        try
        {
            var result = await imageAnalysisClient.AnalyzeAsync(new BinaryData(image), VisualFeatures.Read, options, cancellationToken);
            var text = result.Value.Read.Blocks[0].Lines.Select(l => l.Text);
            return string.Join(", ", text);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to perform OCR.");
            throw;
        }
    }
}