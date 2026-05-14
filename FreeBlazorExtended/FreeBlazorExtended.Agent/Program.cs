// FreeBlazorExtended.Agent -- Program.cs
// Host builder for the FreeBlazorExtended Agent worker service.
// Runs as a Windows Service via sc.exe or as a console app for debugging.
//
// Windows-only by design. To install as a service:
//   sc.exe create FreeBlazorExtendedAgent binPath= "C:\Program Files\FreeBlazorExtended.Agent\FreeBlazorExtended.Agent.exe"
//   sc.exe start FreeBlazorExtendedAgent
//
// Linux equivalents to research for future port:
//   1. systemd unit (.service file + systemctl enable/start)
//   2. OpenRC service (rc-service / older distros)
//   3. Supervisor (supervisord, for non-init managed processes)

using FreeBlazorExtended.Agent;

var builder = Host.CreateDefaultBuilder(args)
    .UseWindowsService(options => {
        options.ServiceName = "FreeBlazorExtendedAgent";
    })
    .ConfigureAppConfiguration((context, config) => {
        config.SetBasePath(AppContext.BaseDirectory);
        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
    })
    .ConfigureLogging((context, logging) => {
        // Default file logger -- one rolling file next to the executable.
        // Production deployments typically replace this with Serilog or NLog.
        var logPath = Path.Combine(AppContext.BaseDirectory, "agent.log");
        logging.AddProvider(new FileLoggerProvider(logPath));
    })
    .ConfigureServices((context, services) => {
        services.AddHostedService<AgentWorkerService>();
    });

var host = builder.Build();

// Expose the lifetime so the SignalR Shutdown handler can trigger graceful stop.
FreeBlazorExtended.Agent.Program.Lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();

host.Run();

namespace FreeBlazorExtended.Agent
{
    public static class Program
    {
        public static IHostApplicationLifetime? Lifetime { get; set; }
    }
}
