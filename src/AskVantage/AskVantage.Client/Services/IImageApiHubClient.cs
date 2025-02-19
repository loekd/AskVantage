using AskVantage.Client.Models;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace AskVantage.Client.Services;

public interface IImageApiHubClient : IAsyncDisposable
{
    event Func<string, ImageOcrResult, Task>? OcrCompleted;
    event Func<string, Task>? OcrFailed;

    event Func<string, QuestionGenerationResult, Task>? GenerationCompleted;
    event Func<string, Task>? GenerationFailed;
    event Func<string?, Task>? Reconnected;
    event Func<Exception?, Task>? Closed;
    Task StartAsync();
    Task StopAsync();
}

public class ImageApiHubClient(NavigationManager navigationManager) : IImageApiHubClient, IAsyncDisposable
{
    private readonly HubConnection _hubConnection = new HubConnectionBuilder()
        .WithUrl(new Uri(new Uri(navigationManager.BaseUri), "hub"))
        .WithAutomaticReconnect()
        .Build();

    public event Func<string, ImageOcrResult, Task>? OcrCompleted;
    public event Func<string, Task>? OcrFailed;
    public event Func<string, QuestionGenerationResult, Task>? GenerationCompleted;
    public event Func<string, Task>? GenerationFailed;

    public event Func<string?, Task>? Reconnected;
    public event Func<Exception?, Task>? Closed;

    // Ensure this matches the server endpoint

    public async Task StartAsync()
    {
        _hubConnection.On<string, ImageOcrResult>(nameof(IImageApiHubClient.OcrCompleted), async (user, response) =>
        {
            if (OcrCompleted != null)
                await OcrCompleted.Invoke(user, response);
        });

        _hubConnection.On<string, QuestionGenerationResult>(nameof(IImageApiHubClient.GenerationCompleted), async (user, response) =>
        {
            if (GenerationCompleted != null)
                await GenerationCompleted.Invoke(user, response);
        });
        
        _hubConnection.On<string>(nameof(IImageApiHubClient.OcrFailed), async (error) =>
        {
            if (OcrFailed != null)
                await OcrFailed.Invoke(error);
        });

        _hubConnection.On<string>(nameof(IImageApiHubClient.GenerationFailed), async (error) =>
        {
            if (GenerationFailed != null)
                await GenerationFailed.Invoke(error);
        });
        
        _hubConnection.Reconnected += x => this.Reconnected?.Invoke(x) ?? Task.CompletedTask;
        _hubConnection.Closed += x => this.Closed?.Invoke(x) ?? Task.CompletedTask;
        
        await _hubConnection.StartAsync();
    }

    public async Task StopAsync()
    {
        await _hubConnection.StopAsync();
    }

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        await _hubConnection.DisposeAsync();
    }


}