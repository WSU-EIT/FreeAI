namespace FreeManager;

#region FreeManager Platform - Application Templates
// ┌─────────────────────────────────────────────────────────────────┐
// │        DataObjects.App.FreeManager.ApplicationTemplates.cs      │
// │           Pre-built Application Templates                        │
// ├─────────────────────────────────────────────────────────────────┤
// │ APPLICATION TEMPLATES                                           │
// │   ApplicationTemplate      → Complete app starter with entities │
// │   ApplicationTemplates.All → FreeBase, FreeTracker, FreeAudit   │
// ├─────────────────────────────────────────────────────────────────┤
// │ TEMPLATE PROGRESSION                                            │
// │   FreeBase    → Collection + Categories (foundation)            │
// │   FreeTracker → + Assignment + Checkout + Status                │
// │   FreeAudit   → + Logging + External API + Reports              │
// └─────────────────────────────────────────────────────────────────┘
// Part of: DataObjects.App.FreeManager (partial)
// ============================================================================

public partial class DataObjects
{
    // ============================================================
    // APPLICATION TEMPLATE
    // ============================================================

    /// <summary>
    /// A complete application template with multiple entities, relationships, and features.
    /// </summary>
    public class ApplicationTemplate
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ShortDescription { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string IconColor { get; set; } = "primary";
        public string Difficulty { get; set; } = "Beginner";
        public List<string> UseCases { get; set; } = new();
        public List<string> Features { get; set; } = new();
        public List<EntityDefinition> Entities { get; set; } = new();
        public List<RelationshipDefinition> Relationships { get; set; } = new();
        public EntityWizardOptions DefaultOptions { get; set; } = new();
        public bool IsImplemented { get; set; } = true;

        // Advanced generation configuration (for FreeAudit and similar)
        public ExternalApiConfig? ExternalApi { get; set; }
        public DashboardConfig? Dashboard { get; set; }

        // Helper to check if template has a feature
        public bool HasFeature(string feature) =>
            Features.Any(f => f.Equals(feature, StringComparison.OrdinalIgnoreCase));
    }

    // ============================================================
    // EXTERNAL API CONFIGURATION
    // ============================================================

    /// <summary>
    /// Configuration for generating external API endpoints (e.g., for FreeAudit).
    /// </summary>
    public class ExternalApiConfig
    {
        /// <summary>Name of the entity that receives external events.</summary>
        public string EntityName { get; set; } = string.Empty;

        /// <summary>Route prefix for the external API (e.g., "api/glba").</summary>
        public string RoutePrefix { get; set; } = "api/external";

        /// <summary>Controller class name (e.g., "Glba" generates GlbaController).</summary>
        public string ControllerName { get; set; } = "External";

        /// <summary>Authentication type: "ApiKey", "Bearer", or "None".</summary>
        public string AuthType { get; set; } = "ApiKey";

        /// <summary>Entity that stores API keys (typically "SourceSystem").</summary>
        public string ApiKeyEntity { get; set; } = "SourceSystem";

        /// <summary>Property on ApiKeyEntity that stores the hashed key.</summary>
        public string ApiKeyProperty { get; set; } = "ApiKey";

        /// <summary>Enable batch insert endpoint.</summary>
        public bool AllowBatch { get; set; } = true;

        /// <summary>Maximum events per batch request.</summary>
        public int BatchLimit { get; set; } = 1000;

        /// <summary>Property used for deduplication (e.g., "SourceEventId").</summary>
        public string? DedupeProperty { get; set; }

        /// <summary>Entity to update stats on each event (e.g., "DataSubject").</summary>
        public string? StatsEntity { get; set; }

        /// <summary>Property on StatsEntity to match (e.g., "ExternalId" matched to event's SubjectId).</summary>
        public string? StatsMatchProperty { get; set; }
    }

    // ============================================================
    // DASHBOARD CONFIGURATION
    // ============================================================

    /// <summary>
    /// Configuration for generating a dashboard page.
    /// </summary>
    public class DashboardConfig
    {
        /// <summary>Page name (e.g., "GlbaDashboard").</summary>
        public string PageName { get; set; } = "Dashboard";

        /// <summary>Page title shown in UI.</summary>
        public string PageTitle { get; set; } = "Dashboard";

        /// <summary>Icon for nav menu (FontAwesome class).</summary>
        public string Icon { get; set; } = "fa-chart-line";

        /// <summary>Entity to count for stats cards.</summary>
        public string StatsEntity { get; set; } = string.Empty;

        /// <summary>Date property on StatsEntity for time-based grouping.</summary>
        public string StatsDateProperty { get; set; } = "CreatedAt";

        /// <summary>Time periods for stats cards.</summary>
        public List<string> StatsPeriods { get; set; } = new() { "Today", "ThisWeek", "ThisMonth" };

        /// <summary>Entity for recent feed table.</summary>
        public string RecentFeedEntity { get; set; } = string.Empty;

        /// <summary>Number of items in recent feed.</summary>
        public int RecentFeedLimit { get; set; } = 20;

        /// <summary>Properties to show in recent feed columns.</summary>
        public List<string> RecentFeedColumns { get; set; } = new();

        /// <summary>Entity for status grid (e.g., source systems).</summary>
        public string? StatusGridEntity { get; set; }

        /// <summary>Property that indicates active status.</summary>
        public string StatusActiveProperty { get; set; } = "IsActive";

        /// <summary>Property that shows last activity timestamp.</summary>
        public string? StatusLastActivityProperty { get; set; }

        /// <summary>Property that shows count/total.</summary>
        public string? StatusCountProperty { get; set; }
    }

    // ============================================================
    // APPLICATION TEMPLATES CATALOG
    // ============================================================

    /// <summary>
    /// Catalog of pre-built application templates.
    /// </summary>
    public static class ApplicationTemplates
    {
        /// <summary>All available application templates.</summary>
        public static List<ApplicationTemplate> All => new()
        {
            FreeBase,
            FreeTracker,
            FreeAudit
        };

        /// <summary>Get template by ID.</summary>
        public static ApplicationTemplate? GetById(string id) =>
            All.FirstOrDefault(t => t.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// Converts an ApplicationTemplate to EntityWizardState for code generation.
        /// </summary>
        public static EntityWizardState ToWizardState(
            ApplicationTemplate template,
            string projectName,
            string? displayName = null,
            Dictionary<string, string>? entityRenames = null)
        {
            var state = new EntityWizardState
            {
                Project = new FMCreateProjectRequest
                {
                    Name = projectName,
                    DisplayName = displayName ?? projectName,
                    Template = FMProjectTemplate.FullCrud
                },
                Options = new EntityWizardOptions
                {
                    GenerateListPage = template.DefaultOptions.GenerateListPage,
                    GenerateEditPage = template.DefaultOptions.GenerateEditPage,
                    IncludeAuditFields = template.DefaultOptions.IncludeAuditFields,
                    IncludeSoftDelete = template.DefaultOptions.IncludeSoftDelete,
                    IncludeTenantId = template.DefaultOptions.IncludeTenantId,
                    GenerateSignalRUpdates = template.DefaultOptions.GenerateSignalRUpdates
                },
                Entities = new List<EntityDefinition>(),
                Relationships = new List<RelationshipDefinition>()
            };

            // Copy entities, applying renames if specified
            foreach (var entity in template.Entities)
            {
                var newName = entity.Name;
                if (entityRenames != null && entityRenames.TryGetValue(entity.Name, out var rename) && !string.IsNullOrWhiteSpace(rename))
                {
                    newName = rename;
                }

                var newEntity = new EntityDefinition
                {
                    Id = Guid.NewGuid(),
                    Name = newName,
                    PluralName = newName + "s",
                    TableName = newName + "s",
                    PrimaryKeyName = newName + "Id",
                    PrimaryKeyType = entity.PrimaryKeyType,
                    Description = entity.Description,
                    SortOrder = entity.SortOrder,
                    Properties = entity.Properties.Select(p => new PropertyDefinition
                    {
                        Id = Guid.NewGuid(),
                        Name = p.Name,
                        Type = p.Type,
                        IsRequired = p.IsRequired,
                        IsNullable = p.IsNullable,
                        DefaultValue = p.DefaultValue,
                        MaxLength = p.MaxLength,
                        SortOrder = p.SortOrder,
                        Description = p.Description,
                        IsFilterable = p.IsFilterable
                    }).ToList()
                };

                state.Entities.Add(newEntity);
            }

            // Copy relationships, updating entity names if renamed
            foreach (var rel in template.Relationships)
            {
                var sourceEntityName = rel.SourceEntityName;
                var targetEntityName = rel.TargetEntityName;

                if (entityRenames != null)
                {
                    if (entityRenames.TryGetValue(rel.SourceEntityName, out var sourceRename) && !string.IsNullOrWhiteSpace(sourceRename))
                        sourceEntityName = sourceRename;
                    if (entityRenames.TryGetValue(rel.TargetEntityName, out var targetRename) && !string.IsNullOrWhiteSpace(targetRename))
                        targetEntityName = targetRename;
                }

                state.Relationships.Add(new RelationshipDefinition
                {
                    Id = Guid.NewGuid(),
                    Name = $"{sourceEntityName}_{targetEntityName}",
                    Type = rel.Type,
                    SourceEntityName = sourceEntityName,
                    TargetEntityName = targetEntityName,
                    SourceNavigationProperty = rel.SourceNavigationProperty,
                    TargetNavigationProperty = rel.TargetNavigationProperty,
                    ForeignKeyProperty = rel.ForeignKeyProperty,
                    IsRequired = rel.IsRequired,
                    OnDelete = rel.OnDelete,
                    SortOrder = rel.SortOrder
                });
            }

            // Copy advanced generation configs (for FreeAudit-style templates)
            if (template.ExternalApi != null)
            {
                state.ExternalApi = new ExternalApiConfig
                {
                    EntityName = template.ExternalApi.EntityName,
                    RoutePrefix = template.ExternalApi.RoutePrefix,
                    ControllerName = template.ExternalApi.ControllerName,
                    AuthType = template.ExternalApi.AuthType,
                    ApiKeyEntity = template.ExternalApi.ApiKeyEntity,
                    ApiKeyProperty = template.ExternalApi.ApiKeyProperty,
                    AllowBatch = template.ExternalApi.AllowBatch,
                    BatchLimit = template.ExternalApi.BatchLimit,
                    DedupeProperty = template.ExternalApi.DedupeProperty,
                    StatsEntity = template.ExternalApi.StatsEntity,
                    StatsMatchProperty = template.ExternalApi.StatsMatchProperty
                };
            }

            if (template.Dashboard != null)
            {
                state.Dashboard = new DashboardConfig
                {
                    PageName = template.Dashboard.PageName,
                    PageTitle = template.Dashboard.PageTitle,
                    Icon = template.Dashboard.Icon,
                    StatsEntity = template.Dashboard.StatsEntity,
                    StatsDateProperty = template.Dashboard.StatsDateProperty,
                    StatsPeriods = new List<string>(template.Dashboard.StatsPeriods),
                    RecentFeedEntity = template.Dashboard.RecentFeedEntity,
                    RecentFeedLimit = template.Dashboard.RecentFeedLimit,
                    RecentFeedColumns = new List<string>(template.Dashboard.RecentFeedColumns),
                    StatusGridEntity = template.Dashboard.StatusGridEntity,
                    StatusActiveProperty = template.Dashboard.StatusActiveProperty,
                    StatusLastActivityProperty = template.Dashboard.StatusLastActivityProperty,
                    StatusCountProperty = template.Dashboard.StatusCountProperty
                };
            }

            return state;
        }

        // ============================================================
        // FREEBASE - Collection of Things
        // ============================================================

        public static ApplicationTemplate FreeBase => new()
        {
            Id = "freebase",
            Name = "FreeBase",
            Description = "Simple collection management with items and categories. Perfect for catalogs, inventories, and contact lists.",
            ShortDescription = "Collection of things",
            Icon = "fa-box",
            IconColor = "primary",
            Difficulty = "Beginner",
            UseCases = new() { "Product catalog", "Contact list", "Inventory", "Asset registry" },
            Features = new() { "Items + Categories", "Basic CRUD operations", "List + Edit pages" },
            Entities = new()
            {
                // Category entity
                new EntityDefinition
                {
                    Id = Guid.Parse("10000001-0001-0001-0001-000000000001"),
                    Name = "Category",
                    PluralName = "Categories",
                    TableName = "Categories",
                    PrimaryKeyName = "CategoryId",
                    PrimaryKeyType = "Guid",
                    Description = "Organize items into groups",
                    SortOrder = 1,
                    Properties = new()
                    {
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "Name", Type = "string", IsRequired = true, MaxLength = 200, SortOrder = 1 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "Description", Type = "string", IsRequired = false, MaxLength = 500, SortOrder = 2 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "SortOrder", Type = "int", IsRequired = false, DefaultValue = "0", SortOrder = 3 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "IsActive", Type = "bool", IsRequired = false, DefaultValue = "true", SortOrder = 4 }
                    }
                },
                // Item entity
                new EntityDefinition
                {
                    Id = Guid.Parse("10000001-0001-0001-0001-000000000002"),
                    Name = "Item",
                    PluralName = "Items",
                    TableName = "Items",
                    PrimaryKeyName = "ItemId",
                    PrimaryKeyType = "Guid",
                    Description = "Main entity for your collection",
                    SortOrder = 2,
                    Properties = new()
                    {
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "CategoryId", Type = "Guid?", IsRequired = false, IsNullable = true, SortOrder = 1, Description = "FK to Category" },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "Name", Type = "string", IsRequired = true, MaxLength = 200, SortOrder = 2 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "Description", Type = "string", IsRequired = false, MaxLength = 2000, SortOrder = 3 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "IsActive", Type = "bool", IsRequired = false, DefaultValue = "true", SortOrder = 4 }
                    }
                }
            },
            Relationships = new()
            {
                new RelationshipDefinition
                {
                    Id = Guid.NewGuid(),
                    Name = "Category_Items",
                    Type = RelationshipType.OneToMany,
                    SourceEntityName = "Category",
                    TargetEntityName = "Item",
                    SourceNavigationProperty = "Items",
                    TargetNavigationProperty = "Category",
                    ForeignKeyProperty = "CategoryId",
                    IsRequired = false,
                    OnDelete = RelationshipDeleteBehavior.SetNull
                }
            },
            DefaultOptions = new()
            {
                GenerateListPage = true,
                GenerateEditPage = true,
                IncludeAuditFields = true,
                IncludeSoftDelete = true,
                IncludeTenantId = true,
                GenerateSignalRUpdates = true
            }
        };

        // ============================================================
        // FREETRACKER - Assignment + Checkout + Status
        // ============================================================

        public static ApplicationTemplate FreeTracker => new()
        {
            Id = "freetracker",
            Name = "FreeTracker",
            Description = "Track assignments, checkouts, and status workflows. Great for helpdesk, equipment loans, and task management.",
            ShortDescription = "+ Assignment + Checkout + Status",
            Icon = "fa-clipboard-list",
            IconColor = "warning",
            Difficulty = "Intermediate",
            UseCases = new() { "Helpdesk tickets", "Equipment loans", "Task management", "Library checkouts" },
            Features = new()
            {
                "Everything in FreeBase",
                "Assign items to users",
                "Checkout/return tracking",
                "Status workflow",
                "Due date notifications"
            },
            Entities = new()
            {
                // Category (inherited from FreeBase)
                new EntityDefinition
                {
                    Id = Guid.Parse("10000002-0001-0001-0001-000000000001"),
                    Name = "Category",
                    PluralName = "Categories",
                    PrimaryKeyType = "Guid",
                    Description = "Organize items into groups",
                    SortOrder = 1,
                    Properties = new()
                    {
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "Name", Type = "string", IsRequired = true, MaxLength = 200, SortOrder = 1 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "Description", Type = "string", IsRequired = false, SortOrder = 2 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "SortOrder", Type = "int", IsRequired = false, DefaultValue = "0", SortOrder = 3 }
                    }
                },
                // Status lookup
                new EntityDefinition
                {
                    Id = Guid.Parse("10000002-0001-0001-0001-000000000002"),
                    Name = "Status",
                    PluralName = "Statuses",
                    PrimaryKeyType = "Guid",
                    Description = "Workflow states for items",
                    SortOrder = 2,
                    Properties = new()
                    {
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "Name", Type = "string", IsRequired = true, MaxLength = 100, SortOrder = 1 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "SortOrder", Type = "int", IsRequired = false, DefaultValue = "0", SortOrder = 2 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "IsDefault", Type = "bool", IsRequired = false, DefaultValue = "false", SortOrder = 3 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "IsCompleted", Type = "bool", IsRequired = false, DefaultValue = "false", SortOrder = 4 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "Color", Type = "string", IsRequired = false, MaxLength = 50, SortOrder = 5 }
                    }
                },
                // Item (extended from FreeBase)
                new EntityDefinition
                {
                    Id = Guid.Parse("10000002-0001-0001-0001-000000000003"),
                    Name = "Item",
                    PluralName = "Items",
                    PrimaryKeyType = "Guid",
                    Description = "Main entity with status tracking",
                    SortOrder = 3,
                    Properties = new()
                    {
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "CategoryId", Type = "Guid?", IsRequired = false, IsNullable = true, SortOrder = 1 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "StatusId", Type = "Guid?", IsRequired = false, IsNullable = true, SortOrder = 2 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "Name", Type = "string", IsRequired = true, MaxLength = 200, SortOrder = 3 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "Description", Type = "string", IsRequired = false, MaxLength = 2000, SortOrder = 4 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "IsActive", Type = "bool", IsRequired = false, DefaultValue = "true", SortOrder = 5 }
                    }
                },
                // Assignment
                new EntityDefinition
                {
                    Id = Guid.Parse("10000002-0001-0001-0001-000000000004"),
                    Name = "Assignment",
                    PluralName = "Assignments",
                    PrimaryKeyType = "Guid",
                    Description = "Track who is responsible for items",
                    SortOrder = 4,
                    Properties = new()
                    {
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "ItemId", Type = "Guid", IsRequired = true, SortOrder = 1 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "AssignedToUserId", Type = "Guid", IsRequired = true, SortOrder = 2 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "AssignedByUserId", Type = "Guid", IsRequired = true, SortOrder = 3 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "AssignedDate", Type = "DateTime", IsRequired = true, SortOrder = 4 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "DueDate", Type = "DateTime?", IsRequired = false, IsNullable = true, SortOrder = 5 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "CompletedDate", Type = "DateTime?", IsRequired = false, IsNullable = true, SortOrder = 6 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "Notes", Type = "string", IsRequired = false, MaxLength = 2000, SortOrder = 7 }
                    }
                },
                // CheckoutRecord
                new EntityDefinition
                {
                    Id = Guid.Parse("10000002-0001-0001-0001-000000000005"),
                    Name = "CheckoutRecord",
                    PluralName = "CheckoutRecords",
                    PrimaryKeyType = "Guid",
                    Description = "Track borrowing/checkout history",
                    SortOrder = 5,
                    Properties = new()
                    {
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "ItemId", Type = "Guid", IsRequired = true, SortOrder = 1 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "CheckedOutToUserId", Type = "Guid", IsRequired = true, SortOrder = 2 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "CheckedOutByUserId", Type = "Guid", IsRequired = true, SortOrder = 3 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "CheckedOutDate", Type = "DateTime", IsRequired = true, SortOrder = 4 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "DueDate", Type = "DateTime?", IsRequired = false, IsNullable = true, SortOrder = 5 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "ReturnedDate", Type = "DateTime?", IsRequired = false, IsNullable = true, SortOrder = 6 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "Notes", Type = "string", IsRequired = false, MaxLength = 2000, SortOrder = 7 }
                    }
                }
            },
            Relationships = new()
            {
                new RelationshipDefinition { Id = Guid.NewGuid(), Name = "Category_Items", Type = RelationshipType.OneToMany, SourceEntityName = "Category", TargetEntityName = "Item", ForeignKeyProperty = "CategoryId", IsRequired = false },
                new RelationshipDefinition { Id = Guid.NewGuid(), Name = "Status_Items", Type = RelationshipType.OneToMany, SourceEntityName = "Status", TargetEntityName = "Item", ForeignKeyProperty = "StatusId", IsRequired = false },
                new RelationshipDefinition { Id = Guid.NewGuid(), Name = "Item_Assignments", Type = RelationshipType.OneToMany, SourceEntityName = "Item", TargetEntityName = "Assignment", ForeignKeyProperty = "ItemId", IsRequired = true },
                new RelationshipDefinition { Id = Guid.NewGuid(), Name = "Item_Checkouts", Type = RelationshipType.OneToMany, SourceEntityName = "Item", TargetEntityName = "CheckoutRecord", ForeignKeyProperty = "ItemId", IsRequired = true }
            },
            DefaultOptions = new()
            {
                GenerateListPage = true,
                GenerateEditPage = true,
                IncludeAuditFields = true,
                IncludeSoftDelete = true,
                IncludeTenantId = true,
                GenerateSignalRUpdates = true
            }
        };

        // ============================================================
        // FREEAUDIT - Logging + External API + Reports
        // ============================================================

        public static ApplicationTemplate FreeAudit => new()
        {
            Id = "freeaudit",
            Name = "FreeAudit",
            Description = "Full GLBA compliance tracking with external API, event logging, and reports. Built for audit trails and regulatory compliance.",
            ShortDescription = "+ Compliance + Logging + Reports",
            Icon = "fa-shield-halved",
            IconColor = "danger",
            Difficulty = "Advanced",
            UseCases = new() { "GLBA compliance", "Audit trails", "Access logging", "Regulatory reporting" },
            Features = new()
            {
                "External API endpoint",
                "API key authentication",
                "Access event logging",
                "Compliance dashboard",
                "Report generation",
                "Subject lookup"
            },

            // External API configuration for receiving events from source systems
            ExternalApi = new ExternalApiConfig
            {
                EntityName = "AccessEvent",
                RoutePrefix = "api/glba",
                ControllerName = "Glba",
                AuthType = "ApiKey",
                ApiKeyEntity = "SourceSystem",
                ApiKeyProperty = "ApiKey",
                AllowBatch = true,
                BatchLimit = 1000,
                DedupeProperty = "SourceEventId",
                StatsEntity = "DataSubject",
                StatsMatchProperty = "ExternalId"
            },

            // Dashboard configuration for GLBA compliance view
            Dashboard = new DashboardConfig
            {
                PageName = "GlbaDashboard",
                PageTitle = "GLBA Compliance Dashboard",
                Icon = "fa-shield-halved",
                StatsEntity = "AccessEvent",
                StatsDateProperty = "AccessedAt",
                StatsPeriods = new() { "Today", "ThisWeek", "ThisMonth" },
                RecentFeedEntity = "AccessEvent",
                RecentFeedLimit = 20,
                RecentFeedColumns = new() { "AccessedAt", "UserId", "SubjectId", "DataCategory", "AccessType" },
                StatusGridEntity = "SourceSystem",
                StatusActiveProperty = "IsActive",
                StatusLastActivityProperty = "LastEventReceivedAt",
                StatusCountProperty = "EventCount"
            },
            Entities = new()
            {
                // SourceSystem
                new EntityDefinition
                {
                    Id = Guid.Parse("10000003-0001-0001-0001-000000000001"),
                    Name = "SourceSystem",
                    PluralName = "SourceSystems",
                    PrimaryKeyType = "Guid",
                    Description = "Systems that send events to FreeAudit",
                    SortOrder = 1,
                    Properties = new()
                    {
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "Name", Type = "string", IsRequired = true, MaxLength = 200, SortOrder = 1 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "DisplayName", Type = "string", IsRequired = false, MaxLength = 200, SortOrder = 2 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "ApiKey", Type = "string", IsRequired = true, MaxLength = 500, SortOrder = 3, Description = "Hashed API key" },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "ContactEmail", Type = "string", IsRequired = false, MaxLength = 200, SortOrder = 4 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "IsActive", Type = "bool", IsRequired = false, DefaultValue = "true", SortOrder = 5 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "LastEventReceivedAt", Type = "DateTime?", IsRequired = false, IsNullable = true, SortOrder = 6 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "EventCount", Type = "long", IsRequired = false, DefaultValue = "0", SortOrder = 7 }
                    }
                },
                // AccessEvent
                new EntityDefinition
                {
                    Id = Guid.Parse("10000003-0001-0001-0001-000000000002"),
                    Name = "AccessEvent",
                    PluralName = "AccessEvents",
                    PrimaryKeyType = "Guid",
                    Description = "Individual access log entries",
                    SortOrder = 2,
                    Properties = new()
                    {
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "SourceSystemId", Type = "Guid", IsRequired = true, SortOrder = 1 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "SourceEventId", Type = "string", IsRequired = false, MaxLength = 200, SortOrder = 2, Description = "Dedupe key from source" },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "AccessedAt", Type = "DateTime", IsRequired = true, SortOrder = 3 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "ReceivedAt", Type = "DateTime", IsRequired = true, SortOrder = 4 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "UserId", Type = "string", IsRequired = true, MaxLength = 200, SortOrder = 5 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "UserName", Type = "string", IsRequired = false, MaxLength = 200, SortOrder = 6 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "UserEmail", Type = "string", IsRequired = false, MaxLength = 200, SortOrder = 7 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "UserDepartment", Type = "string", IsRequired = false, MaxLength = 200, SortOrder = 8 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "SubjectId", Type = "string", IsRequired = true, MaxLength = 200, SortOrder = 9, Description = "Student/employee ID" },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "SubjectType", Type = "string", IsRequired = false, MaxLength = 50, SortOrder = 10 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "DataCategory", Type = "string", IsRequired = false, MaxLength = 100, SortOrder = 11 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "AccessType", Type = "string", IsRequired = true, MaxLength = 50, SortOrder = 12 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "Purpose", Type = "string", IsRequired = false, MaxLength = 500, SortOrder = 13 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "IpAddress", Type = "string", IsRequired = false, MaxLength = 50, SortOrder = 14 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "AdditionalData", Type = "string", IsRequired = false, SortOrder = 15, Description = "JSON for flexible extras" }
                    }
                },
                // DataSubject
                new EntityDefinition
                {
                    Id = Guid.Parse("10000003-0001-0001-0001-000000000003"),
                    Name = "DataSubject",
                    PluralName = "DataSubjects",
                    PrimaryKeyType = "Guid",
                    Description = "Aggregated stats per subject (student/employee)",
                    SortOrder = 3,
                    Properties = new()
                    {
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "ExternalId", Type = "string", IsRequired = true, MaxLength = 200, SortOrder = 1 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "SubjectType", Type = "string", IsRequired = false, MaxLength = 50, SortOrder = 2 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "FirstAccessedAt", Type = "DateTime", IsRequired = true, SortOrder = 3 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "LastAccessedAt", Type = "DateTime", IsRequired = true, SortOrder = 4 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "TotalAccessCount", Type = "long", IsRequired = false, DefaultValue = "0", SortOrder = 5 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "UniqueAccessorCount", Type = "int", IsRequired = false, DefaultValue = "0", SortOrder = 6 }
                    }
                },
                // ComplianceReport
                new EntityDefinition
                {
                    Id = Guid.Parse("10000003-0001-0001-0001-000000000004"),
                    Name = "ComplianceReport",
                    PluralName = "ComplianceReports",
                    PrimaryKeyType = "Guid",
                    Description = "Generated compliance reports",
                    SortOrder = 4,
                    Properties = new()
                    {
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "ReportType", Type = "string", IsRequired = true, MaxLength = 50, SortOrder = 1 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "GeneratedAt", Type = "DateTime", IsRequired = true, SortOrder = 2 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "GeneratedBy", Type = "string", IsRequired = false, MaxLength = 200, SortOrder = 3 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "PeriodStart", Type = "DateTime", IsRequired = true, SortOrder = 4 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "PeriodEnd", Type = "DateTime", IsRequired = true, SortOrder = 5 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "TotalEvents", Type = "long", IsRequired = false, DefaultValue = "0", SortOrder = 6 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "UniqueUsers", Type = "int", IsRequired = false, DefaultValue = "0", SortOrder = 7 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "UniqueSubjects", Type = "int", IsRequired = false, DefaultValue = "0", SortOrder = 8 },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "ReportData", Type = "string", IsRequired = false, SortOrder = 9, Description = "JSON report content" },
                        new PropertyDefinition { Id = Guid.NewGuid(), Name = "FileUrl", Type = "string", IsRequired = false, MaxLength = 500, SortOrder = 10 }
                    }
                }
            },
            Relationships = new()
            {
                new RelationshipDefinition { Id = Guid.NewGuid(), Name = "SourceSystem_Events", Type = RelationshipType.OneToMany, SourceEntityName = "SourceSystem", TargetEntityName = "AccessEvent", ForeignKeyProperty = "SourceSystemId", IsRequired = true }
            },
            DefaultOptions = new()
            {
                GenerateListPage = true,
                GenerateEditPage = true,
                IncludeAuditFields = true,
                IncludeSoftDelete = false, // Audit logs shouldn't be soft-deleted
                IncludeTenantId = true,
                GenerateSignalRUpdates = true
            }
        };
    }
}

#endregion
