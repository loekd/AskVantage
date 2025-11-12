using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Azure;
using Azure.AI.OpenAI;
using Azure.AI.Vision.ImageAnalysis;
using Grpc.Net.Client;
using ImageApi.Hubs;
using ImageApi.Services;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Prompty;

namespace ImageApi;

public static class Program
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver
        {
            Modifiers =
            {
                static typeInfo =>
                {
                    Console.WriteLine("In callback for {0}", typeInfo.Type.Name);
                    if (typeInfo.Type.Name == "ChatTokenUsage")
                    {
                        Console.WriteLine("Found ChatTokenUsage");
                        // Remove the problematic Patch property from serialization
                        var patchProperty = typeInfo.Properties
                            .FirstOrDefault(p => p.Name == "Patch");
                        if (patchProperty != null)
                        {
                            Console.WriteLine("Removing Patch property");
                            typeInfo.Properties.Remove(patchProperty);
                        }
                        else
                        {
                            Console.WriteLine("Patch property not found");
                        }
                    }
                }
            }
        }
    };

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddSingleton(JsonSerializerOptions);
        builder.Services.Configure<JsonSerializerOptions>(options =>
        {
            options.PropertyNameCaseInsensitive = true;
            options.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
            options.TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver
            {
                Modifiers =
                {
                    static typeInfo =>
                    {
                        if (typeInfo.Type.Name != "ChatTokenUsage") 
                            return;
                        
                        Console.WriteLine("Found ChatTokenUsage");
                        // Remove the problematic Patch property from serialization
                        var patchProperty = typeInfo.Properties
                            .FirstOrDefault(p => p.Name == "Patch");
                        if (patchProperty != null)
                        {
                            Console.WriteLine("Removing Patch property");
                            typeInfo.Properties.Remove(patchProperty);
                        }
                        else
                        {
                            Console.WriteLine("Patch property not found");
                        }
                    }
                }
            };
        });

        builder.AddServiceDefaults();

        // Add services to the container:

        //Add Dapr state store:
        builder.AddDaprStateStore();

        //Add Azure OCR client:
        builder.AddAzureCognitiveServicesOcr();
        //Add question generator:
        builder.Services.AddQuestionGenerator(builder.Configuration, builder.Environment);

        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                // Configure JSON options to handle .NET 10 serialization issues
                options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
            });
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddSignalR()
            .AddJsonProtocol(options =>
            {
                // Configure SignalR JSON options to handle .NET 10 serialization issues
                options.PayloadSerializerOptions.PropertyNameCaseInsensitive = true;
                options.PayloadSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
            });

        var app = builder.Build();

        app.MapDefaultEndpoints();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        else
        {
            app.UseHttpsRedirection();
        }

        app.UseAuthorization();


        app.MapControllers();
        app.MapHub<ImageApiHub>("/hub");
        app.Run();
    }
}

internal static class BuilderExtensions
{
    internal static WebApplicationBuilder AddDaprStateStore(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<ITextStateService, DaprTextStateService>();

        //configure Dapr Client with support for 10MB messages
        builder.Services.AddDaprClient(bld => bld.UseGrpcChannelOptions(new GrpcChannelOptions
        {
            MaxReceiveMessageSize = 10 * 1024 * 1024,
            MaxSendMessageSize = 10 * 1024 * 1024
        }));
        return builder;
    }

    internal static WebApplicationBuilder AddAzureCognitiveServicesOcr(this WebApplicationBuilder builder)
    {
        builder.Services.AddScoped<IImageOcrService, AzureImageOcrService>();
        builder.Services.AddScoped(sp =>
        {
            //requires the following configuration or user secrets:
            //"ComputerVision:Endpoint": "https://your-own.cognitiveservices.azure.com",
            //"ComputerVision:ApiKey": "secret",
            //get started here: https://portal.vision.cognitive.azure.com/demo/extract-text-from-images

            string key = builder.Configuration["ComputerVision:ApiKey"]!;
            string endpoint = builder.Configuration["ComputerVision:Endpoint"]!;

            if (string.IsNullOrWhiteSpace(key))
                throw new InvalidOperationException("ComputerVision:ApiKey must be set to a valid string");
            if (string.IsNullOrWhiteSpace(endpoint))
                throw new InvalidOperationException("ComputerVision:Endpoint must be set to a valid string");

            var client = new ImageAnalysisClient(
                new Uri(endpoint),
                new AzureKeyCredential(key));
            return client;
        });
        return builder;
    }


    internal static IServiceCollection AddQuestionGenerator(this IServiceCollection services,
        IConfiguration configuration, IHostEnvironment environment)
    {
        var config = OpenAiConfiguration.Get(configuration);
        
        // Materializing the services here allows us to resolve ILoggerFactory.
        // This is necessary for the Prompty functions to log.
        // This gives us access to the rendered prompt.
        var loggerFactory = services.BuildServiceProvider().GetService<ILoggerFactory>();
        if (config.RunLocal)
        {
            services
                .AddOpenAIChatCompletion(ModelNames.Llama3_2_3b, new Uri(config.ApiEndpoint), config.ApiKey);
        }
        else
        {
            // Configure Azure OpenAI with standard settings
            //services.AddAzureOpenAIChatCompletion(ModelNames.Gpt4oMini, config.ApiEndpoint, config.ApiKey);
            
            
            services.AddSingleton<IChatCompletionService>(sp =>
            {
                var client = new AzureOpenAIClient(
                    new Uri(config.ApiEndpoint), 
                    new AzureKeyCredential(config.ApiKey),
                    new AzureOpenAIClientOptions { /* custom JSON options */ }
                );
                return new AzureOpenAIChatCompletionService(ModelNames.Gpt4oMini, client);
            });
        }
        var kernelBuilder = services.AddKernel();
        kernelBuilder.Services.ConfigureHttpClientDefaults(c => 
        {
            c.ConfigureHttpClient(client =>
            {
                client.Timeout = TimeSpan.FromMinutes(2);
            });
        });
        kernelBuilder.Plugins.AddPromptyFunctions(loggerFactory, config.RunLocal);
        services.AddPromptyTemplates();
        services.AddTransient<IQuestionGeneratorService, OpenAIQuestionGeneratorService>();

        return services;
    }

    private static IKernelBuilderPlugins AddPromptyFunctions(this IKernelBuilderPlugins kernelPlugins,
        ILoggerFactory? factory, bool runLocal)
    {
        var functions = CreatePromptyFunctions(factory, runLocal).ToArray();

        return kernelPlugins.AddFromFunctions(PluginNames.Prompty, functions);
    }

    private static IEnumerable<KernelFunction> CreatePromptyFunctions(ILoggerFactory? factory, bool runLocal)
    {
        string basePath = AppContext.BaseDirectory;
        string promptyDirectory = Path.Join(basePath, "Prompts");
        const string searchPattern = "*.prompty";
        const string localTestFilePattern = ".local.";
        foreach (string file in Directory.EnumerateFiles(promptyDirectory, searchPattern))
        {
            bool isLocalTestFile = file.Contains(localTestFilePattern);
            switch (isLocalTestFile)
            {
                case true when runLocal:
                case false when !runLocal:
                    yield return CreatePromptyFunction(file, factory);
                    break;
            }
        }
    }

    private static KernelFunction CreatePromptyFunction(string fileName, ILoggerFactory? factory)
    {
        string text = File.ReadAllText(fileName);
        return KernelFunctionPrompty.FromPrompty(text, loggerFactory: factory);
    }


    private static IServiceCollection AddPromptyTemplates(this IServiceCollection services)
    {
        string basePath = AppContext.BaseDirectory;
        string promptyDirectory = Path.Join(basePath, "Prompts");
        foreach (string file in Directory.EnumerateFiles(promptyDirectory, "*.prompty"))
        {
            var promptTemplateConfig = CreatePromptyTemplateConfig(file);
            services.AddKeyedSingleton(promptTemplateConfig.Name, promptTemplateConfig);
        }

        return services;
    }

    private static PromptTemplateConfig CreatePromptyTemplateConfig(string fileName)
    {
        string text = File.ReadAllText(fileName);
        return KernelFunctionPrompty.ToPromptTemplateConfig(text);
    }


    private sealed class OpenAiConfiguration
    {
        private OpenAiConfiguration(string apiUrl, string apiKey, bool runLocal = false)
        {
            ApiEndpoint = apiUrl;
            ApiKey = apiKey;
            RunLocal = runLocal;
        }

        public string ApiEndpoint { get; }
        public string ApiKey { get; }

        public bool RunLocal { get; }

        public static OpenAiConfiguration Get(IConfiguration config)
        {
            //run local if the RunLocal setting is true, or if the ApiKey is not set
            bool runLocal = config.GetValue<bool>("OpenAI:RunLocal") || string.IsNullOrWhiteSpace(config["OpenAI:ApiKey"]);
            string openAiUrl = config["OpenAI:Endpoint"]!;
            string openAiKey = config["OpenAI:ApiKey"]!;
            return new OpenAiConfiguration(openAiUrl, openAiKey, runLocal);
        }
    }

    private static class ModelNames
    {
        public const string Llama3_2_3b = "llama3.2:3b";

        public const string Gpt4oMini = "gpt-4o-mini";
    }
}