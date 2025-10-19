using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;

namespace Ollama.Hosting;

public static class DistributedApplicationBuilderExtensions
{
    private const string OllamaImageName = "ollama/ollama";
    private const string OllamaImageTag = "0.3.9"; //pinned host version matches model version

    private const string ModelVolumeName = "ollama";
    private const string ModelVolumePath = "/root/.ollama";

    private const string DefaultModelName = "llama3.2:3b"; //model name should match the name in the model file, in the 'FROM' expression.

    /// <summary>
    /// Builder extensions that add an Ollama container to the application model.
    /// </summary>
    public static IResourceBuilder<OllamaResource> AddOllama(this IDistributedApplicationBuilder builder,
        string name = "Ollama", int? port = null, string modelName = DefaultModelName, bool useNvidiaGpu = false, bool verboseLogging = true)
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
            .WithHttpEndpoint(port, 11434, OllamaResource.OllamaEndpointName)
            .WithLifetime(ContainerLifetime.Persistent)
            .ExcludeFromManifest()
            .PublishAsContainer();
        
        if (verboseLogging)
        {
            resourceBuilder = resourceBuilder.WithEnvironment(opt =>
            {
                opt.EnvironmentVariables["OLLAMA_DEBUG"] = "1";
                opt.EnvironmentVariables["OLLAMA_LOG_LEVEL"] = "debug";
                opt.EnvironmentVariables["OLLAMA_VERBOSE"] = "true";
            });
        }

        if (useNvidiaGpu)
            resourceBuilder = resourceBuilder
                .WithEnvironment(opt => { opt.EnvironmentVariables["ENABLE_NVIDIA_DOCKER"] = "true"; })
                .WithContainerRuntimeArgs("--gpus=all");
        return resourceBuilder;
    }
}