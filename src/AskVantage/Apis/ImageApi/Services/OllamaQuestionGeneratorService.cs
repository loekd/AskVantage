using OllamaSharp;
using OllamaSharp.Models;
using OllamaSharp.Models.Chat;
using System.Text;
using System.Text.Json;

namespace ImageApi.Services;

/// <summary>
/// Calls a locally running Ollama API to generate questions from a given text input.
/// </summary>
/// <param name="ollamaApiClientFactory"></param>
/// <param name="logger"></param>
public class OllamaQuestionGeneratorService(IOllamaApiClientFactory ollamaApiClientFactory, ILogger<OllamaQuestionGeneratorService> logger) : IQuestionGeneratorService
{
    public async Task<IEnumerable<QuestionAnswerResponse>> GenerateQuestions(string input, CancellationToken cancellationToken)
    {
        try
        {
            OllamaApiClient ollamaApiClient = await ollamaApiClientFactory.CreateClient();
            input = CleanInput(input);
            var chatRequest = GetOcrChatRequest(input);
            string answer = await SendChatRequest(input, ollamaApiClient, chatRequest, cancellationToken);
            var response = GetAnswerFromResult(answer);
            if (response != null)
                return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to call Ollama API.");
        }

        logger.LogWarning("No (proper) response from Ollama API.");
        return [];
    }

    private ChatRequest GetOcrChatRequest(string input)
    {
        return new ChatRequest
        {
            Stream = true,
            Format = "json",
            Model = ollamaApiClientFactory.Model,
            Options = new RequestOptions
            {
                Temperature = 0.5F
            },
            Messages =
            [
                new Message
                {
                    Content = "Always answer in a JSON array of the provided schema. Even if you cannot generate 3 questions.", //needed despite the system prompt
                    Role = "user"
                },
                new Message
                {
                    Content = input,
                    Role = "user"
                }
            ]
        };
    }

    private static async Task<string> SendChatRequest(string input, OllamaApiClient ollamaApiClient, ChatRequest ocrChatRequest, CancellationToken cancellationToken)
    {
        StringBuilder stringBuilder = new();

        //get the answer from the chat, it will be streamed in chunks
        await foreach (var result in ollamaApiClient.ChatAsync(ocrChatRequest, cancellationToken))
        {
            if (result?.Message != null && result.Message.Role == ChatRole.Assistant)
            {
                stringBuilder.Append(result.Message.Content);
            }
        }

        //parse the JSON array from the response message
        string answer = ParseAnswer(stringBuilder);
        return answer;
    }

    private static string ParseAnswer(StringBuilder stringBuilder)
    {
        string answer = stringBuilder.ToString().Trim();

        int start = answer.IndexOf('[');
        int end = answer.LastIndexOf(']');
        if (start > 0 && end > 0)
        {
            answer = answer.Substring(start, (end - start) + 1);
        }
        else
        {
            //sometimes the model refuses to return a proper JSON array. :(
            if (answer.StartsWith('{') && answer.EndsWith('}'))
                answer = $"[{answer}]";
        }

        return answer;
    }

    /// <summary>
    /// Constructs a <see cref="QuestionAnswerResponse"/> array from returned JSON string.
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    private QuestionAnswerResponse[]? GetAnswerFromResult(string json)
    {
        if (json == null)
            return null;

        try
        {
            QuestionAnswerResponse[]? response = JsonSerializer.Deserialize<QuestionAnswerResponse[]>(json);
            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process answer.");
        }
        return null;
    }

    /// <summary>
    /// Removes unwanted characters from the input string.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private static string CleanInput(string input)
    {
        input = input
        .Replace("\r", " ")
        .Replace(",", string.Empty)
        .Replace("&", string.Empty)
        .Replace("-", string.Empty)
        .Replace("\n", string.Empty);
        return input;
    }
}
