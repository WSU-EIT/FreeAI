using System.Text.RegularExpressions;
using System.Text.Json;

namespace FreeManager.Client {
// ---- Helpers ----

// FreeManager menu items and icons, called from one line in each hook in Helpers.App.cs.

public static partial class Helpers
{
    public static Dictionary<string, List<string>> FreeManagerAppIcons => new Dictionary<string, List<string>> {
        { "fa:fa-solid fa-cubes", new List<string> { "Builder", "AppBuilder" }},
        { "fa:fa-solid fa-gauge-high", new List<string> { "Dashboard" }},
        { "fa:fa-solid fa-wand-magic-sparkles", new List<string> { "Wizard", "EntityWizard" }},
        { "fa:fa-solid fa-layer-group", new List<string> { "Templates", "TemplateGallery" }},
        { "fa:fa-solid fa-hat-wizard", new List<string> { "SetupWizard", "SetupWizardBuilder" }},
    };

    public static List<DataObjects.MenuItem> FreeManagerMenuItemsAdminApp {
        get {
            var output = new List<DataObjects.MenuItem>();

            // Archive section - old pages moved here
            if (Model.User.Admin) {
                // Entity Wizard (legacy - for custom entity building)
                output.Add(new DataObjects.MenuItem {
                    Title = "Entity Wizard (Archive)",
                    Icon = "Wizard",
                    PageNames = new List<string> { "appbuilder/entitywizard" },
                    SortOrder = 800,
                    url = BuildUrl("AppBuilder/EntityWizard"),
                    AppAdminOnly = false,
                });

                // Template Gallery (legacy - view all template types)
                output.Add(new DataObjects.MenuItem {
                    Title = "Template Gallery (Archive)",
                    Icon = "Templates",
                    PageNames = new List<string> { "fm/templates" },
                    SortOrder = 810,
                    url = BuildUrl("fm/templates"),
                    AppAdminOnly = false,
                });

                // Dashboard (legacy)
                output.Add(new DataObjects.MenuItem {
                    Title = "Dashboard (Archive)",
                    Icon = "Dashboard",
                    PageNames = new List<string> { "appbuilder/dashboard" },
                    SortOrder = 820,
                    url = BuildUrl("AppBuilder/Dashboard"),
                    AppAdminOnly = false,
                });

                // Setup Wizard Builder (legacy)
                output.Add(new DataObjects.MenuItem {
                    Title = "Setup Wizard (Archive)",
                    Icon = "SetupWizard",
                    PageNames = new List<string> { "appbuilder/setupwizardbuilder" },
                    SortOrder = 830,
                    url = BuildUrl("AppBuilder/SetupWizardBuilder"),
                    AppAdminOnly = false,
                });
            }

            return output;
        }
    }

    public static List<DataObjects.MenuItem> FreeManagerMenuItemsApp {
        get {
            var output = new List<DataObjects.MenuItem>();

            // New Template Selection - main entry point
            output.Add(new DataObjects.MenuItem {
                Title = "New Project",
                Icon = "Templates",
                PageNames = new List<string> { "templates", "templates/setup" },
                SortOrder = 100,
                url = BuildUrl("Templates"),
                AppAdminOnly = false,
            });

            // My Projects - view saved projects
            output.Add(new DataObjects.MenuItem {
                Title = "My Projects",
                Icon = "Builder",
                PageNames = new List<string> { "appbuilder", "appbuilder/new", "appbuilder/edit" },
                SortOrder = 200,
                url = BuildUrl("AppBuilder"),
                AppAdminOnly = false,
            });

            return output;
        }
    }
}
}

namespace FreeManager {
// ---- Client.EntityWizard.ImportParser ----
// EntityWizardImportParser (section of FreeManager.App.Client.cs) - Parses C# classes and JSON schemas to extract entity definitions
// Provides confidence scoring based on parse success

/// <summary>
/// Parser for importing entity definitions from various sources.
/// </summary>
public static class EntityWizardImportParser
{
    // ============================================================
    // HELPER METHODS
    // ============================================================

    private static bool IsNavigationProperty(string typeStr)
    {
        return typeStr.StartsWith("ICollection") ||
               typeStr.StartsWith("IEnumerable") ||
               typeStr.StartsWith("List<") ||
               typeStr.StartsWith("HashSet<") ||
               typeStr.Contains("virtual");
    }

    private static bool IsSystemFieldName(string propName)
    {
        var systemNames = new[] {
            "CreatedAt", "UpdatedAt", "DeletedAt", "Deleted",
            "CreatedBy", "UpdatedBy", "LastModifiedBy",
            "TenantId", "RowVersion"
        };
        return systemNames.Contains(propName);
    }

    private static string JsonTypeToCSharp(string jsonType)
    {
        return jsonType.ToLower() switch
        {
            "string" => "string",
            "number" => "decimal",
            "integer" => "int",
            "boolean" => "bool",
            "array" => "string", // Simplified
            "object" => "string", // Simplified
            _ => "string"
        };
    }

    private static string NormalizeCSharpType(string typeStr)
    {
        typeStr = typeStr.Trim().TrimEnd('?');

        return typeStr switch
        {
            "String" => "string",
            "Int32" => "int",
            "Int64" => "long",
            "Boolean" => "bool",
            "Decimal" => "decimal",
            "Double" => "decimal",
            "Single" => "decimal",
            "float" => "decimal",
            "double" => "decimal",
            _ => typeStr
        };
    }

    private static void ParseAttributes(string attributes, DataObjects.PropertyDefinition prop)
    {
        // [Required]
        if (attributes.Contains("Required"))
        {
            prop.IsRequired = true;
            prop.IsNullable = false;
        }

        // [MaxLength(100)] or [StringLength(100)]
        var maxLengthMatch = Regex.Match(attributes, @"(?:MaxLength|StringLength)\s*\(\s*(\d+)\s*\)");
        if (maxLengthMatch.Success && int.TryParse(maxLengthMatch.Groups[1].Value, out int maxLen))
        {
            prop.MaxLength = maxLen;
        }

        // [Key]
        if (attributes.Contains("Key"))
        {
            prop.IsPrimaryKey = true;
        }

        // [Column("column_name")]
        var columnMatch = Regex.Match(attributes, @"Column\s*\(\s*""([^""]+)""\s*\)");
        if (columnMatch.Success)
        {
            prop.Description = $"Column: {columnMatch.Groups[1].Value}";
        }
    }
    /// <summary>
    /// Parse a C# class definition into an EntityDefinition.
    /// </summary>
    public static DataObjects.ImportResult ParseCSharpClass(string classText)
    {
        var result = new DataObjects.ImportResult
        {
            Success = false,
            ConfidenceScore = 0,
            Entity = new DataObjects.EntityDefinition(),
            Warnings = new List<DataObjects.ImportWarning>(),
            ImportedFieldNames = new HashSet<string>()
        };

        if (string.IsNullOrWhiteSpace(classText))
        {
            result.Message = "No input provided";
            return result;
        }

        try
        {
            int totalFields = 0;
            int parsedFields = 0;

            // Extract class name
            var classMatch = Regex.Match(classText, @"class\s+(\w+)", RegexOptions.IgnoreCase);
            if (classMatch.Success)
            {
                result.Entity.Name = classMatch.Groups[1].Value;
                result.Entity.PluralName = result.Entity.Name + "s";
                result.Entity.TableName = result.Entity.PluralName;
            }
            else
            {
                result.Warnings.Add(new DataObjects.ImportWarning
                {
                    FieldName = "ClassName",
                    Message = "Could not extract class name",
                    Suggestion = "Ensure the class declaration is visible (e.g., 'public class MyEntity')",
                    Severity = "Warning"
                });
            }

            // Extract properties using regex
            // Matches: public Type PropertyName { get; set; }
            // Also matches: [Attributes] public Type PropertyName { get; set; } = default;
            var propertyPattern = @"(?:\[([^\]]+)\]\s*)*public\s+([\w\?<>,\s]+)\s+(\w+)\s*\{\s*get;\s*set;\s*\}(?:\s*=\s*([^;]+))?";
            var propertyMatches = Regex.Matches(classText, propertyPattern, RegexOptions.Multiline);

            foreach (Match match in propertyMatches)
            {
                totalFields++;

                var attributes = match.Groups[1].Value;
                var typeStr = match.Groups[2].Value.Trim();
                var propName = match.Groups[3].Value;
                var defaultValue = match.Groups[4].Success ? match.Groups[4].Value.Trim().TrimEnd(';') : null;

                // Skip navigation properties (ICollection, List, other entity types)
                if (IsNavigationProperty(typeStr))
                {
                    result.Warnings.Add(new DataObjects.ImportWarning
                    {
                        FieldName = propName,
                        Message = $"Skipped navigation property of type '{typeStr}'",
                        Suggestion = "Navigation properties are not imported",
                        Severity = "Info"
                    });
                    continue;
                }

                var prop = new DataObjects.PropertyDefinition
                {
                    Id = Guid.NewGuid(),
                    Name = propName,
                    Type = NormalizeCSharpType(typeStr),
                    IsNullable = typeStr.Contains("?") || typeStr == "string",
                    DefaultValue = defaultValue,
                    SortOrder = result.Entity.Properties.Count
                };

                // Parse attributes
                if (!string.IsNullOrEmpty(attributes))
                {
                    ParseAttributes(attributes, prop);
                }

                // Detect primary key
                if (propName.EndsWith("Id") && propName.Length > 2 &&
                    (result.Entity.Name == null || propName == result.Entity.Name + "Id"))
                {
                    prop.IsPrimaryKey = true;
                    result.Entity.PrimaryKeyName = propName;
                    result.Entity.PrimaryKeyType = prop.Type;
                }

                // Detect system fields
                if (IsSystemFieldName(propName))
                {
                    prop.IsSystemField = true;
                }

                result.Entity.Properties.Add(prop);
                result.ImportedFieldNames.Add(propName);
                parsedFields++;
            }

            // Calculate confidence score
            if (totalFields > 0)
            {
                result.ConfidenceScore = (int)((parsedFields / (double)totalFields) * 100);
            }

            // Adjust score based on class name detection
            if (string.IsNullOrEmpty(result.Entity.Name))
            {
                result.ConfidenceScore = Math.Max(0, result.ConfidenceScore - 20);
            }

            result.Success = result.Entity.Properties.Count > 0;
            result.Message = result.Success
                ? $"Successfully imported {parsedFields} of {totalFields} properties"
                : "No properties could be imported";

            return result;
        }
        catch (Exception ex)
        {
            result.Message = $"Parse error: {ex.Message}";
            result.ConfidenceScore = 0;
            return result;
        }
    }

    /// <summary>
    /// Parse a JSON schema into an EntityDefinition.
    /// </summary>
    public static DataObjects.ImportResult ParseJsonSchema(string jsonText)
    {
        var result = new DataObjects.ImportResult
        {
            Success = false,
            ConfidenceScore = 0,
            Entity = new DataObjects.EntityDefinition(),
            Warnings = new List<DataObjects.ImportWarning>(),
            ImportedFieldNames = new HashSet<string>()
        };

        if (string.IsNullOrWhiteSpace(jsonText))
        {
            result.Message = "No input provided";
            return result;
        }

        try
        {
            // Simple JSON property detection
            // Matches: "propertyName": "type" or "propertyName": { "type": "string" }
            var simplePattern = @"""(\w+)""\s*:\s*""(string|number|integer|boolean)""";
            var objectPattern = @"""(\w+)""\s*:\s*\{\s*""type""\s*:\s*""(\w+)""";

            var simpleMatches = Regex.Matches(jsonText, simplePattern);
            var objectMatches = Regex.Matches(jsonText, objectPattern);

            foreach (Match match in simpleMatches)
            {
                var propName = match.Groups[1].Value;
                var jsonType = match.Groups[2].Value;

                // Skip if it looks like a schema keyword
                if (propName == "type" || propName == "properties" || propName == "required")
                    continue;

                var prop = new DataObjects.PropertyDefinition
                {
                    Id = Guid.NewGuid(),
                    Name = ToPascalCase(propName),
                    Type = JsonTypeToCSharp(jsonType),
                    SortOrder = result.Entity.Properties.Count
                };

                result.Entity.Properties.Add(prop);
                result.ImportedFieldNames.Add(prop.Name);
            }

            foreach (Match match in objectMatches)
            {
                var propName = match.Groups[1].Value;
                var jsonType = match.Groups[2].Value;

                if (!result.ImportedFieldNames.Contains(ToPascalCase(propName)))
                {
                    var prop = new DataObjects.PropertyDefinition
                    {
                        Id = Guid.NewGuid(),
                        Name = ToPascalCase(propName),
                        Type = JsonTypeToCSharp(jsonType),
                        SortOrder = result.Entity.Properties.Count
                    };

                    result.Entity.Properties.Add(prop);
                    result.ImportedFieldNames.Add(prop.Name);
                }
            }

            // Try to extract title as entity name
            var titleMatch = Regex.Match(jsonText, @"""title""\s*:\s*""([^""]+)""");
            if (titleMatch.Success)
            {
                result.Entity.Name = ToPascalCase(titleMatch.Groups[1].Value.Replace(" ", ""));
                result.Entity.PluralName = result.Entity.Name + "s";
            }

            result.Success = result.Entity.Properties.Count > 0;
            result.ConfidenceScore = result.Success ? 70 : 0; // JSON parsing is less precise
            result.Message = result.Success
                ? $"Imported {result.Entity.Properties.Count} properties from JSON"
                : "No properties could be extracted";

            return result;
        }
        catch (Exception ex)
        {
            result.Message = $"JSON parse error: {ex.Message}";
            return result;
        }
    }

    private static string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        // Handle snake_case and kebab-case
        var words = Regex.Split(input, @"[-_]");
        return string.Concat(words.Select(w =>
            string.IsNullOrEmpty(w) ? "" : char.ToUpper(w[0]) + w.Substring(1).ToLower()
        ));
    }
}
}

namespace FreeManager.Client {
// ---- Client.EntityWizard.UndoRedo ----
// EntityWizardUndoRedo (section of FreeManager.App.Client.cs) - Undo/Redo state management for Entity Wizard
// Provides history tracking with snapshot-based state restoration

/// <summary>
/// Manages undo/redo history for the Entity Wizard.
/// Uses JSON serialization for state snapshots.
/// </summary>
public class EntityWizardUndoRedo
{
    private readonly List<UndoRedoEntry> _history = new();
    private int _currentIndex = -1;
    private const int MaxHistorySize = 50;

    /// <summary>
    /// True if there are actions that can be undone.
    /// </summary>
    public bool CanUndo => _currentIndex > 0;

    /// <summary>
    /// True if there are actions that can be redone.
    /// </summary>
    public bool CanRedo => _currentIndex < _history.Count - 1;

    /// <summary>
    /// Current history position (for display).
    /// </summary>
    public int HistoryPosition => _currentIndex + 1;

    /// <summary>
    /// Total history count (for display).
    /// </summary>
    public int HistoryCount => _history.Count;

    /// <summary>
    /// Description of the last action (for tooltip).
    /// </summary>
    public string? LastActionDescription => _currentIndex >= 0 ? _history[_currentIndex].Description : null;

    /// <summary>
    /// Record a state change that can be undone.
    /// </summary>
    public void RecordChange(DataObjects.EntityWizardState state, string description)
    {
        // Remove any redo history
        if (_currentIndex < _history.Count - 1)
        {
            _history.RemoveRange(_currentIndex + 1, _history.Count - _currentIndex - 1);
        }

        // Serialize current state
        var snapshot = SerializeState(state);

        // Don't record if identical to last entry
        if (_history.Count > 0 && _history[^1].StateSnapshot == snapshot)
        {
            return;
        }

        // Add new entry
        _history.Add(new UndoRedoEntry
        {
            Description = description,
            Timestamp = DateTime.UtcNow,
            StateSnapshot = snapshot
        });

        // Trim history if too large
        while (_history.Count > MaxHistorySize)
        {
            _history.RemoveAt(0);
        }

        _currentIndex = _history.Count - 1;
    }

    /// <summary>
    /// Undo the last change and return the restored state.
    /// </summary>
    public DataObjects.EntityWizardState? Undo()
    {
        if (!CanUndo) return null;

        _currentIndex--;
        return DeserializeState(_history[_currentIndex].StateSnapshot);
    }

    /// <summary>
    /// Redo the previously undone change and return the restored state.
    /// </summary>
    public DataObjects.EntityWizardState? Redo()
    {
        if (!CanRedo) return null;

        _currentIndex++;
        return DeserializeState(_history[_currentIndex].StateSnapshot);
    }

    /// <summary>
    /// Get list of recent actions (for history panel).
    /// </summary>
    public List<UndoRedoEntry> GetRecentHistory(int count = 10)
    {
        return _history
            .Skip(Math.Max(0, _history.Count - count))
            .Reverse()
            .ToList();
    }

    /// <summary>
    /// Clear all history.
    /// </summary>
    public void Clear()
    {
        _history.Clear();
        _currentIndex = -1;
    }

    private static string SerializeState(DataObjects.EntityWizardState state)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = false
        };
        return JsonSerializer.Serialize(state, options);
    }

    private static DataObjects.EntityWizardState? DeserializeState(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<DataObjects.EntityWizardState>(json);
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// Single entry in the undo/redo history.
/// </summary>
public class UndoRedoEntry
{
    public string Description { get; set; } = string.Empty;
    public string StateSnapshot { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
}
