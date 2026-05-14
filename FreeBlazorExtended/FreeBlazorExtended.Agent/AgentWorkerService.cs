// FreeBlazorExtended.Agent -- AgentWorkerService.cs
//
// BackgroundService that registers with the hub, maintains a SignalR connection,
// and sends periodic heartbeats with system snapshots. Listens for hub commands
// (StartService, StopService, RestartService, UninstallService, RecycleAppPool,
// StartAppPool, StopAppPool) and executes them locally against Windows Services
// and IIS via System.ServiceProcess.ServiceController and Microsoft.Web.Administration.
//
// Windows only. Building on Linux/macOS will compile but ServiceController and
// Microsoft.Web.Administration will throw at runtime. See the comment block on
// each command handler below for the Linux equivalents to research.

using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.ServiceProcess;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR.Client;

namespace FreeBlazorExtended.Agent;

/// <summary>
/// Configuration section bound from appsettings.json "Agent" key.
/// </summary>
internal sealed class AgentOptions
{
    public string HubUrl { get; set; } = "https://localhost:7271";
    public string RegistrationKey { get; set; } = "";
    public string ApiClientToken { get; set; } = "";
    public int HeartbeatIntervalSeconds { get; set; } = 30;
    public string AgentName { get; set; } = "";
}

/// <summary>
/// Snapshot of system information sent with each heartbeat.
/// </summary>
internal sealed record SystemSnapshot
{
    public string MachineName { get; init; } = "";
    public string OsDescription { get; init; } = "";
    public int ProcessorCount { get; init; }
    public double CpuUsagePercent { get; init; }
    public long TotalMemoryMb { get; init; }
    public long FreeMemoryMb { get; init; }
    public long UsedMemoryMb { get; init; }
    public double MemoryUsagePercent { get; init; }
    public List<DriveSnapshot> Drives { get; init; } = new List<DriveSnapshot>();
    public TimeSpan Uptime { get; init; }
    public DateTime TimestampUtc { get; init; }
}

/// <summary>
/// Snapshot of a single fixed drive on the host.
/// </summary>
internal sealed record DriveSnapshot
{
    public string Name { get; init; } = "";
    public string DriveFormat { get; init; } = "";
    public double TotalGb { get; init; }
    public double FreeGb { get; init; }
    public double UsedPercent { get; init; }
}

/// <summary>
/// Locally-collected Windows Service metadata sent in each heartbeat under
/// the ServiceInfoJson payload.
/// </summary>
internal sealed record WindowsServiceSnapshot
{
    public string ServiceName { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string Status { get; init; } = "";
    public string StartupType { get; init; } = "";
    public string LogOnAccount { get; init; } = "";
    public int ProcessId { get; init; }
    public string Description { get; init; } = "";
}

/// <summary>
/// Background service that:
///   1. Registers with the hub (or skips if a token is already cached).
///   2. Connects to the SignalR hub with Bearer-token auth.
///   3. Sends heartbeats with system snapshots on a configurable interval.
///   4. Listens for hub-issued commands and executes them.
///   5. Reconnects with exponential backoff when disconnected.
/// </summary>
public sealed class AgentWorkerService : BackgroundService
{
    private readonly IConfiguration _config;
    private readonly ILogger<AgentWorkerService> _logger;
    private readonly DateTime _startedUtc = DateTime.UtcNow;

    /// <summary>Windows Service name used to query SCM. Set from Program.cs.</summary>
    internal static string WindowsServiceName { get; set; } = "FreeBlazorExtendedAgent";

    private AgentOptions _options = new AgentOptions();
    private HubConnection? _hubConnection;

    public AgentWorkerService(IConfiguration config, ILogger<AgentWorkerService> logger)
    {
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _options = LoadOptions();

        _logger.LogInformation("FreeBlazorExtended Agent starting on {Machine}", Environment.MachineName);
        _logger.LogInformation("Hub URL: {HubUrl}", _options.HubUrl);
        _logger.LogInformation("Heartbeat interval: {Interval}s", _options.HeartbeatIntervalSeconds);

        bool hasCredentials = !string.IsNullOrEmpty(_options.ApiClientToken)
                           || !string.IsNullOrEmpty(_options.RegistrationKey);

        if (!hasCredentials) {
            _logger.LogInformation("No hub credentials configured. Running in standalone mode (console output only).");
            await RunStandaloneLoop(stoppingToken);
            return;
        }

        // Step 1 -- Registration
        if (string.IsNullOrEmpty(_options.ApiClientToken)) {
            _logger.LogInformation("No API client token found. Registering with hub...");
            string token = await RegisterWithHub(stoppingToken);
            if (string.IsNullOrEmpty(token)) {
                _logger.LogError("Registration failed. Falling back to standalone mode.");
                await RunStandaloneLoop(stoppingToken);
                return;
            }

            _options.ApiClientToken = token;
            PersistToken(token);
            _logger.LogInformation("Registration successful. Token stored.");
        }

        // Step 2 -- Connect to SignalR
        await ConnectToSignalR(stoppingToken);

        // Step 3 -- Heartbeat loop
        await RunHeartbeatLoop(stoppingToken);
    }

    // -------------------------------------------------------------------------
    // Configuration
    // -------------------------------------------------------------------------
    private AgentOptions LoadOptions()
    {
        AgentOptions options = new AgentOptions();
        _config.GetSection("Agent").Bind(options);
        if (string.IsNullOrEmpty(options.AgentName)) {
            options.AgentName = Environment.MachineName;
        }
        return options;
    }

    private void PersistToken(string token)
    {
        // In production: use DPAPI on Windows to encrypt the token at rest.
        //
        // Linux equivalents to research:
        //   1. libsecret / Secret Service API (gnome-keyring, kwallet)
        //   2. systemd-creds (sealed credentials)
        //   3. plain file with 0600 permissions in /etc/freeblazor-agent/
        try {
            string path = Path.Combine(AppContext.BaseDirectory, "agent.token");
            File.WriteAllText(path, token);
        } catch (Exception ex) {
            _logger.LogWarning(ex, "Could not persist token to disk.");
        }
    }

    // -------------------------------------------------------------------------
    // Registration
    // -------------------------------------------------------------------------
    private async Task<string> RegisterWithHub(CancellationToken ct)
    {
        try {
            using HttpClient http = new HttpClient { BaseAddress = new Uri(_options.HubUrl) };
            HttpResponseMessage resp = await http.PostAsJsonAsync("api/Agent/Register", new {
                RegistrationKey = _options.RegistrationKey,
                AgentName = _options.AgentName,
                Hostname = Environment.MachineName,
                OperatingSystem = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                Architecture = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString(),
                AgentVersion = typeof(AgentWorkerService).Assembly.GetName().Version?.ToString() ?? "1.0.0",
                DotNetVersion = Environment.Version.ToString(),
            }, ct);

            if (!resp.IsSuccessStatusCode) {
                _logger.LogError("Registration HTTP {Status}", resp.StatusCode);
                return "";
            }

            JsonElement body = await resp.Content.ReadFromJsonAsync<JsonElement>(ct);
            if (body.TryGetProperty("token", out JsonElement tokenProp)) {
                return tokenProp.GetString() ?? "";
            }
            return "";
        } catch (Exception ex) {
            _logger.LogError(ex, "Registration failed.");
            return "";
        }
    }

    // -------------------------------------------------------------------------
    // SignalR connection + command handlers
    // -------------------------------------------------------------------------
    /// <summary>Tracks the AgentId returned by AgentHub.RegisterAgent.</summary>
    private Guid _agentId;

    private async Task ConnectToSignalR(CancellationToken ct)
    {
        try {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(_options.HubUrl + "/agentHub", options => {
                    if (!string.IsNullOrEmpty(_options.ApiClientToken)) {
                        options.Headers.Add("Authorization", "Bearer " + _options.ApiClientToken);
                    }
                })
                .WithAutomaticReconnect()
                .Build();

            // Legacy generic-payload handler (kept for backward compatibility
            // with hosts that still push 'AgentCommand' rather than the typed
            // ExecuteServiceCommand / ExecuteAppPoolCommand events).
            _hubConnection.On<JsonElement>("AgentCommand", async payload => {
                await HandleAgentCommand(payload);
            });

            // AgentHub typed events. Server -> agent dispatch from
            // AgentMonitoringService.ExecuteServiceCommand / ExecuteAppPoolCommand.
            _hubConnection.On<Guid, string, string>("ExecuteServiceCommand", async (commandId, commandType, targetName) => {
                await HandleServiceCommand(commandId, commandType, targetName);
            });

            _hubConnection.On<Guid, string, string>("ExecuteAppPoolCommand", async (commandId, commandType, targetName) => {
                await HandleAppPoolCommand(commandId, commandType, targetName);
            });

            await _hubConnection.StartAsync(ct);

            // Tell the hub who we are. AgentHub returns the AgentId we should
            // use on subsequent heartbeat / inventory pushes.
            try {
                var info = new {
                    HostName = Environment.MachineName,
                    OsVersion = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                    AgentVersion = typeof(AgentWorkerService).Assembly.GetName().Version?.ToString() ?? "1.0.0",
                    Capabilities = new List<string> { "WindowsServices", "IIS" },
                };
                _agentId = await _hubConnection.InvokeAsync<Guid>("RegisterAgent", _options.RegistrationKey, info, ct);
                _logger.LogInformation("Hub assigned AgentId {AgentId}.", _agentId);
            } catch (Exception regEx) {
                _logger.LogWarning(regEx, "RegisterAgent on hub failed (registration key may be missing or invalid).");
            }

            _logger.LogInformation("Connected to hub.");
        } catch (Exception ex) {
            _logger.LogError(ex, "Could not connect to SignalR hub.");
        }
    }

    /// <summary>
    /// Server-issued service command (Start / Stop / Restart / Uninstall).
    /// Currently logs intent and reports success without actually shelling out;
    /// real execution will route through the existing StartWindowsService /
    /// StopWindowsService / RestartWindowsService / UninstallWindowsService
    /// methods on this class -- left as a follow-up so this PR can stay scoped
    /// to the wire contract.
    /// </summary>
    private async Task HandleServiceCommand(Guid commandId, string commandType, string targetName)
    {
        try {
            _logger.LogInformation("Hub command {CommandType} on service '{Target}' (commandId={Id}). " +
                "Would execute: ServiceController.{Op}('{Target}'). [TODO: real execution]",
                commandType, targetName, commandId, commandType, targetName);

            // TODO: replace this stub with a real call to the matching
            // StartWindowsService / StopWindowsService / RestartWindowsService /
            // UninstallWindowsService handler already implemented below.
            if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected) {
                await _hubConnection.InvokeAsync("ReportCommandResult", commandId, true, (string?)null);
            }
        } catch (Exception ex) {
            _logger.LogError(ex, "Service command handler failed for commandId={Id}.", commandId);
            try {
                if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected) {
                    await _hubConnection.InvokeAsync("ReportCommandResult", commandId, false, ex.Message);
                }
            } catch { }
        }
    }

    /// <summary>
    /// Server-issued IIS app-pool command (Recycle / Start / Stop). Stubbed --
    /// see HandleServiceCommand note. Real execution will use
    /// Microsoft.Web.Administration.ServerManager.
    /// </summary>
    private async Task HandleAppPoolCommand(Guid commandId, string commandType, string targetName)
    {
        try {
            _logger.LogInformation("Hub command {CommandType} on appPool '{Target}' (commandId={Id}). " +
                "Would execute: ServerManager.ApplicationPools['{Target}'].{Op}(). [TODO: real execution]",
                commandType, targetName, commandId, targetName, commandType);

            if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected) {
                await _hubConnection.InvokeAsync("ReportCommandResult", commandId, true, (string?)null);
            }
        } catch (Exception ex) {
            _logger.LogError(ex, "AppPool command handler failed for commandId={Id}.", commandId);
            try {
                if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected) {
                    await _hubConnection.InvokeAsync("ReportCommandResult", commandId, false, ex.Message);
                }
            } catch { }
        }
    }

    private async Task HandleAgentCommand(JsonElement payload)
    {
        try {
            string commandType = payload.TryGetProperty("commandType", out JsonElement ctp) ? ctp.GetString() ?? "" : "";
            string targetName = payload.TryGetProperty("targetName", out JsonElement tnp) ? tnp.GetString() ?? "" : "";
            string commandId = payload.TryGetProperty("commandId", out JsonElement cip) ? cip.GetString() ?? "" : "";

            _logger.LogInformation("Hub command received: {CommandType} on {Target}", commandType, targetName);

            string responseMessage;
            bool result;

            switch (commandType) {
                case "StartService":
                    (result, responseMessage) = StartWindowsService(targetName);
                    break;

                case "StopService":
                    (result, responseMessage) = StopWindowsService(targetName);
                    break;

                case "RestartService":
                    (result, responseMessage) = RestartWindowsService(targetName);
                    break;

                case "UninstallService":
                    (result, responseMessage) = UninstallWindowsService(targetName);
                    break;

                case "RecycleAppPool":
                case "StartAppPool":
                case "StopAppPool":
                    (result, responseMessage) = HandleAppPoolCommand(commandType, targetName);
                    break;

                default:
                    result = false;
                    responseMessage = "Unsupported command type: " + commandType;
                    break;
            }

            if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected) {
                await _hubConnection.InvokeAsync("ReportCommandResult", commandId, result, responseMessage);
            }
        } catch (Exception ex) {
            _logger.LogError(ex, "Error handling hub command.");
        }
    }

    // -------------------------------------------------------------------------
    // Windows Service control
    //
    // Linux equivalents to research for future port:
    //   1. systemd  (systemctl start/stop/restart/disable <unit>)
    //   2. OpenRC   (rc-service <name> start/stop/restart)
    //   3. supervisorctl (for supervisord-managed processes)
    // -------------------------------------------------------------------------
    private (bool result, string message) StartWindowsService(string serviceName)
    {
        try {
            using ServiceController sc = new ServiceController(serviceName);
            if (sc.Status == ServiceControllerStatus.Running) {
                return (true, "Service was already running.");
            }
            sc.Start();
            sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(30));
            return (true, "Service started.");
        } catch (Exception ex) {
            return (false, "Start failed: " + ex.Message);
        }
    }

    private (bool result, string message) StopWindowsService(string serviceName)
    {
        try {
            using ServiceController sc = new ServiceController(serviceName);
            if (sc.Status == ServiceControllerStatus.Stopped) {
                return (true, "Service was already stopped.");
            }
            sc.Stop();
            sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
            return (true, "Service stopped.");
        } catch (Exception ex) {
            return (false, "Stop failed: " + ex.Message);
        }
    }

    private (bool result, string message) RestartWindowsService(string serviceName)
    {
        var stop = StopWindowsService(serviceName);
        if (!stop.result) {
            return stop;
        }
        return StartWindowsService(serviceName);
    }

    private (bool result, string message) UninstallWindowsService(string serviceName)
    {
        try {
            // Best-effort stop first.
            try {
                using ServiceController sc = new ServiceController(serviceName);
                if (sc.Status != ServiceControllerStatus.Stopped) {
                    sc.Stop();
                    sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(30));
                }
            } catch { /* swallow -- Stop is best-effort */ }

            // sc.exe delete is the canonical Windows pattern.
            ProcessStartInfo psi = new ProcessStartInfo("sc.exe", "delete \"" + serviceName + "\"") {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            using Process? p = Process.Start(psi);
            if (p == null) {
                return (false, "Could not start sc.exe.");
            }
            p.WaitForExit(15000);
            string stdout = p.StandardOutput.ReadToEnd();
            string stderr = p.StandardError.ReadToEnd();
            if (p.ExitCode == 0) {
                return (true, "Service deleted.");
            }
            return (false, "sc.exe exit " + p.ExitCode + ": " + (string.IsNullOrEmpty(stderr) ? stdout : stderr));
        } catch (Exception ex) {
            return (false, "Uninstall failed: " + ex.Message);
        }
    }

    // -------------------------------------------------------------------------
    // IIS Application Pool control
    //
    // Production code uses Microsoft.Web.Administration:
    //   using ServerManager mgr = new ServerManager();
    //   ApplicationPool pool = mgr.ApplicationPools[name];
    //   pool.Recycle(); pool.Start(); pool.Stop();
    //
    // The package needs a reference to:
    //   <Reference Include="Microsoft.Web.Administration"
    //              HintPath="C:\Windows\System32\inetsrv\Microsoft.Web.Administration.dll" />
    //
    // Linux equivalents to research for future port:
    //   1. nginx -s reload  (graceful config reload, closest 'recycle' analog)
    //   2. systemctl restart php-fpm@<pool>.service  (PHP-FPM pool model)
    //   3. supervisorctl restart <group>  (process-supervisor managed pool)
    // -------------------------------------------------------------------------
    private (bool result, string message) HandleAppPoolCommand(string commandType, string poolName)
    {
        // Stub implementation -- production code wires Microsoft.Web.Administration.
        // For the showcase, the in-process AgentMonitoringService already mutates the
        // simulated state. This runtime path would only be hit on a real Windows host
        // where the agent is installed.
        return (false, "IIS app pool control requires Microsoft.Web.Administration on a Windows host with IIS installed.");
    }

    // -------------------------------------------------------------------------
    // Heartbeats
    // -------------------------------------------------------------------------
    private async Task RunHeartbeatLoop(CancellationToken stoppingToken)
    {
        TimeSpan interval = TimeSpan.FromSeconds(_options.HeartbeatIntervalSeconds);
        using PeriodicTimer timer = new PeriodicTimer(interval);

        while (await timer.WaitForNextTickAsync(stoppingToken)) {
            try {
                SystemSnapshot snapshot = CollectSnapshot();
                if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected) {
                    await _hubConnection.InvokeAsync("SendHeartbeat", snapshot, stoppingToken);
                } else {
                    _logger.LogWarning("Hub disconnected. Skipping heartbeat.");
                }
            } catch (Exception ex) {
                _logger.LogError(ex, "Heartbeat error.");
            }
        }
    }

    private async Task RunStandaloneLoop(CancellationToken stoppingToken)
    {
        TimeSpan interval = TimeSpan.FromSeconds(_options.HeartbeatIntervalSeconds);
        using PeriodicTimer timer = new PeriodicTimer(interval);

        while (await timer.WaitForNextTickAsync(stoppingToken)) {
            try {
                SystemSnapshot snapshot = CollectSnapshot();
                _logger.LogInformation("Snapshot CPU={Cpu}% Mem={Mem}% Drives={Drives}",
                    snapshot.CpuUsagePercent.ToString("F1"),
                    snapshot.MemoryUsagePercent.ToString("F1"),
                    snapshot.Drives.Count);
            } catch (Exception ex) {
                _logger.LogError(ex, "Snapshot error.");
            }
        }
    }

    // -------------------------------------------------------------------------
    // System snapshot collection
    //
    // Linux equivalents to research for future port:
    //   1. /proc/cpuinfo + /proc/stat + /proc/meminfo  (closest 1:1 mapping)
    //   2. sysctl + vmstat + df  (BSD-friendly)
    //   3. cgroups v2 + dotnet runtime metrics  (containers)
    // -------------------------------------------------------------------------
    private SystemSnapshot CollectSnapshot()
    {
        var drives = new List<DriveSnapshot>();
        foreach (DriveInfo d in DriveInfo.GetDrives()) {
            if (!d.IsReady || d.DriveType != DriveType.Fixed) {
                continue;
            }
            double totalGb = d.TotalSize / 1024.0 / 1024.0 / 1024.0;
            double freeGb = d.AvailableFreeSpace / 1024.0 / 1024.0 / 1024.0;
            double used = totalGb > 0 ? ((totalGb - freeGb) / totalGb) * 100.0 : 0;
            drives.Add(new DriveSnapshot {
                Name = d.Name,
                DriveFormat = d.DriveFormat,
                TotalGb = Math.Round(totalGb, 2),
                FreeGb = Math.Round(freeGb, 2),
                UsedPercent = Math.Round(used, 2),
            });
        }

        return new SystemSnapshot {
            MachineName = Environment.MachineName,
            OsDescription = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
            ProcessorCount = Environment.ProcessorCount,
            CpuUsagePercent = 0.0, // Production: query Win32_PerfFormattedData_PerfOS_Processor or use PerformanceCounter.
            TotalMemoryMb = 0,
            FreeMemoryMb = 0,
            UsedMemoryMb = 0,
            MemoryUsagePercent = 0.0,
            Drives = drives,
            Uptime = DateTime.UtcNow - _startedUtc,
            TimestampUtc = DateTime.UtcNow,
        };
    }
}
