namespace FreeExamples;

public partial class DataObjects
{
    /// <summary>
    /// Represents a single entry (file or folder) in a git repository tree.
    /// </summary>
    public class GitRepoEntry
    {
        public string Extension { get; set; } = "";
        public bool IsDirectory { get; set; }
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
        public long Size { get; set; }
    }

    /// <summary>
    /// Represents the contents of a file in a git repository.
    /// </summary>
    public class GitFileContent
    {
        public string Content { get; set; } = "";
        public string Extension { get; set; } = "";
        public bool IsBinary { get; set; }
        public string Name { get; set; } = "";
        public string Path { get; set; } = "";
        public long Size { get; set; }
    }

    /// <summary>
    /// Request DTO for browsing a git repository.
    /// </summary>
    public class GitBrowseRequest
    {
        public string? Path { get; set; }
        public string? RepoUrl { get; set; }
    }

    /// <summary>
    /// Request DTO for reading a file from a git repository.
    /// </summary>
    public class GitFileRequest
    {
        public string? FilePath { get; set; }
        public string? RepoUrl { get; set; }
    }
}
