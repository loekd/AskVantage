using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace Ollama.Hosting;

/// <summary>
/// Runs an Ollama container.
/// </summary>
/// <param name="name">Name for resource</param>
/// <param name="modelName">LLM to download on first startup</param>
public class OllamaResource(string name, string modelName) : ContainerResource(name), IResourceWithEndpoints
{
    internal const string OllamaEndpointName = "http";

    private EndpointReference? _endpointReference;

    internal string ModelName { get; set; } = modelName;

    /// <summary>
    /// Returns the HTTP endpoint for Ollama
    /// </summary>
    public EndpointReference Endpoint
    {
        get { return _endpointReference ??= new EndpointReference(this, OllamaEndpointName); }
    }
}

public static class ModelNames
{
    public const string Llama3_2b = "llama3.2:3b";
    public const string Llama3_1b = "llama3.2:1b";
    public const string Phi3_Mini = "phi3:mini";
    public const string Qwen2_7b = "qwen2.5:7b";
    // Add more model names as needed
}

public static class AppHostExtensions
{
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