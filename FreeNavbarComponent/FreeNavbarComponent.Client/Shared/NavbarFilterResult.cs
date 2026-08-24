namespace FreeNavbarComponent.Client.Shared;

/// <summary>
/// The outcome of a <see cref="NavbarMenu"/> search, passed to its OnFilter handler.
/// </summary>
public class NavbarFilterResult
{
    /// <summary>
    /// How many leaf items match the query. Equals the total leaf count when the
    /// query is empty.
    /// </summary>
    public int Matches { get; set; }

    /// <summary>
    /// The text currently in the search box. Empty when the search was cleared.
    /// </summary>
    public string Query { get; set; } = String.Empty;
}
