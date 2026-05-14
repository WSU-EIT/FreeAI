using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace FreeA11yChecker.Console.SourceAnalysis;

public static class ViolationsCsvExporter
{
    private static readonly string[] ToolFiles = new[]
    {
        "a11y-axe.json",
        "a11y-ibm.json",
        "a11y-htmlcs.json",
        "a11y-htmlcheck.json",
    };

    public static int Export(string outputDir, string host)
    {
        var hostDir = Path.Combine(outputDir, host);
        if (!Directory.Exists(hostDir))
        {
            return 0;
        }

        var rows = new List<string[]>();
        var foundAny = false;

        foreach (var pageDir in Directory.EnumerateDirectories(hostDir))
        {
            var pageSlug = Path.GetFileName(pageDir);

            foreach (var toolFile in ToolFiles)
            {
                var file = Path.Combine(pageDir, toolFile);
                if (!File.Exists(file))
                {
                    continue;
                }

                foundAny = true;
                var toolName = Path.GetFileNameWithoutExtension(toolFile);
                if (toolName.StartsWith("a11y-", StringComparison.OrdinalIgnoreCase))
                {
                    toolName = toolName.Substring("a11y-".Length);
                }

                try
                {
                    using var doc = JsonDocument.Parse(File.ReadAllText(file));
                    if (doc.RootElement.ValueKind != JsonValueKind.Array)
                    {
                        continue;
                    }

                    foreach (var item in doc.RootElement.EnumerateArray())
                    {
                        if (item.ValueKind != JsonValueKind.Object)
                        {
                            continue;
                        }

                        var ruleId = GetStringProp(item, "RuleId", "ruleId");
                        var severity = GetStringProp(item, "Severity", "severity");
                        var selector = GetStringProp(item, "Selector", "selector");
                        var message = GetStringProp(item, "Message", "message");
                        var helpUrl = GetStringProp(item, "HelpUrl", "helpUrl");

                        rows.Add(new[]
                        {
                            pageSlug,
                            toolName,
                            ruleId,
                            severity,
                            selector,
                            message,
                            helpUrl,
                        });
                    }
                }
                catch (Exception ex)
                {
                    System.Console.Error.WriteLine($"[warn] Failed to parse {file}: {ex.Message}");
                    continue;
                }
            }
        }

        if (!foundAny)
        {
            return 0;
        }

        var csvPath = Path.Combine(hostDir, "_violations.csv");
        using (var writer = new StreamWriter(csvPath, append: false, Encoding.UTF8))
        {
            writer.WriteLine(FormatRow(new[] { "Page", "Tool", "RuleId", "Severity", "Selector", "Message", "HelpUrl" }));
            foreach (var row in rows)
            {
                writer.WriteLine(FormatRow(row));
            }
        }

        return rows.Count;
    }

    private static string GetStringProp(JsonElement obj, params string[] names)
    {
        foreach (var name in names)
        {
            if (obj.TryGetProperty(name, out var el))
            {
                return JsonElementToString(el);
            }
        }

        // Case-insensitive fallback.
        foreach (var prop in obj.EnumerateObject())
        {
            foreach (var name in names)
            {
                if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return JsonElementToString(prop.Value);
                }
            }
        }

        return string.Empty;
    }

    private static string JsonElementToString(JsonElement el)
    {
        return el.ValueKind switch
        {
            JsonValueKind.String => el.GetString() ?? string.Empty,
            JsonValueKind.Null => string.Empty,
            JsonValueKind.Undefined => string.Empty,
            _ => el.ToString(),
        };
    }

    private static string FormatRow(string[] fields)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < fields.Length; i++)
        {
            if (i > 0)
            {
                sb.Append(',');
            }

            sb.Append(QuoteField(fields[i] ?? string.Empty));
        }

        return sb.ToString();
    }

    private static string QuoteField(string value)
    {
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
