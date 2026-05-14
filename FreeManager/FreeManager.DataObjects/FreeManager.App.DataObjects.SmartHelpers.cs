namespace FreeManager;

#region FreeManager Platform - Smart Helpers & Standard Fields
// ┌─────────────────────────────────────────────────────────────────┐
// │        DataObjects.App.FreeManager.SmartHelpers.cs              │
// │          Property Types, Snippets & Smart Detection              │
// ├─────────────────────────────────────────────────────────────────┤
// │ PROPERTY TYPES                                                  │
// │   PropertyTypeInfo      → Type display info (icon, tile, etc)   │
// │   PropertyTypes         → Static: string, int, bool, DateTime.. │
// │   PropertyTypes.All     → List of all types with metadata       │
// ├─────────────────────────────────────────────────────────────────┤
// │ STANDARD FIELDS (Reusable property groups)                      │
// │   StandardFields.Audit    → CreatedAt, UpdatedAt, ModifiedBy    │
// │   StandardFields.SoftDelete → IsDeleted, DeletedAt, DeletedBy   │
// │   StandardFields.Ownership  → OwnerId, OwnerName, shared flags  │
// │   StandardFields.Common     → Name, Description, Notes, Tags    │
// ├─────────────────────────────────────────────────────────────────┤
// │ PROPERTY SNIPPETS (Quick-add patterns)                          │
// │   PropertySnippets.All   → Common property patterns (Status,    │
// │                           Priority, Email, Phone, Currency...)  │
// ├─────────────────────────────────────────────────────────────────┤
// │ SMART NAME DETECTION                                            │
// │   SmartNameDetection     → Auto-suggest type from property name │
// │                           (Email→string, IsActive→bool, etc)    │
// └─────────────────────────────────────────────────────────────────┘
// Part of: DataObjects.App.FreeManager (partial)
// ============================================================================

public partial class DataObjects
{
    // ============================================================
    // PROPERTY TYPE DEFINITIONS
    // ============================================================

    /// <summary>
    /// Property type display information.
    /// </summary>
    public class PropertyTypeInfo
    {
        public string Type { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;

        /// <summary>Short label for visual tile picker</summary>
        public string TileLabel { get; set; } = string.Empty;

        /// <summary>Background color class for tile</summary>
        public string TileColorClass { get; set; } = "bg-secondary";

        /// <summary>Common default value for this type</summary>
        public string? CommonDefault { get; set; }
    }

    /// <summary>
    /// Supported property types for the wizard.
    /// </summary>
    public static class PropertyTypes
    {
        public const string String = "string";
        public const string Int = "int";
        public const string Long = "long";
        public const string Decimal = "decimal";
        public const string Bool = "bool";
        public const string DateTime = "DateTime";
        public const string DateOnly = "DateOnly";
        public const string Guid = "Guid";
        public const string Enum = "enum";

        /// <summary>All available types for dropdown and visual picker</summary>
        public static readonly List<PropertyTypeInfo> All = new()
        {
            new() { Type = String, DisplayName = "Text (string)", Icon = "fa-font", TileLabel = "Abc", TileColorClass = "bg-success", CommonDefault = "string.Empty" },
            new() { Type = Int, DisplayName = "Integer (int)", Icon = "fa-hashtag", TileLabel = "123", TileColorClass = "bg-info", CommonDefault = "0" },
            new() { Type = Long, DisplayName = "Long Integer (long)", Icon = "fa-hashtag", TileLabel = "123L", TileColorClass = "bg-info", CommonDefault = "0L" },
            new() { Type = Decimal, DisplayName = "Decimal", Icon = "fa-dollar-sign", TileLabel = "$", TileColorClass = "bg-warning", CommonDefault = "0m" },
            new() { Type = Bool, DisplayName = "Yes/No (bool)", Icon = "fa-toggle-on", TileLabel = "✓/✗", TileColorClass = "bg-purple", CommonDefault = "false" },
            new() { Type = DateTime, DisplayName = "Date & Time", Icon = "fa-calendar", TileLabel = "📅", TileColorClass = "bg-primary", CommonDefault = "DateTime.UtcNow" },
            new() { Type = DateOnly, DisplayName = "Date Only", Icon = "fa-calendar-day", TileLabel = "📆", TileColorClass = "bg-primary", CommonDefault = "DateOnly.FromDateTime(DateTime.Today)" },
            new() { Type = Guid, DisplayName = "Unique ID (Guid)", Icon = "fa-fingerprint", TileLabel = "🔑", TileColorClass = "bg-secondary", CommonDefault = "Guid.NewGuid()" },
            new() { Type = Enum, DisplayName = "Enum (choice list)", Icon = "fa-list", TileLabel = "📋", TileColorClass = "bg-danger", CommonDefault = null }
        };

        /// <summary>Get PropertyTypeInfo by type string</summary>
        public static PropertyTypeInfo? Get(string type) => All.FirstOrDefault(t => t.Type == type);
    }

    // ============================================================
    // STANDARD FIELDS (Reusable property groups)
    // ============================================================

    /// <summary>
    /// Standard field groups for common patterns.
    /// Matches FreeManager/FreeCRM naming conventions.
    /// </summary>
    public static class StandardFields
    {
        /// <summary>Audit fields: Added, AddedBy, LastModified, LastModifiedBy (FreeManager convention)</summary>
        public static readonly List<PropertyDefinition> Audit = new()
        {
            new() { Name = "Added", Type = "DateTime", IsRequired = true, DefaultValue = "DateTime.UtcNow", IsSystemField = true, Description = "When created" },
            new() { Name = "AddedBy", Type = "string", IsRequired = false, IsNullable = true, MaxLength = 100, IsSystemField = true, Description = "Who created" },
            new() { Name = "LastModified", Type = "DateTime", IsRequired = true, DefaultValue = "DateTime.UtcNow", IsSystemField = true, Description = "When last modified" },
            new() { Name = "LastModifiedBy", Type = "string", IsRequired = false, IsNullable = true, MaxLength = 100, IsSystemField = true, Description = "Who modified" }
        };

        /// <summary>Soft delete fields: Deleted, DeletedAt</summary>
        public static readonly List<PropertyDefinition> SoftDelete = new()
        {
            new() { Name = "Deleted", Type = "bool", IsRequired = true, DefaultValue = "false", IsSystemField = true, Description = "Soft delete flag" },
            new() { Name = "DeletedAt", Type = "DateTime", IsRequired = false, IsNullable = true, IsSystemField = true, Description = "When deleted" }
        };

        /// <summary>Tenant isolation field</summary>
        public static readonly List<PropertyDefinition> Tenant = new()
        {
            new() { Name = "TenantId", Type = "Guid", IsRequired = true, IsSystemField = true, Description = "Tenant ID" }
        };
    }

    // ============================================================
    // PROPERTY SNIPPETS (Reusable field groups)
    // ============================================================

    /// <summary>
    /// A reusable group of properties that can be inserted at once.
    /// </summary>
    public class PropertySnippet
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Category { get; set; } = "General";
        public List<PropertyDefinition> Properties { get; set; } = new();
        public bool IsBuiltIn { get; set; } = true;
    }

    /// <summary>
    /// Standard property snippets for quick add.
    /// </summary>
    public static class PropertySnippets
    {
        public static readonly PropertySnippet BasicEntity = new()
        {
            Id = "basic-entity",
            Name = "Basic Entity",
            Description = "Name and Description fields",
            Icon = "fa-file-lines",
            Category = "Common",
            Properties = new()
            {
                new() { Name = "Name", Type = "string", IsRequired = true, MaxLength = 200, Description = "Display name" },
                new() { Name = "Description", Type = "string", IsRequired = false, IsNullable = true, Description = "Optional description" }
            }
        };

        public static readonly PropertySnippet AuditFields = new()
        {
            Id = "audit-fields",
            Name = "Audit Fields",
            Description = "CreatedAt, UpdatedAt, LastModifiedBy",
            Icon = "fa-clock",
            Category = "System",
            Properties = StandardFields.Audit
        };

        public static readonly PropertySnippet SoftDeleteFields = new()
        {
            Id = "soft-delete",
            Name = "Soft Delete",
            Description = "Deleted flag and DeletedAt timestamp",
            Icon = "fa-trash-can",
            Category = "System",
            Properties = StandardFields.SoftDelete
        };

        public static readonly PropertySnippet TenantFields = new()
        {
            Id = "tenant",
            Name = "Tenant ID",
            Description = "Multi-tenant isolation field",
            Icon = "fa-building",
            Category = "System",
            Properties = StandardFields.Tenant
        };

        public static readonly PropertySnippet StatusFields = new()
        {
            Id = "status",
            Name = "Status & Active",
            Description = "IsActive flag and Status enum",
            Icon = "fa-toggle-on",
            Category = "Common",
            Properties = new()
            {
                new() { Name = "IsActive", Type = "bool", IsRequired = true, DefaultValue = "true", Description = "Is active" },
                new() { Name = "Status", Type = "string", IsRequired = false, MaxLength = 50, Description = "Current status" }
            }
        };

        public static readonly PropertySnippet ContactInfo = new()
        {
            Id = "contact-info",
            Name = "Contact Info",
            Description = "Email, Phone, Website fields",
            Icon = "fa-address-card",
            Category = "Common",
            Properties = new()
            {
                new() { Name = "Email", Type = "string", IsRequired = false, MaxLength = 255, Description = "Email" },
                new() { Name = "Phone", Type = "string", IsRequired = false, MaxLength = 20, Description = "Phone" },
                new() { Name = "Website", Type = "string", IsRequired = false, MaxLength = 500, Description = "Website" }
            }
        };

        public static readonly PropertySnippet AddressFields = new()
        {
            Id = "address",
            Name = "Address Block",
            Description = "Street, City, State, Zip, Country",
            Icon = "fa-location-dot",
            Category = "Common",
            Properties = new()
            {
                new() { Name = "Street", Type = "string", IsRequired = false, MaxLength = 200, Description = "Street" },
                new() { Name = "City", Type = "string", IsRequired = false, MaxLength = 100, Description = "City" },
                new() { Name = "State", Type = "string", IsRequired = false, MaxLength = 50, Description = "State" },
                new() { Name = "ZipCode", Type = "string", IsRequired = false, MaxLength = 20, Description = "Zip" },
                new() { Name = "Country", Type = "string", IsRequired = false, MaxLength = 100, Description = "Country" }
            }
        };

        public static readonly PropertySnippet NotesFields = new()
        {
            Id = "notes",
            Name = "Notes & Comments",
            Description = "Notes and InternalNotes fields",
            Icon = "fa-sticky-note",
            Category = "Common",
            Properties = new()
            {
                new() { Name = "Notes", Type = "string", IsRequired = false, IsNullable = true, Description = "Public notes" },
                new() { Name = "InternalNotes", Type = "string", IsRequired = false, IsNullable = true, Description = "Internal notes" }
            }
        };

        /// <summary>All built-in snippets</summary>
        public static readonly List<PropertySnippet> All = new()
        {
            BasicEntity, AuditFields, SoftDeleteFields, TenantFields,
            StatusFields, ContactInfo, AddressFields, NotesFields
        };

        /// <summary>Get snippets by category</summary>
        public static List<PropertySnippet> GetByCategory(string category)
            => All.Where(s => s.Category == category).ToList();
    }

    // ============================================================
    // SMART NAME DETECTION
    // ============================================================

    /// <summary>
    /// A suggestion for property type based on the property name.
    /// </summary>
    public class SmartNameSuggestion
    {
        public string SuggestedType { get; set; } = "string";
        public string? SuggestedDefault { get; set; }
        public bool SuggestSystemField { get; set; }
        public bool SuggestRequired { get; set; } = true;
        public int? SuggestMaxLength { get; set; }
        public string Reason { get; set; } = string.Empty;
        public float Confidence { get; set; } = 0.5f;
    }

    /// <summary>
    /// Smart name pattern detection for property type suggestions.
    /// </summary>
    public static class SmartNameDetection
    {
        /// <summary>
        /// Analyze a property name and suggest type and defaults.
        /// </summary>
        public static SmartNameSuggestion Analyze(string propertyName)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
                return new SmartNameSuggestion();

            var name = propertyName.Trim();
            var nameLower = name.ToLowerInvariant();

            // DateTime patterns
            if (nameLower.EndsWith("at") || nameLower.EndsWith("date") ||
                nameLower.EndsWith("time") || nameLower.EndsWith("on"))
            {
                var isCreated = nameLower.Contains("created") || nameLower.Contains("added");
                return new SmartNameSuggestion
                {
                    SuggestedType = "DateTime",
                    SuggestedDefault = isCreated ? "DateTime.UtcNow" : null,
                    SuggestSystemField = nameLower.Contains("created") || nameLower.Contains("updated") || nameLower.Contains("deleted"),
                    SuggestRequired = isCreated,
                    Reason = $"'{name}' looks like a timestamp",
                    Confidence = 0.9f
                };
            }

            // Boolean patterns
            if (nameLower.StartsWith("is") || nameLower.StartsWith("has") ||
                nameLower.StartsWith("can") || nameLower.StartsWith("should") ||
                nameLower.StartsWith("enable") || nameLower.StartsWith("allow") ||
                nameLower.EndsWith("enabled") || nameLower.EndsWith("active") ||
                nameLower.EndsWith("deleted") || nameLower.EndsWith("visible"))
            {
                return new SmartNameSuggestion
                {
                    SuggestedType = "bool",
                    SuggestedDefault = nameLower.Contains("active") || nameLower.Contains("enabled") ? "true" : "false",
                    SuggestSystemField = nameLower.Contains("deleted"),
                    SuggestRequired = true,
                    Reason = $"'{name}' looks like a boolean flag",
                    Confidence = 0.95f
                };
            }

            // Guid patterns
            if ((nameLower.EndsWith("id") && nameLower != "id" && nameLower.Length > 2) ||
                nameLower.EndsWith("guid") || nameLower.EndsWith("key"))
            {
                return new SmartNameSuggestion
                {
                    SuggestedType = "Guid",
                    SuggestSystemField = nameLower.Contains("tenant"),
                    SuggestRequired = nameLower.Contains("tenant"),
                    Reason = $"'{name}' looks like a unique identifier",
                    Confidence = 0.85f
                };
            }

            // Integer patterns
            if (nameLower.EndsWith("count") || nameLower.EndsWith("number") ||
                nameLower.EndsWith("quantity") || nameLower.EndsWith("index") ||
                nameLower.EndsWith("order") || nameLower.EndsWith("rank") ||
                nameLower.EndsWith("level") || nameLower.EndsWith("age") ||
                nameLower.Contains("sortorder") || nameLower.Contains("displayorder"))
            {
                return new SmartNameSuggestion
                {
                    SuggestedType = "int",
                    SuggestedDefault = "0",
                    SuggestRequired = true,
                    Reason = $"'{name}' looks like a numeric count",
                    Confidence = 0.8f
                };
            }

            // Decimal patterns
            if (nameLower.EndsWith("amount") || nameLower.EndsWith("price") ||
                nameLower.EndsWith("cost") || nameLower.EndsWith("total") ||
                nameLower.EndsWith("balance") || nameLower.EndsWith("rate") ||
                nameLower.EndsWith("percentage") || nameLower.EndsWith("fee"))
            {
                return new SmartNameSuggestion
                {
                    SuggestedType = "decimal",
                    SuggestedDefault = "0m",
                    SuggestRequired = true,
                    Reason = $"'{name}' looks like a monetary value",
                    Confidence = 0.85f
                };
            }

            // Enum patterns
            if (nameLower.EndsWith("status") || nameLower.EndsWith("state") ||
                nameLower.EndsWith("type") || nameLower.EndsWith("category") ||
                nameLower.EndsWith("priority") || nameLower.EndsWith("role") ||
                nameLower.EndsWith("kind"))
            {
                return new SmartNameSuggestion
                {
                    SuggestedType = "enum",
                    SuggestRequired = true,
                    Reason = $"'{name}' looks like a choice list (enum)",
                    Confidence = 0.75f
                };
            }

            // Email pattern
            if (nameLower.Contains("email"))
            {
                return new SmartNameSuggestion
                {
                    SuggestedType = "string",
                    SuggestMaxLength = 255,
                    SuggestRequired = false,
                    Reason = $"'{name}' looks like an email",
                    Confidence = 0.9f
                };
            }

            // URL patterns
            if (nameLower.Contains("url") || nameLower.Contains("website") || nameLower.Contains("link"))
            {
                return new SmartNameSuggestion
                {
                    SuggestedType = "string",
                    SuggestMaxLength = 2000,
                    SuggestRequired = false,
                    Reason = $"'{name}' looks like a URL",
                    Confidence = 0.85f
                };
            }

            // Phone pattern
            if (nameLower.Contains("phone") || nameLower.Contains("fax") || nameLower.Contains("mobile"))
            {
                return new SmartNameSuggestion
                {
                    SuggestedType = "string",
                    SuggestMaxLength = 20,
                    SuggestRequired = false,
                    Reason = $"'{name}' looks like a phone number",
                    Confidence = 0.85f
                };
            }

            // Name/Title patterns
            if (nameLower == "name" || nameLower == "title" || nameLower.EndsWith("name") || nameLower.EndsWith("title"))
            {
                return new SmartNameSuggestion
                {
                    SuggestedType = "string",
                    SuggestMaxLength = 200,
                    SuggestRequired = nameLower == "name" || nameLower == "title",
                    Reason = $"'{name}' looks like a name field",
                    Confidence = 0.8f
                };
            }

            // Description/Notes
            if (nameLower.Contains("description") || nameLower.Contains("notes") ||
                nameLower.Contains("comment") || nameLower.Contains("content") ||
                nameLower.Contains("body") || nameLower.Contains("text"))
            {
                return new SmartNameSuggestion
                {
                    SuggestedType = "string",
                    SuggestMaxLength = null,
                    SuggestRequired = false,
                    Reason = $"'{name}' looks like text content",
                    Confidence = 0.7f
                };
            }

            // Default
            return new SmartNameSuggestion
            {
                SuggestedType = "string",
                SuggestMaxLength = 200,
                SuggestRequired = false,
                Reason = "Default to text field",
                Confidence = 0.3f
            };
        }
    }
}

#endregion
