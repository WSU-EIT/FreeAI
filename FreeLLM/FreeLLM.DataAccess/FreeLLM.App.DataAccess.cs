namespace FreeLLM;

// File enumeration and reading for the FreeLLM prompt packager.

public partial interface IDataAccess
{
    List<DataObjects.FreeLLMFileItem>? FreeLLMGetFiles(string path);
    Task<List<DataObjects.FreeLLMFileContentItem>> FreeLLMGetFileContents(List<string> filePaths);
    Task<List<DataObjects.FreeLLMFileMetadataItem>> FreeLLMGetFileMetadata(List<string> filePaths);
}

public partial class DataAccess
{
    // Reads a file through the 5 minute cache.
    private static async Task<string> FreeLLMReadFileCached(string path)
    {
        // Create a composite key using a fixed identifier and the file path.
        string cacheKey = $"FileContent_{path}";
        var content = CacheStore.GetCachedItem<string>(Guid.Empty, cacheKey);
        if (content == null) {
            content = await System.IO.File.ReadAllTextAsync(path);
            CacheStore.SetCacheItem(Guid.Empty, cacheKey, content, DateTimeOffset.Now.AddMinutes(5));
        }
        return content;
    }

    // Retrieves full file contents for a given list of files.
    public async Task<List<DataObjects.FreeLLMFileContentItem>> FreeLLMGetFileContents(List<string> filePaths)
    {
        var results = new List<DataObjects.FreeLLMFileContentItem>();

        foreach (var path in filePaths) {
            if (System.IO.File.Exists(path)) {
                var content = await FreeLLMReadFileCached(path);
                results.Add(new DataObjects.FreeLLMFileContentItem {
                    FileName = Path.GetFileName(path),
                    FullPath = path,
                    Content = content
                });
            }
        }

        return results;
    }

    // Retrieves only metadata (line count and character count) for a given list of files.
    public async Task<List<DataObjects.FreeLLMFileMetadataItem>> FreeLLMGetFileMetadata(List<string> filePaths)
    {
        var results = new List<DataObjects.FreeLLMFileMetadataItem>();

        foreach (var path in filePaths) {
            if (System.IO.File.Exists(path)) {
                var content = await FreeLLMReadFileCached(path);
                int lineCount = content.Split('\n').Length;
                int charCount = content.Length;
                results.Add(new DataObjects.FreeLLMFileMetadataItem {
                    FileName = Path.GetFileName(path),
                    FullPath = path,
                    LineCount = lineCount,
                    CharCount = charCount
                });
            }
        }
        return results;
    }

    // Retrieves a list of files (excluding bin/obj folders and hidden folders).
    // Returns null when the directory does not exist.
    public List<DataObjects.FreeLLMFileItem>? FreeLLMGetFiles(string path)
    {
        if (!Directory.Exists(path)) {
            return null;
        }

        var files = Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories)
            .Where(f => {
                var segments = f.Split(Path.DirectorySeparatorChar);
                bool inHiddenFolder = segments.Take(segments.Length - 1)
                    .Any(seg => seg.StartsWith("."));
                if (inHiddenFolder)
                    return false;
                bool binOrObj = segments.Any(seg =>
                    seg.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
                    seg.Equals("obj", StringComparison.OrdinalIgnoreCase));
                return !binOrObj;
            })
            .Select(f => new DataObjects.FreeLLMFileItem {
                FileName = Path.GetFileName(f),
                FullPath = f
            })
            .ToList();

        return files;
    }
}
