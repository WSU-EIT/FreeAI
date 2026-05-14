# 202 — Feature: ADA Scanning — Phases, Tasks, and Pseudo-Code

> **Document ID:** 202  
> **Category:** Feature  
> **Purpose:** Detailed implementation spec — phases, tasks, before/after architecture, pseudo-code  
> **Audience:** Implementer, AI agents  
> **Predicted Outcome:** Clear enough to build from without asking questions  
> **Actual Outcome:** ✅ Plan approved with consensus-ranking additions from doc 203  
> **Resolution:** Implementation in progress  

---

## Architecture: Before and After

### BEFORE — Current Data Flow

```
┌─────────────────────────────────────────────────────────────┐
│                    ScanPageAsync                             │
│                                                             │
│  ┌──────────┐   ┌──────────┐   ┌──────────┐   ┌─────────┐ │
│  │ Navigate │──▶│Screenshot│──▶│ Auth Flow│──▶│Save HTML│ │
│  │  + Wait  │   │  capture │   │(optional)│   │ to disk │ │
│  └──────────┘   └──────────┘   └──────────┘   └────┬────┘ │
│                                                     │      │
│  ┌──────────┐   ┌──────────┐   ┌──────────────────┐│      │
│  │ Download │──▶│  Write   │──▶│  Write report.md ││      │
│  │  Images  │   │  Logs    │   │  + metadata.json │◀┘      │
│  └──────────┘   └──────────┘   └──────────────────┘        │
└─────────────────────────────────────────────────────────────┘

Output per page:
  ├── 01-page-loaded.png
  ├── page.html                 ◀── saved but NOT analyzed
  ├── metadata.json
  ├── report.md
  ├── errors.log
  ├── warnings.log
  ├── info.log
  ├── actions.log
  └── images/
```

### AFTER — With ADA Scanning

```
┌─────────────────────────────────────────────────────────────────────┐
│                        ScanPageAsync                                │
│                                                                     │
│  ┌──────────┐   ┌──────────┐   ┌──────────┐   ┌─────────┐         │
│  │ Navigate │──▶│Screenshot│──▶│ Auth Flow│──▶│Save HTML│         │
│  │  + Wait  │   │  capture │   │(optional)│   │ to disk │         │
│  └──────────┘   └──────────┘   └──────────┘   └────┬────┘         │
│                                                     │              │
│  ┌──────────┐   ┌──────────┐                        │              │
│  │ Download │──▶│  Write   │                        │              │
│  │  Images  │   │  Logs    │                        │              │
│  └──────────┘   └──────────┘                        │              │
│                                                     │              │
│  ╔══════════════════════════════════════════════════╧════════════╗  │
│  ║                 NEW: A11y Scanning Block                      ║  │
│  ║                                                               ║  │
│  ║  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐        ║  │
│  ║  │  axe-core    │  │  htmlcheck   │  │   pa11y      │        ║  │
│  ║  │  (live page) │  │ (page.html)  │  │ (CLI / live) │        ║  │
│  ║  │              │  │              │  │              │        ║  │
│  ║  │ Inject JS    │  │ Parse HTML   │  │ Shell out    │        ║  │
│  ║  │ axe.run()    │  │ regex checks │  │ pa11y --json │        ║  │
│  ║  │ parse JSON   │  │ build issues │  │ parse JSON   │        ║  │
│  ║  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘        ║  │
│  ║         │                 │                  │                ║  │
│  ║         ▼                 ▼                  ▼                ║  │
│  ║  ┌──────────────────────────────────────────────────────┐     ║  │
│  ║  │           MergeA11yResults                           │     ║  │
│  ║  │                                                      │     ║  │
│  ║  │  Normalize all to A11yIssue[] ──▶ A11yPageSummary   │     ║  │
│  ║  └──────────────────────┬───────────────────────────────┘     ║  │
│  ╚═════════════════════════╪═════════════════════════════════════╝  │
│                            │                                       │
│                            ▼                                       │
│  ┌──────────────────────────────────────────────────────────┐      │
│  │  Write outputs:                                          │      │
│  │    a11y-axe.json        ← raw axe results                │      │
│  │    a11y-htmlcheck.json  ← raw htmlcheck results           │      │
│  │    a11y-pa11y.json      ← raw pa11y results (or skipped)  │      │
│  │    a11y-summary.json    ← merged cross-tool view          │      │
│  │    report.md            ← updated with ♿ section          │      │
│  │    metadata.json        ← updated with Accessibility{}    │      │
│  └──────────────────────────────────────────────────────────┘      │
└─────────────────────────────────────────────────────────────────────┘
```

### Report Hierarchy — Before vs After

```
BEFORE                                AFTER
======                                =====

Run report.md                         Run report.md
├── Dashboard (pass/fail)             ├── Dashboard (pass/fail)
├── Sites table                       ├── Sites table
├── Screenshot gallery                ├── Screenshot gallery
└── Failed pages                      ├── Failed pages
                                      └── ♿ Accessibility Dashboard  ◀── NEW
                                          ├── Severity totals by tool
                                          └── Top 20 violations

Site report.md                        Site report.md
├── Summary (pass/fail)               ├── Summary (pass/fail)
├── Pages table                       ├── Pages table
├── Screenshots                       ├── Screenshots
└── JS errors                         ├── JS errors
                                      └── ♿ Accessibility Summary    ◀── NEW
                                          ├── Severity by tool
                                          └── Top 10 issues

Page report.md                        Page report.md
├── Summary                           ├── Summary
├── Screenshots                       ├── Screenshots
├── Images + alt check                ├── Images + alt check
├── JS errors                         ├── JS errors
└── Files list                        ├── ♿ Accessibility             ◀── NEW
                                      │   ├── Cross-tool summary
                                      │   ├── Violations by rule
                                      │   └── Code snippets (expand)
                                      └── Files list
```

---

## Phase 1: Models + Config

### Task 1.1 — Add A11yIssue model

```
BEFORE: No accessibility models exist
AFTER:  Three new classes at bottom of Program.cs with existing models
```

**Pseudo-code:**

```csharp
// One unified issue — every tool normalizes to this
internal class A11yIssue
{
    Tool        // "axe" | "htmlcheck" | "pa11y"
    RuleId      // "image-alt", "heading-order", etc.
    Severity    // "critical" | "serious" | "moderate" | "minor"
    Message     // Human-readable: "Images must have alternate text"
    Selector    // CSS selector: "img.hero-banner"
    Snippet     // HTML: "<img src=\"banner.jpg\">"
    HelpUrl     // "https://dequeuniversity.com/rules/axe/..."
    WcagCriteria // "1.1.1" (optional)
}

// One tool's complete result for a page
internal class A11yToolResult
{
    ToolName    // "axe"
    Status      // "completed" | "skipped" | "error"
    DurationMs  // how long it took
    Issues      // List<A11yIssue>
    ErrorMessage // null or error details
}

// Merged view across all tools for one page
internal class A11yPageSummary
{
    ToolResults    // Dictionary<string, A11yToolResult>
    TotalViolations
    Critical, Serious, Moderate, Minor  // counts
}
```

### Task 1.2 — Add WcagLevel to ScannerConfig

```
BEFORE appsettings.json:              AFTER appsettings.json:
{                                     {
  "Scanner": {                          "Scanner": {
    "SettleDelayMs": 3000,                "SettleDelayMs": 3000,
    "TimeoutMs": 10000,                   "TimeoutMs": 10000,
    "MaxConcurrency": 5,                  "MaxConcurrency": 5,
    "Headless": true,                     "Headless": true,
    ...                                   "WcagLevel": "wcag21aa",    ◀── NEW
  }                                       ...
}                                       }
                                      }
```

### Task 1.3 — Add a11y fields to PageResult and SiteResult

```
BEFORE PageResult:                    AFTER PageResult:
  PagePath                              PagePath
  FullUrl                               FullUrl
  StatusCode                            StatusCode
  Images[]                              Images[]
  ConsoleErrors[]                       ConsoleErrors[]
  ...                                   ...
                                        A11ySummary  ◀── NEW (A11yPageSummary)
```

---

## Phase 2: axe-core Integration

### Task 2.1 — EnsureAxeCoreAsync (download + cache)

```
First run:
  axe.min.js not found in project dir
  → Download from https://cdn.jsdelivr.net/npm/axe-core/axe.min.js
  → Save to {projectDir}/axe.min.js (~400KB)
  → Log: "Downloaded axe-core vX.Y.Z (400KB)"

Subsequent runs:
  axe.min.js already exists
  → Log: "axe-core cached (400KB)"
  → Skip download
```

**Pseudo-code:**

```csharp
static async Task<string> EnsureAxeCoreAsync(string projectDir)
{
    path = Path.Combine(projectDir, "axe.min.js")

    if File.Exists(path):
        return File.ReadAllText(path)

    // Download from CDN
    using HttpClient http = new()
    content = await http.GetStringAsync(AXE_CDN_URL)
    await File.WriteAllTextAsync(path, content)
    Console.WriteLine($"Downloaded axe-core ({content.Length / 1024}KB)")

    return content
}
```

### Task 2.2 — RunAxeCoreAsync (the core scan)

```
Input:  Live Playwright IPage (already navigated + settled)
        WCAG level from config ("wcag21aa")
Output: A11yToolResult with normalized A11yIssue list

Flow:
  1. Inject axe.min.js into page via EvaluateAsync
  2. Run axe.run() with WCAG tags filter
  3. Parse violations[] from JSON result
  4. Normalize each violation + node to A11yIssue
  5. Return A11yToolResult
```

**Pseudo-code:**

```csharp
static async Task<A11yToolResult> RunAxeCoreAsync(IPage page, string axeJs, string wcagLevel)
{
    stopwatch.Start()

    // 1. Inject axe-core into the page
    await page.EvaluateAsync(axeJs)

    // 2. Run axe with WCAG filter
    //    axe.run() returns { violations: [...], passes: [...], ... }
    jsonResult = await page.EvaluateAsync<JsonElement>("""
        async () => {
            var results = await axe.run({
                runOnly: { type: 'tag', values: ['wcag21aa', 'wcag2aa', 'wcag2a'] }
            });
            return {
                violations: results.violations.map(v => ({
                    id: v.id,
                    impact: v.impact,
                    description: v.description,
                    help: v.help,
                    helpUrl: v.helpUrl,
                    tags: v.tags,
                    nodes: v.nodes.map(n => ({
                        html: n.html,
                        target: n.target,
                        failureSummary: n.failureSummary
                    }))
                }))
            };
        }
    """)

    // 3. Parse violations into A11yIssue list
    issues = new List<A11yIssue>()
    foreach violation in jsonResult.violations:
        foreach node in violation.nodes:
            issues.Add(new A11yIssue {
                Tool = "axe",
                RuleId = violation.id,           // "image-alt"
                Severity = violation.impact,      // "critical"
                Message = violation.help,          // "Images must have alt text"
                Selector = node.target[0],         // "img.hero"
                Snippet = node.html,               // "<img src=...>"
                HelpUrl = violation.helpUrl,
                WcagCriteria = ExtractWcag(violation.tags)  // "1.1.1"
            })

    stopwatch.Stop()

    return new A11yToolResult {
        ToolName = "axe",
        Status = "completed",
        DurationMs = stopwatch.ElapsedMilliseconds,
        Issues = issues
    }
}
```

**axe.run() result shape (what we parse):**

```json
{
    "violations": [
        {
            "id": "image-alt",
            "impact": "critical",
            "help": "Images must have alternate text",
            "helpUrl": "https://dequeuniversity.com/rules/axe/4.10/image-alt",
            "tags": ["wcag2a", "wcag111"],
            "nodes": [
                {
                    "html": "<img src=\"banner.jpg\" class=\"hero\">",
                    "target": ["img.hero"],
                    "failureSummary": "Fix: add alt attribute"
                },
                {
                    "html": "<img src=\"photo.jpg\">",
                    "target": [".content > img:nth-child(3)"],
                    "failureSummary": "Fix: add alt attribute"
                }
            ]
        }
    ]
}
```

---

## Phase 3: HTML Checker

### Task 3.1 — RunHtmlCheckAsync

```
Input:  Saved page.html file path (string)
        Full URL (for report context)
Output: A11yToolResult with A11yIssue list from 15+ structural checks

Flow:
  1. Read page.html into string
  2. Run each check function against the HTML string
  3. Each check returns zero or more A11yIssue objects
  4. Combine into A11yToolResult
```

**Pseudo-code — each check is a small function:**

```csharp
static A11yToolResult RunHtmlCheck(string html, string url)
{
    issues = new List<A11yIssue>()

    issues.AddRange(CheckImgAlt(html))
    issues.AddRange(CheckHeadingOrder(html))
    issues.AddRange(CheckHtmlLang(html))
    issues.AddRange(CheckFormLabels(html))
    issues.AddRange(CheckEmptyLinks(html))
    issues.AddRange(CheckEmptyButtons(html))
    issues.AddRange(CheckSkipLink(html))
    issues.AddRange(CheckLandmarkMain(html))
    issues.AddRange(CheckLandmarkNav(html))
    issues.AddRange(CheckDivButton(html))
    issues.AddRange(CheckTabindexPositive(html))
    issues.AddRange(CheckMetaRefresh(html))
    issues.AddRange(CheckTableHeaders(html))

    return new A11yToolResult {
        ToolName = "htmlcheck",
        Status = "completed",
        Issues = issues
    }
}
```

**Example check — heading order:**

```csharp
static IEnumerable<A11yIssue> CheckHeadingOrder(string html)
{
    // Find all heading tags in order: <h1>, <h2>, ... <h6>
    // Regex: <h([1-6])[^>]*>
    headings = Regex.Matches(html, @"<h([1-6])[^>]*>")

    lastLevel = 0
    foreach match in headings:
        level = int.Parse(match.Groups[1].Value)

        if lastLevel > 0 AND level > lastLevel + 1:
            // Skipped a level: h1 → h3 (missing h2)
            yield return new A11yIssue {
                Tool = "htmlcheck",
                RuleId = "heading-order",
                Severity = "moderate",
                Message = $"Heading level skipped: <h{lastLevel}> to <h{level}>",
                Snippet = match.Value,
                WcagCriteria = "1.3.1"
            }

        lastLevel = level
}
```

**Example check — missing form labels:**

```csharp
static IEnumerable<A11yIssue> CheckFormLabels(string html)
{
    // Find all <input> that are not hidden/submit/button
    inputs = Regex.Matches(html, @"<input\b([^>]*)>")

    foreach input in inputs:
        attrs = input.Groups[1].Value
        type = ExtractAttr(attrs, "type") ?? "text"

        // Skip types that don't need labels
        if type in ["hidden", "submit", "button", "image", "reset"]:
            continue

        id = ExtractAttr(attrs, "id")
        ariaLabel = ExtractAttr(attrs, "aria-label")
        ariaLabelledBy = ExtractAttr(attrs, "aria-labelledby")

        // Has accessible name?
        if ariaLabel != null OR ariaLabelledBy != null:
            continue

        // Has associated <label for="id">?
        if id != null AND html.Contains($"for=\"{id}\""):
            continue

        yield return new A11yIssue {
            Tool = "htmlcheck",
            RuleId = "label-missing",
            Severity = "serious",
            Message = "Form input has no associated label or aria-label",
            Snippet = Truncate(input.Value, 120),
            WcagCriteria = "1.3.1"
        }
}
```

---

## Phase 4: Pa11y CLI (Optional)

### Task 4.1 — Detect pa11y at startup

```
Startup:
  Run: "where pa11y" (Windows) or "which pa11y" (Unix)
  If exit code 0  → _pa11yAvailable = true
  If exit code != 0 → _pa11yAvailable = false, log "pa11y not installed — skipping"
```

### Task 4.2 — RunPa11yAsync

```
Input:  Live URL (string)
Output: A11yToolResult (completed or skipped)

Flow:
  if not _pa11yAvailable:
      return A11yToolResult { Status = "skipped" }

  1. Run: pa11y --reporter json --standard WCAG2AA {url}
  2. Capture stdout (JSON array of issues)
  3. Parse and normalize to A11yIssue list
```

**Pseudo-code:**

```csharp
static async Task<A11yToolResult> RunPa11yAsync(string url, bool available)
{
    if not available:
        return new A11yToolResult { ToolName = "pa11y", Status = "skipped" }

    // Shell out to pa11y
    process = new Process {
        FileName = "pa11y",
        Arguments = $"--reporter json --standard WCAG2AA --timeout 30000 \"{url}\"",
        RedirectStandardOutput = true,
        RedirectStandardError = true
    }

    process.Start()
    stdout = await process.StandardOutput.ReadToEndAsync()
    await process.WaitForExitAsync()

    if process.ExitCode == 2:  // pa11y returns 2 when violations found (not an error)
        pa11yItems = JsonSerializer.Deserialize<List<Pa11yItem>>(stdout)
    else if process.ExitCode == 0:
        pa11yItems = []  // no violations
    else:
        return A11yToolResult { Status = "error", ErrorMessage = stderr }

    // Normalize to unified schema
    issues = pa11yItems.Select(p => new A11yIssue {
        Tool = "pa11y",
        RuleId = p.Code,              // "WCAG2AA.Principle1.Guideline1_1.1_1_1.H37"
        Severity = MapPa11ySeverity(p.Type),  // "error" → "serious"
        Message = p.Message,
        Selector = p.Selector,
        Snippet = p.Context
    })

    return A11yToolResult { ToolName = "pa11y", Status = "completed", Issues = issues }
}
```

**Pa11y JSON output shape:**

```json
[
    {
        "code": "WCAG2AA.Principle1.Guideline1_1.1_1_1.H37",
        "type": "error",
        "message": "Img element missing an alt attribute.",
        "context": "<img src=\"banner.jpg\" class=\"hero\">",
        "selector": "#content > img:nth-child(2)"
    }
]
```

---

## Phase 5: Merge + Write Output Files

### Task 5.1 — MergeA11yResults

```
Input:  A11yToolResult from axe, htmlcheck, pa11y (any may be null/skipped)
Output: A11yPageSummary + four JSON files written to page dir
```

**Pseudo-code:**

```csharp
static A11yPageSummary MergeA11yResults(
    A11yToolResult axeResult,
    A11yToolResult htmlResult,
    A11yToolResult pa11yResult,
    string pageDir)
{
    summary = new A11yPageSummary()
    allIssues = new List<A11yIssue>()

    // Write individual tool files
    foreach (tool in [axeResult, htmlResult, pa11yResult]):
        if tool != null:
            summary.ToolResults[tool.ToolName] = tool
            filename = $"a11y-{tool.ToolName}.json"
            WriteJson(Path.Combine(pageDir, filename), tool)
            if tool.Status == "completed":
                allIssues.AddRange(tool.Issues)

    // Calculate severity totals
    summary.Critical = allIssues.Count(i => i.Severity == "critical")
    summary.Serious  = allIssues.Count(i => i.Severity == "serious")
    summary.Moderate = allIssues.Count(i => i.Severity == "moderate")
    summary.Minor    = allIssues.Count(i => i.Severity == "minor")
    summary.TotalViolations = allIssues.Count

    // Write merged summary
    WriteJson(Path.Combine(pageDir, "a11y-summary.json"), summary)

    // NEW (from doc 203): Consensus ranking
    ranked = BuildConsensusRanking(allIssues, completedTools)
    WriteJson(Path.Combine(pageDir, "a11y-ranked.json"), ranked)

    return summary
}
```

### Task 5.3 — Rule ID normalization map (from doc 203)

Maps each tool's native rule IDs to canonical (axe) IDs for cross-tool matching.

```csharp
static readonly Dictionary<string, string> RuleNormalization = new() {
    // htmlcheck → canonical (axe) IDs
    ["img-alt"] = "image-alt",
    ["html-lang"] = "html-has-lang",
    ["label-missing"] = "label",
    ["link-empty"] = "link-name",
    ["button-empty"] = "button-name",
    ["skip-link-missing"] = "skip-link",
    ["landmark-main"] = "landmark-one-main",
    // Pa11y WCAG codes → canonical IDs
    ["WCAG2AA.1_1.1_1_1.H37"] = "image-alt",
    ["WCAG2AA.1_3.1_3_1_A.G141"] = "heading-order",
    ["WCAG2AA.3_1.3_1_1.H57.2"] = "html-has-lang",
    ["WCAG2AA.1_3.1_3_1.F68"] = "label",
    ["WCAG2AA.1_4.1_4_3.G18"] = "color-contrast",
};

static string NormalizeRuleId(string ruleId)
    => RuleNormalization.TryGetValue(ruleId, out var canonical) ? canonical : ruleId;
```

### Task 5.4 — Consensus scoring (from doc 203)

```csharp
static List<RankedRule> BuildConsensusRanking(
    List<A11yIssue> allIssues, List<string> completedTools)
{
    // Normalize all rule IDs
    foreach issue in allIssues:
        issue.CanonicalRuleId = NormalizeRuleId(issue.RuleId)

    // Group by canonical rule ID
    groups = allIssues.GroupBy(i => i.CanonicalRuleId)

    ranked = new List<RankedRule>()
    foreach group in groups:
        toolsFound = group.Select(i => i.Tool).Distinct().ToList()
        toolsCapable = GetCapableTools(group.Key, completedTools)
        score = toolsFound.Count / (double)toolsCapable.Count

        ranked.Add(new RankedRule {
            CanonicalRuleId = group.Key,
            Severity = group.First().Severity,
            ToolsFound = toolsFound,
            Consensus = $"{toolsFound.Count}/{toolsCapable.Count}",
            ConfidenceScore = score,
            Confidence = score >= 0.8 ? "high" : score >= 0.5 ? "medium" : "low",
            TotalInstances = group.Count(),
            Message = group.First().Message,
            HelpUrl = group.First().HelpUrl,
        })

    // Sort: confidence desc → severity rank → instance count desc
    return ranked
        .OrderByDescending(r => r.ConfidenceScore)
        .ThenBy(r => SeverityRank(r.Severity))
        .ThenByDescending(r => r.TotalInstances)
        .ToList()
}
```

### Task 5.5 — Write `a11y-ranked.csv` at run level (from doc 203)

```
Written after all sites complete, at the run root directory.
One row per canonical rule, aggregated across all sites/pages.

Columns:
  Rank, Rule, Severity, Confidence, Consensus, Sites, Pages, Instances, WCAG, Message
```

### Task 5.2 — Integration point in ScanPageAsync

```
BEFORE (end of ScanPageAsync):
    ...
    await DownloadPageImagesAsync(...)
    await WritePageOutputAsync(...)     ← writes report.md + metadata.json
    return result

AFTER (end of ScanPageAsync):
    ...
    await DownloadPageImagesAsync(...)

    // NEW: Run accessibility scans
    var axeResult = await RunAxeCoreAsync(page, axeJs, config.WcagLevel)
    var htmlResult = RunHtmlCheck(htmlContent, fullUrl)
    var pa11yResult = await RunPa11yAsync(fullUrl, pa11yAvailable)
    result.A11ySummary = MergeA11yResults(axeResult, htmlResult, pa11yResult, pageDir)

    await WritePageOutputAsync(...)     ← now includes a11y data in report + metadata
    return result
```

---

## Phase 6: Report Updates

### Task 6.1 — Page report.md accessibility section

```
Inserted AFTER the "🖼️ Page Images" section, BEFORE "📁 Files":

## ♿ Accessibility

| Severity | axe | htmlcheck | pa11y |
|----------|:---:|:---------:|:-----:|
| 🔴 Critical | 3 | 0 | — |
| 🟠 Serious | 8 | 13 | — |
| 🟡 Moderate | 4 | 3 | — |
| 🔵 Minor | 2 | 1 | — |
| **Total** | **17** | **17** | **—** |

<details>
<summary><strong>17 violations from axe-core</strong></summary>

| Rule | Severity | Count | Example |
|------|----------|:-----:|---------|
| image-alt | 🔴 critical | 13 | `<img src="banner.jpg">` |
| color-contrast | 🟠 serious | 8 | `<span style="color:#999">` |
| heading-order | 🟡 moderate | 2 | `<h3>` after `<h1>` |

</details>
```

### Task 6.2 — Site report.md accessibility rollup

```
Appended after existing JS Errors section:

## ♿ Accessibility Summary

| Metric | Value |
|--------|-------|
| Total Violations | 255 |
| 🔴 Critical | 45 |
| 🟠 Serious | 120 |
| 🟡 Moderate | 67 |
| 🔵 Minor | 23 |

### Top 10 Issues

| # | Rule | Severity | Pages | Instances |
|--:|------|----------|:-----:|:---------:|
| 1 | image-alt | 🔴 | 14/16 | 89 |
| 2 | color-contrast | 🟠 | 12/16 | 67 |
...
```

### Task 6.3 — Run report.md accessibility dashboard

```
Appended to Dashboard section:

### ♿ Accessibility

    Critical:     [██░░░░░░░░░░░░░░░░░░░░░░░░░░░░] 7%
    Serious:      [████████████░░░░░░░░░░░░░░░░░░] 40%
    Moderate:     [████████████████████░░░░░░░░░░] 35%
    Minor:        [██████████████████████████░░░░] 18%

| 🔴 Critical | 🟠 Serious | 🟡 Moderate | 🔵 Minor | Total |
|:-----------:|:----------:|:-----------:|:--------:|:-----:|
| 423 | 2,891 | 1,456 | 789 | 5,559 |

### Top 20 Violations (all sites)

| # | Rule | Sev | Sites | Pages | Count |
|--:|------|:---:|:-----:|:-----:|:-----:|
| 1 | image-alt | 🔴 | 98/120 | 389/502 | 1,189 |
| 2 | color-contrast | 🟠 | 87/120 | 312/502 | 856 |
| 3 | link-name | 🟠 | 76/120 | 278/502 | 634 |
...
```

---

## Task Checklist

### Phase 1: Models + Config
- [ ] **1.1** Add `A11yIssue` class (~15 lines)
- [ ] **1.2** Add `A11yToolResult` class (~10 lines)
- [ ] **1.3** Add `A11yPageSummary` class (~15 lines)
- [ ] **1.4** Add `A11ySummary` field to `PageResult` (~2 lines)
- [ ] **1.5** Add `WcagLevel` to `ScannerConfig` + `appsettings.json` (~5 lines)

### Phase 2: axe-core
- [ ] **2.1** Implement `EnsureAxeCoreAsync` — CDN download + cache (~30 lines)
- [ ] **2.2** Implement `RunAxeCoreAsync` — inject + run + parse (~80 lines)
- [ ] **2.3** Add axe-core call to `ScanPageAsync` (~5 lines)
- [ ] **2.4** Write `a11y-axe.json` per page (~5 lines)

### Phase 3: HTML Checker
- [ ] **3.1** Implement `RunHtmlCheck` coordinator (~20 lines)
- [ ] **3.2** Implement `CheckImgAlt` (~15 lines)
- [ ] **3.3** Implement `CheckHeadingOrder` (~20 lines)
- [ ] **3.4** Implement `CheckHtmlLang` (~15 lines)
- [ ] **3.5** Implement `CheckFormLabels` (~25 lines)
- [ ] **3.6** Implement `CheckEmptyLinks` (~15 lines)
- [ ] **3.7** Implement `CheckEmptyButtons` (~15 lines)
- [ ] **3.8** Implement `CheckSkipLink` (~10 lines)
- [ ] **3.9** Implement `CheckLandmarkMain` + `CheckLandmarkNav` (~15 lines)
- [ ] **3.10** Implement `CheckDivButton` (~15 lines)
- [ ] **3.11** Implement `CheckTabindexPositive` (~10 lines)
- [ ] **3.12** Implement `CheckMetaRefresh` (~10 lines)
- [ ] **3.13** Implement `CheckTableHeaders` (~15 lines)
- [ ] **3.14** Add htmlcheck call to `ScanPageAsync` (~3 lines)
- [ ] **3.15** Write `a11y-htmlcheck.json` per page (~5 lines)

### Phase 4: Pa11y (Optional)
- [ ] **4.1** Detect pa11y availability at startup (~15 lines)
- [ ] **4.2** Implement `RunPa11yAsync` — shell out + parse (~60 lines)
- [ ] **4.3** Add pa11y call to `ScanPageAsync` (~5 lines)
- [ ] **4.4** Write `a11y-pa11y.json` per page (~5 lines)

### Phase 5: Merge + Output + Consensus Ranking
- [ ] **5.1** Implement `MergeA11yResults` — combine + write summary (~40 lines)
- [ ] **5.2** Update `metadata.json` output with `Accessibility` block (~15 lines)
- [ ] **5.3** Add rule ID normalization map (~40 lines) *(from doc 203)*
- [ ] **5.4** Add consensus scoring to `MergeA11yResults` (~30 lines) *(from doc 203)*
- [ ] **5.5** Write `a11y-ranked.json` per page (~10 lines) *(from doc 203)*
- [ ] **5.6** Write `a11y-ranked.csv` at run level (~30 lines) *(from doc 203)*

### Phase 6: Reports
- [ ] **6.1** Add ♿ section to page `report.md` — cross-tool table + violations (~80 lines)
- [ ] **6.2** Add ♿ rollup to site `report.md` — aggregated top-10 (~50 lines)
- [ ] **6.3** Add ♿ dashboard to run `report.md` — progress bar + top-20 (~60 lines)
- [ ] **6.4** Add tool columns to existing Sites table in run report (~10 lines)
- [ ] **6.5** Add consensus-ranked table to run `report.md` (~25 lines) *(from doc 203)*
- [ ] **6.6** Add consensus-ranked table to site `report.md` (~25 lines) *(from doc 203)*

### Phase 7: Build + Test
- [ ] **7.1** Build — verify no compile errors
- [ ] **7.2** Test run on 1 site (wsu.edu, 16 pages) — verify a11y JSON files created
- [ ] **7.3** Verify existing reports unchanged (screenshots, images, JS errors)
- [ ] **7.4** Full run on 120 sites — review reports

---

*Created: 2026-02-17*  
*Maintained by: [Quality]*
