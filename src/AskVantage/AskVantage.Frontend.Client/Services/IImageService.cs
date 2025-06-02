using AskVantage.Frontend.Client.Models;

namespace AskVantage.Frontend.Client.Services;

public interface IImageService
{
    /// <summary>
    ///     Uploads image to the server and analyzes it using OCR. Returns the recognized text through SignalR.
    /// </summary>
    /// <param name="imageRequest"></param>
    /// <returns></returns>
    Task AnalyzeImage(Image imageRequest);

    /// <summary>
    ///     Deletes all questions from the server.
    /// </summary>
    /// <returns></returns>
    Task DeleteAllQuestion();

    /// <summary>
    ///     Deletes a question from the server.
    /// </summary>
    /// <param name="question"></param>
    /// <returns></returns>
    Task DeleteQuestion(QuestionGenerationResult question);

    /// <summary>
    ///     Uploads the text to the server and generates questions and answers based on it. Returns the questions through
    ///     SignalR.
    /// </summary>
    /// <param name="questionRequest"></param>
    /// <returns></returns>
    Task GenerateQuestions(QuestionGenerationRequest questionRequest);

    /// <summary>
    ///     Fetches questions from the server.
    /// </summary>
    /// <returns></returns>
    Task<IEnumerable<QuestionGenerationResult>> GetQuestions();
}