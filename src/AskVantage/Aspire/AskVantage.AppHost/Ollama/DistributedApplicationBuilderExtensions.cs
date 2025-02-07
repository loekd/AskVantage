using Aspire.Hosting.Lifecycle;
using AskVantage.AppHost.Ollama;

namespace AskVantage.AppHost.Ollama;


public static class DistributedApplicationBuilderExtensions
{
    private const string OllamaImageName = "ollama/ollama";
    private const string OllamaImageTag = "0.3.9"; //pinned host version matches model version

    private const string ModelVolumeName = "ollama";
    private const string ModelVolumePath = "/root/.ollama";

    private const string ModelFileVolume = "./Ollama/ModelFile";
    private const string ModelFilePath = "/root/.ollama/ModelFile";

    private const string DefaultModelName = "llama3.1"; //model name should match the name in the model file, in the 'FROM' expression.

    /// <summary>
    /// Adds an Ollama container to the application model.
    /// </summary>
    public static IResourceBuilder<OllamaResource> AddOllama(this IDistributedApplicationBuilder builder,
      string name = "Ollama", int? port = null, string modelName = DefaultModelName, bool useNvidiaGpu = false)
    {
        builder.Services.TryAddLifecycleHook<OllamaResourceLifecycleHook>();
        var ollama = new OllamaResource(name, modelName);
        IResourceBuilder<OllamaResource> resourceBuilder = builder.AddResource(ollama)
                  .WithAnnotation(new ContainerImageAnnotation
                  {
                      Image = OllamaImageName,
                      Tag = OllamaImageTag
                  })
                  .WithVolume(ModelVolumeName, ModelVolumePath)
                  .WithBindMount(ModelFileVolume, ModelFilePath, true)
                  .WithHttpEndpoint(port, 11434, OllamaResource.OllamaEndpointName)
                  //.WithExternalHttpEndpoints()
                  .ExcludeFromManifest()
                  .PublishAsContainer();

        if (useNvidiaGpu)
        {
            resourceBuilder = resourceBuilder
                .WithEnvironment(opt =>
                {
                    opt.EnvironmentVariables["ENABLE_NVIDIA_DOCKER"] = "true";
                })
                .WithContainerRuntimeArgs("--gpus=all");
        }

        return resourceBuilder;
    }
}
