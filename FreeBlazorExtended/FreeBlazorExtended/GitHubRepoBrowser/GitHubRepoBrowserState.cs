/*
    Purpose: Holds all mutable UI state for a GitHubRepoBrowser component instance.
    Fits: Created as a private field inside the component (not DI-injected).
*/
namespace FreeBlazorExtended.GitHubRepoBrowser;

public class GitHubRepoBrowserState
{
    // ── Credential / navigation inputs ────────────────────────────────────────
    public string Owner  { get; set; } = string.Empty;
    public string Repo   { get; set; } = string.Empty;
    public string Pat    { get; set; } = string.Empty;
    public string Branch { get; set; } = string.Empty;

    // ── Loaded data ────────────────────────────────────────────────────────────
    public GitHubRepo?                  CurrentRepo { get; set; }
    public GitHubTreeResponse?          Tree        { get; set; }
    public IReadOnlyList<GitHubBranch>  Branches    { get; set; } = Array.Empty<GitHubBranch>();

    // ── Tree UI state ──────────────────────────────────────────────────────────
    public Dictionary<string, bool> ExpandedPaths { get; } = new(StringComparer.Ordinal);

    // ── Selected file ──────────────────────────────────────────────────────────
    public GitHubTreeItem?    SelectedItem    { get; set; }
    public GitHubFileContent? LoadedFile      { get; set; }
    public string?            DecodedContent  { get; set; }

    // ── Loading flags ──────────────────────────────────────────────────────────
    public bool IsLoadingTree { get; set; }
    public bool IsLoadingFile { get; set; }

    // ── Error ──────────────────────────────────────────────────────────────────
    public GitHubBrowseError? LastError { get; set; }

    // ── Helpers ────────────────────────────────────────────────────────────────

    /// <summary>Zeros the PAT (call on component dispose to avoid PAT lingering in memory).</summary>
    public void ClearPat() => Pat = string.Empty;

    /// <summary>
    /// Clears tree, file viewer, and error state so a fresh browse can begin,
    /// while preserving Owner / Repo / Pat / Branch and the loaded branch list.
    /// </summary>
    public void Reset()
    {
        Tree           = null;
        SelectedItem   = null;
        LoadedFile     = null;
        DecodedContent = null;
        LastError      = null;
        ExpandedPaths.Clear();
    }
}
