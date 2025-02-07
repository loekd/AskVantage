namespace AskVantage.AppHost.Ollama;

/// <summary>
/// An Ollama container.
/// </summary>
/// <param name="name">Name for resource</param>
/// <param name="modelName">LLM to download on first startup</param>
public class OllamaResource(string name, string modelName) : ContainerResource(name), IResourceWithEndpoints
{
    internal const string OllamaEndpointName = "http";

    private EndpointReference? _endpointReference;

    internal string ModelName { get; set; } = modelName;

    /// <summary>
    /// Returns the endpoint for Ollama
    /// </summary>
    public EndpointReference Endpoint
    {
        get
        {
            return _endpointReference ??= new EndpointReference(this, OllamaEndpointName);
        }
    }
}
