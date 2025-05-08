using Aspire.Hosting.Dapr;
using AskVantage.AppHost.Ollama;
using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

//fix dashboard settings, so it can run in codespace:
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["ASPNETCORE_URLS"] = "http://localhost:18888",
    ["ASPIRE_ALLOW_UNSECURED_TRANSPORT"] = "True",
    ["DOTNET_DASHBOARD_OTLP_ENDPOINT_URL"] = "http://localhost:18889",
    ["Dashboard:Frontend:AuthMode"] = "Unsecured",
    ["Dashboard:Otlp:AuthMode"] = "Unsecured",
    ["Dashboard:Otlp:Cors"] = "*"
});

var imageApiStateStore = builder.AddDaprStateStore("imageapistatestorecomponent", new DaprComponentOptions { });

var localQuestionGenerator = builder.AddOllama("questiongenerator", modelName: "llama3.2:3b", port: 11434, useNvidiaGpu: false);

var openai = builder.AddConnectionString("openAiConnection");

var ocrApiKey = builder.AddParameter("ComputerVision-ApiKey", secret: true, valueGetter: ()=> builder.Configuration["ComputerVision:ApiKey"]!);
var ocrEndpoint = builder.AddParameter("ComputerVision-Endpoint", secret: true, valueGetter: ()=> builder.Configuration["ComputerVision:Endpoint"]!);

var imageApi = builder.AddProject<Projects.ImageApi>("imageapi")
    .WithReference(localQuestionGenerator.GetEndpoint(OllamaResource.OllamaEndpointName))
    .WithReference(imageApiStateStore)
    .WithDaprSidecar()
    //.WithReference(openai)
    .WithEnvironment("COMPUTERVISION__ENDPOINT", ocrEndpoint)
    .WithEnvironment("COMPUTERVISION__APIKEY", ocrApiKey);

var frontend = builder.AddProject<Projects.AskVantage>("AskVantage")
    .WithReference(imageApi)
    .WithExternalHttpEndpoints();

builder.Build().Run();
