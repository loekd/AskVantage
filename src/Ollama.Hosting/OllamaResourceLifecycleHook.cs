using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;
using OllamaSharp;

namespace Ollama.Hosting;

// ReSharper disable once ClassNeverInstantiated.Global
internal class OllamaResourceLifecycleHook(ResourceNotificationService notificationService)
    : IDistributedApplicationLifecycleHook, IAsyncDisposable
{
    private readonly CancellationTokenSource _tokenSource = new();
    
    public ValueTask DisposeAsync()
    {
        _tokenSource.Cancel();
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
            DownloadModelInBackground(resource, _tokenSource.Token);
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
                string httpEndpoint = resource.GetEndpoint(OllamaResource.OllamaEndpointName).Url;
                OllamaApiClient ollamaClient = new(new Uri(httpEndpoint));

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
            if (Math.Abs(newPercentage - oldPercentage) > 0.1)
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