namespace FreeManager;

#region FreeManager Platform - Entity Wizard DTOs
// ┌─────────────────────────────────────────────────────────────────┐
// │        DataObjects.App.FreeManager.EntityWizard.cs              │
// │              Entity Builder Wizard DTOs (20+ classes)            │
// ├─────────────────────────────────────────────────────────────────┤
// │ WIZARD STATE                                                    │
// │   EntityWizardState     → Complete wizard state container       │
// │   EntityWizardOptions   → Generation options (tests, API, etc)  │
// │   GeneratedFileInfo     → Info about generated output files     │
// ├─────────────────────────────────────────────────────────────────┤
// │ SETUP WIZARD BUILDER                                            │
// │   SetupWizardDefinition      → Multi-step wizard definition     │
// │   SetupWizardStepDefinition  → Individual step with fields      │
// ├─────────────────────────────────────────────────────────────────┤
// │ ENTITY DEFINITION                                               │
// │   EntityDefinition      → Entity to scaffold (name, pk, props)  │
// │   PropertyDefinition    → Property with type, validation, UI    │
// │   EnumTypeDefinition    → Custom enum with values               │
// │   EnumValue             → Single enum value                     │
// ├─────────────────────────────────────────────────────────────────┤
// │ RELATIONSHIPS                                                   │
// │   RelationshipDefinition → FK between entities (1:N, N:N)       │
// │   ManyToManyConfig       → Join table configuration             │
// ├─────────────────────────────────────────────────────────────────┤
// │ TEMPLATES                                                       │
// │   SavedEntityTemplate    → Reusable entity starter              │
// │   SavedWizardStateTemplate → Complete project template          │
// └─────────────────────────────────────────────────────────────────┘
// Part of: DataObjects.App.FreeManager (partial)
// ============================================================================

public partial class DataObjects
{
    // ============================================================
    // ENTITY WIZARD STATE
    // ============================================================

    /// <summary>
    /// Complete state for the entity builder wizard.
    /// Supports single entity (backward compatible) or multiple entities with relationships.
    /// </summary>
    public class EntityWizardState
    {
        public int CurrentStep { get; set; }
        public FMCreateProjectRequest Project { get; set; } = new();

        /// <summary>All entities in this project (multi-entity support)</summary>
        public List<EntityDefinition> Entities { get; set; } = new();

        /// <summary>Relationships between entities</summary>
        public List<RelationshipDefinition> Relationships { get; set; } = new();

        /// <summary>Currently selected entity index for editing (null = overview)</summary>
        public int? SelectedEntityIndex { get; set; }

        public EntityWizardOptions Options { get; set; } = new();
        public List<GeneratedFileInfo> GeneratedFiles { get; set; } = new();
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }

        // Advanced generation configs (from ApplicationTemplate, for FreeAudit-style apps)
        /// <summary>External API configuration (generates controller, middleware, DTOs)</summary>
        public ExternalApiConfig? ExternalApi { get; set; }

        /// <summary>Dashboard configuration (generates dashboard page with stats)</summary>
        public DashboardConfig? Dashboard { get; set; }

        /// <summary>Backward compatible single entity access</summary>
        public EntityDefinition Entity
        {
            get => Entities.FirstOrDefault() ?? new();
            set { if (Entities.Count == 0) Entities.Add(value); else Entities[0] = value; }
        }
    }

    // ============================================================
    // SETUP WIZARD BUILDER TYPES
    // ============================================================

    /// <summary>
    /// Definition for a setup wizard (multi-step onboarding/setup flow).
    /// </summary>
    public class SetupWizardDefinition
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = "fa-wand-magic-sparkles";
        public bool TrackCompletion { get; set; } = true;
        public bool AllowSkip { get; set; } = true;
        public bool PersistStateAcrossSessions { get; set; } = true;
        public List<SetupWizardStepDefinition> Steps { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// A step within a setup wizard.
    /// </summary>
    public class SetupWizardStepDefinition
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid StepId => Id;
        public int Order { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ShortName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = "fa-circle-check";
        public bool IsOptional { get; set; }
        public List<PropertyDefinition> Fields { get; set; } = new();
    }

    // ============================================================
    // ENTITY DEFINITION
    // ============================================================

    /// <summary>
    /// Definition of an entity to scaffold.
    /// </summary>
    public class EntityDefinition
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>Singular name (e.g., "Task")</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Plural name (e.g., "Tasks")</summary>
        public string PluralName { get; set; } = string.Empty;

        /// <summary>Database table name (defaults to plural)</summary>
        public string TableName { get; set; } = string.Empty;

        /// <summary>Primary key property name (e.g., "TaskId")</summary>
        public string PrimaryKeyName { get; set; } = string.Empty;

        /// <summary>Primary key type: Guid, int, long</summary>
        public string PrimaryKeyType { get; set; } = "Guid";

        /// <summary>All properties on the entity</summary>
        public List<PropertyDefinition> Properties { get; set; } = new();

        /// <summary>All enums defined for this entity</summary>
        public List<EnumDefinition> Enums { get; set; } = new();

        /// <summary>Display order in entity list</summary>
        public int SortOrder { get; set; }

        /// <summary>True if this is a join table for M:M</summary>
        public bool IsJoinTable { get; set; }

        /// <summary>Description for this entity</summary>
        public string? Description { get; set; }
    }

    // ============================================================
    // ENTITY RELATIONSHIPS
    // ============================================================

    /// <summary>Types of entity relationships.</summary>
    public enum RelationshipType
    {
        /// <summary>One-to-Many: Source has collection of Target.</summary>
        OneToMany = 0,
        /// <summary>Many-to-Many via join entity.</summary>
        ManyToMany = 1,
        /// <summary>Self-Reference (hierarchies).</summary>
        SelfReference = 2,
        /// <summary>One-to-One.</summary>
        OneToOne = 3
    }

    /// <summary>Delete behavior for relationships.</summary>
    public enum RelationshipDeleteBehavior
    {
        Cascade = 0,
        Restrict = 1,
        SetNull = 2,
        NoAction = 3
    }

    /// <summary>Defines a relationship between two entities.</summary>
    public class RelationshipDefinition
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public RelationshipType Type { get; set; }
        public string SourceEntityName { get; set; } = string.Empty;
        public string TargetEntityName { get; set; } = string.Empty;
        public string SourceNavigationProperty { get; set; } = string.Empty;
        public string TargetNavigationProperty { get; set; } = string.Empty;
        public string ForeignKeyProperty { get; set; } = string.Empty;
        public bool IsRequired { get; set; } = true;
        public RelationshipDeleteBehavior OnDelete { get; set; } = RelationshipDeleteBehavior.Cascade;
        public string? JoinEntityName { get; set; }
        public int SortOrder { get; set; }
    }

    // ============================================================
    // PROPERTY DEFINITION
    // ============================================================

    /// <summary>How a property should be filtered in search interfaces.</summary>
    public enum PropertyFilterType
    {
        /// <summary>No filter generated.</summary>
        None = 0,
        /// <summary>Text contains search (default for strings).</summary>
        TextContains = 1,
        /// <summary>Exact text match.</summary>
        TextExact = 2,
        /// <summary>Date range picker (default for DateTime).</summary>
        DateRange = 3,
        /// <summary>Yes/No/All dropdown (default for bool).</summary>
        BooleanTriState = 4,
        /// <summary>Multi-select checkboxes (default for enum).</summary>
        EnumMultiSelect = 5,
        /// <summary>Single-select dropdown.</summary>
        EnumSingleSelect = 6,
        /// <summary>Numeric range (min/max).</summary>
        NumericRange = 7
    }

    /// <summary>
    /// Definition of a property on an entity.
    /// </summary>
    public class PropertyDefinition
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>Property name (e.g., "Name", "Status")</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>C# type: string, int, bool, DateTime, Guid, decimal, or enum:{EnumName}</summary>
        public string Type { get; set; } = "string";

        /// <summary>True if property is required</summary>
        public bool IsRequired { get; set; } = true;

        /// <summary>True if property can be null</summary>
        public bool IsNullable { get; set; } = false;

        /// <summary>Default value expression</summary>
        public string? DefaultValue { get; set; }

        /// <summary>Max length for strings</summary>
        public int? MaxLength { get; set; }

        /// <summary>If type is enum, this is the enum name</summary>
        public string? EnumName { get; set; }

        /// <summary>Display order</summary>
        public int SortOrder { get; set; }

        /// <summary>True if this is the primary key</summary>
        public bool IsPrimaryKey { get; set; }

        /// <summary>True if this is a system field</summary>
        public bool IsSystemField { get; set; }

        /// <summary>Description/comment</summary>
        public string? Description { get; set; }

        /// <summary>Should this appear in filters?</summary>
        public bool IsFilterable { get; set; } = true;

        /// <summary>How to filter this property</summary>
        public PropertyFilterType? FilterType { get; set; }
    }

    /// <summary>
    /// Definition of an enum for the entity.
    /// </summary>
    public class EnumDefinition
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public List<string> Values { get; set; } = new();
        public string? DefaultValue { get; set; }
    }

    // ============================================================
    // WIZARD OPTIONS
    // ============================================================

    /// <summary>
    /// Options for code generation.
    /// </summary>
    public class EntityWizardOptions
    {
        /// <summary>Selected page pattern template</summary>
        public FMPagePattern SelectedPagePattern { get; set; } = FMPagePattern.CrudListEdit;

        /// <summary>When true: no Save/Delete endpoints, no Edit page.</summary>
        public bool IsReadOnly { get; set; } = false;

        /// <summary>Generate a paginated list page</summary>
        public bool GenerateListPage { get; set; } = true;

        /// <summary>Generate an edit/create form page</summary>
        public bool GenerateEditPage { get; set; } = true;

        /// <summary>Include soft delete (Deleted, DeletedAt fields)</summary>
        public bool IncludeSoftDelete { get; set; } = true;

        /// <summary>Include audit fields (CreatedAt, UpdatedAt, etc.)</summary>
        public bool IncludeAuditFields { get; set; } = true;

        /// <summary>Default page size for list pagination</summary>
        public int DefaultPageSize { get; set; } = 25;

        /// <summary>Generate SignalR update calls</summary>
        public bool GenerateSignalRUpdates { get; set; } = true;

        /// <summary>Include TenantId for multi-tenant support</summary>
        public bool IncludeTenantId { get; set; } = true;
    }

    /// <summary>
    /// Information about a generated file.
    /// </summary>
    public class GeneratedFileInfo
    {
        public string FileName { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        /// <summary>Original content before any manual edits</summary>
        public string OriginalContent { get; set; } = string.Empty;

        /// <summary>True if content has been manually edited</summary>
        public bool IsEdited { get; set; } = false;

        /// <summary>Monaco editor language for syntax highlighting</summary>
        public string Language => FileType switch {
            "EFModel" or "DataObjects" or "DataAccess" or "Controller" => "csharp",
            "RazorPage" or "RazorComponent" => "razor",
            "Stylesheet" => "css",
            _ => "plaintext"
        };
    }

    // ============================================================
    // WIZARD UI HELPER TYPES
    // ============================================================

    /// <summary>Step information for wizard stepper display.</summary>
    public class WizardStepInfo
    {
        public int StepNumber { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ShortName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public bool IsOptional { get; set; }
        public string? SelectedValue { get; set; }
    }

    /// <summary>Selection item for wizard summary display.</summary>
    public class WizardSelectionItem
    {
        public string Label { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }

    /// <summary>Saved entity template for reuse.</summary>
    public class SavedEntityTemplate
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TemplateId => Id;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = "fa-file-code";
        public bool IsBuiltIn { get; set; }
        public EntityDefinition Entity { get; set; } = new();
        public EntityWizardOptions Options { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    // ============================================================
    // DASHBOARD & STATS TYPES
    // ============================================================

    /// <summary>Entity list item for dashboard display.</summary>
    public class EntityListItem
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid EntityId { get => Id; set => Id = value; }
        public string Name { get; set; } = string.Empty;
        public string PluralName { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;
        public int PropertyCount { get; set; }
        public int EnumCount { get; set; }
        public int FileCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastGeneratedAt { get; set; }
        public string Status { get; set; } = "Draft";
        public bool HasAuditFields { get; set; }
        public bool HasSoftDelete { get; set; }
        public bool IsReadOnly { get; set; }
    }

    /// <summary>Entity builder statistics for dashboard.</summary>
    public class EntityBuilderStats
    {
        public int TotalEntities { get; set; }
        public int TotalProperties { get; set; }
        public int TotalEnums { get; set; }
        public int TotalFilesGenerated { get; set; }
        public int EntitiesThisWeek { get; set; }
        public DateTime? LastGeneratedAt { get; set; }
        public List<RecentEntityActivity> RecentActivity { get; set; } = new();
    }

    /// <summary>Activity item for dashboard.</summary>
    public class RecentEntityActivity
    {
        public string EntityName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Icon { get; set; } = "fa-circle";
        public string UserName { get; set; } = string.Empty;
    }

    // ============================================================
    // IMPORT TYPES
    // ============================================================

    /// <summary>Source type for entity import.</summary>
    public enum ImportSourceType
    {
        CSharpClass,
        JsonSchema,
        SqlTable,
        TypeScript
    }

    /// <summary>Result of an import operation.</summary>
    public class ImportResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? EntityName { get; set; }
        public EntityDefinition Entity { get; set; } = new();
        public List<PropertyDefinition> Properties { get; set; } = new();
        public List<EnumDefinition> Enums { get; set; } = new();
        public List<ImportWarning> Warnings { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public int ConfidenceScore { get; set; }
        public HashSet<string> ImportedFieldNames { get; set; } = new();
    }

    /// <summary>Warning from import parsing.</summary>
    public class ImportWarning
    {
        public string Message { get; set; } = string.Empty;
        public string? PropertyName { get; set; }
        public string? FieldName { get; set; }
        public string? Suggestion { get; set; }
        public string Severity { get; set; } = "Warning";
    }
}

#endregion
