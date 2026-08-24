namespace FreeNavbarComponent.Client.Shared;

/// <summary>
/// One entry in a <see cref="NavbarMenu"/> tree. An item with children renders as
/// an expandable branch; an item without them renders as a clickable link.
/// </summary>
/// <remarks>
/// Declared at namespace level, rather than nested inside the component, so pages
/// can write <c>new NavbarMenuItem { ... }</c> without a component prefix.
/// </remarks>
public class NavbarMenuItem
{
    public NavbarMenuItem() { }

    /// <summary>
    /// Creates a leaf item.
    /// </summary>
    public NavbarMenuItem(string text, string? url = null)
    {
        Text = text;
        Url = url;
    }

    /// <summary>
    /// Creates a branch item with the supplied children.
    /// </summary>
    public NavbarMenuItem(string text, params NavbarMenuItem[] children)
    {
        Text = text;
        Children = children.ToList();
    }

    /// <summary>
    /// Marks the item as the current page.
    /// </summary>
    public bool Active { get; set; }

    /// <summary>
    /// Child items. Any depth is supported.
    /// </summary>
    public List<NavbarMenuItem>? Children { get; set; }

    /// <summary>
    /// Renders the item greyed out and non-interactive.
    /// </summary>
    public bool Disabled { get; set; }

    /// <summary>
    /// An optional icon, either a Bootstrap Icons / Font Awesome class name
    /// (for example "bi bi-folder" or "fa-solid fa-folder") or raw markup.
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// Arbitrary value carried through to OnItemClicked, so the parent can identify
    /// an item without matching on its display text.
    /// </summary>
    public object? Tag { get; set; }

    /// <summary>
    /// Anchor target, for example "_blank".
    /// </summary>
    public string? Target { get; set; }

    /// <summary>
    /// The label shown in the menu.
    /// </summary>
    public string Text { get; set; } = String.Empty;

    /// <summary>
    /// Optional navigation URL. Items without one raise OnItemClicked only.
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// True when this item has children and therefore renders as a branch.
    /// </summary>
    public bool HasChildren {
        get { return Children != null && Children.Any(); }
    }

    /// <summary>
    /// Builds a leaf item. Shorthand for the constructor, for use in collection
    /// initialisers: <c>NavbarMenuItem.Link("Mesonet", "/mesonet")</c>.
    /// </summary>
    public static NavbarMenuItem Link(string text, string? url = null, object? tag = null)
    {
        return new NavbarMenuItem { Text = text, Url = url, Tag = tag };
    }

    /// <summary>
    /// Builds a branch item: <c>NavbarMenuItem.Branch("Water", child1, child2)</c>.
    /// </summary>
    public static NavbarMenuItem Branch(string text, params NavbarMenuItem[] children)
    {
        return new NavbarMenuItem { Text = text, Children = children.ToList() };
    }
}
