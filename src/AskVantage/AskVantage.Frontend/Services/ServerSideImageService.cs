using AskVantage.Frontend.Client.Models;
using AskVantage.Frontend.Client.Services;

namespace AskVantage.Frontend.Services;

public class ServerSideImageService : IImageService
{
    //Called when rendering pages on the server side, but currently not implemented
    //All service calls are intended to be made from the AskVantage.Frontend.Client project (browser, WASM)

    public Task AnalyzeImage(Image imageRequest)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAllQuestion()
    {
        throw new NotImplementedException();
    }

    public Task DeleteQuestion(QuestionGenerationResult question)
    {
        throw new NotImplementedException();
    }

    public Task GenerateQuestions(QuestionGenerationRequest questionRequest)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<QuestionGenerationResult>> GetQuestions()
    {
        throw new NotImplementedException();
    }
}