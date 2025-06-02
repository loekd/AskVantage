using Aspire.Hosting.Lifecycle;
using OllamaSharp;

namespace AskVantage.AppHost.Ollama;

internal class OllamaResourceLifecycleHook(ResourceNotificationService notificationService)
    : IDistributedApplicationLifecycleHook, IAsyncDisposable
{
    private readonly CancellationTokenSource tokenSource = new();

    /// <summary>
    ///     Cleans up (un)managed resources.
    /// </summary>
    /// <returns></returns>
    public ValueTask DisposeAsync()
    {
        tokenSource.Cancel();
        return default;
    }

    /// <summary>
    ///     Starts downloads of the models for each Ollama resource in the background.
    /// </summary>
    /// <param name="appModel"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task AfterResourcesCreatedAsync(DistributedApplicationModel appModel,
        CancellationToken cancellationToken = default)
    {
        foreach (var resource in appModel.Resources.OfType<OllamaResource>())
            DownloadModelInBackground(resource, tokenSource.Token);
        return Task.CompletedTask;
    }

    private void DownloadModelInBackground(OllamaResource resource, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(resource.ModelName))
            throw new InvalidOperationException("Resource Model name must be set");

        string model = resource.ModelName;
        _ = Task.Run(async () =>
        {
            try
            {
                string? connectionString = resource.GetEndpoint(OllamaResource.OllamaEndpointName).Url;
                OllamaApiClient ollamaClient = new(new Uri(connectionString!));

                bool hasModel = await ModelExists(resource, ollamaClient, model, cancellationToken);
                if (!hasModel) await PullModel(resource, ollamaClient, model, cancellationToken);

                await notificationService.PublishUpdateAsync(resource,
                    state => state with
                    {
                        State = new ResourceStateSnapshot($"Running model '{model}'",
                            KnownResourceStateStyles.Success)
                    });
            }
            catch (Exception ex)
            {
                await notificationService.PublishUpdateAsync(resource,
                    state => state with
                    {
                        State = new ResourceStateSnapshot($"Error during download of model '{model}': {ex.Message}",
                            KnownResourceStateStyles.Error)
                    });
            }
        }, cancellationToken);
    }

    private async Task PullModel(OllamaResource resource, OllamaApiClient ollamaClient, string model,
        CancellationToken cancellationToken)
    {
        double oldPercentage = 0;
        await notificationService.PublishUpdateAsync(resource,
            state => state with
            {
                State = new ResourceStateSnapshot($"Downloading model '{model}' for first use",
                    KnownResourceStateStyles.Info)
            });
        await foreach (var status in ollamaClient.PullModelAsync(model, cancellationToken))
        {
            if (status is null)
                continue;

            double newPercentage = status.Percent;
            if (newPercentage != oldPercentage)
            {
                var message = $"Downloading model '{model}' ({newPercentage:F1}%)";
                await notificationService.PublishUpdateAsync(resource,
                    state => state with { State = new ResourceStateSnapshot(message, KnownResourceStateStyles.Info) });
                oldPercentage = newPercentage;
            }
        }
    }

    private async Task<bool> ModelExists(OllamaResource resource, OllamaApiClient ollamaClient, string model,
        CancellationToken cancellationToken)
    {
        var localModels = await ollamaClient.ListLocalModelsAsync(cancellationToken);
        bool exists = localModels.Any(m => m.Name.StartsWith(model));
        await notificationService.PublishUpdateAsync(resource,
            state => state with
            {
                State = new ResourceStateSnapshot($"Model '{model}' exists locally: {exists}",
                    KnownResourceStateStyles.Info)
            });
        return exists;
    }
}