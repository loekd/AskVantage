using Microsoft.AspNetCore.Mvc;
using System.Net;
using ImageApi.Hubs;
using ImageApi.Models;
using ImageApi.Services;
using Microsoft.AspNetCore.SignalR;

namespace ImageApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImageController(ILogger<ImageController> logger, IImageOcrService imageOcrService, IHubContext<ImageApiHub, IImageApiHubClient> hubContext) : ControllerBase
{
    [HttpPost("analyze")]
    [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(ImageOcrResult))]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
    public IActionResult AnalyzeImage([FromBody] Models.Image imageRequest)
    {
        logger.LogInformation("Processing OCR for image {ImageId}", imageRequest.Id);
        Task.Run(async () =>
        {
            try
            {
                using var cts = new CancellationTokenSource();
                string result = await imageOcrService.GetTextAsync(imageRequest.Content, cts.Token);
                var imageOcrResult = new ImageOcrResult
                {
                    ImageId = imageRequest.Id,
                    Text = result
                };
                await hubContext.Clients.All.OcrCompleted("user", imageOcrResult);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to perform OCR.");
                await hubContext.Clients.All.OcrFailed(ex.Message);
            }
        });

        return Accepted();
    }
}
