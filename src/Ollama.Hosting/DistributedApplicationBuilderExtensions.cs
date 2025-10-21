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
    
    /// <summary>
    /// Injects service discovery information from the specified endpoint into the project resource using the source resource's name as the service name.
    /// Each endpoint will be injected using the format "services__{sourceResourceName}__{endpointName}__{endpointIndex}={uriString}".
    /// </summary>
    /// <typeparam name="TDestination">The destination resource.</typeparam>
    /// <param name="builder">The resource where the service discovery information will be injected.</param>
    /// <param name="endpointReference">The endpoint from which to extract the url. Or null</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<TDestination> WithOptionalReference<TDestination>(this IResourceBuilder<TDestination> builder, EndpointReference? endpointReference)
        where TDestination : IResourceWithEnvironment
    {
        ArgumentNullException.ThrowIfNull(builder);

        if (endpointReference != null)
        {
            builder.WithReference(endpointReference);
        }
        return builder;
    }
}