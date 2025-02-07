using AskVantage.Client.Models;

namespace AskVantage.Client.Services;

public interface IImageService
{
    /// <summary>
    /// Uploads image to the server and analyzes it using OCR. Returns the recognized text.
    /// </summary>
    /// <param name="imageRequest"></param>
    /// <returns></returns>
    Task<ImageOcrResult> AnalyzeImage(Image imageRequest);
    Task DeleteAllQuestion();
    Task DeleteQuestion(QuestionGenerationResult question);

    /// <summary>
    /// Uploads the text to the server and generates questions and answers based on it.
    /// </summary>
    /// <param name="questionRequest"></param>
    /// <returns></returns>
    Task<QuestionGenerationResult> GenerateQuestions(QuestionGenerationRequest questionRequest);
    Task<IEnumerable<QuestionGenerationResult>> GetQuestions();
}
