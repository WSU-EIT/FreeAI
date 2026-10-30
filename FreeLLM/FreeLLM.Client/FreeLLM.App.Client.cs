using System.Net.Http.Json;
using System.Text.RegularExpressions;

namespace FreeLLM.Client;

// ---- PromptPackager state and models ----
public class FreeLLMFileItem
{
    public int? CharCount { get; set; }
    public string FileName { get; set; }
    public string FullPath { get; set; }
    public bool IsSelected { get; set; }
    public int? LineCount { get; set; }
}

public class FreeLLMModificationOption
{
    public string Category { get; set; }
    public string Description { get; set; }
    public string Key { get; set; }
}

/// <summary>
/// Page state and server calls for the FreeLLM prompt packager. The FreeLLM_App_UI_PromptPackager* components share one instance.
/// </summary>
public class FreeLLMPromptPackagerState : IDisposable
{
    private readonly HttpClient Http;

    public FreeLLMPromptPackagerState(HttpClient http)
    {
        Http = http;
    }

    /// <summary>Raised when the page needs to render again (the owner calls StateHasChanged).</summary>
    public Action? OnChanged { get; set; }

    /// <summary>Runs work on the component's render context (the owner passes InvokeAsync).</summary>
    public Func<Func<Task>, Task>? Dispatch { get; set; }

    public void Changed()
    {
        OnChanged?.Invoke();
    }

    public void Dispose()
    {
        debounceTimer?.Dispose();
    }

    #region Parameters & Basic Page State
    private string _modificationInstructions = "";
    public string ModificationInstructions {
        get => _modificationInstructions;
        set {
            if(_modificationInstructions != value) {
                _modificationInstructions = value;
                UpdateSummaryDebounced();
            }
        }
    }

    public void ToggleFileType(string type, object value)
    {
        // Cast the value to bool to determine if the checkbox is checked.
        bool isChecked = (bool)value;
        if(isChecked)
            selectedFileTypes.Add(type.ToLower());
        else
            selectedFileTypes.Remove(type.ToLower());
        UpdateSummaryDebounced();
    }

    public void RemoveIgnoredFolder(string folder)
    {
        ignoredFolders.Remove(folder);
        UpdateSummaryDebounced();
        Changed();
    }

    public void AddIgnoredFolder()
    {
        if(!string.IsNullOrWhiteSpace(customIgnoredFolder)) {
            var folder = customIgnoredFolder.Trim().ToLower();
            if(!ignoredFolders.Contains(folder))
                ignoredFolders.Add(folder);
            customIgnoredFolder = "";
            UpdateSummaryDebounced();
            Changed();
        }
    }

    public void AddCustomFileType()
    {
        if(!string.IsNullOrWhiteSpace(customFileType)) {
            var type = customFileType.Trim().ToLower();
            if(!fileTypes.Contains(type))
                fileTypes.Add(type);
            selectedFileTypes.Add(type);
            customFileType = "";
            UpdateSummaryDebounced();
            Changed();
        }
    }


    // Local Files state
    private string _directoryPath = @"C:\\Repos\\FileGpt";
    public string DirectoryPath {
        get => _directoryPath;
        set {
            if(_directoryPath != value) {
                _directoryPath = value;
                _ = LoadFiles();
            }
        }
    }
    #endregion

    #region File Items, Modification Options & Toggles
    public List<FreeLLMFileItem> fileItems = new();
    public string filterText { get; set; } = "";
    public string plainTextFilter { get; set; } = "";
    public List<string> fileContentsSummaryChunks = new();

    // Default before/after texts are now editable via inputs in the hidden modification section.
    public string textBefore { get; set; } = Helpers.FreeLLMDefaultTextBefore;

    public string textAfter { get; set; } = Helpers.FreeLLMDefaultTextAfter;


    public List<string> fileTypes = new() { ".cs", ".razor", ".cshtml", ".md", ".js", ".css", ".ts", ".json", ".html", ".xml", ".scss" };
    public HashSet<string> selectedFileTypes = new() { ".cs", ".razor", ".cshtml", ".md" };

    public List<string> ignoredFolders = new() { "fontawesome", "bootstrap" };
    public string customFileType { get; set; } = "";
    public string customIgnoredFolder { get; set; } = "";

    public bool isUpdating = false;
    public int overallLineCount = 0;
    public int overallCharCount = 0;
    public Dictionary<string, string> fileSizeIndicators = new();

    private string _additionalRequest = "";
    public string AdditionalRequest {
        get => _additionalRequest;
        set {
            if(_additionalRequest != value) {
                _additionalRequest = value;
                UpdateSummaryDebounced();
            }
        }
    }

    public List<FreeLLMModificationOption> ModificationOptions = new();
    public List<string> SelectedModificationOptionKeys = new();
    public bool showCommonPhrases = false;
    public void ToggleCommonPhrases()
    {
        showCommonPhrases = !showCommonPhrases;
        Changed();
    }
    public void ToggleModificationOption(string key)
    {
        if(SelectedModificationOptionKeys.Contains(key))
            SelectedModificationOptionKeys.Remove(key);
        else
            SelectedModificationOptionKeys.Add(key);

        var phrase = ModificationOptions.Where(opt => SelectedModificationOptionKeys.Contains(opt.Key))
                                          .Select(opt => $"{opt.Key}: {opt.Description}");
        additionalSummary = phrase.Any() ? string.Join("\n", phrase) : "";
        UpdateSummaryDebounced();
        Changed();
    }
    public string additionalSummary = "";
    public string SelectedToolAction { get; set; } = "summarize";

    private bool _showFullFileList = false;
    public bool ShowFullFileList {
        get => _showFullFileList;
        set {
            if(_showFullFileList != value) {
                _showFullFileList = value;
                UpdateSummaryDebounced();
            }
        }
    }
    #endregion

    #region ChatGPT Model & Sorting Options

    private int _selectedNumberOfChunks = 1;
    public int SelectedNumberOfChunks {
        get => _selectedNumberOfChunks;
        set {
            if(_selectedNumberOfChunks != value) {
                _selectedNumberOfChunks = value;
                UpdateSummaryDebounced();
            }
        }
    }

    private string _sortOption = "Name";
    public string SortOption {
        get => _sortOption;
        set {
            if(_sortOption != value) {
                _sortOption = value;
                UpdateSummaryDebounced();
            }
        }
    }
    private string _sortDirection = "asc";
    public string SortDirection {
        get => _sortDirection;
        set {
            if(_sortDirection != value) {
                _sortDirection = value;
                UpdateSummaryDebounced();
            }
        }
    }
    #endregion

    #region Update & Load Methods
    private System.Threading.Timer? debounceTimer;
    public void UpdateSummaryDebounced()
    {
        debounceTimer?.Dispose();
        debounceTimer = new System.Threading.Timer(async _ => {
            if(Dispatch != null)
                await Dispatch(UpdateSummary);
        }, null, 300, System.Threading.Timeout.Infinite);
    }

    public async Task UpdateSummary()
    {
        isUpdating = true;
        Changed();
        await GetSelectedFileContents();
        overallLineCount = Helpers.LinesInString(string.Join("\n", fileContentsSummaryChunks));
        overallCharCount = string.Join("\n", fileContentsSummaryChunks).Length;
        isUpdating = false;
        Changed();
    }

    public async Task LoadFiles()
    {
        try {
            var response = await Http.PostAsJsonAsync($"/{DataObjects.Endpoints.FreeLLM.GetFiles}", new { Path = DirectoryPath });
            if(response.IsSuccessStatusCode) {
                fileItems = await response.Content.ReadFromJsonAsync<List<FreeLLMFileItem>>() ?? new();
                await UpdateSummary();
            }
        } catch(Exception ex) {
            // Optionally log error.
        }
    }

    public async Task GetSelectedFileContents()
    {
        var selectedFiles = fileItems.Where(f => f.IsSelected).ToList();
        var unselectedFiles = fileItems.Where(f => !f.IsSelected).Select(f => f.FullPath).ToList();

        var summary = new System.Text.StringBuilder();
        summary.AppendLine("=== Selected Files ===");
        if(!selectedFiles.Any()) {
            summary.AppendLine("No files selected.");
        } else {
            foreach(var file in selectedFiles) {
                summary.AppendLine($"- {file.FullPath}");
            }
            summary.AppendLine();
        }

        summary.AppendLine("=== Modification Instructions (Top) ===");
        summary.AppendLine(ModificationInstructions);
        summary.AppendLine();
        summary.AppendLine(textBefore);
        summary.AppendLine();

        fileSizeIndicators.Clear();

        // Retrieve metadata for unselected files.
        if(unselectedFiles.Any()) {
            try {
                var metaResponseUnselected = await Http.PostAsJsonAsync($"/{DataObjects.Endpoints.FreeLLM.GetFileMetadata}", new { FilePaths = unselectedFiles });
                if(metaResponseUnselected.IsSuccessStatusCode) {
                    var metaItemsUnselected = await metaResponseUnselected.Content.ReadFromJsonAsync<List<DataObjects.FreeLLMFileMetadataItem>>();
                    if(metaItemsUnselected != null) {
                        foreach(var meta in metaItemsUnselected) {
                            var file = FilteredFiles.FirstOrDefault(f => f.FullPath == meta.FullPath);
                            if(file != null) {
                                file.LineCount = meta.LineCount;
                                file.CharCount = meta.CharCount;
                            }
                            fileSizeIndicators[meta.FullPath] = $"{meta.LineCount}L/{meta.CharCount}C";
                        }
                    }
                }
            } catch(Exception ex) {
                // Optionally log error.
            }
        }

        // Retrieve full contents for selected files.
        if(selectedFiles.Any()) {
            try {
                var contentResponse = await Http.PostAsJsonAsync($"/{DataObjects.Endpoints.FreeLLM.GetFileContents}", new { FilePaths = selectedFiles.Select(f => f.FullPath).ToList() });
                if(contentResponse.IsSuccessStatusCode) {
                    var fileContents = await contentResponse.Content.ReadFromJsonAsync<List<DataObjects.FreeLLMFileContentItem>>();
                    if(fileContents != null) {
                        foreach(var file in fileContents) {
                            int lineCount = file.Content.Split('\n').Length;
                            int charCount = file.Content.Length;
                            var item = selectedFiles.FirstOrDefault(f => f.FullPath == file.FullPath);
                            if(item != null) {
                                item.LineCount = lineCount;
                                item.CharCount = charCount;
                            }
                            fileSizeIndicators[file.FullPath] = $"{lineCount}L/{charCount}C";
                            summary.AppendLine($"----- {file.FullPath} [ {lineCount}L/{charCount}C ] -----");
                            summary.AppendLine("\"\"\"\"");
                            summary.AppendLine(file.Content);
                            summary.AppendLine("\"\"\"\"");
                            summary.AppendLine();
                        }
                    }
                } else {
                    summary.AppendLine("Error retrieving file contents.");
                }
            } catch(Exception ex) {
                summary.AppendLine("Error retrieving file contents: " + ex.Message);
            }
        }

        summary.AppendLine(textAfter);
        if(!string.IsNullOrWhiteSpace(AdditionalRequest)) {
            summary.AppendLine();
            summary.AppendLine("=== Bottom Modification Instructions ===");
            summary.AppendLine(AdditionalRequest);
        }
        if(!string.IsNullOrWhiteSpace(additionalSummary)) {
            summary.AppendLine();
            summary.AppendLine("=== Common Modification Phrases ===");
            summary.AppendLine(additionalSummary);
        }

        fileContentsSummaryChunks = Helpers.FreeLLMSplitIntoChunks(summary.ToString(), SelectedNumberOfChunks);
        Changed();
    }
    #endregion

    #region File Selection & Filtering
    public void SelectAllVisible()
    {
        foreach(var item in FilteredFiles) {
            item.IsSelected = true;
        }
        UpdateSummaryDebounced();
        Changed();
    }

    public void DeselectAllVisible()
    {
        foreach(var item in FilteredFiles) {
            item.IsSelected = false;
        }
        UpdateSummaryDebounced();
        Changed();
    }

    public void ClearAllSelections()
    {
        foreach(var item in fileItems) {
            item.IsSelected = false;
        }
        UpdateSummaryDebounced();
        Changed();
    }

    public bool showFilters = false;
    public void ToggleFilters()
    {
        showFilters = !showFilters;
        Changed();
    }

    public async Task CheckDefaults()
    {
        List<string> stringList = Helpers.FreeLLMDefaultFileSuffixes();
        var listCopy = fileItems.ToList();
        // now we need to select any file that matches the name
        foreach(FreeLLMFileItem item in listCopy) {
            if(stringList.Any(o => (string.Empty + item.FullPath).Trim().ToLower().EndsWith(((string.Empty + o).ToLower().Trim())))) {
                item.IsSelected = true;
            }
        }
        fileItems = listCopy.ToList();
        await UpdateSummary();
        Changed();
    }

    // FilteredFiles based on fileItems
    public IEnumerable<FreeLLMFileItem> FilteredFiles {
        get {
            if(fileItems == null)
                return Enumerable.Empty<FreeLLMFileItem>();
            IEnumerable<FreeLLMFileItem> filtered = fileItems;
            if(selectedFileTypes.Any()) {
                filtered = filtered.Where(item => {
                    string ext = System.IO.Path.GetExtension(item.FullPath).ToLower();
                    return selectedFileTypes.Contains(ext);
                });
            }
            if(!string.IsNullOrWhiteSpace(filterText))
                filtered = filtered.Where(item => Helpers.FreeLLMIsWildcardMatch(item.FullPath, filterText));
            if(!string.IsNullOrWhiteSpace(plainTextFilter))
                filtered = filtered.Where(item => item.FullPath.IndexOf(plainTextFilter, StringComparison.OrdinalIgnoreCase) >= 0);
            filtered = filtered.Where(item => !ignoredFolders.Any(ignored => item.FullPath.IndexOf(ignored, StringComparison.OrdinalIgnoreCase) >= 0));

            if(SortOption == "LineCount") {
                filtered = SortDirection == "asc" ?
                           filtered.OrderBy(item => item.LineCount ?? int.MaxValue) :
                           filtered.OrderByDescending(item => item.LineCount ?? int.MaxValue);
            } else if(SortOption == "CharCount") {
                filtered = SortDirection == "asc" ?
                           filtered.OrderBy(item => item.CharCount ?? int.MaxValue) :
                           filtered.OrderByDescending(item => item.CharCount ?? int.MaxValue);
            } else {
                filtered = SortDirection == "asc" ?
                           filtered.OrderBy(item => item.FileName) :
                           filtered.OrderByDescending(item => item.FileName);
            }

            return filtered;
        }
    }
    #endregion
}

// ---- Helpers ----
// Pure helpers for the FreeLLM prompt packager.

public static partial class Helpers
{
    public const string FreeLLMDefaultTextBefore = @"=== Instruction Summary ===
Please review the contents of the files below carefully. Use this code as the foundation for solving the proposed problem.

IMPORTANT: Do not skip or renumber any of the steps. Make sure to call them out as you do them.

1. **Explain our previous chats:** If this is not our first chat, please summarize the previous discussions and decisions made in at most two sentences and three bullet points. Otherwise, acknowledge that this is the first chat and say you are ready and understand the task.
2. **Explain the Problem:** Begin by summarizing the problem as you understand it from our Modification Instructions (Top), or from the Optional Modification Instructions (Bottom). This should be a brief summary of the problem and the desired outcome.
3. **Analyze the Code:** Thoroughly review the code to understand its current functionality and structure.
4. **Study:** List all the software development topics being presented in the existing code and become an expert on them. Identify the languages, patterns, or techniques shown. In the output, make a table with a list of the types, categories, descriptions, and a link to their wiki pages.
5. **Standards:** Stick to industry standards and best practices as offered by Microsoft or others as they apply when making suggestions.
6. **Propose Solutions:** List three potential solutions in order from best to worst, including detailed pseudocode and explanations for each.
7. **Implement the Fix:** Apply the chosen solution by updating the code. Return the entire file where changes were made, including all comments, code blocks, and formatting.
8. **Explain fix and future improvements:** Explain why we did what we did, then follow it up with 1 to 3 paragraphs of text explaining potential further improvements with 3 to 5 bullet points reinforcing.
When returning modifications, ensure that only the files which have been modified are provided—unaltered files should be omitted. This will make it easier to integrate changes using a git diff tool. Thank you!
";

    public const string FreeLLMDefaultTextAfter = @"=== Instruction After Code ===
Ensure that all modifications preserve the complete file’s functionality and structure.
Ensure you do not include sections like '// For brevity, not every method is re-implemented.' as part of your changes. Always actually implement the changes or include the complete code that should be there.
Return only the fully updated files that have been modified; do not include files that remain unchanged.
Every detail is critical and must be maintained without omissions or placeholder content, unless explicitly modified per the request.

If you make any assumptions or need clarification, please list them in a section at the bottom of the output and we can address them after your best effort has been made.
I believe in you and do your best!
I look forward to working with you to accomplish our goals on this project.

==== Optional Modification Instructions (Bottom) ====
";

    // Files that "Check Defaults" selects (matched against the end of the full path).
    public static List<string> FreeLLMDefaultFileSuffixes()
    {
        return ["Readme.md", "Program.cs", "App.razor", "MainLayout.razor", "\\DataAccess.cs", "\\DataObjects.cs", "Controllers\\DataController.cs", "\\DataModel.cs", "\\EFDataModel.cs"];
    }

    public static string FreeLLMPreviewColor(int overallLineCount)
    {
        int limit = 7000;
        double ratio = overallLineCount / (double)limit;

        if(ratio < 0.5)
            return "var(--bs-success-bg-subtle)";   // good
        else if(ratio < 0.7)
            return "var(--bs-info-bg-subtle)";      // info
        else if(ratio < 0.9)
            return "var(--bs-warning-bg-subtle)";   // warn
        else
            return "var(--bs-danger-bg-subtle)";    // error
    }

    public static string FreeLLMFileBadgeColor(int? fileLineCount)
    {
        // hardcoded thresholds preserved
        double lineCount = (fileLineCount ?? 0);

        if(lineCount < 1000)
            return "var(--bs-success-bg-subtle)";   // good
        else if(lineCount < 2000)
            return "var(--bs-info-bg-subtle)";      // info
        else if(lineCount < 3000)
            return "var(--bs-warning-bg-subtle)";   // warn
        else
            return "var(--bs-danger-bg-subtle)";    // error
    }

    public static bool FreeLLMIsWildcardMatch(string text, string pattern)
    {
        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";
        return Regex.IsMatch(text, regexPattern, RegexOptions.IgnoreCase);
    }

    #region Splitting Summary into Chunks
    public static List<string> FreeLLMSplitIntoChunks(string summary, int selectedNumberOfChunks)
    {
        int chunkCount = Math.Max(1, selectedNumberOfChunks);

        // 1) Fast path: no splitting requested
        if(chunkCount <= 1)
        {
            return new List<string> { summary };
        }

        // 2) Split into lines
        var lines = summary.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).ToList();
        int totalLines = lines.Count;

        // 3) Partition into "blocks" separated by lines that start with "----- "
        //    (this preserves the preamble as block[0])
        var blocks = new List<List<string>>();
        var currentBlock = new List<string>();
        foreach(var line in lines) {
            if(line.StartsWith("----- ")) {
                if(currentBlock.Count > 0) {
                    blocks.Add(currentBlock);
                    currentBlock = new List<string>();
                }
            }
            currentBlock.Add(line);
        }
        if(currentBlock.Count > 0)
            blocks.Add(currentBlock);

        // If we somehow failed to detect any blocks, just return the whole thing.
        if(blocks.Count == 0)
            return new List<string> { summary };

        // If there are fewer blocks than requested chunks, we’ll return one chunk per block.
        // (If you want to split *inside* blocks to always reach chunkCount, we can add that later.)
        int targetChunkCount = Math.Min(chunkCount, blocks.Count);

        // 4) Greedy pack blocks into ~equal line-count chunks
        int targetLinesPerChunk = (int)Math.Ceiling(totalLines / (double)targetChunkCount);

        var chunks = new List<List<string>>();
        var chunk = new List<string>();
        int chunkLineCount = 0;

        for(int i = 0; i < blocks.Count; i++) {
            var block = blocks[i];
            int remainingBlocks = blocks.Count - i;                 // including this one
            int remainingChunks = targetChunkCount - chunks.Count;  // including the current one

            // Ensure we leave at least one block for each remaining chunk.
            bool mustBreakHere = (remainingBlocks - 1) >= (remainingChunks - 1)    // enough blocks left
                                 && chunk.Count > 0
                                 && (chunkLineCount + block.Count) > targetLinesPerChunk;

            // Also force a break if keeping this block would starve remaining chunks of at least 1 block each
            bool needToBreakToReserve = chunk.Count > 0
                                        && (remainingBlocks - 1) == (remainingChunks - 1)
                                        && (chunkLineCount > 0);

            if((mustBreakHere || needToBreakToReserve) && chunks.Count < targetChunkCount - 1) {
                chunks.Add(chunk);
                chunk = new List<string>();
                chunkLineCount = 0;
            }

            chunk.AddRange(block);
            chunkLineCount += block.Count;
        }

        if(chunk.Count > 0)
            chunks.Add(chunk);

        // It’s possible (e.g., tiny input) we ended with fewer than targetChunkCount chunks.
        // That’s fine—we won’t split blocks further in this version.

        // 5) Materialize chunks with headers
        var result = new List<string>(chunks.Count);
        for(int i = 0; i < chunks.Count; i++) {
            string header =
                $"=== Chunk {i + 1} of {chunks.Count} ===\nPlease wait until all chunks are received before responding.\n";
            result.Add(header + "\n" + string.Join("\n", chunks[i]));
        }
        return result;
    }

    #endregion
}
