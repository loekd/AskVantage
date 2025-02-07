using ImageApi.Mappings;
using ImageApi.Models;
using ImageApi.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace ImageApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class QuestionController(IQuestionGeneratorService ocrTextRecognizerService, ITextStateService textStateService, ILogger<QuestionController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(IEnumerable<QuestionGenerationResult>))]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
    public async Task<IActionResult> GetAllTexts()
    {
        logger.LogInformation("Getting questions");

        try
        {
            var allTexts = await textStateService.GetAllTexts(HttpContext.RequestAborted);
            var response = allTexts
                .OrderBy(t => t.Title)
                .Select(t => TextStateExtensions.BuildResponse(t));
            return base.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while getting texts.");
            return StatusCode((int)HttpStatusCode.InternalServerError);
        }
    }

    [HttpPost("generate")]
    [ProducesResponseType((int)HttpStatusCode.OK, Type = typeof(QuestionGenerationResult))]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
    public async Task<IActionResult> GenerateQuestionsForText([FromBody] QuestionGenerationRequest request)
    {
        logger.LogInformation("Generating questions for request {RequestId}", request.RequestId);

        try
        {
            TextState? textState;
            var generatedResult = await ocrTextRecognizerService.GenerateQuestions(request.Text, HttpContext.RequestAborted);

            //save state
            if (generatedResult is not null && generatedResult.Any())
            {
                textState = new TextState(request.TextTitle, request.Text, generatedResult.Select(r => new QuestionState(r.Question, r.Answer, r.Reference)).ToArray());
                await textStateService.SaveText(textState.Value, HttpContext.RequestAborted);
            }

            //fetch all questions and answers for this text
            textState = await textStateService.GetSingleText(request.TextTitle, HttpContext.RequestAborted)!;
            var response = TextStateExtensions.BuildResponse(textState.Value, request.RequestId);

            return base.Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while improving OCR text.");
            return StatusCode((int)HttpStatusCode.InternalServerError);
        }
    }

    [HttpDelete("{key}")]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
    public async Task<IActionResult> DeleteText([FromRoute] string key)
    {
        logger.LogInformation("Deleting text {Key}", key);

        try
        {
            await textStateService.DeleteSingleText(key, HttpContext.RequestAborted);
            return base.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while deleting text {Key}.", key);
            return StatusCode((int)HttpStatusCode.InternalServerError);
        }
    }

    [HttpDelete]
    [ProducesResponseType((int)HttpStatusCode.OK)]
    [ProducesResponseType((int)HttpStatusCode.InternalServerError)]
    public async Task<IActionResult> DeleteAllTexts()
    {
        logger.LogInformation("Deleting all texts");

        try
        {
            await textStateService.DeleteAllTexts(HttpContext.RequestAborted);
            return base.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while deleting texts.");
            return StatusCode((int)HttpStatusCode.InternalServerError);
        }
    }
}
