using AskVantage.AppHost.Ollama;
using CommunityToolkit.Aspire.Hosting.Dapr;
using Microsoft.Extensions.Configuration;
using Projects;

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

//A containerized pub/sub message broker & state store:
var redisPassword =
    builder.AddParameter("Redis-Password", secret: true, valueGetter: () => builder.Configuration["RedisPassword"]!);
var redis = builder
    .AddRedis("redis", password: redisPassword, port: 6380)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithRedisInsight();

const string daprComponentsPath =
    "/Users/loekd/projects/AskVantage/src/AskVantage/Aspire/AskVantage.AppHost/DaprComponents";
var imageApiStateStore = builder.AddDaprStateStore("imageapistatestorecomponent", new DaprComponentOptions
{
    LocalPath = $"{daprComponentsPath}/statestore.yaml"
}).WaitFor(redis);

bool runLocalOllama = builder.Configuration.GetValue<bool>("OpenAILocal:RunLocal");

var localQuestionGenerator = builder.AddOllama("questiongenerator", modelName: "llama3.2:3b", port: 11434, useNvidiaGpu: false);

var openai = builder.AddConnectionString("openAiConnection");
var openAiApiKey = builder.AddParameter("OpenAiApiKey", secret: true,
    valueGetter: () => runLocalOllama ? builder.Configuration["OpenAILocal:ApiKey"]! : builder.Configuration["OpenAIApiKey"]!);
var openAiEndpoint = builder.AddParameter("OpenAiEndpoint", secret: true,
    valueGetter: () => runLocalOllama ? builder.Configuration["OpenAILocal:Endpoint"]! : builder.Configuration["OpenAIEndpoint"]!);


var ocrApiKey = builder.AddParameter("ComputerVisionApiKey", secret: true,
    valueGetter: () => builder.Configuration["ComputerVisionApiKey"]!);
var ocrEndpoint = builder.AddParameter("ComputerVisionEndpoint", secret: true,
    valueGetter: () => builder.Configuration["ComputerVisionEndpoint"]!);

var imageApi = builder.AddProject<ImageApi>("imageapi")
    .WithReference(localQuestionGenerator.Resource.Endpoint)
    .WithReference(imageApiStateStore)
    .WithDaprSidecar(opt =>
    {
        opt.WithOptions(new DaprSidecarOptions
        {
            AppId = "imageapi",
            SchedulerHostAddress = "" // Set to empty string to disable scheduler
        });
    })
    .WithReference(openai)
    .WithEnvironment("COMPUTERVISION__ENDPOINT", ocrEndpoint)
    .WithEnvironment("COMPUTERVISION__APIKEY", ocrApiKey)
    .WithEnvironment("OPENAI__ENDPOINT", openAiEndpoint)
    .WithEnvironment("OPENAI__APIKEY", openAiApiKey)
    .WithEnvironment("OPENAI__RUNLOCAL", runLocalOllama ? "true" : "false")
    //.WithReplicas(2)
    .WaitFor(redis);

var frontend = builder.AddProject<AskVantage_Frontend>("AskVantageFrontend")
    .WithReference(imageApi)
    .WithExternalHttpEndpoints();

builder.Build().Run();