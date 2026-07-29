namespace FreeExamples;

public partial class DataObjects
{
    /// <summary>
    /// A saved code snippet for the Code Playground demo.
    /// Stored in-memory via CodeSnippetService.
    /// </summary>
    public class CodeSnippet
    {
        public string Content { get; set; } = "";
        public string Language { get; set; } = "plaintext";
        public DateTime LastSaved { get; set; }
        public Guid SnippetId { get; set; }
        public string Title { get; set; } = "";
    }

    /// <summary>
    /// Request DTO for the API Notebook "execute" feature.
    /// Sends editor content to a selected endpoint and returns the response.
    /// </summary>
    public class CodePlaygroundRequest
    {
        public string Body { get; set; } = "";
        public string Endpoint { get; set; } = "";
    }

    /// <summary>
    /// Response DTO wrapping whatever the target endpoint returns.
    /// </summary>
    public class CodePlaygroundResponse
    {
        public long DurationMs { get; set; }
        public string ResponseBody { get; set; } = "";
        public int StatusCode { get; set; }
        public bool Success { get; set; }
    }
}
