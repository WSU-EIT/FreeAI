/*
    Role: Core shared data-contract file.
    Purpose: Defines lightweight DTOs and response envelopes reused across multiple FreeBlazorExtended features.
    Contains: A common action-response wrapper, a boolean response payload, and a minimal user identity model.
    Used by: Services and components that need a shared success or failure contract without depending on host-specific entities.
*/
namespace FreeBlazorExtended;

public partial class DataObjects
{
    /// <summary>
    /// Wraps a <see cref="BooleanResponse"/> for operations that follow the
    /// action → result pattern (e.g., save, delete, validate).
    /// </summary>
    public partial class ActionResponseObject
    {
        /// <summary>
        /// The success/failure result and any diagnostic messages produced by the action.
        /// Inspect <see cref="BooleanResponse.Result"/> to determine whether the action succeeded.
        /// </summary>
        public BooleanResponse ActionResponse { get; set; } = new BooleanResponse();
    }

    /// <summary>
    /// Lightweight success/failure envelope returned by service operations.
    /// Check <see cref="Result"/> first; read <see cref="Messages"/> for context
    /// when <see cref="Result"/> is <c>false</c>.
    /// </summary>
    public partial class BooleanResponse
    {
        /// <summary>
        /// Diagnostic messages explaining the outcome. Populated on failure with
        /// user-friendly or developer-oriented strings. May also contain warnings
        /// on a successful operation.
        /// </summary>
        public List<string> Messages { get; set; } = new List<string>();

        /// <summary>
        /// <c>true</c> when the operation succeeded; <c>false</c> when it did not.
        /// Always check this before reading <see cref="Messages"/>.
        /// </summary>
        public bool Result { get; set; }
    }

    /// <summary>
    /// Minimal user-identity stub used by service audit trails (<c>AddedBy</c>,
    /// <c>LastModifiedBy</c>) and permission checks throughout FreeBlazorExtended.
    /// Hosts populate this from their own authentication layer and pass it into
    /// service calls; the library never reads from HTTP context directly.
    /// </summary>
    public partial class User
    {
        /// <summary>Stable identifier for the user. <see cref="Guid.Empty"/> indicates an unauthenticated or system caller.</summary>
        public Guid UserId { get; set; }

        /// <summary>The tenant this user belongs to. Passed through to tenant-scoped service methods.</summary>
        public Guid TenantId { get; set; }

        /// <summary>User's given name — displayed in audit logs and UI greetings.</summary>
        public string FirstName { get; set; } = String.Empty;

        /// <summary>User's family name — combined with <see cref="FirstName"/> for display.</summary>
        public string LastName { get; set; } = String.Empty;

        /// <summary>Primary email address. Used for display; not validated by this library.</summary>
        public string Email { get; set; } = String.Empty;

        /// <summary>
        /// <c>true</c> when the user has administrative privileges in the host application.
        /// Feature services may use this flag to gate destructive operations.
        /// </summary>
        public bool Admin { get; set; }
    }
}
