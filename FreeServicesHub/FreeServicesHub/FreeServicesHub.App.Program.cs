using FreeServicesHub.EFModels.EFModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration.Json;
using System.Collections.Concurrent;
using FreeServicesHub.Server.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.Security.Cryptography;
using System.Text;
using System.Security.Claims;

namespace FreeServicesHub;

// ---- FreeServicesHub.App.Program.cs ----
// FreeServicesHub startup code, called from one line in each hook in Program.App.cs.
// Program.cs stays stock FreeCRM so a framework upgrade can overwrite it.

public partial class Program
{
    /// <summary>
    /// Loads FreeServicesHub's own settings files (appsettings.FreeServicesHub.App.json and its environment variant), so
    /// the stock appsettings files stay untouched. They go right after the stock appsettings files: they override those,
    /// while user secrets, environment variables and the command line still override them. Called from AppModifyBuilderStart.
    /// </summary>
    public static WebApplicationBuilder FreeServicesHubAppModifyBuilderStart(WebApplicationBuilder builder)
    {
        var sources = builder.Configuration.Sources;
        int at = sources.ToList().FindLastIndex(x => x is JsonConfigurationSource json
            && (json.Path ?? "").StartsWith("appsettings", StringComparison.OrdinalIgnoreCase)) + 1;

        sources.Insert(at, new JsonConfigurationSource {
            Path = "appsettings.FreeServicesHub.App.json",
            Optional = true,
            ReloadOnChange = true,
        });
        sources.Insert(at + 1, new JsonConfigurationSource {
            Path = $"appsettings.FreeServicesHub.App.{builder.Environment.EnvironmentName}.json",
            Optional = true,
            ReloadOnChange = true,
        });

        return builder;
    }

    /// <summary>
    /// Registers the EF context, the background-service log sink, the agent monitor and the dev registration-key
    /// seeder. Called from AppModifyBuilderEnd.
    /// </summary>
    public static WebApplicationBuilder FreeServicesHubAppModifyBuilderEnd(WebApplicationBuilder Output)
    {
        var connectionString = Output.Configuration.GetConnectionString("AppData") ?? string.Empty;
        var databaseType = (Output.Configuration.GetValue<string>("DatabaseType") ?? string.Empty).ToLower();

        Output.Services.AddDbContext<EFDataModel>(options =>
        {
            switch (databaseType)
            {
                case "inmemory":
                    options.UseInMemoryDatabase("InMemory");
                    break;
                case "mysql":
                    options.UseMySQL(connectionString, o => o.EnableRetryOnFailure());
                    break;
                case "postgresql":
                    options.UseNpgsql(connectionString, o => o.EnableRetryOnFailure());
                    break;
                case "sqlite":
                    options.UseSqlite(connectionString);
                    break;
                case "sqlserver":
                    options.UseSqlServer(connectionString, o => o.EnableRetryOnFailure());
                    break;
            }
        });

        Output.Services.AddSingleton<BackgroundServiceLogSink>();
        Output.Services.AddSingleton<ILoggerProvider>(sp => sp.GetRequiredService<BackgroundServiceLogSink>());

        Output.Services.AddHostedService<AgentMonitorService>();
        Output.Services.AddHostedService<DevRegistrationKeySeeder>();
        return Output;
    }

    /// <summary>
    /// Agent bearer-token check for /api/agent/* and the hub. Called from AppModifyStart, so it runs before UseRouting
    /// and UseAuthentication; it only looks at the request path and sets HttpContext.User for the agent.
    /// </summary>
    public static WebApplication FreeServicesHubAppModifyStart(WebApplication Output)
    {
        Output.UseMiddleware<ApiKeyMiddleware>();
        return Output;
    }

    /// <summary>
    /// Endpoints added after the framework's maps. Called from AppModifyEnd.
    /// </summary>
    public static WebApplication FreeServicesHubAppModifyEnd(WebApplication Output)
    {
        // Health check endpoint for CI/CD pipeline verification
        Output.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

        return Output;
    }

    // See if we have app-specific configuration values to load. Called from ConfigurationHelpersLoadApp.
    public static ConfigurationHelperLoader FreeServicesHubConfigurationHelpersLoadApp(
        ConfigurationHelperLoader output, WebApplicationBuilder builder){
        output.CiCdAdminToken = builder.Configuration.GetValue<string>("App:CiCdAdminToken", "") ?? string.Empty;
        output.CiCdMaxAgentRegistrations = builder.Configuration.GetValue<int>("App:CiCdMaxAgentRegistrations", 10);
        output.AgentHeartbeatIntervalSeconds = builder.Configuration.GetValue<int>("App:AgentHeartbeatIntervalSeconds", 30);
        output.AgentStaleThresholdSeconds = builder.Configuration.GetValue<int>("App:AgentStaleThresholdSeconds", 120);
        output.RegistrationKeyExpiryHours = builder.Configuration.GetValue<int>("App:RegistrationKeyExpiryHours", 24);
        output.HeartbeatRetentionHours = builder.Configuration.GetValue<int>("App:HeartbeatRetentionHours", 24);
        output.CpuWarningThreshold = builder.Configuration.GetValue<int>("App:CpuWarningThreshold", 70);
        output.CpuErrorThreshold = builder.Configuration.GetValue<int>("App:CpuErrorThreshold", 90);
        output.MemoryWarningThreshold = builder.Configuration.GetValue<int>("App:MemoryWarningThreshold", 70);
        output.MemoryErrorThreshold = builder.Configuration.GetValue<int>("App:MemoryErrorThreshold", 90);
        output.DiskWarningThreshold = builder.Configuration.GetValue<int>("App:DiskWarningThreshold", 50);
        output.DiskErrorThreshold = builder.Configuration.GetValue<int>("App:DiskErrorThreshold", 90);

        return output;
    }
}

// ---- FreeServicesHub.App.Services.AgentMonitor.cs ----
// Background service that polls for agent staleness and broadcasts status
// changes to dashboard viewers in the "AgentMonitor" SignalR group.
// Follows the same poll-detect-broadcast pattern as FreeCICD's PipelineMonitorService.

public class AgentMonitorService : BackgroundService
{
    private readonly IHubContext<freeserviceshubHub, IsrHub> _hubContext;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AgentMonitorService> _logger;

    // In-memory cache of last-known agent status, keyed by AgentId
    private readonly ConcurrentDictionary<Guid, string> _cachedStatuses = new();

    // Default poll interval — 5 seconds
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(5);

    // Track consecutive errors for exponential backoff
    private int _consecutiveErrors = 0;
    private const int MaxBackoffMultiplier = 12; // 5s * 12 = 60s max

    // Broadcast to the tenant's own SignalR group (matching the framework pattern
    // where clients join via JoinTenantId and listen on the TenantId GUID group).
    private static string GetMonitorGroup(Guid tenantId) => tenantId.ToString();

    public AgentMonitorService(
        IHubContext<freeserviceshubHub, IsrHub> HubContext,
        IServiceProvider ServiceProvider,
        ILogger<AgentMonitorService> Logger){
        _hubContext = HubContext;
        _serviceProvider = ServiceProvider;
        _logger = Logger;
    }

    protected override async Task ExecuteAsync(CancellationToken StoppingToken)
    {
        _logger.LogInformation("AgentMonitorService started");

        // Give the app a moment to finish startup before we start polling
        await Task.Delay(TimeSpan.FromSeconds(10), StoppingToken);

        while (!StoppingToken.IsCancellationRequested) {
            try {
                await PollAndBroadcast(StoppingToken);
                _consecutiveErrors = 0;
            } catch (OperationCanceledException) {
                break;
            } catch (Exception ex) {
                _consecutiveErrors++;
                _logger.LogWarning(ex, "AgentMonitorService poll error (attempt {Count})", _consecutiveErrors);
            }

            int backoffMultiplier = Math.Min(_consecutiveErrors + 1, MaxBackoffMultiplier);
            TimeSpan delay = _pollInterval * backoffMultiplier;
            await Task.Delay(delay, StoppingToken);
        }

        _logger.LogInformation("AgentMonitorService stopped");
    }

    private async Task PollAndBroadcast(CancellationToken StoppingToken)
    {
        using IServiceScope scope = _serviceProvider.CreateScope();
        IDataAccess da = scope.ServiceProvider.GetRequiredService<IDataAccess>();
        IConfigurationHelper config = scope.ServiceProvider.GetRequiredService<IConfigurationHelper>();

        int staleThreshold = config.AgentStaleThresholdSeconds;
        DateTime staleEdge = DateTime.UtcNow.AddSeconds(-staleThreshold);

        // Load all non-deleted agents via DataAccess
        List<DataObjects.Agent> agents = await da.GetAgents(null, Guid.Empty, null);

        List<DataObjects.Agent> changedAgents = new();

        foreach (DataObjects.Agent agent in agents) {
            string previousStatus = agent.Status ?? DataObjects.AgentStatuses.Offline;

            // See if the agent is stale based on last heartbeat
            string newStatus = previousStatus;
            if (agent.LastHeartbeat.HasValue && agent.LastHeartbeat.Value < staleEdge) {
                newStatus = previousStatus == DataObjects.AgentStatuses.Online
                    ? DataObjects.AgentStatuses.Stale
                    : DataObjects.AgentStatuses.Offline;
            }

            // Detect status change against our in-memory cache
            bool changed = false;
            if (_cachedStatuses.TryGetValue(agent.AgentId, out string? cached)) {
                changed = cached != newStatus;
            } else {
                // First time seeing this agent — treat as a change so dashboard gets initial state
                changed = true;
            }

            _cachedStatuses[agent.AgentId] = newStatus;

            if (changed) {
                agent.Status = newStatus;
                changedAgents.Add(agent);
            }
        }

        // Broadcast status changes to tenant-scoped groups
        if (changedAgents.Count > 0) {
            foreach (var tenantGroup in changedAgents.GroupBy(a => a.TenantId)) {
                DataObjects.SignalRUpdate changeUpdate = new() {
                    TenantId = tenantGroup.Key,
                    UpdateType = DataObjects.SignalRUpdateType.AgentStatusChanged,
                    Message = $"{tenantGroup.Count()} agent(s) status changed",
                    Object = tenantGroup.ToList(),
                };

                await _hubContext.Clients.Group(GetMonitorGroup(tenantGroup.Key)).SignalRUpdate(changeUpdate);
            }
        }

        // Always send a heartbeat per tenant so dashboard knows the service is alive
        foreach (var tenantGroup in agents.GroupBy(a => a.TenantId)) {
            DataObjects.SignalRUpdate heartbeatUpdate = new() {
                TenantId = tenantGroup.Key,
                UpdateType = DataObjects.SignalRUpdateType.AgentHeartbeat,
                Message = changedAgents.Any(a => a.TenantId == tenantGroup.Key)
                    ? $"{changedAgents.Count(a => a.TenantId == tenantGroup.Key)} change(s) detected across {tenantGroup.Count()} agents"
                    : $"Checked {tenantGroup.Count()} agents — no changes",
                Object = tenantGroup.ToList(),
            };

            await _hubContext.Clients.Group(GetMonitorGroup(tenantGroup.Key)).SignalRUpdate(heartbeatUpdate);
        }
    }

}

// ---- FreeServicesHub.App.Services.BackgroundServiceLogSink.cs ----
/// <summary>
/// In-memory ring buffer that captures log entries from hub-side background services
/// and broadcasts each entry to the "BackgroundServiceLogs" SignalR group in real time.
/// Registered as a singleton <see cref="ILoggerProvider"/> so it automatically receives
/// all ILogger output from the monitored categories.
/// </summary>
public sealed class BackgroundServiceLogSink : ILoggerProvider
{
    private const int MaxEntries = 500;
    private const string SignalRGroup = "BackgroundServiceLogs";

    // Categories whose output we capture.  Keyed by logger category → display name.
    private static readonly Dictionary<string, string> MonitoredServices = new(StringComparer.OrdinalIgnoreCase)
    {
        { "FreeServicesHub.AgentMonitorService", "Agent Monitor" },
        { "FreeServicesHub.DevRegistrationKeySeeder", "Dev Registration Key Seeder" },
        { "FreeServicesHub.BackgroundProcessor", "Background Processor" },
    };

    private readonly ConcurrentQueue<DataObjects.BackgroundServiceLogEntry> _entries = new();
    private readonly IServiceProvider _serviceProvider;

    // Track last-activity time per service for the service list endpoint.
    private readonly ConcurrentDictionary<string, DateTime> _lastActivity = new(StringComparer.OrdinalIgnoreCase);

    public BackgroundServiceLogSink(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    // ── Internal plumbing ────────────────────────────────────────────────────

    private void AddEntry(DataObjects.BackgroundServiceLogEntry entry)
    {
        _entries.Enqueue(entry);

        // Trim the ring buffer.
        while (_entries.Count > MaxEntries)
        {
            _entries.TryDequeue(out _);
        }

        _lastActivity[entry.ServiceName] = entry.Timestamp;

        // Fire-and-forget SignalR broadcast.
        _ = BroadcastAsync(entry);
    }

    private async Task BroadcastAsync(DataObjects.BackgroundServiceLogEntry entry)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var hubContext = scope.ServiceProvider.GetService<IHubContext<freeserviceshubHub, IsrHub>>();
            if (hubContext != null)
            {
                var update = new DataObjects.SignalRUpdate
                {
                    UpdateType = DataObjects.SignalRUpdateType.BackgroundServiceLog,
                    Message = entry.Message,
                    Object = entry,
                };
                await hubContext.Clients.Group(SignalRGroup).SignalRUpdate(update);
            }
        }
        catch
        {
            // Swallow – logging infra should never crash the host.
        }
    }

    // ── ILoggerProvider ──────────────────────────────────────────────────────

    public ILogger CreateLogger(string categoryName)
    {
        if (MonitoredServices.TryGetValue(categoryName, out var displayName))
        {
            return new SinkLogger(this, categoryName, displayName);
        }

        return NullLogger.Instance;
    }

    public void Dispose() { }

    /// <summary>
    /// Returns up to <paramref name="count"/> most recent log entries (newest first).
    /// </summary>
    public List<DataObjects.BackgroundServiceLogEntry> GetRecentEntries(int count = 200)
    {
        return _entries.Reverse().Take(count).ToList();
    }

    /// <summary>
    /// Returns descriptors for every monitored background service.
    /// </summary>
    public List<DataObjects.BackgroundServiceInfo> GetServiceInfos()
    {
        return MonitoredServices.Select(kv => new DataObjects.BackgroundServiceInfo
        {
            ServiceName = kv.Value,
            Description = kv.Key,
            Status = "Running",
            LastActivity = _lastActivity.TryGetValue(kv.Key, out var ts) ? ts : null,
        }).ToList();
    }

    // ── Nested logger that writes into the ring buffer ───────────────────────

    private sealed class SinkLogger(BackgroundServiceLogSink sink, string category, string displayName) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter){
            if (!IsEnabled(logLevel)) return;

            var message = formatter(state, exception);
            if (exception != null)
            {
                message += Environment.NewLine + exception.ToString();
            }

            sink.AddEntry(new DataObjects.BackgroundServiceLogEntry
            {
                LogId = Guid.NewGuid(),
                ServiceName = displayName,
                LogLevel = logLevel.ToString(),
                Message = message,
                Timestamp = DateTime.UtcNow,
            });
        }
    }

    /// <summary>No-op logger for categories we don't monitor.</summary>
    private sealed class NullLogger : ILogger
    {
        public static readonly NullLogger Instance = new();
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => false;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter) { }
    }
}

// ---- FreeServicesHub.App.Services.DevRegistrationKeySeeder.cs ----
/// <summary>
/// Seeds a known registration key when running in Development mode with InMemory database.
/// This allows the Aspire AppHost to start the agent with a pre-seeded key for auto-registration.
/// </summary>
public class DevRegistrationKeySeeder : IHostedService
{
    private const string DevKey = "dev-test-key-seed";

    private readonly IServiceProvider _serviceProvider;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<DevRegistrationKeySeeder> _logger;

    public DevRegistrationKeySeeder(
        IServiceProvider serviceProvider,
        IHostEnvironment environment,
        ILogger<DevRegistrationKeySeeder> logger){
        _serviceProvider = serviceProvider;
        _environment = environment;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_environment.IsDevelopment())
            return;

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EFDataModel>();

        bool anyKeys = await db.RegistrationKeys.AnyAsync(cancellationToken);
        if (anyKeys)
            return;

        var keyHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(DevKey)));

        var regKey = new EFModels.EFModels.RegistrationKey
        {
            RegistrationKeyId = Guid.NewGuid(),
            TenantId = Guid.Empty,
            KeyHash = keyHash,
            KeyPrefix = DevKey[..8],
            ExpiresAt = DateTime.UtcNow.AddDays(365),
            Used = false,
            Created = DateTime.UtcNow,
            CreatedBy = "DevSeeder",
        };

        await db.RegistrationKeys.AddAsync(regKey, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seeded dev registration key: {Prefix}...", regKey.KeyPrefix);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

// ---- FreeServicesHub.App.Middleware.ApiKey.cs ----
// Validates Bearer tokens on agent-specific API routes (/api/agent/*)
// and SignalR negotiate (/freeserviceshubHub).
// Hashes the incoming token with SHA-256 and looks it up in ApiClientTokens.
// On success, stashes AgentId and TenantId in HttpContext.Items and sets
// HttpContext.User with a ClaimsPrincipal so downstream [Authorize] passes.

public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;

    public ApiKeyMiddleware(RequestDelegate Next)
    {
        _next = Next;
    }

    private static string HashToken(string Plaintext)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(Plaintext));
        return Convert.ToBase64String(bytes);
    }

    public async Task InvokeAsync(HttpContext Context)
    {
        string path = Context.Request.Path.Value ?? "";

        bool requiresAgentAuth = path.StartsWith("/api/agent/", StringComparison.OrdinalIgnoreCase);
        bool isSignalR = path.StartsWith("/freeserviceshubHub", StringComparison.OrdinalIgnoreCase);

        if (!requiresAgentAuth && !isSignalR) {
            await _next(Context);
            return;
        }

        // Extract token -- SignalR sends via query string, HTTP API via Authorization header
        string? token = null;

        if (isSignalR) {
            token = Context.Request.Query["access_token"];
        }

        if (string.IsNullOrEmpty(token)) {
            if (!Context.Request.Headers.TryGetValue("Authorization", out var authHeader)) {
                if (requiresAgentAuth) {
                    await WriteUnauthorized(Context, "missing_token", "Authorization header required");
                    return;
                }
                // For SignalR without a token, let it fall through to normal auth (Blazor UI users)
                await _next(Context);
                return;
            }

            string headerValue = authHeader.ToString();
            if (!headerValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) {
                if (requiresAgentAuth) {
                    await WriteUnauthorized(Context, "invalid_format", "Authorization header must use Bearer scheme");
                    return;
                }
                await _next(Context);
                return;
            }

            token = headerValue.Substring(7).Trim();
        }

        if (string.IsNullOrEmpty(token)) {
            if (requiresAgentAuth) {
                await WriteUnauthorized(Context, "empty_token", "Bearer token cannot be empty");
                return;
            }
            await _next(Context);
            return;
        }

        // Hash the token and look it up
        string tokenHash = HashToken(token);

        using IServiceScope scope = Context.RequestServices.CreateScope();
        EFDataModel db = scope.ServiceProvider.GetRequiredService<EFDataModel>();

        EFModels.EFModels.ApiClientToken? clientToken = await db.ApiClientTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && t.Active && t.RevokedAt == null);

        if (clientToken == null) {
            if (requiresAgentAuth) {
                await WriteUnauthorized(Context, "invalid_token", "Token is invalid, revoked, or inactive");
                return;
            }
            // For SignalR, let it fall through -- might be a Blazor UI user with cookie auth
            await _next(Context);
            return;
        }

        // Stash agent identity for downstream controllers
        Context.Items["AgentId"] = clientToken.AgentId;
        Context.Items["AgentTenantId"] = clientToken.TenantId;

        // Create a ClaimsPrincipal so [Authorize] on the hub passes
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, clientToken.AgentId.ToString()),
            new Claim("AgentId", clientToken.AgentId.ToString()),
            new Claim("TenantId", clientToken.TenantId.ToString()),
        };
        var identity = new ClaimsIdentity(claims, "AgentToken");
        Context.User = new ClaimsPrincipal(identity);

        await _next(Context);
    }

    private static async Task WriteUnauthorized(HttpContext Context, string Error, string Message)
    {
        Context.Response.StatusCode = 401;
        Context.Response.ContentType = "application/json";
        await Context.Response.WriteAsJsonAsync(new { error = Error, message = Message });
    }
}
