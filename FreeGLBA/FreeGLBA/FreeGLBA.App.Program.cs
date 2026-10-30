using FreeGLBA.Middleware;
using Microsoft.Extensions.Configuration.Json;
using Scalar.AspNetCore;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;

// ---- Program ----

namespace FreeGLBA
{
// FreeGLBA startup code, called from one line in each hook in Program.App.cs.
// Program.cs stays stock FreeCRM so a framework upgrade can overwrite it.

public partial class Program
{
    /// <summary>
    /// Loads FreeGLBA's own settings files (appsettings.FreeGLBA.App.json and its environment variant), so the stock
    /// appsettings files stay untouched. They go right after the stock appsettings files: they override those, while user
    /// secrets, environment variables and the command line still override them. Called from AppModifyBuilderStart.
    /// </summary>
    public static WebApplicationBuilder FreeGLBAAppModifyBuilderStart(WebApplicationBuilder builder)
    {
        var sources = builder.Configuration.Sources;
        int at = sources.ToList().FindLastIndex(x => x is JsonConfigurationSource json
            && (json.Path ?? "").StartsWith("appsettings", StringComparison.OrdinalIgnoreCase)) + 1;

        sources.Insert(at, new JsonConfigurationSource {
            Path = "appsettings.FreeGLBA.App.json",
            Optional = true,
            ReloadOnChange = true,
        });
        sources.Insert(at + 1, new JsonConfigurationSource {
            Path = $"appsettings.FreeGLBA.App.{builder.Environment.EnvironmentName}.json",
            Optional = true,
            ReloadOnChange = true,
        });

        return builder;
    }

    /// <summary>
    /// Registers the OpenAPI document for the GLBA API. Called from AppModifyBuilderEnd.
    /// </summary>
    public static WebApplicationBuilder FreeGLBAAppModifyBuilderEnd(WebApplicationBuilder builder)
    {
        // OpenAPI documentation (Scalar UI)
        builder.Services.AddOpenApi("glba-api", options =>
        {
            options.AddDocumentTransformer((document, context, cancellationToken) =>
            {
                document.Info = new()
                {
                    Title = "FreeGLBA API",
                    Version = "v1",
                    Description = """
                        ## GLBA Compliance Data Access Tracking API
                        
                        FreeGLBA provides endpoints for logging and querying access to protected financial information 
                        as required by the Gramm-Leach-Bliley Act (GLBA).
                        
                        ### Authentication
                        
                        **External Endpoints** (POST /api/glba/events, /api/glba/events/batch):
                        - Use API Key authentication via the `Authorization: Bearer {api-key}` header
                        - API keys are generated in the Source Systems admin page
                        
                        **Internal Endpoints** (GET /api/glba/...):
                        - Require user authentication (JWT bearer token)
                        - Used by the dashboard for querying data
                        
                        ### Quick Start
                        
                        ```bash
                        curl -X POST /api/glba/events \
                          -H "Authorization: Bearer YOUR_API_KEY" \
                          -H "Content-Type: application/json" \
                          -d '{"userId": "jsmith", "subjectId": "STU-12345", "accessType": "View"}'
                        ```
                        """,
                    Contact = new() { Name = "WSU Enrollment IT", Url = new Uri("https://em.wsu.edu/eit") }
                };
                return Task.CompletedTask;
            });
        });

        return builder;
    }

    /// <summary>
    /// API-key authentication for external GLBA event submissions, and the OpenAPI/Scalar pages in Development.
    /// Called from AppModifyEnd. Middleware added here runs after UseAuthentication/UseAuthorization and before the
    /// endpoint, because WebApplication adds the endpoint middleware at the end of the pipeline, so the API-key check
    /// keeps the same place it had when it sat after UseAuthorization in Program.cs.
    /// </summary>
    public static WebApplication FreeGLBAAppModifyEnd(WebApplication app)
    {
        // API Key authentication for external GLBA event submissions
        app.UseApiKeyAuth();

        if (app.Environment.IsDevelopment()) {
            // OpenAPI documentation (available at /scalar/glba-api)
            app.MapOpenApi();
            app.MapScalarApiReference(options =>
            {
                options
                    .WithTitle("FreeGLBA API Documentation")
                    .WithTheme(ScalarTheme.BluePlanet)
                    .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
                    .WithPreferredScheme("Bearer");
            });
        }

        return app;
    }
}
}

// ---- Middleware.ApiKey ----

namespace FreeGLBA.Middleware
{
// ============================================================================
// API KEY AUTHENTICATION MIDDLEWARE
// Validates Bearer token for external API endpoints.
// Generated by FreeManager for template: FreeAudit
// ============================================================================

public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;

    public ApiKeyMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IDataAccess da)
    {
        // Only apply to external API paths that need API key auth
        // POST /api/glba/events - external event submission
        // POST /api/glba/events/batch - external batch submission
        // GET requests to /api/glba/* are internal and use user auth
        var path = context.Request.Path.Value ?? "";
        var method = context.Request.Method;

        // Only intercept POST requests to the events endpoint
        // GET requests (like /api/glba/events/recent) use [Authorize] user auth
        var requiresApiKey = method.Equals("POST", StringComparison.OrdinalIgnoreCase) &&
                             (path.Equals("/api/glba/events", StringComparison.OrdinalIgnoreCase) ||
                              path.Equals("/api/glba/events/batch", StringComparison.OrdinalIgnoreCase));

        if (!requiresApiKey)
        {
            await _next(context);
            return;
        }

        // Extract API key from Authorization header
        if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            await WriteUnauthorized(context, "missing_api_key", "Authorization header required");
            return;
        }

        var headerValue = authHeader.ToString();
        if (!headerValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            await WriteUnauthorized(context, "invalid_format", "Authorization header must use Bearer scheme");
            return;
        }

        var apiKey = headerValue.Substring(7).Trim();
        if (string.IsNullOrEmpty(apiKey))
        {
            await WriteUnauthorized(context, "empty_api_key", "API key cannot be empty");
            return;
        }

        // Validate API key
        var source = await da.ValidateApiKeyAsync(apiKey);
        if (source == null)
        {
            await WriteUnauthorized(context, "invalid_api_key", "API key is invalid or inactive");
            return;
        }

        // Store source system in context for controller
        context.Items["SourceSystem"] = source;

        await _next(context);
    }

    private static async Task WriteUnauthorized(HttpContext context, string error, string message)
    {
        context.Response.StatusCode = 401;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { error, message });
    }
}

// ============================================================================
// MIDDLEWARE EXTENSION
// ============================================================================

public static class ApiKeyMiddlewareExtensions
{
    public static IApplicationBuilder UseApiKeyAuth(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ApiKeyMiddleware>();
    }
}
}
