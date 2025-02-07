using AskVantage.Client.Models;
using AskVantage.Client.Services;

namespace AskVantage.Services;

public class ServerSideImageService : IImageService
{
    //Called when rendering pages on the server side, but currently not implemented
    //All service calls are intended to be made from the AskVantage.Client project (browser, WASM)

    public Task<ImageOcrResult> AnalyzeImage(Image imageRequest)
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

    public Task<QuestionGenerationResult> GenerateQuestions(QuestionGenerationRequest questionRequest)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<QuestionGenerationResult>> GetQuestions()
    {
        throw new NotImplementedException();
    }
}
