using Azure.AI.Vision.ImageAnalysis;
using Azure;
using ImageApi.Services;
using Grpc.Net.Client;
using ImageApi.Hubs;
using Microsoft.SemanticKernel;
using System.Reflection;
using Microsoft.SemanticKernel.Prompty;

namespace ImageApi;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        builder.AddServiceDefaults();

        // Add services to the container:

        //Add Dapr state store:
        builder.AddDaprStateStore();

        //Add Azure OCR client:
        builder.AddAzureCognitiveServicesOcr();
        //Add question generator:
        builder.Services.AddQuestionGenerator(builder.Configuration, builder.Environment);

        builder.Services.AddControllers();
        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddSignalR();

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
            {
                throw new InvalidOperationException("ComputerVision:ApiKey must be set to a valid string");
            }
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                throw new InvalidOperationException("ComputerVision:Endpoint must be set to a valid string");
            }
            
            var client = new ImageAnalysisClient(
                new Uri(endpoint),
                new AzureKeyCredential(key));
            return client;
        });
        return builder;
    }


    internal static IServiceCollection AddQuestionGenerator(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        // Materializing the services here allows us to resolve ILoggerFactory.
        // This is necessary for the Prompty functions to log.
        // This gives us access to the rendered prompt.
        var loggerFactory = services.BuildServiceProvider().GetService<ILoggerFactory>();
        if (false) //environment.IsDevelopment())
        {
            services
                .AddOpenAIChatCompletion(ModelNames.Llama3_2_3b, new Uri("http://questiongenerator/v1"), apiKey: "Not Needed");
        }
        else
        {
            var config = OpenAiConfiguration.Get(configuration);
            services
                .AddAzureOpenAIChatCompletion(ModelNames.Gpt4oMini, config.ApiEndpoint, config.ApiKey);
        }

        var kernelBuilder = services.AddKernel();
        kernelBuilder.Plugins.AddPromptyFunctions(loggerFactory, environment.IsDevelopment());
        services.AddPromptyTemplates();
        services.AddTransient<IQuestionGeneratorService, OpenAIQuestionGeneratorService>();

        return services;
    }

    private static IKernelBuilderPlugins AddPromptyFunctions(this IKernelBuilderPlugins kernelPlugins,
        ILoggerFactory? factory, bool isDevelopment)
    {
        var functions = CreatePromptyFunctions(factory, isDevelopment).ToArray();

        return kernelPlugins.AddFromFunctions(PluginNames.Prompty, functions);
    }

    private static IEnumerable<KernelFunction> CreatePromptyFunctions(ILoggerFactory? factory, bool isDevelopment)
    {
        var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var promptyDirectory = Path.Join(basePath, "Prompts");
        const string searchPattern = "*.prompty";
        const string localTestFilePattern = ".local.";
        foreach (string file in Directory.EnumerateFiles(promptyDirectory, searchPattern))
        {
            bool isLocalTestFile = file.Contains(localTestFilePattern);
            switch (isLocalTestFile)
            {
                case true when isDevelopment:
                case false when !isDevelopment:
                    yield return CreatePromptyFunction(file, factory);
                    break;
            }
        }
    }

    private static KernelFunction CreatePromptyFunction(string fileName, ILoggerFactory? factory)
    {
        var text = File.ReadAllText(fileName);
        return KernelFunctionPrompty.FromPrompty(text, loggerFactory: factory);
    }


    private static IServiceCollection AddPromptyTemplates(this IServiceCollection services)
    {
        var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var promptyDirectory = Path.Join(basePath, "Prompts");
        foreach (var file in Directory.EnumerateFiles(promptyDirectory, "*.prompty"))
        {
            var promptTemplateConfig = CreatePromptyTemplateConfig(file);
            services.AddKeyedSingleton(promptTemplateConfig.Name, promptTemplateConfig);
        }

        return services;
    }

    private static PromptTemplateConfig CreatePromptyTemplateConfig(string fileName)
    {
        var text = File.ReadAllText(fileName);
        return KernelFunctionPrompty.ToPromptTemplateConfig(text);
    }


    private sealed class OpenAiConfiguration
    {
        public string ApiEndpoint { get; }
        
        public string ApiKey { get; }

        private OpenAiConfiguration(string apiUrl, string apiKey)
        {
            ApiEndpoint = apiUrl;
            ApiKey = apiKey;
        }

        public static OpenAiConfiguration Get(IConfiguration config)
        {
            //use either Azure OpenAI or fallback to Ollama locally
            string openAiUrl = config["OpenAI:Endpoint"] ?? "http://questiongenerator";
            string openAiKey = config["OpenAI:ApiKey"] ?? "Not Needed";

            return new OpenAiConfiguration(openAiUrl, openAiKey);
        }
    }

    private static class ModelNames
    {
        public const string Llama3_2_3b = "llama3.2:3b";

        public const string Gpt4oMini = "gpt-4o-mini";
    }

}
