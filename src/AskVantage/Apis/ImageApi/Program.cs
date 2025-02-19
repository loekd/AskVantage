using Azure.AI.Vision.ImageAnalysis;
using Azure;
using ImageApi.Services;
using Grpc.Net.Client;
using ImageApi.Hubs;

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

        //builder.AddAzureOpenAIClient("openAiConnection");
        //builder.Services.AddScoped<IQuestionGeneratorService, AzureQuestionGeneratorService>();
        
        builder.AddOllamaQuestionGenerator();

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

    internal static WebApplicationBuilder AddOllamaQuestionGenerator(this WebApplicationBuilder builder)
    {
        builder.Services.AddHttpClient<IOllamaApiClientFactory, OllamaApiClientFactory>(c => c.BaseAddress = new("http://questiongenerator"));
        builder.Services.AddSingleton<IOllamaApiClientFactory, OllamaApiClientFactory>();

        builder.Services.AddScoped<IQuestionGeneratorService, OllamaQuestionGeneratorService>();
        return builder;
    }
}
