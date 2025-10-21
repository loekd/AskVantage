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