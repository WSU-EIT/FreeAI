using FreePlugins.Abstractions;

namespace FreePlugins.AuthExamplePlugin;

/// <summary>
/// An example compiled auth plugin demonstrating custom authentication
/// with username/password prompts.
/// 
/// This is a compiled NuGet-based version of the file-based LoginWithPrompts.cs plugin.
/// 
/// Features demonstrated:
/// - ICompiledAuthPlugin interface implementation
/// - LoginAsync and LogoutAsync methods
/// - Username and Password prompts
/// - Custom login button styling
/// - Tenant restriction
/// - Error handling and validation
/// </summary>
[Plugin(
    Id = "a5a8354d-f5c6-435e-90e6-2bf86f0b4d36",
    Name = "Custom Auth Example (Compiled)",
    Type = PluginTypes.Auth,
    Version = "1.0.0",
    Author = "WSU EIT",
    Description = "A compiled example auth plugin demonstrating custom authentication with username/password prompts. Converted from file-based LoginWithPrompts.cs.",
    SortOrder = 0,
    Enabled = true
)]
public class CustomAuthPlugin : ICompiledAuthPlugin
{
    /// <summary>
    /// Static property required by ICompiledPlugin interface.
    /// </summary>
    public static Type PluginType => typeof(CustomAuthPlugin);

    /// <summary>
    /// Plugin properties for compatibility with the existing plugin system.
    /// </summary>
    public Dictionary<string, object> Properties() => new() {
        { "Id", Guid.Parse("a5a8354d-f5c6-435e-90e6-2bf86f0b4d36") },
        { "Author", "WSU EIT" },
        { "ContainsSensitiveData", true },
        { "Description", "Custom authentication with username/password prompts." },
        { "Name", "Custom Auth Example (Compiled)" },
        { "Prompts", BuildPrompts() },
        { "Type", PluginTypes.Auth },
        { "Version", "1.0.0" },
        { "Enabled", true },
        { "LimitToTenants", new List<Guid> { Guid.Parse("00000000-0000-0000-0000-000000000001") } },
        { "ButtonText", "Custom Auth Login" },
        { "ButtonClass", "btn btn-primary" },
        { "ButtonIcon", "<i class=\"icon fa-solid fa-sign-in-alt\"></i>" },
    };

    /// <summary>
    /// Handle user login with username/password credentials.
    /// </summary>
    public async Task<PluginResult> LoginAsync(IPluginAuthContext context)
    {
        await Task.CompletedTask;

        var messages = new List<string>();
        var plugin = context.Plugin;

        context.LogInfo($"Auth plugin login attempt for tenant: {context.TenantId}");

        // Make sure the tenant ID is valid
        if (context.TenantId == Guid.Empty) {
            messages.Add("Invalid tenant ID");
            return PluginResult.Failure(messages);
        }

        // Make sure the URL is provided
        if (String.IsNullOrWhiteSpace(context.Url)) {
            messages.Add("Missing URL parameter");
            return PluginResult.Failure(messages);
        }

        // Extract username and password from prompts
        string username = "";
        string password = "";

        // In a real implementation, prompt values would be available through the context.
        // For this example, we document the expected behavior.

        // Simulate extracting credentials from prompts.
        // In actual usage, these would come from plugin.PromptValues.
        foreach (var prompt in plugin.Prompts) {
            context.LogInfo($"Processing prompt: {prompt.Name}");

            // Note: In compiled plugins, prompt values would be passed through
            // an extended context or separate mechanism.
        }

        // See if credentials were provided
        if (String.IsNullOrWhiteSpace(username)) {
            messages.Add("Auth plugin executed - Username prompt configured");
        }

        if (String.IsNullOrWhiteSpace(password)) {
            messages.Add("Auth plugin executed - Password prompt configured");
        }

        // In a real implementation:
        // 1. Validate credentials against your auth system (database, LDAP, OAuth, etc.)
        // 2. Create/retrieve user object
        // 3. Set authentication cookies/tokens
        // 4. Return the authenticated user

        messages.Add($"Login attempted for tenant: {context.TenantId}");
        messages.Add($"Redirect URL: {context.Url}");
        messages.Add("Note: Implement actual authentication logic here");

        context.LogInfo("Auth plugin login completed");

        return PluginResult.Success(messages);
    }

    /// <summary>
    /// Handle user logout.
    /// </summary>
    public async Task<PluginResult> LogoutAsync(IPluginAuthContext context)
    {
        await Task.CompletedTask;

        var messages = new List<string>();

        context.LogInfo($"Auth plugin logout for tenant: {context.TenantId}");

        // In a real implementation:
        // 1. Clear authentication cookies/tokens
        // 2. Invalidate session
        // 3. Call external logout endpoints if needed
        // 4. Redirect to login page or specified URL

        messages.Add($"Logout completed for tenant: {context.TenantId}");
        messages.Add($"Redirect URL: {context.Url}");
        messages.Add("Note: Implement actual logout logic here");

        context.LogInfo("Auth plugin logout completed");

        return PluginResult.Success(messages);
    }

    /// <summary>
    /// Builds the prompts for username and password.
    /// </summary>
    private static List<PluginPrompt> BuildPrompts() =>
    [
        new PluginPrompt {
            Name = "Username",
            Description = "Enter your username",
            PromptType = PluginPromptType.Text,
            Required = true,
            SortOrder = 0,
        },
        new PluginPrompt {
            Name = "Password",
            Description = "Enter your password",
            PromptType = PluginPromptType.Password,
            Required = true,
            SortOrder = 1,
        },
    ];
}
