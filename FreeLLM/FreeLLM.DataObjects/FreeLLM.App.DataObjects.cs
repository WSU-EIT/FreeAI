namespace FreeLLM;

// ---- File DTOs ----
// FreeLLM data objects for the prompt packager file endpoints.

public partial class DataObjects
{
    public class FreeLLMFileContentItem
    {
        public string Content { get; set; }
        public string FileName { get; set; }
        public string FullPath { get; set; }
    }

    // Existing FileItem used in API responses.
    public class FreeLLMFileItem
    {
        public string FileName { get; set; }
        public string FullPath { get; set; }
    }

    // NEW: Metadata for files
    public class FreeLLMFileMetadataItem
    {
        public int CharCount { get; set; }
        public string FileName { get; set; }
        public string FullPath { get; set; }
        public int LineCount { get; set; }
    }


    public record FreeLLMFilePathRequest(string Path);
    public record FreeLLMFileContentRequest(List<string> FilePaths);
}

// ---- Endpoints ----
// FreeLLM endpoint constants.

public partial class DataObjects
{
    public static partial class Endpoints
    {
        public static class FreeLLM
        {
            public const string GetFileContents = "api/Data/GetFileContents";
            public const string GetFileMetadata = "api/Data/GetFileMetadata";
            public const string GetFiles = "api/Data/GetFiles";
        }
    }
}
