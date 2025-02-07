using System.Text.Json.Serialization;

namespace ImageApi.Services;

public class QuestionAnswerResponse
{
    [JsonPropertyName("question")]
    public string Question { get; set; } = default!;

    [JsonPropertyName("answer")]
    public string Answer { get; set; } = default!;

    [JsonPropertyName("reference")]
    public string Reference { get; set; } = default!;
}

/// <summary>
/// Generates questions from a given text input.
/// </summary>
public interface IQuestionGeneratorService
{
    /// <summary>
    /// Takes the provided text and attempts to generate questions from it. (using an LLM)
    /// </summary>
    /// <param name="input"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<IEnumerable<QuestionAnswerResponse>> GenerateQuestions(string input, CancellationToken cancellationToken);
}