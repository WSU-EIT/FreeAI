// EntityWizardImportParser.App.cs - Parses C# classes and JSON schemas to extract entity definitions
// Provides confidence scoring based on parse success

using System.Text.RegularExpressions;

namespace FreeManager;

/// <summary>
/// Parser for importing entity definitions from various sources.
/// </summary>
public static class EntityWizardImportParser
{
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
