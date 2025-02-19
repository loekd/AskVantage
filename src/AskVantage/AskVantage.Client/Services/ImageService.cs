using AskVantage.Client.Models;
using System.Net.Http.Json;

namespace AskVantage.Client.Services;

public class ImageService(HttpClient imageApiClient) : IImageService
{
    public async Task AnalyzeImage(Image imageRequest)
    {
        var result = await imageApiClient.PostAsJsonAsync("api/image/analyze", imageRequest);
        result.EnsureSuccessStatusCode();
    }

    public async Task GenerateQuestions(QuestionGenerationRequest questionRequest)
    {
        var result = await imageApiClient.PostAsJsonAsync("api/question/generate", questionRequest);
        result.EnsureSuccessStatusCode();
    }

    public async Task<IEnumerable<QuestionGenerationResult>> GetQuestions()
    {
        var result = await imageApiClient.GetFromJsonAsync<QuestionGenerationResult[]>("api/question");
        return result ?? [];
    }

    public async Task DeleteQuestion(QuestionGenerationResult question)
    {
        var result = await imageApiClient.DeleteAsync($"api/question/{question.TextTitle}");
        result.EnsureSuccessStatusCode();
    }

    public async Task DeleteAllQuestion()
    {
        var result = await imageApiClient.DeleteAsync($"api/question");
        result.EnsureSuccessStatusCode();
    }
}
