using System.Collections.Immutable;
using Ollama.Hosting;
using CommunityToolkit.Aspire.Hosting.Dapr;
using Microsoft.Extensions.Configuration;
using Projects;
namespace AskVantage.AppHost;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = DistributedApplication.CreateBuilder(args);

        //fix dashboard settings, so it can run in codespace:
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ASPNETCORE_URLS"] = "http://localhost:18888",
            ["ASPIRE_ALLOW_UNSECURED_TRANSPORT"] = "True",
            ["DOTNET_DASHBOARD_OTLP_ENDPOINT_URL"] = "http://localhost:18889",
            ["ASPIRE_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS"] = "true"            
        });

        //A containerized pub/sub message broker & state store:
        var redisPassword =
            builder.AddParameter("Redis-Password", secret: true, valueGetter: () => "S3cr3tPassw0rd!");
        var redis = builder
            .AddRedis("redis", password: redisPassword)
            .WithHostPort(6380)     
            .WithLifetime(ContainerLifetime.Persistent)
            .WithRedisInsight();

        string daprComponentsPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!, "..", "..", "..", "DaprComponents"));
        var imageApiStateStore = builder.AddDaprStateStore("imageapistatestorecomponent", new DaprComponentOptions
        {
            LocalPath = $"{daprComponentsPath}/statestore.yaml"
        }).WaitFor(redis);

        bool runLocalOllama = builder.Configuration.GetValue<bool>("OpenAILocal:RunLocal");

        var localQuestionGenerator = runLocalOllama
                                        ? builder.AddOllama("questiongenerator", modelName: ModelNames.Llama3_2b, port: 11434, useNvidiaGpu: false)
                                        : null;
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
            .WithOptionalReference(localQuestionGenerator?.Resource.Endpoint)
            //.WithReference(imageApiStateStore)
            .WithDaprSidecar(opt =>
            {
                opt.WithOptions(new DaprSidecarOptions
                {
                    AppId = "imageapi",
                    SchedulerHostAddress = "", // Set to empty string to disable scheduler
                    ResourcesPaths = ImmutableHashSet.Create(daprComponentsPath)
                });
                opt.WithReference(imageApiStateStore);
            })
            .WithReference(openai)
            .WithEnvironment("COMPUTERVISION__ENDPOINT", ocrEndpoint)
            .WithEnvironment("COMPUTERVISION__APIKEY", ocrApiKey)
            .WithEnvironment("OPENAI__ENDPOINT", openAiEndpoint)
            .WithEnvironment("OPENAI__APIKEY", openAiApiKey)
            .WithEnvironment("OPENAI__RUNLOCAL", runLocalOllama ? "true" : "false")
            .WaitFor(redis);

        _ = builder.AddProject<AskVantage_Frontend>("AskVantageFrontend")
            .WithReference(imageApi)
            .WithExternalHttpEndpoints()
            .WaitFor(imageApi);

        builder.Build().Run();
    }
}