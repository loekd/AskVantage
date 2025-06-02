using System.Net;
using ImageApi.Hubs;
using ImageApi.Mappings;
using ImageApi.Models;
using ImageApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace ImageApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class QuestionController(
    IQuestionGeneratorService ocrTextRecognizerService,
    ITextStateService textStateService,
    IServiceScopeFactory serviceScopeFactory,
    ILogger<QuestionController> logger) : ControllerBase
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
                .Select(t => t.BuildResponse());
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
    public IActionResult GenerateQuestionsForText([FromBody] QuestionGenerationRequest request)
    {
        logger.LogInformation("Generating questions for request {RequestId}", request.RequestId);
        Task.Run(async () =>
        {
            using var scope = serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IHubContext<ImageApiHub, IImageApiHubClient>>();
            var stateService = scope.ServiceProvider.GetRequiredService<ITextStateService>();

            try
            {
                using var cts = new CancellationTokenSource();
                TextState? textState;
                var generatedResult =
                    (await ocrTextRecognizerService.GenerateQuestions(request.Text, cts.Token)).ToArray();

                //save state
                if (generatedResult.Length != 0)
                {
                    textState = new TextState(request.TextTitle, request.Text,
                        generatedResult.Select(r => new QuestionState(r.Question, r.Answer, r.Reference)).ToArray());
                    await stateService.SaveText(textState.Value, cts.Token);
                }

                //fetch all questions and answers for this text
                textState = await stateService.GetSingleText(request.TextTitle, cts.Token)!;
                if (textState != null)
                {
                    var response = textState.Value.BuildResponse(request.RequestId);
                    await context.Clients.All.GenerationCompleted("user", response);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while improving OCR text.");
                await context.Clients.All.GenerationFailed(ex.Message);
            }
        });

        return Accepted();
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