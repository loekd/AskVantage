using System.Text.Json;
using ImageApi.Models;
using Microsoft.AspNetCore.SignalR;

namespace ImageApi.Hubs;

public interface IImageApiHubClient
{
    Task OcrCompleted(string user, ImageOcrResult response);

    Task OcrFailed(string error);

    Task GenerationCompleted(string user, QuestionGenerationResult response);

    Task GenerationFailed(string error);
}

public class ImageApiHub : Hub<IImageApiHubClient>
{
}

public static class Extensions
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    /// <summary>
    ///     Response to JSON
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="input"></param>
    /// <returns></returns>
    public static string ToJson<T>(this T input)
    {
        return JsonSerializer.Serialize(input, Options);
    }
}