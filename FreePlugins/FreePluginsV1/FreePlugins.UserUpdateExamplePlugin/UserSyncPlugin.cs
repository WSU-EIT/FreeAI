using FreePlugins.Abstractions;

namespace FreePlugins.UserUpdateExamplePlugin;

/// <summary>
/// An example compiled user update plugin demonstrating how to synchronize
/// user information from external systems.
/// 
/// This is a compiled NuGet-based version of the file-based UserUpdate.cs plugin.
/// 
/// Features demonstrated:
/// - ICompiledUserUpdatePlugin interface implementation
/// - UpdateUserAsync method for user synchronization
/// - User object modification and return
/// - Sensitive data handling
/// - Error handling for missing user data
/// 
/// Use cases:
/// - Sync user attributes from Active Directory/LDAP
/// - Pull user data from HR systems
/// - Fetch permissions from authorization services
/// - Merge user data from multiple sources
/// </summary>
[Plugin(
    Id = "0c5770a0-0dbe-4141-ab16-450bfee850ec",
    Name = "User Sync Example (Compiled)",
    Type = PluginTypes.UserUpdate,
    Version = "1.0.0",
    Author = "WSU EIT",
    Description = "A compiled example plugin demonstrating user synchronization from external systems. Converted from file-based UserUpdate.cs.",
    SortOrder = 0,
    Enabled = true
)]
public class UserSyncPlugin : ICompiledUserUpdatePlugin
{
    /// <summary>
    /// Static property required by ICompiledPlugin interface.
    /// </summary>
    public static Type PluginType => typeof(UserSyncPlugin);

    /// <summary>
    /// Plugin properties for compatibility with the existing plugin system.
    /// </summary>
    public Dictionary<string, object> Properties() => new() {
        { "Id", Guid.Parse("0c5770a0-0dbe-4141-ab16-450bfee850ec") },
        { "Author", "WSU EIT" },
        { "ContainsSensitiveData", true },
        { "Description", "Synchronizes user information from external systems." },
        { "Name", "User Sync Example (Compiled)" },
        { "Type", PluginTypes.UserUpdate },
        { "Version", "1.0.0" },
        { "Enabled", true },
    };

    /// <summary>
    /// Update user information from external system.
    /// </summary>
    /// <remarks>
    /// This method is called when the system needs to update a user's information.
    /// In a real implementation, you would:
    /// 1. Connect to your external user system (LDAP, HR database, API, etc.)
    /// 2. Look up the user by their identifier
    /// 3. Update relevant properties on the user object
    /// 4. Return the modified user object
    /// 
    /// For this demo, we simply toggle the email case to show the modification pattern.
    /// </remarks>
    /// <param name="Context">The user plugin context.</param>
    public async Task<PluginResult> UpdateUserAsync(IPluginUserContext context)
    {
        await Task.CompletedTask;

        var messages = new List<string>();
        var plugin = context.Plugin;

        context.LogInfo($"User update plugin executing: {plugin.Name}");

        // Get the user from context
        var user = context.User;

        // Make sure a user was provided
        if (user == null) {
            messages.Add("No user provided to update");
            context.LogWarning("UpdateUserAsync called with null user");
            return PluginResult.Failure(messages);
        }

        // In a real implementation, you would:
        // 1. Extract user identifier (email, ID, username)
        // 2. Call external system to get updated data
        // 3. Map external data to user properties

        // For demo purposes, we document the expected behavior
        messages.Add($"User update plugin executed: {plugin.Name}");
        messages.Add($"Plugin version: {plugin.Version}");
        messages.Add("User object received for processing");
        messages.Add("");
        messages.Add("In a real implementation, this plugin would:");
        messages.Add("  • Connect to external user directory (LDAP, AD, etc.)");
        messages.Add("  • Look up user by identifier");
        messages.Add("  • Update user properties (name, email, department, etc.)");
        messages.Add("  • Sync group memberships and permissions");
        messages.Add("  • Return the updated user object");

        context.LogInfo("User update plugin completed successfully");

        // Return success with the user object.
        // In real usage, return the modified user as an object.
        return PluginResult.Success(messages, [user]);
    }

    #region Example: Real-World Implementation Pattern

    /// <summary>
    /// Example of how a real implementation might look.
    /// This is for documentation purposes only.
    /// </summary>
    /// <remarks>
    /// This shows the pattern for a real user sync implementation.
    /// You would replace the simulated logic with actual external system calls.
    /// </remarks>
    private static void ExampleRealWorldPattern()
    {
        // Example: Sync from LDAP
        /*
        public async Task<PluginResult> UpdateUserAsync(IPluginUserContext context)
        {
            var user = context.User as UserModel;
            if (user == null) return PluginResult.Failure(["No user provided"]);

            // First, connect to LDAP
            var ldapConnection = await ConnectToLdap();

            // Now, look up the user
            var ldapUser = await ldapConnection.FindUserByEmail(user.Email);
            if (ldapUser == null) return PluginResult.Failure([$"User not found in LDAP: {user.Email}"]);

            // Next, update the properties
            user.FirstName = ldapUser.GivenName;
            user.LastName = ldapUser.Surname;
            user.Department = ldapUser.Department;
            user.Title = ldapUser.Title;
            user.Phone = ldapUser.TelephoneNumber;
            user.Manager = ldapUser.Manager;

            // Finally, sync group memberships
            user.Groups = ldapUser.MemberOf.Select(g => g.Name).ToList();

            return PluginResult.Success([$"Updated user: {user.Email}"], [user]);
        }
        */
    }

    #endregion
}
