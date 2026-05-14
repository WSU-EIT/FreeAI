using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using MudBlazor.Services;
using Radzen;

namespace FreeBlazorExample.Client
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);

            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
            builder.Services.AddBlazoredLocalStorage();
            builder.Services.AddSingleton<BlazorDataModel>();
            builder.Services.AddBlazorBootstrap();
            builder.Services.AddMudServices();
            builder.Services.AddRadzenComponents();
            builder.Services.AddScoped<Radzen.DialogService>();
            builder.Services.AddScoped<Radzen.NotificationService>();
            builder.Services.AddScoped<Radzen.ThemeService>();
            builder.Services.AddSingleton(serviceProvider => (IJSInProcessRuntime)serviceProvider.GetRequiredService<IJSRuntime>());

            // FreeBlazorExtended showcase services (Features 100-114)
            builder.Services.AddSingleton<FreeBlazorExtended.Foundation.Services.NotificationService>();
            builder.Services.AddSingleton<FreeBlazorExtended.DynamicForms.FormService>();
            builder.Services.AddSingleton<FreeBlazorExtended.UserPreferences.UserPreferencesService>();
            builder.Services.AddSingleton<FreeBlazorExtended.Calendar.CalendarEventService>();
            builder.Services.AddSingleton<FreeBlazorExtended.MultiViewSync.RealtimeSyncService>();
            builder.Services.AddSingleton<FreeBlazorExtended.AgentMonitoring.AgentMonitoringService>();
            builder.Services.AddSingleton<FreeBlazorExtended.HierarchicalTree.TreeService>();
            // NOTE: GitHubRepoService, GitRepoBrowserService, SmartsheetService removed.
            // Components now call server-proxy endpoints (/api/Showcase/*/...) via HttpClient.
            // Credentials are held server-side in ExternalApiSessionStore, keyed by HttpOnly cookie.

            await builder.Build().RunAsync();
        }
    }
}