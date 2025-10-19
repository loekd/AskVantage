using AskVantage.Frontend.Client.Models;
using AskVantage.Frontend.Client.Services;

namespace AskVantage.Frontend.Services;

public class ServerSideImageApiHubClient : IImageApiHubClient
{
    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
#pragma warning disable CS0067  
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