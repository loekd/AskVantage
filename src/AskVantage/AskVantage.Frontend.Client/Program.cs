using AskVantage.Frontend.Client.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor;
using MudBlazor.Services;

namespace AskVantage.Frontend.Client;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);

        builder.Services.AddMudServices(config =>
        {
            config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.TopCenter;
            config.SnackbarConfiguration.PreventDuplicates = true;
            config.SnackbarConfiguration.NewestOnTop = true;
            config.SnackbarConfiguration.ShowCloseIcon = false;
            config.SnackbarConfiguration.VisibleStateDuration = 2000;
            config.SnackbarConfiguration.HideTransitionDuration = 500;
            config.SnackbarConfiguration.ShowTransitionDuration = 500;
            config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
            config.SnackbarConfiguration.RequireInteraction = false;
            config.SnackbarConfiguration.MaxDisplayedSnackbars = 3;
        });

        builder.Services.AddScoped<IImageService, ImageService>();
        builder.Services
            .AddHttpClient<IImageService, ImageService>(client =>
                client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress))
            .AddPolicyHandler(HttpClientPolicies.GetTimeoutPolicy())
            .AddPolicyHandler((sp, req) => HttpClientPolicies.GetRetryPolicy(sp));

        builder.Services.AddSingleton<IImageApiHubClient, ImageApiHubClient>();

        await builder.Build().RunAsync();
    }
}