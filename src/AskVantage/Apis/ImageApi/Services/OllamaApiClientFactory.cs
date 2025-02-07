using OllamaSharp;

namespace ImageApi.Services;

public interface IOllamaApiClientFactory
{
    /// <summary>
    /// Name of the model used by the factory.
    /// </summary>
    string Model { get; }

    /// <summary>
    /// Creates a new instance of <see cref="OllamaApiClient"/> and ensures the model is initialized.
    /// </summary>
    /// <param name="forceRecreate">When set to true, any existing model with the same name will be deleted and recreated. Useful for when the Modelfile has changed.</param>
    /// <returns></returns>
    ValueTask<OllamaApiClient> CreateClient(bool forceRecreate = false);
}

/// <summary>
/// Creates instances of <see cref="OllamaApiClient"/> using the provided <see cref="IHttpClientFactory"/>.
/// </summary>
/// <param name="httpClientFactory"></param>
/// <param name="logger"></param>
public class OllamaApiClientFactory(IHttpClientFactory httpClientFactory, ILogger<OllamaApiClientFactory> logger) : IOllamaApiClientFactory
{
    public const string ClientName = "OllamaClient";
    private const string CustomModelName = "questiongenerator";
    private const string CustomModelTag = "latest";
    private const string CustomModelNameAndTag = $"{CustomModelName}:{CustomModelTag}";

    private bool _isInitialized = false;

    public string Model => CustomModelNameAndTag;

    public async ValueTask<OllamaApiClient> CreateClient(bool forceRecreate = false)
    {
        HttpClient ollamaHttpClient = httpClientFactory.CreateClient(nameof(IOllamaApiClientFactory));
        OllamaApiClient client = new(ollamaHttpClient, CustomModelNameAndTag);

        if (_isInitialized)
        {
            logger.LogDebug("Ollama client already initialized");
            return client;
        }

        await InitializeModel(client, forceRecreate);

        client.SelectedModel = CustomModelNameAndTag;
        _isInitialized = true;
        return client;
    }

    private async Task InitializeModel(OllamaApiClient client, bool forceRecreate)
    {
        var existing = await client.ListLocalModelsAsync();
        if (existing.Any(e => e.Name == CustomModelNameAndTag))
        {
            if (forceRecreate)
            {
                logger.LogDebug("Deleting existing model {CustomModelNameAndTag}", CustomModelNameAndTag);
                await client.DeleteModelAsync(CustomModelNameAndTag);
            }
        }

        await foreach (var model in client.CreateModelAsync(new OllamaSharp.Models.CreateModelRequest
        {
            Model = CustomModelName,
            Path = "/root/.ollama/ModelFile",
            Stream = false
        }))
        {
            logger.LogDebug("Building model {CustomModelNameAndTag}", CustomModelNameAndTag);
        };
    }
}
