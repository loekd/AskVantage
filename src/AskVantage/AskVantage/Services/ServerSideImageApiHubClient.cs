using AskVantage.Client.Models;
using AskVantage.Client.Services;

namespace AskVantage.Services;

public class ServerSideImageApiHubClient : IImageApiHubClient
{
    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    public event Func<string, ImageOcrResult, Task>? OcrCompleted;
    public event Func<string, Task>? OcrFailed;
    public event Func<string, QuestionGenerationResult, Task>? GenerationCompleted;
    public event Func<string, Task>? GenerationFailed;
    public event Func<string?, Task>? Reconnected;
    public event Func<Exception?, Task>? Closed;

    public Task StartAsync()
    {
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        return Task.CompletedTask;
    }
}