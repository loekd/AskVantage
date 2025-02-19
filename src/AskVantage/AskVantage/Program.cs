using MudBlazor.Services;
using AskVantage.Client.Services;
using AskVantage.Services;
using AskVantage.Components;

namespace AskVantage
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.AddServiceDefaults();
            
            //Add health endpoint support
            builder.Services.AddHealthChecks();

            //Configure yarp with forwarders
            builder.Services.AddHttpForwarderWithServiceDiscovery();


            // Add services to the container.
            builder.Services.AddMudServices();
            builder.Services.AddSingleton<IImageService, ServerSideImageService>();
            builder.Services.AddSingleton<IImageApiHubClient, ServerSideImageApiHubClient>();

            builder.Services.AddRazorComponents()
                .AddInteractiveWebAssemblyComponents();

            var app = builder.Build();

            app.MapDefaultEndpoints();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseWebAssemblyDebugging();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            //app.UseHttpsRedirection();

            app.UseStaticFiles();
            app.UseAntiforgery();

            app.MapHealthChecks("/api/healthz");

            app.MapRazorComponents<App>()
                .AddInteractiveWebAssemblyRenderMode()
                .AddAdditionalAssemblies(typeof(Client._Imports).Assembly);

            //Forward calls to image API
            app.MapForwarder("/api/image/{**catch-all}", "http://imageapi", "/api/image/{**catch-all}");
            app.MapForwarder("/api/question/{**catch-all}", "http://imageapi", "/api/question/{**catch-all}");
            app.MapForwarder("/hub/{**catch-all}", "http://imageapi", "/hub/{**catch-all}");

            app.Run();
        }
    }
}
