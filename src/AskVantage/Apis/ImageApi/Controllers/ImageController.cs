using Microsoft.AspNetCore.Mvc;
using System.Net;
using ImageApi.Models;
using ImageApi.Services;

namespace ImageApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ImageController(ILogger<ImageController> logger, IImageOcrService imageOcrService) : ControllerBase
{
    [HttpPost("analyze")]
    [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(ImageOcrResult))]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
    public async Task<IActionResult> AnalyzeImage([FromBody] Models.Image imageRequest)
    {
        logger.LogInformation("Processing OCR for image {ImageId}", imageRequest.Id);
        try
        {
            var result = await imageOcrService.GetTextAsync(imageRequest.Content, HttpContext.RequestAborted);
            return Ok(new ImageOcrResult
            {
                ImageId = imageRequest.Id,
                Text = result
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to perform OCR.");
            return StatusCode((int)HttpStatusCode.InternalServerError);
        }
    }
}
