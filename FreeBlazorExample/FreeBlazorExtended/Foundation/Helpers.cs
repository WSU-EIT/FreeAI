/*
    Role: Core shared infrastructure.
    Purpose: Holds the small cross-cutting helper methods that multiple FreeBlazorExtended features rely on.
    Contains: Formatting and exception-flattening helpers that are intentionally generic and host-agnostic.
    Used by: Shared services and feature folders that need common utility behavior without pulling in a larger dependency.
*/
namespace FreeBlazorExtended;

public static class Helpers
{
    /// <summary>
    /// Returns a stable string identifier for the acting user, suitable for audit-trail
    /// fields such as <c>AddedBy</c> or <c>LastModifiedBy</c>.
    /// </summary>
    /// <param name="CurrentUser">
    /// The authenticated user context supplied by the host. Pass <c>null</c> or a user
    /// whose <see cref="DataObjects.User.UserId"/> is <see cref="Guid.Empty"/> to
    /// represent a system-initiated operation (returns the string <c>"System"</c>).
    /// </param>
    /// <returns>
    /// The user's <see cref="Guid"/> as a string, or <c>"System"</c> when no real user
    /// is present.
    /// </returns>
    public static string CurrentUserIdString(DataObjects.User? CurrentUser)
    {
        if (CurrentUser == null || CurrentUser.UserId == Guid.Empty) {
            return "System";
        }
        return CurrentUser.UserId.ToString();
    }

    /// <summary>
    /// Flattens an exception and all of its inner exceptions into an ordered list of
    /// human-readable message strings. Useful for surfacing the full error chain in
    /// a <see cref="DataObjects.BooleanResponse"/> without losing nested context.
    /// </summary>
    /// <param name="ex">The exception to recurse. Safe to call with <c>null</c> — returns an empty list.</param>
    /// <param name="ShowExceptionType">
    /// When <c>true</c> (default), each message is prefixed with the exception's
    /// CLR type (e.g. <c>"System.IO.IOException: ..."</c>). Set to <c>false</c>
    /// for cleaner end-user messages.
    /// </param>
    /// <returns>
    /// An ordered list of messages starting with the outermost exception and
    /// working inward through any <see cref="Exception.InnerException"/> chain.
    /// Returns an empty list when <paramref name="ex"/> is <c>null</c>.
    /// </returns>
    public static List<string> RecurseException(Exception ex, bool ShowExceptionType = true)
    {
        List<string> output = new List<string>();

        if (ex != null) {
            if (!String.IsNullOrWhiteSpace(ex.Message)) {
                if (ShowExceptionType) {
                    output.Add(ex.GetType().ToString() + ": " + ex.Message);
                } else {
                    output.Add(ex.Message);
                }
            }

            if (ex.InnerException != null) {
                var inner = RecurseException(ex.InnerException, ShowExceptionType);
                if (inner.Any()) {
                    foreach (var message in inner) {
                        output.Add(message);
                    }
                }
            }
        }

        return output;
    }
}
