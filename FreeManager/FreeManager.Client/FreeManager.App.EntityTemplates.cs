using System.Text;

namespace FreeManager;

// ---- EntityTemplates ----

#region EntityTemplates - Entity Wizard Code Generation (Coordinator)
// ============================================================================
// ENTITY BUILDER WIZARD - CODE GENERATION TEMPLATES
// This is the coordinator file for Entity Wizard template generation.
// All generators are split into feature-specific partial files:
//
// - EntityTemplates.EFModel section      → EF Model generation
// - EntityTemplates.DataObjects section  → DTOs, filters, enums
// - EntityTemplates.DataAccess section   → Data access layer
// - EntityTemplates.Controller section   → REST API endpoints
// - EntityTemplates.RazorPages section   → List and Edit Blazor pages
//
// This file contains the orchestrator and shared helpers.
// ============================================================================

/// <summary>
/// Entity code generation templates for the Entity Builder Wizard.
/// Generates EF Models, DataObjects, DataAccess, Controllers, and Blazor pages.
/// Supports multiple entities with relationships.
/// </summary>
public static partial class EntityTemplates
{
    /// <summary>
    /// Generates all files for all entities based on wizard state.
    /// Supports multi-entity projects with relationships.
    /// </summary>
    public static List<DataObjects.GeneratedFileInfo> GenerateAllFiles(DataObjects.EntityWizardState state)
    {
        var files = new List<DataObjects.GeneratedFileInfo>();
        var options = state.Options;
        var projectName = state.Project.Name;
        var entities = state.Entities;
        var relationships = state.Relationships;

        // For backward compatibility - if Entities is empty, use single Entity
        if (entities.Count == 0 && !string.IsNullOrEmpty(state.Entity?.Name))
        {
            entities = new List<DataObjects.EntityDefinition> { state.Entity };
        }

        // Auto-generate join entities for M:M relationships (W-016)
        var joinEntities = GenerateJoinEntitiesForManyToMany(relationships, entities);
        var allEntities = entities.Concat(joinEntities).ToList();

        // Generate files for each entity (including auto-generated join tables)
        foreach (var entity in allEntities)
        {
            // 1. EF Model - one per entity
            var efContent = GenerateEFModel(entity, relationships, options, projectName);
            files.Add(new DataObjects.GeneratedFileInfo
            {
                FileName = $"{projectName}.App.{entity.Name}.cs",
                FileType = DataObjects.FMFileTypes.EFModel,
                Description = entity.IsJoinTable
                    ? $"Join table for {entity.Description ?? "many-to-many relationship"}"
                    : $"Entity Framework model for {entity.Name}",
                Content = efContent,
                OriginalContent = efContent
            });

            // 5. List Page (optional) - one per entity (skip join tables)
            if (options.GenerateListPage && !entity.IsJoinTable)
            {
                var listContent = GenerateListPage(entity, relationships, allEntities, options, projectName);
                files.Add(new DataObjects.GeneratedFileInfo
                {
                    FileName = $"{projectName}.App.{entity.PluralName}Page.razor",
                    FileType = DataObjects.FMFileTypes.RazorPage,
                    Description = $"{entity.PluralName} list page",
                    Content = listContent,
                    OriginalContent = listContent
                });
            }

            // 6. Edit Page (optional) - one per entity (skip join tables)
            if (options.GenerateEditPage && !options.IsReadOnly && !entity.IsJoinTable)
            {
                var editContent = GenerateEditPage(entity, relationships, allEntities, options, projectName);
                files.Add(new DataObjects.GeneratedFileInfo
                {
                    FileName = $"{projectName}.App.Edit{entity.Name}.razor",
                    FileType = DataObjects.FMFileTypes.RazorComponent,
                    Description = $"Edit/create {entity.Name} form",
                    Content = editContent,
                    OriginalContent = editContent
                });
            }
        }

        // 2. DataObjects - combined for all entities (excluding join tables from main DTOs)
        var dataObjectsContent = GenerateDataObjectsMulti(entities, relationships, options, projectName);
        files.Add(new DataObjects.GeneratedFileInfo
        {
            FileName = $"{projectName}.App.DataObjects.cs",
            FileType = DataObjects.FMFileTypes.DataObjects,
            Description = $"DTOs, enums, and filter classes for {entities.Count} entities",
            Content = dataObjectsContent,
            OriginalContent = dataObjectsContent
        });

        // 3. DataAccess - combined for all entities
        var dataAccessContent = GenerateDataAccessMulti(entities, relationships, options, projectName);
        files.Add(new DataObjects.GeneratedFileInfo
        {
            FileName = $"{projectName}.App.DataAccess.cs",
            FileType = DataObjects.FMFileTypes.DataAccess,
            Description = $"Data access methods for {entities.Count} entities",
            Content = dataAccessContent,
            OriginalContent = dataAccessContent
        });

        // 4. Controller - combined for all entities
        var controllerContent = GenerateControllerMulti(entities, relationships, options, projectName);
        files.Add(new DataObjects.GeneratedFileInfo
        {
            FileName = $"{projectName}.App.DataController.cs",
            FileType = DataObjects.FMFileTypes.Controller,
            Description = $"REST API endpoints for {entities.Count} entities",
            Content = controllerContent,
            OriginalContent = controllerContent
        });

        // 5. Endpoints - API route constants for all entities
        var endpointsContent = GenerateEndpoints(entities, options, projectName);
        files.Add(new DataObjects.GeneratedFileInfo
        {
            FileName = $"{projectName}.App.Endpoints.cs",
            FileType = DataObjects.FMFileTypes.DataObjects,
            Description = $"API endpoint route constants for {entities.Count} entities",
            Content = endpointsContent,
            OriginalContent = endpointsContent
        });

        // 6. EFDataModel partial - DbSet properties for new entities
        var efDataModelContent = GenerateEFDataModelPartial(allEntities, projectName);
        files.Add(new DataObjects.GeneratedFileInfo
        {
            FileName = $"{projectName}.App.EFDataModel.cs",
            FileType = DataObjects.FMFileTypes.EFDataModel,
            Description = $"DbSet properties for {allEntities.Count} entities",
            Content = efDataModelContent,
            OriginalContent = efDataModelContent
        });

        // 7. IDataAccess interface partial - method signatures
        var iDataAccessContent = GenerateIDataAccessPartial(entities, options, projectName);
        files.Add(new DataObjects.GeneratedFileInfo
        {
            FileName = $"{projectName}.App.IDataAccess.cs",
            FileType = DataObjects.FMFileTypes.DataAccess,
            Description = $"Interface method signatures for {entities.Count} entities",
            Content = iDataAccessContent,
            OriginalContent = iDataAccessContent
        });

        // ============================================================
        // ADVANCED GENERATION (FreeAudit-style templates)
        // ============================================================

        // Generate External API files if configured
        if (state.ExternalApi != null)
        {
            var apiConfig = state.ExternalApi;
            var eventEntity = allEntities.FirstOrDefault(e => e.Name == apiConfig.EntityName);
            var apiKeyEntity = allEntities.FirstOrDefault(e => e.Name == apiConfig.ApiKeyEntity);

            if (eventEntity != null)
            {
                // External API Controller (NO DTOs - those are separate)
                var externalApiContent = GenerateExternalApiController(apiConfig, eventEntity, apiKeyEntity, projectName);
                files.Add(new DataObjects.GeneratedFileInfo
                {
                    FileName = $"{projectName}.App.{apiConfig.ControllerName}Controller.cs",
                    FileType = DataObjects.FMFileTypes.Controller,
                    Description = $"External API controller for {apiConfig.EntityName} events",
                    Content = externalApiContent,
                    OriginalContent = externalApiContent
                });

                // External API DataObjects (DTOs in separate file)
                var externalApiDataObjects = GenerateExternalApiDataObjects(apiConfig, eventEntity, projectName);
                files.Add(new DataObjects.GeneratedFileInfo
                {
                    FileName = $"{projectName}.App.DataObjects.ExternalApi.cs",
                    FileType = DataObjects.FMFileTypes.DataObjects,
                    Description = $"DTOs for {apiConfig.ControllerName} external API",
                    Content = externalApiDataObjects,
                    OriginalContent = externalApiDataObjects
                });

                // External API DataAccess methods
                var externalApiDataAccess = GenerateExternalApiDataAccess(apiConfig, eventEntity, projectName);
                files.Add(new DataObjects.GeneratedFileInfo
                {
                    FileName = $"{projectName}.App.DataAccess.ExternalApi.cs",
                    FileType = DataObjects.FMFileTypes.DataAccess,
                    Description = $"Data access for external API event processing",
                    Content = externalApiDataAccess,
                    OriginalContent = externalApiDataAccess
                });

                // API Key Middleware
                if (apiConfig.AuthType == "ApiKey" && apiKeyEntity != null)
                {
                    var middlewareContent = GenerateApiKeyMiddleware(apiConfig, projectName);
                    files.Add(new DataObjects.GeneratedFileInfo
                    {
                        FileName = $"{projectName}.App.ApiKeyMiddleware.cs",
                        FileType = DataObjects.FMFileTypes.Middleware,
                        Description = "API key authentication middleware",
                        Content = middlewareContent,
                        OriginalContent = middlewareContent
                    });

                    // API Key DataAccess methods
                    var apiKeyDataAccess = GenerateApiKeyDataAccess(apiConfig, apiKeyEntity, projectName);
                    files.Add(new DataObjects.GeneratedFileInfo
                    {
                        FileName = $"{projectName}.App.DataAccess.ApiKey.cs",
                        FileType = DataObjects.FMFileTypes.DataAccess,
                        Description = "API key validation and generation",
                        Content = apiKeyDataAccess,
                        OriginalContent = apiKeyDataAccess
                    });

                    // Program.cs snippet
                    var programSnippet = GenerateProgramCsSnippet(apiConfig, projectName);
                    files.Add(new DataObjects.GeneratedFileInfo
                    {
                        FileName = $"{projectName}.App.Program.snippet",
                        FileType = DataObjects.FMFileTypes.Snippet,
                        Description = "Add to Program.cs for middleware registration",
                        Content = programSnippet,
                        OriginalContent = programSnippet
                    });

                    // API Key UI section
                    var apiKeyUi = GenerateApiKeyEditSection(apiConfig, projectName);
                    files.Add(new DataObjects.GeneratedFileInfo
                    {
                        FileName = $"{projectName}.App.Edit{apiConfig.ApiKeyEntity}.ApiKey.snippet",
                        FileType = DataObjects.FMFileTypes.Snippet,
                        Description = $"API key generation UI for {apiConfig.ApiKeyEntity} edit page",
                        Content = apiKeyUi,
                        OriginalContent = apiKeyUi
                    });
                }
            }
        }

        // Generate Dashboard page if configured
        if (state.Dashboard != null)
        {
            var dashboardContent = GenerateDashboardPage(state.Dashboard, state.ExternalApi, allEntities, projectName);
            files.Add(new DataObjects.GeneratedFileInfo
            {
                FileName = $"{projectName}.App.{state.Dashboard.PageName}.razor",
                FileType = DataObjects.FMFileTypes.RazorPage,
                Description = state.Dashboard.PageTitle,
                Content = dashboardContent,
                OriginalContent = dashboardContent
            });
        }

        return files;
    }

    // ============================================================
    // AUTO-GENERATE JOIN ENTITIES FOR M:M RELATIONSHIPS (W-016)
    // ============================================================

    /// <summary>
    /// Creates join table entities for Many-to-Many relationships.
    /// Each join table has two FK properties pointing to the related entities.
    /// </summary>
    private static List<DataObjects.EntityDefinition> GenerateJoinEntitiesForManyToMany(
        List<DataObjects.RelationshipDefinition> relationships,
        List<DataObjects.EntityDefinition> existingEntities){
        var joinEntities = new List<DataObjects.EntityDefinition>();

        foreach (var rel in relationships.Where(r => r.Type == DataObjects.RelationshipType.ManyToMany))
        {
            var joinName = rel.JoinEntityName ?? $"{rel.SourceEntityName}{rel.TargetEntityName}Link";

            // Skip if user already manually defined the join entity
            if (existingEntities.Any(e => e.Name == joinName))
                continue;

            // Get PK types from source and target entities
            var sourceEntity = existingEntities.FirstOrDefault(e => e.Name == rel.SourceEntityName);
            var targetEntity = existingEntities.FirstOrDefault(e => e.Name == rel.TargetEntityName);
            var sourcePkType = sourceEntity?.Properties.FirstOrDefault(p => p.IsPrimaryKey)?.Type ?? "Guid";
            var targetPkType = targetEntity?.Properties.FirstOrDefault(p => p.IsPrimaryKey)?.Type ?? "Guid";

            var joinEntity = new DataObjects.EntityDefinition
            {
                Id = Guid.NewGuid(),
                Name = joinName,
                PluralName = joinName + "s",
                TableName = joinName + "s",
                IsJoinTable = true,
                Description = $"{rel.SourceEntityName} ↔ {rel.TargetEntityName}",
                Properties = new List<DataObjects.PropertyDefinition>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        Name = $"{joinName}Id",
                        Type = "Guid",
                        IsPrimaryKey = true,
                        IsRequired = true,
                        SortOrder = 0,
                        DefaultValue = "Guid.NewGuid()"
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        Name = $"{rel.SourceEntityName}Id",
                        Type = sourcePkType,
                        IsRequired = true,
                        SortOrder = 1,
                        Description = $"FK to {rel.SourceEntityName}"
                    },
                    new()
                    {
                        Id = Guid.NewGuid(),
                        Name = $"{rel.TargetEntityName}Id",
                        Type = targetPkType,
                        IsRequired = true,
                        SortOrder = 2,
                        Description = $"FK to {rel.TargetEntityName}"
                    }
                }
            };

            joinEntities.Add(joinEntity);
        }

        return joinEntities;
    }

    /// <summary>
    /// Get relationships where this entity is the parent (has children).
    /// </summary>
    private static List<DataObjects.RelationshipDefinition> GetChildRelationships(
        string entityName,
        List<DataObjects.RelationshipDefinition> relationships){
        return relationships
            .Where(r => r.SourceEntityName == entityName &&
                       (r.Type == DataObjects.RelationshipType.OneToMany ||
                        r.Type == DataObjects.RelationshipType.ManyToMany))
            .ToList();
    }

    // ============================================================
    // RELATIONSHIP HELPERS
    // ============================================================

    /// <summary>
    /// Get relationships where this entity is the child (has FK to parent).
    /// </summary>
    private static List<DataObjects.RelationshipDefinition> GetParentRelationships(
        string entityName,
        List<DataObjects.RelationshipDefinition> relationships){
        return relationships
            .Where(r => r.TargetEntityName == entityName && r.SourceEntityName != entityName)
            .ToList();
    }
}

#endregion

// ---- EntityTemplates.EFModel ----

#region EntityTemplates - EF Model Generation
// ============================================================================
// ENTITY WIZARD - EF MODEL TEMPLATES
// Generates Entity Framework model classes with relationships.
// Part of: EntityTemplates.App (partial)
// ============================================================================

public static partial class EntityTemplates
{
    // ============================================================
    // EFDATAMODEL PARTIAL GENERATION
    // ============================================================

    /// <summary>
    /// Generates a partial class for EFDataModel with DbSet properties for all entities.
    /// </summary>
    private static string GenerateEFDataModelPartial(List<DataObjects.EntityDefinition> entities, string projectName)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"using Microsoft.EntityFrameworkCore;");
        sb.AppendLine();
        sb.AppendLine($"namespace {projectName}.EFModels.EFModels;");
        sb.AppendLine();
        sb.AppendLine($"// ============================================================================");
        sb.AppendLine($"// {projectName.ToUpper()} PROJECT - EFDataModel Extension");
        sb.AppendLine($"// DbSet properties for generated entities");
        sb.AppendLine($"// Generated by Entity Builder Wizard");
        sb.AppendLine($"// ============================================================================");
        sb.AppendLine();
        sb.AppendLine($"public partial class EFDataModel : DbContext");
        sb.AppendLine($"{{");

        foreach (var entity in entities)
        {
            sb.AppendLine($"    public virtual DbSet<{entity.Name}Item> {entity.PluralName} {{ get; set; }} = null!;");
        }

        sb.AppendLine($"}}");

        return sb.ToString();
    }
    // ============================================================
    // EF MODEL TEMPLATE (with relationship support)
    // ============================================================

    private static string GenerateEFModel(
        DataObjects.EntityDefinition entity,
        List<DataObjects.RelationshipDefinition> relationships,
        DataObjects.EntityWizardOptions options,
        string projectName){
        var sb = new StringBuilder();
        var tableName = string.IsNullOrWhiteSpace(entity.TableName) ? entity.PluralName : entity.TableName;

        sb.AppendLine("using System.ComponentModel.DataAnnotations;");
        sb.AppendLine("using System.ComponentModel.DataAnnotations.Schema;");
        sb.AppendLine();
        sb.AppendLine($"namespace {projectName}.EFModels.EFModels;");
        sb.AppendLine();
        sb.AppendLine($"/// <summary>");
        sb.AppendLine($"/// {entity.Name} entity - stored in [{tableName}] table.");
        if (entity.IsJoinTable)
        {
            sb.AppendLine($"/// This is a join table for many-to-many relationships.");
        }
        sb.AppendLine($"/// </summary>");
        sb.AppendLine($"[Table(\"{tableName}\")]");
        sb.AppendLine($"public partial class {entity.Name}Item");
        sb.AppendLine("{");

        // Properties
        foreach (var prop in entity.Properties.OrderBy(p => p.SortOrder).ThenBy(p => p.Name))
        {
            if (prop.IsPrimaryKey)
            {
                sb.AppendLine("    [Key]");
            }
            if (prop.MaxLength.HasValue)
            {
                sb.AppendLine($"    [MaxLength({prop.MaxLength})]");
            }

            var csharpType = GetEFType(prop, entity.Enums);
            var defaultValue = GetEFDefaultValue(prop, entity.Enums);

            sb.Append($"    public {csharpType} {prop.Name} {{ get; set; }}");
            if (!string.IsNullOrWhiteSpace(defaultValue))
            {
                sb.Append($" = {defaultValue};");
            }
            sb.AppendLine();
            sb.AppendLine();
        }

        // Navigation properties from relationships
        var sourceRels = relationships.Where(r => r.SourceEntityName == entity.Name).ToList();
        var targetRels = relationships.Where(r => r.TargetEntityName == entity.Name && r.SourceEntityName != entity.Name).ToList();
        var selfRefs = relationships.Where(r => r.Type == DataObjects.RelationshipType.SelfReference && r.SourceEntityName == entity.Name).ToList();

        if (sourceRels.Any() || targetRels.Any() || selfRefs.Any())
        {
            sb.AppendLine("    // Navigation properties");
        }

        // Source side navigation (collections for 1:N)
        foreach (var rel in sourceRels)
        {
            // Default navigation property name if not specified
            var navPropName = !string.IsNullOrWhiteSpace(rel.SourceNavigationProperty)
                ? rel.SourceNavigationProperty
                : rel.TargetEntityName + "s"; // e.g., "AccessEvents"

            if (rel.Type == DataObjects.RelationshipType.OneToMany || rel.Type == DataObjects.RelationshipType.ManyToMany)
            {
                sb.AppendLine($"    public virtual ICollection<{rel.TargetEntityName}Item> {navPropName} {{ get; set; }} = new List<{rel.TargetEntityName}Item>();");
            }
            else if (rel.Type == DataObjects.RelationshipType.OneToOne)
            {
                sb.AppendLine($"    public virtual {rel.TargetEntityName}Item? {navPropName} {{ get; set; }}");
            }
        }

        // Target side navigation (references for 1:N)
        foreach (var rel in targetRels)
        {
            var fkNullable = !rel.IsRequired;
            var fkType = "Guid" + (fkNullable ? "?" : "");

            // Default FK property name if not specified
            var fkPropName = !string.IsNullOrWhiteSpace(rel.ForeignKeyProperty)
                ? rel.ForeignKeyProperty
                : rel.SourceEntityName + "Id"; // e.g., "SourceSystemId"

            // Default navigation property name if not specified
            var navPropName = !string.IsNullOrWhiteSpace(rel.TargetNavigationProperty)
                ? rel.TargetNavigationProperty
                : rel.SourceEntityName; // e.g., "SourceSystem"

            // Only add FK property if not already defined in entity properties
            var fkAlreadyDefined = entity.Properties.Any(p =>
                p.Name.Equals(fkPropName, StringComparison.OrdinalIgnoreCase));

            if (!fkAlreadyDefined)
            {
                sb.AppendLine($"    public {fkType} {fkPropName} {{ get; set; }}");
            }

            var navNullable = fkNullable ? "?" : "";
            var initValue = fkNullable ? "" : " = null!";
            sb.AppendLine($"    [ForeignKey(\"{fkPropName}\")]");
            sb.AppendLine($"    public virtual {rel.SourceEntityName}Item{navNullable} {navPropName} {{ get; set; }}{initValue};");
            sb.AppendLine();
        }

        // Self-referencing navigation
        foreach (var rel in selfRefs)
        {
            var fkPropName = !string.IsNullOrWhiteSpace(rel.ForeignKeyProperty)
                ? rel.ForeignKeyProperty
                : "Parent" + entity.Name + "Id";
            var parentNavName = !string.IsNullOrWhiteSpace(rel.TargetNavigationProperty)
                ? rel.TargetNavigationProperty
                : "Parent";
            var childrenNavName = !string.IsNullOrWhiteSpace(rel.SourceNavigationProperty)
                ? rel.SourceNavigationProperty
                : "Children";

            sb.AppendLine($"    public Guid? {fkPropName} {{ get; set; }}");
            sb.AppendLine($"    [ForeignKey(\"{fkPropName}\")]");
            sb.AppendLine($"    public virtual {entity.Name}Item? {parentNavName} {{ get; set; }}");
            sb.AppendLine($"    public virtual ICollection<{entity.Name}Item> {childrenNavName} {{ get; set; }} = new List<{entity.Name}Item>();");
        }

        sb.AppendLine("}");

        return sb.ToString();
    }

    // Keep single-entity overload for backward compatibility
    private static string GenerateEFModel(DataObjects.EntityDefinition entity, DataObjects.EntityWizardOptions options)
    {
        return GenerateEFModel(entity, new List<DataObjects.RelationshipDefinition>(), options, "FreeManager");
    }

    private static string GetEFDefaultValue(DataObjects.PropertyDefinition prop, List<DataObjects.EnumDefinition> enums)
    {
        if (!string.IsNullOrWhiteSpace(prop.DefaultValue))
        {
            if (prop.Type.StartsWith("enum:"))
            {
                var enumDef = enums.FirstOrDefault(e => e.Name == prop.EnumName);
                var defaultVal = prop.DefaultValue ?? enumDef?.DefaultValue ?? enumDef?.Values.FirstOrDefault() ?? "";
                return $"\"{defaultVal}\"";
            }
            return prop.DefaultValue;
        }

        if (prop.Type.StartsWith("enum:"))
        {
            var enumDef = enums.FirstOrDefault(e => e.Name == prop.EnumName);
            var defaultVal = enumDef?.DefaultValue ?? enumDef?.Values.FirstOrDefault() ?? "";
            return $"\"{defaultVal}\"";
        }

        return prop.Type switch
        {
            "string" => "string.Empty",
            "bool" => "false",
            "Guid" when prop.IsPrimaryKey => null!,
            "Guid" => "Guid.Empty",
            _ => null!
        };
    }

    // ============================================================
    // EF TYPE HELPERS
    // ============================================================

    private static string GetEFType(DataObjects.PropertyDefinition prop, List<DataObjects.EnumDefinition> enums)
    {
        if (prop.Type.StartsWith("enum:"))
        {
            return prop.IsNullable ? "string?" : "string";
        }

        var baseType = prop.Type;
        if (prop.IsNullable && !baseType.EndsWith("?"))
        {
            baseType += "?";
        }
        return baseType;
    }
}

#endregion

// ---- EntityTemplates.DataObjects ----

#region EntityTemplates - DataObjects Generation
// ============================================================================
// ENTITY WIZARD - DATAOBJECTS TEMPLATES
// Generates DTOs, filter classes, and enums.
// Part of: EntityTemplates.App (partial)
// ============================================================================

public static partial class EntityTemplates
{
    // ============================================================
    // SINGLE-ENTITY DATAOBJECTS TEMPLATE (with relationship support)
    // ============================================================

    private static string GenerateDataObjects(
        DataObjects.EntityDefinition entity,
        List<DataObjects.RelationshipDefinition> relationships,
        List<DataObjects.EntityDefinition> allEntities,
        DataObjects.EntityWizardOptions options,
        string projectName){
        var sb = new StringBuilder();
        var pkProp = entity.Properties.FirstOrDefault(p => p.IsPrimaryKey);
        var pkName = pkProp?.Name ?? $"{entity.Name}Id";
        var pkType = pkProp?.Type ?? "Guid";

        // Get parent relationships (where this entity has FK to parent)
        var parentRels = GetParentRelationships(entity.Name, relationships);

        // Main DTO class
        sb.AppendLine($"    /// <summary>{entity.Name} data transfer object.</summary>");
        sb.AppendLine($"    public class {entity.Name}");
        sb.AppendLine($"    {{");
        foreach (var prop in entity.Properties.OrderBy(p => p.SortOrder))
        {
            var type = GetDTOType(prop, entity.Enums);
            var defaultVal = GetDTODefault(prop, entity.Enums);
            if (!string.IsNullOrWhiteSpace(prop.Description))
            {
                sb.AppendLine($"        /// <summary>{prop.Description}</summary>");
            }
            sb.AppendLine($"        public {type} {prop.Name} {{ get; set; }}{(string.IsNullOrWhiteSpace(defaultVal) ? "" : $" = {defaultVal};")}");
        }

        // Add FK display name properties for relationships
        foreach (var rel in parentRels)
        {
            sb.AppendLine($"        /// <summary>Display name for related {rel.SourceEntityName}.</summary>");
            sb.AppendLine($"        public string {rel.SourceEntityName}Name {{ get; set; }} = string.Empty;");
        }

        sb.AppendLine($"    }}");
        sb.AppendLine();

        // Filter class with typed filters
        sb.AppendLine($"    /// <summary>Filter criteria for {entity.Name} queries.</summary>");
        sb.AppendLine($"    public class {entity.Name}Filter");
        sb.AppendLine($"    {{");
        sb.AppendLine($"        public string? Search {{ get; set; }}");
        sb.AppendLine($"        public string SortColumn {{ get; set; }} = \"{pkName}\";");
        sb.AppendLine($"        public bool SortDescending {{ get; set; }} = true;");
        sb.AppendLine($"        public int Page {{ get; set; }} = 1;");
        sb.AppendLine($"        public int PageSize {{ get; set; }} = {options.DefaultPageSize};");
        sb.AppendLine($"        public int Skip => (Page - 1) * PageSize;");
        if (options.IncludeSoftDelete)
        {
            sb.AppendLine($"        public bool IncludeDeleted {{ get; set; }} = false;");
        }
        // Add FK filter for parent relationships
        foreach (var rel in parentRels)
        {
            var fkNullable = !rel.IsRequired;
            sb.AppendLine($"        public Guid{(fkNullable ? "?" : "")} {rel.ForeignKeyProperty}Filter {{ get; set; }}");
        }
        // Add typed filters for filterable properties
        foreach (var prop in entity.Properties.Where(p => p.IsFilterable && !p.IsPrimaryKey && !p.IsSystemField))
        {
            var filterType = GetFilterPropertyType(prop, entity.Enums);
            if (!string.IsNullOrEmpty(filterType))
            {
                sb.AppendLine($"        public {filterType} {prop.Name}Filter {{ get; set; }}");
            }
        }
        sb.AppendLine($"    }}");
        sb.AppendLine();

        // Filter result with pagination
        sb.AppendLine($"    /// <summary>Paginated result for {entity.Name} queries.</summary>");
        sb.AppendLine($"    public class {entity.Name}FilterResult");
        sb.AppendLine($"    {{");
        sb.AppendLine($"        public List<{entity.Name}> Records {{ get; set; }} = new();");
        sb.AppendLine($"        public int TotalRecords {{ get; set; }}");
        sb.AppendLine($"        public int Page {{ get; set; }}");
        sb.AppendLine($"        public int PageSize {{ get; set; }}");
        sb.AppendLine($"        public int TotalPages => (int)Math.Ceiling((double)TotalRecords / PageSize);");
        sb.AppendLine($"    }}");

        // Enums
        foreach (var enumDef in entity.Enums)
        {
            sb.AppendLine();
            sb.AppendLine($"    /// <summary>{enumDef.Name} enumeration.</summary>");
            sb.AppendLine($"    public enum {enumDef.Name}");
            sb.AppendLine($"    {{");
            for (int i = 0; i < enumDef.Values.Count; i++)
            {
                var comma = i < enumDef.Values.Count - 1 ? "," : "";
                sb.AppendLine($"        {enumDef.Values[i]}{comma}");
            }
            sb.AppendLine($"    }}");
        }

        return sb.ToString();
    }

    // Backward compatibility overload
    private static string GenerateDataObjects(DataObjects.EntityDefinition entity, DataObjects.EntityWizardOptions options, string projectName)
    {
        return GenerateDataObjects(entity, new List<DataObjects.RelationshipDefinition>(), new List<DataObjects.EntityDefinition>(), options, projectName);
    }
    // ============================================================
    // MULTI-ENTITY DATAOBJECTS TEMPLATE
    // ============================================================

    private static string GenerateDataObjectsMulti(
        List<DataObjects.EntityDefinition> entities,
        List<DataObjects.RelationshipDefinition> relationships,
        DataObjects.EntityWizardOptions options,
        string projectName){
        var sb = new StringBuilder();

        sb.AppendLine($"namespace {projectName};");
        sb.AppendLine();
        sb.AppendLine($"#region {projectName} DataObjects");
        sb.AppendLine($"// ============================================================================");
        sb.AppendLine($"// {projectName.ToUpper()} PROJECT - Generated by Entity Builder Wizard");
        sb.AppendLine($"// Entities: {string.Join(", ", entities.Select(e => e.Name))}");
        if (relationships.Any())
        {
            sb.AppendLine($"// Relationships: {relationships.Count}");
        }
        sb.AppendLine($"// ============================================================================");
        sb.AppendLine();

        // Open the partial class wrapper
        sb.AppendLine($"public partial class DataObjects");
        sb.AppendLine($"{{");

        // Generate for each entity (with relationship awareness)
        foreach (var entity in entities)
        {
            sb.AppendLine(GenerateDataObjects(entity, relationships, entities, options, projectName));
            sb.AppendLine();
        }

        // Generate lookup DTOs for FK dropdowns
        foreach (var entity in entities.Where(e => !e.IsJoinTable))
        {
            var displayProp = entity.Properties
                .FirstOrDefault(p => p.Name.Contains("Name") || p.Name.Contains("Title") || p.Name.Contains("Description"))
                ?? entity.Properties.FirstOrDefault(p => p.Type == "string" && !p.IsPrimaryKey);
            var pkProp = entity.Properties.FirstOrDefault(p => p.IsPrimaryKey);

            if (displayProp != null && pkProp != null)
            {
                sb.AppendLine($"    /// <summary>Lookup DTO for {entity.Name} dropdowns.</summary>");
                sb.AppendLine($"    public class {entity.Name}Lookup");
                sb.AppendLine($"    {{");
                sb.AppendLine($"        public {pkProp.Type} {pkProp.Name} {{ get; set; }}");
                sb.AppendLine($"        public string DisplayName {{ get; set; }} = string.Empty;");
                sb.AppendLine($"    }}");
                sb.AppendLine();
            }
        }

        // Close the partial class wrapper
        sb.AppendLine($"}}");
        sb.AppendLine();
        sb.AppendLine("#endregion");

        return sb.ToString();
    }

    // ============================================================
    // ENDPOINTS FILE GENERATION
    // ============================================================

    /// <summary>
    /// Generates API endpoint route constants for all entities.
    /// Routes follow FreeCRM convention: api/Data/{MethodName}
    /// </summary>
    private static string GenerateEndpoints(
        List<DataObjects.EntityDefinition> entities,
        DataObjects.EntityWizardOptions options,
        string projectName){
        var sb = new StringBuilder();

        sb.AppendLine($"namespace {projectName};");
        sb.AppendLine();
        sb.AppendLine($"#region {projectName} API Endpoints");
        sb.AppendLine($"// ============================================================================");
        sb.AppendLine($"// {projectName.ToUpper()} PROJECT - API Endpoint Constants");
        sb.AppendLine($"// Generated by Entity Builder Wizard");
        sb.AppendLine($"// ============================================================================");
        sb.AppendLine();
        sb.AppendLine($"public partial class DataObjects");
        sb.AppendLine($"{{");
        sb.AppendLine($"    public static partial class Endpoints");
        sb.AppendLine($"    {{");
        sb.AppendLine($"        public static class {projectName}");
        sb.AppendLine($"        {{");

        foreach (var entity in entities.Where(e => !e.IsJoinTable))
        {
            sb.AppendLine($"            // ============================================================");
            sb.AppendLine($"            // {entity.Name.ToUpper()} ENDPOINTS");
            sb.AppendLine($"            // ============================================================");
            sb.AppendLine();

            // Get filtered list
            sb.AppendLine($"            /// <summary>Get filtered list of {entity.PluralName}</summary>");
            sb.AppendLine($"            public const string Get{entity.PluralName} = \"api/Data/Get{entity.PluralName}\";");
            sb.AppendLine();

            // Get by ID
            sb.AppendLine($"            /// <summary>Get single {entity.Name} by ID</summary>");
            sb.AppendLine($"            public const string Get{entity.Name} = \"api/Data/Get{entity.Name}\";");
            sb.AppendLine();

            // Lookups
            sb.AppendLine($"            /// <summary>Get {entity.Name} lookup list for dropdowns</summary>");
            sb.AppendLine($"            public const string Get{entity.Name}Lookups = \"api/Data/Get{entity.Name}Lookups\";");
            sb.AppendLine();

            if (!options.IsReadOnly)
            {
                // Save
                sb.AppendLine($"            /// <summary>Create or update {entity.Name}</summary>");
                sb.AppendLine($"            public const string Save{entity.Name} = \"api/Data/Save{entity.Name}\";");
                sb.AppendLine();

                // Delete
                sb.AppendLine($"            /// <summary>Delete {entity.Name} by ID</summary>");
                sb.AppendLine($"            public const string Delete{entity.Name} = \"api/Data/Delete{entity.Name}\";");
                sb.AppendLine();
            }
        }

        sb.AppendLine($"        }}");
        sb.AppendLine($"    }}");
        sb.AppendLine($"}}");
        sb.AppendLine();
        sb.AppendLine($"#endregion");

        return sb.ToString();
    }

    private static string GetDTODefault(DataObjects.PropertyDefinition prop, List<DataObjects.EnumDefinition> enums)
    {
        if (!string.IsNullOrWhiteSpace(prop.DefaultValue)) return prop.DefaultValue;
        return prop.Type switch
        {
            "string" => "string.Empty",
            _ => null!
        };
    }

    // ============================================================
    // DTO TYPE HELPERS
    // ============================================================

    private static string GetDTOType(DataObjects.PropertyDefinition prop, List<DataObjects.EnumDefinition> enums)
    {
        var type = prop.Type.StartsWith("enum:") ? prop.EnumName! : prop.Type;
        if (prop.IsNullable && !type.EndsWith("?")) type += "?";
        return type;
    }

    private static string? GetFilterPropertyType(DataObjects.PropertyDefinition prop, List<DataObjects.EnumDefinition> enums)
    {
        if (prop.Type.StartsWith("enum:"))
        {
            return $"List<{prop.EnumName}>?";
        }
        return prop.Type switch
        {
            "bool" => "bool?",
            "DateTime" => "(DateTime? Start, DateTime? End)?",
            "int" or "decimal" or "double" => "(decimal? Min, decimal? Max)?",
            "string" => null,
            _ => null
        };
    }
}

#endregion

// ---- EntityTemplates.DataAccess ----

#region EntityTemplates - DataAccess Generation
// ============================================================================
// ENTITY WIZARD - DATAACCESS TEMPLATES
// Generates data access layer with CRUD operations.
// Part of: EntityTemplates.App (partial)
// ============================================================================

public static partial class EntityTemplates
{
    // ============================================================
    // SINGLE-ENTITY DATAACCESS TEMPLATE (Relationship-Aware)
    // ============================================================

    private static string GenerateDataAccess(
        DataObjects.EntityDefinition entity,
        List<DataObjects.RelationshipDefinition> relationships,
        List<DataObjects.EntityDefinition> allEntities,
        DataObjects.EntityWizardOptions options,
        string projectName){
        var sb = new StringBuilder();
        var pkProp = entity.Properties.FirstOrDefault(p => p.IsPrimaryKey);
        var pkName = pkProp?.Name ?? $"{entity.Name}Id";
        var pkType = pkProp?.Type ?? "Guid";

        var parentRels = GetParentRelationships(entity.Name, relationships);

        sb.AppendLine($"// {entity.Name} Data Access Methods");
        sb.AppendLine($"public partial class DataAccess");
        sb.AppendLine($"{{");
        sb.AppendLine($"    #region {entity.Name}");
        sb.AppendLine();

        // Get filtered list
        sb.AppendLine($"    public async Task<DataObjects.{entity.Name}FilterResult> Get{entity.PluralName}Async(DataObjects.{entity.Name}Filter filter)");
        sb.AppendLine($"    {{");
        sb.AppendLine($"        var query = data.{entity.PluralName}");

        foreach (var rel in parentRels)
        {
            // Use default nav property name if not specified
            var navPropName = !string.IsNullOrWhiteSpace(rel.TargetNavigationProperty)
                ? rel.TargetNavigationProperty
                : rel.SourceEntityName;
            sb.AppendLine($"            .Include(x => x.{navPropName})");
        }
        sb.AppendLine($"            .AsQueryable();");
        sb.AppendLine();

        if (options.IncludeSoftDelete)
        {
            sb.AppendLine($"        if (!filter.IncludeDeleted)");
            sb.AppendLine($"            query = query.Where(x => !x.Deleted);");
            sb.AppendLine();
        }

        foreach (var rel in parentRels)
        {
            var fkPropName = !string.IsNullOrWhiteSpace(rel.ForeignKeyProperty)
                ? rel.ForeignKeyProperty
                : rel.SourceEntityName + "Id";
            var fkNullable = !rel.IsRequired;
            if (fkNullable)
            {
                sb.AppendLine($"        if (filter.{fkPropName}Filter.HasValue)");
                sb.AppendLine($"            query = query.Where(x => x.{fkPropName} == filter.{fkPropName}Filter.Value);");
            }
            else
            {
                sb.AppendLine($"        if (filter.{fkPropName}Filter != default)");
                sb.AppendLine($"            query = query.Where(x => x.{fkPropName} == filter.{fkPropName}Filter);");
            }
            sb.AppendLine();
        }

        sb.AppendLine($"        if (!string.IsNullOrWhiteSpace(filter.Search))");
        sb.AppendLine($"        {{");
        var searchableProps = entity.Properties.Where(p => p.Type == "string" && !p.IsSystemField).Take(3).ToList();
        if (searchableProps.Any())
        {
            var searchConditions = string.Join(" || ", searchableProps.Select(p => $"x.{p.Name}.Contains(filter.Search)"));
            sb.AppendLine($"            query = query.Where(x => {searchConditions});");
        }
        sb.AppendLine($"        }}");
        sb.AppendLine();
        sb.AppendLine($"        var total = await query.CountAsync();");
        sb.AppendLine();
        sb.AppendLine($"        query = filter.SortColumn switch");
        sb.AppendLine($"        {{");
        foreach (var prop in entity.Properties.Where(p => !p.IsSystemField).Take(5))
        {
            sb.AppendLine($"            \"{prop.Name}\" => filter.SortDescending ? query.OrderByDescending(x => x.{prop.Name}) : query.OrderBy(x => x.{prop.Name}),");
        }
        sb.AppendLine($"            _ => query.OrderByDescending(x => x.{pkName})");
        sb.AppendLine($"        }};");
        sb.AppendLine();
        sb.AppendLine($"        var items = await query.Skip(filter.Skip).Take(filter.PageSize).ToListAsync();");
        sb.AppendLine();
        sb.AppendLine($"        return new DataObjects.{entity.Name}FilterResult");
        sb.AppendLine($"        {{");
        sb.AppendLine($"            Records = items.Select(x => new DataObjects.{entity.Name}");
        sb.AppendLine($"            {{");
        foreach (var prop in entity.Properties.Where(p => !p.IsSystemField || p.IsPrimaryKey))
        {
            sb.AppendLine($"                {prop.Name} = x.{prop.Name},");
        }
        foreach (var rel in parentRels)
        {
            var parentEntity = allEntities.FirstOrDefault(e => e.Name == rel.SourceEntityName);
            var displayProp = parentEntity?.Properties
                .FirstOrDefault(p => p.Name.Contains("Name") || p.Name.Contains("Title") || p.Name.Contains("Description"))
                ?? parentEntity?.Properties.FirstOrDefault(p => p.Type == "string" && !p.IsPrimaryKey);

            var navPropName = !string.IsNullOrWhiteSpace(rel.TargetNavigationProperty)
                ? rel.TargetNavigationProperty
                : rel.SourceEntityName;

            if (displayProp != null)
            {
                sb.AppendLine($"                {rel.SourceEntityName}Name = x.{navPropName}?.{displayProp.Name} ?? string.Empty,");
            }
            else
            {
                sb.AppendLine($"                {rel.SourceEntityName}Name = x.{navPropName}?.ToString() ?? string.Empty,");
            }
        }
        sb.AppendLine($"            }}).ToList(),");
        sb.AppendLine($"            TotalRecords = total,");
        sb.AppendLine($"            Page = filter.Page,");
        sb.AppendLine($"            PageSize = filter.PageSize");
        sb.AppendLine($"        }};");
        sb.AppendLine($"    }}");
        sb.AppendLine();

        // Get by ID
        sb.AppendLine($"    public async Task<DataObjects.{entity.Name}?> Get{entity.Name}Async({pkType} id)");
        sb.AppendLine($"    {{");
        sb.AppendLine($"        var item = await data.{entity.PluralName}");
        foreach (var rel in parentRels)
        {
            var navPropName = !string.IsNullOrWhiteSpace(rel.TargetNavigationProperty)
                ? rel.TargetNavigationProperty
                : rel.SourceEntityName;
            sb.AppendLine($"            .Include(x => x.{navPropName})");
        }
        sb.AppendLine($"            .FirstOrDefaultAsync(x => x.{pkName} == id);");
        sb.AppendLine($"        if (item == null) return null;");
        sb.AppendLine();
        sb.AppendLine($"        return new DataObjects.{entity.Name}");
        sb.AppendLine($"        {{");
        foreach (var prop in entity.Properties.Where(p => !p.IsSystemField || p.IsPrimaryKey))
        {
            sb.AppendLine($"            {prop.Name} = item.{prop.Name},");
        }
        foreach (var rel in parentRels)
        {
            var parentEntity = allEntities.FirstOrDefault(e => e.Name == rel.SourceEntityName);
            var displayProp = parentEntity?.Properties
                .FirstOrDefault(p => p.Name.Contains("Name") || p.Name.Contains("Title") || p.Name.Contains("Description"))
                ?? parentEntity?.Properties.FirstOrDefault(p => p.Type == "string" && !p.IsPrimaryKey);

            var navPropName = !string.IsNullOrWhiteSpace(rel.TargetNavigationProperty)
                ? rel.TargetNavigationProperty
                : rel.SourceEntityName;

            if (displayProp != null)
            {
                sb.AppendLine($"            {rel.SourceEntityName}Name = item.{navPropName}?.{displayProp.Name} ?? string.Empty,");
            }
        }
        sb.AppendLine($"        }};");
        sb.AppendLine($"    }}");

        if (!options.IsReadOnly)
        {
            sb.AppendLine();
            // Save
            sb.AppendLine($"    public async Task<DataObjects.{entity.Name}?> Save{entity.Name}Async(DataObjects.{entity.Name} dto)");
            sb.AppendLine($"    {{");
            sb.AppendLine($"        EFModels.EFModels.{entity.Name}Item item;");
            sb.AppendLine($"        var isNew = dto.{pkName} == default;");
            sb.AppendLine();
            sb.AppendLine($"        if (isNew)");
            sb.AppendLine($"        {{");
            sb.AppendLine($"            item = new EFModels.EFModels.{entity.Name}Item();");
            if (pkType == "Guid")
            {
                sb.AppendLine($"                item.{pkName} = Guid.NewGuid();");
            }
            // Set Added if it exists in the entity (FreeManager convention)
            if (entity.Properties.Any(p => p.Name == "Added"))
            {
                sb.AppendLine($"                item.Added = DateTime.UtcNow;");
            }
            sb.AppendLine($"            data.{entity.PluralName}.Add(item);");
            sb.AppendLine($"        }}");
            sb.AppendLine($"        else");
            sb.AppendLine($"        {{");
            sb.AppendLine($"            item = await data.{entity.PluralName}.FindAsync(dto.{pkName});");
            sb.AppendLine($"            if (item == null) return null;");
            sb.AppendLine($"        }}");
            sb.AppendLine();
            foreach (var prop in entity.Properties.Where(p => !p.IsPrimaryKey && !p.IsSystemField))
            {
                sb.AppendLine($"        item.{prop.Name} = dto.{prop.Name};");
            }
            // Set LastModified if it exists in the entity (FreeManager convention)
            if (entity.Properties.Any(p => p.Name == "LastModified"))
            {
                sb.AppendLine($"        item.LastModified = DateTime.UtcNow;");
            }
            sb.AppendLine();
            sb.AppendLine($"        await data.SaveChangesAsync();");
            sb.AppendLine($"        dto.{pkName} = item.{pkName};");
            sb.AppendLine($"        return dto;");
            sb.AppendLine($"    }}");
            sb.AppendLine();

            // Delete
            if (options.IncludeSoftDelete)
            {
                sb.AppendLine($"    public async Task<bool> Delete{entity.Name}Async({pkType} id)");
                sb.AppendLine($"    {{");
                sb.AppendLine($"        var item = await data.{entity.PluralName}.FindAsync(id);");
                sb.AppendLine($"        if (item == null) return false;");
                sb.AppendLine($"        item.Deleted = true;");
                sb.AppendLine($"        item.DeletedAt = DateTime.UtcNow;");
                sb.AppendLine($"        await data.SaveChangesAsync();");
                sb.AppendLine($"        return true;");
                sb.AppendLine($"    }}");
            }
            else
            {
                sb.AppendLine($"    public async Task<bool> Delete{entity.Name}Async({pkType} id)");
                sb.AppendLine($"    {{");
                sb.AppendLine($"        var item = await data.{entity.PluralName}.FindAsync(id);");
                sb.AppendLine($"        if (item == null) return false;");
                sb.AppendLine($"        data.{entity.PluralName}.Remove(item);");
                sb.AppendLine($"        await data.SaveChangesAsync();");
                sb.AppendLine($"        return true;");
                sb.AppendLine($"    }}");
            }
        }

        // Get Lookups (for FK dropdowns)
        if (!entity.IsJoinTable)
        {
            var displayProp = entity.Properties
                .FirstOrDefault(p => p.Name.Contains("Name") || p.Name.Contains("Title") || p.Name.Contains("Description"))
                ?? entity.Properties.FirstOrDefault(p => p.Type == "string" && !p.IsPrimaryKey);

            sb.AppendLine();
            sb.AppendLine($"    public async Task<List<DataObjects.{entity.Name}Lookup>> Get{entity.Name}LookupsAsync()");
            sb.AppendLine($"    {{");
            sb.AppendLine($"        return await data.{entity.PluralName}");
            if (options.IncludeSoftDelete)
            {
                sb.AppendLine($"            .Where(x => !x.Deleted)");
            }
            sb.AppendLine($"            .Select(x => new DataObjects.{entity.Name}Lookup");
            sb.AppendLine($"            {{");
            sb.AppendLine($"                {pkName} = x.{pkName},");
            if (displayProp != null)
            {
                sb.AppendLine($"                DisplayName = x.{displayProp.Name}");
            }
            else
            {
                sb.AppendLine($"                DisplayName = x.{pkName}.ToString();");
            }
            sb.AppendLine($"            }})");
            sb.AppendLine($"            .ToListAsync();");
            sb.AppendLine($"    }}");
        }

        sb.AppendLine();
        sb.AppendLine($"    #endregion");
        sb.AppendLine($"}}");

        return sb.ToString();
    }

    // Backward compatibility overload
    private static string GenerateDataAccess(DataObjects.EntityDefinition entity, DataObjects.EntityWizardOptions options, string projectName)
    {
        return GenerateDataAccess(entity, new List<DataObjects.RelationshipDefinition>(), new List<DataObjects.EntityDefinition>(), options, projectName);
    }
    // ============================================================
    // MULTI-ENTITY DATAACCESS TEMPLATE
    // ============================================================

    private static string GenerateDataAccessMulti(
        List<DataObjects.EntityDefinition> entities,
        List<DataObjects.RelationshipDefinition> relationships,
        DataObjects.EntityWizardOptions options,
        string projectName){
        var sb = new StringBuilder();

        sb.AppendLine("using Microsoft.EntityFrameworkCore;");
        sb.AppendLine();
        sb.AppendLine($"namespace {projectName};");
        sb.AppendLine();
        sb.AppendLine($"// ============================================================================");
        sb.AppendLine($"// {projectName.ToUpper()} PROJECT DATA ACCESS");
        sb.AppendLine($"// ============================================================================");
        sb.AppendLine();

        foreach (var entity in entities)
        {
            sb.AppendLine(GenerateDataAccess(entity, relationships, entities, options, projectName));
            sb.AppendLine();
        }

        return sb.ToString();
    }

    // ============================================================
    // IDATAACCESS INTERFACE PARTIAL GENERATION
    // ============================================================

    /// <summary>
    /// Generates a partial interface for IDataAccess with method signatures for all entities.
    /// </summary>
    private static string GenerateIDataAccessPartial(
        List<DataObjects.EntityDefinition> entities,
        DataObjects.EntityWizardOptions options,
        string projectName){
        var sb = new StringBuilder();

        sb.AppendLine($"namespace {projectName};");
        sb.AppendLine();
        sb.AppendLine($"// ============================================================================");
        sb.AppendLine($"// {projectName.ToUpper()} PROJECT - IDataAccess Extension");
        sb.AppendLine($"// Interface method signatures for generated entities");
        sb.AppendLine($"// Generated by Entity Builder Wizard");
        sb.AppendLine($"// ============================================================================");
        sb.AppendLine();
        sb.AppendLine($"public partial interface IDataAccess");
        sb.AppendLine($"{{");

        foreach (var entity in entities.Where(e => !e.IsJoinTable))
        {
            var pkProp = entity.Properties.FirstOrDefault(p => p.IsPrimaryKey);
            var pkType = pkProp?.Type ?? "Guid";

            sb.AppendLine($"    // {entity.Name} methods");

            // Get filtered list
            sb.AppendLine($"    Task<DataObjects.{entity.Name}FilterResult> Get{entity.PluralName}Async(DataObjects.{entity.Name}Filter filter);");

            // Get by ID
            sb.AppendLine($"    Task<DataObjects.{entity.Name}?> Get{entity.Name}Async({pkType} id);");

            // Get Lookups (for FK dropdowns)
            sb.AppendLine($"    Task<List<DataObjects.{entity.Name}Lookup>> Get{entity.Name}LookupsAsync();");

            if (!options.IsReadOnly)
            {
                // Save
                sb.AppendLine($"    Task<DataObjects.{entity.Name}?> Save{entity.Name}Async(DataObjects.{entity.Name} dto);");

                // Delete
                sb.AppendLine($"    Task<bool> Delete{entity.Name}Async({pkType} id);");
            }

            sb.AppendLine();
        }

        sb.AppendLine($"}}");

        return sb.ToString();
    }
}

#endregion

// ---- EntityTemplates.Controller ----

#region EntityTemplates - Controller Generation
// ============================================================================
// ENTITY WIZARD - CONTROLLER TEMPLATES
// Generates REST API endpoints for CRUD operations.
// Part of: EntityTemplates.App (partial)
// ============================================================================

public static partial class EntityTemplates
{
    // ============================================================
    // SINGLE-ENTITY CONTROLLER TEMPLATE (with Lookup endpoints)
    // ============================================================

    private static string GenerateController(
        DataObjects.EntityDefinition entity,
        List<DataObjects.RelationshipDefinition> relationships,
        List<DataObjects.EntityDefinition> allEntities,
        DataObjects.EntityWizardOptions options,
        string projectName){
        var sb = new StringBuilder();
        var pkProp = entity.Properties.FirstOrDefault(p => p.IsPrimaryKey);
        var pkType = pkProp?.Type ?? "Guid";
        var pkName = pkProp?.Name ?? $"{entity.Name}Id";

        sb.AppendLine($"    // {entity.Name} API Endpoints");
        sb.AppendLine($"    #region {entity.Name}");
        sb.AppendLine();

        // Get filtered list
        sb.AppendLine($"    [HttpPost(\"api/Data/Get{entity.PluralName}\")]");
        sb.AppendLine($"    public async Task<ActionResult<DataObjects.{entity.Name}FilterResult>> Get{entity.PluralName}([FromBody] DataObjects.{entity.Name}Filter filter)");
        sb.AppendLine($"    {{");
        sb.AppendLine($"        return Ok(await da.Get{entity.PluralName}Async(filter));");
        sb.AppendLine($"    }}");
        sb.AppendLine();

        // Get by ID
        sb.AppendLine($"    [HttpPost(\"api/Data/Get{entity.Name}\")]");
        sb.AppendLine($"    public async Task<ActionResult<DataObjects.{entity.Name}?>> Get{entity.Name}([FromBody] {pkType} id)");
        sb.AppendLine($"    {{");
        sb.AppendLine($"        var item = await da.Get{entity.Name}Async(id);");
        sb.AppendLine($"        if (item == null) return NotFound();");
        sb.AppendLine($"        return Ok(item);");
        sb.AppendLine($"    }}");
        sb.AppendLine();

        // Get Lookups endpoint (for FK dropdowns)
        if (!entity.IsJoinTable)
        {
            sb.AppendLine($"    [HttpGet(\"api/Data/Get{entity.Name}Lookups\")]");
            sb.AppendLine($"    public async Task<ActionResult<List<DataObjects.{entity.Name}Lookup>>> Get{entity.Name}Lookups()");
            sb.AppendLine($"    {{");
            sb.AppendLine($"        return Ok(await da.Get{entity.Name}LookupsAsync());");
            sb.AppendLine($"    }}");
        }

        if (!options.IsReadOnly)
        {
            sb.AppendLine();
            // Save
            sb.AppendLine($"    [HttpPost(\"api/Data/Save{entity.Name}\")]");
            sb.AppendLine($"    public async Task<ActionResult<DataObjects.{entity.Name}?>> Save{entity.Name}([FromBody] DataObjects.{entity.Name} item)");
            sb.AppendLine($"    {{");
            sb.AppendLine($"        var result = await da.Save{entity.Name}Async(item);");
            sb.AppendLine($"        if (result == null) return BadRequest();");
            sb.AppendLine($"        return Ok(result);");
            sb.AppendLine($"    }}");
            sb.AppendLine();

            // Delete
            sb.AppendLine($"    [HttpPost(\"api/Data/Delete{entity.Name}\")]");
            sb.AppendLine($"    public async Task<ActionResult<bool>> Delete{entity.Name}([FromBody] {pkType} id)");
            sb.AppendLine($"    {{");
            sb.AppendLine($"        return Ok(await da.Delete{entity.Name}Async(id));");
            sb.AppendLine($"    }}");
        }

        sb.AppendLine();
        sb.AppendLine($"    #endregion");

        return sb.ToString();
    }

    // Backward compatibility overload
    private static string GenerateController(DataObjects.EntityDefinition entity, DataObjects.EntityWizardOptions options, string projectName)
    {
        return GenerateController(entity, new List<DataObjects.RelationshipDefinition>(), new List<DataObjects.EntityDefinition>(), options, projectName);
    }
    // ============================================================
    // MULTI-ENTITY CONTROLLER TEMPLATE
    // ============================================================

    private static string GenerateControllerMulti(
        List<DataObjects.EntityDefinition> entities,
        List<DataObjects.RelationshipDefinition> relationships,
        DataObjects.EntityWizardOptions options,
        string projectName){
        var sb = new StringBuilder();

        sb.AppendLine("using Microsoft.AspNetCore.Mvc;");
        sb.AppendLine("using Microsoft.EntityFrameworkCore;");
        sb.AppendLine();
        sb.AppendLine($"namespace {projectName}.Server.Controllers;");
        sb.AppendLine();
        sb.AppendLine($"// ============================================================================");
        sb.AppendLine($"// {projectName.ToUpper()} PROJECT API ENDPOINTS");
        sb.AppendLine($"// ============================================================================");
        sb.AppendLine();
        sb.AppendLine($"public partial class DataController");
        sb.AppendLine($"{{");

        foreach (var entity in entities)
        {
            sb.AppendLine(GenerateController(entity, relationships, entities, options, projectName));
            sb.AppendLine();
        }

        sb.AppendLine($"}}");

        return sb.ToString();
    }
}

#endregion

// ---- EntityTemplates.RazorPages ----

#region EntityTemplates - Razor Pages Generation
// ============================================================================
// ENTITY WIZARD - RAZOR PAGE TEMPLATES
// Generates List and Edit Blazor pages.
// Part of: EntityTemplates.App (partial)
// ============================================================================

public static partial class EntityTemplates
{
    // ============================================================
    // EDIT PAGE TEMPLATE (Relationship-Aware)
    // ============================================================

    private static string GenerateEditPage(
        DataObjects.EntityDefinition entity,
        List<DataObjects.RelationshipDefinition> relationships,
        List<DataObjects.EntityDefinition> allEntities,
        DataObjects.EntityWizardOptions options,
        string projectName){
        var sb = new StringBuilder();
        var pkProp = entity.Properties.FirstOrDefault(p => p.IsPrimaryKey);
        var pkName = pkProp?.Name ?? $"{entity.Name}Id";

        var parentRels = GetParentRelationships(entity.Name, relationships);

        // Get FK properties that have parent relationships (for dropdowns)
        var fkPropsWithParents = parentRels
            .Select(r => new { Rel = r, Prop = entity.Properties.FirstOrDefault(p => p.Name == r.ForeignKeyProperty) })
            .Where(x => x.Prop != null)
            .ToList();

        var editableProps = entity.Properties
            .Where(p => !p.IsPrimaryKey && !p.IsSystemField)
            .Where(p => !parentRels.Any(r => r.ForeignKeyProperty == p.Name)) // FK fields handled separately as dropdowns
            .OrderBy(p => p.SortOrder)
            .ToList();

        sb.AppendLine($"@* {projectName}.App.Edit{entity.Name}.razor - Edit/Create form for {entity.Name} *@");
        sb.AppendLine($"@* Generated by Entity Builder Wizard *@");
        if (fkPropsWithParents.Any())
        {
            sb.AppendLine($"@inject HttpClient Http");
        }
        sb.AppendLine();
        sb.AppendLine($"<div class=\"modal fade show d-block\" tabindex=\"-1\" style=\"background: rgba(0,0,0,0.5);\">");
        sb.AppendLine($"    <div class=\"modal-dialog modal-lg\">");
        sb.AppendLine($"        <div class=\"modal-content\">");
        sb.AppendLine($"            <div class=\"modal-header\">");
        sb.AppendLine($"                <h5 class=\"modal-title\">");
        sb.AppendLine($"                    <i class=\"fa-solid @(IsNew ? \"fa-plus\" : \"fa-pen\") me-2\"></i>");
        sb.AppendLine($"                    @(IsNew ? \"New\" : \"Edit\") {entity.Name}");
        sb.AppendLine($"                </h5>");
        sb.AppendLine($"                <button type=\"button\" class=\"btn-close\" @onclick=\"Cancel\"></button>");
        sb.AppendLine($"            </div>");
        sb.AppendLine($"            <div class=\"modal-body\">");
        sb.AppendLine($"                <EditForm Model=\"Item\" OnValidSubmit=\"Save\">");
        sb.AppendLine($"                    <DataAnnotationsValidator />");

        // Generate FK dropdown fields first (for parent relationships)
        foreach (var fkInfo in fkPropsWithParents)
        {
            var parentEntity = allEntities.FirstOrDefault(e => e.Name == fkInfo.Rel.SourceEntityName);
            var parentPkName = parentEntity?.Properties.FirstOrDefault(p => p.IsPrimaryKey)?.Name ?? $"{fkInfo.Rel.SourceEntityName}Id";
            var displayProp = parentEntity?.Properties.FirstOrDefault(p => p.Name == "Name" || p.Name == "Title" || p.Name == "DisplayName")?.Name ?? "Name";
            var isRequired = fkInfo.Prop!.IsRequired;
            var isNullable = !isRequired || fkInfo.Prop.IsNullable;

            sb.AppendLine($"                    <div class=\"mb-3\">");
            sb.AppendLine($"                        <label class=\"form-label\">{fkInfo.Rel.SourceEntityName}{(isRequired ? " <span class=\"text-danger\">*</span>" : "")}</label>");
            sb.AppendLine($"                        <select class=\"form-select\" @bind=\"Item.{fkInfo.Prop.Name}\">");
            if (isNullable)
            {
                sb.AppendLine($"                            <option value=\"\">-- None --</option>");
            }
            sb.AppendLine($"                            @foreach (var parent in _{fkInfo.Rel.SourceEntityName.ToLower()}List) {{");
            sb.AppendLine($"                                <option value=\"@parent.{parentPkName}\">@parent.{displayProp}</option>");
            sb.AppendLine($"                            }}");
            sb.AppendLine($"                        </select>");
            sb.AppendLine($"                    </div>");
        }

        // Generate regular form fields
        foreach (var prop in editableProps)
        {
            sb.AppendLine($"                    <div class=\"mb-3\">");
            sb.AppendLine($"                        <label class=\"form-label\">{prop.Name}{(prop.IsRequired ? " <span class=\"text-danger\">*</span>" : "")}</label>");
            sb.Append(GetFormField(prop, entity.Enums));
            sb.AppendLine($"                    </div>");
        }

        sb.AppendLine($"                </EditForm>");
        sb.AppendLine($"            </div>");
        sb.AppendLine($"            <div class=\"modal-footer\">");
        sb.AppendLine($"                <button type=\"button\" class=\"btn btn-secondary\" @onclick=\"Cancel\">Cancel</button>");
        sb.AppendLine($"                <button type=\"button\" class=\"btn btn-primary\" @onclick=\"Save\">Save</button>");
        sb.AppendLine($"            </div>");
        sb.AppendLine($"        </div>");
        sb.AppendLine($"    </div>");
        sb.AppendLine($"</div>");
        sb.AppendLine();
        sb.AppendLine($"@code {{");
        sb.AppendLine($"    [Parameter] public DataObjects.{entity.Name} Item {{ get; set; }} = new();");
        sb.AppendLine($"    [Parameter] public EventCallback<DataObjects.{entity.Name}> OnSave {{ get; set; }}");
        sb.AppendLine($"    [Parameter] public EventCallback OnCancel {{ get; set; }}");
        sb.AppendLine();

        // Add state for parent entity lists
        foreach (var fkInfo in fkPropsWithParents)
        {
            sb.AppendLine($"    private List<DataObjects.{fkInfo.Rel.SourceEntityName}> _{fkInfo.Rel.SourceEntityName.ToLower()}List = new();");
        }

        sb.AppendLine($"    private bool IsNew => Item.{pkName} == default;");
        sb.AppendLine();

        // Add OnInitializedAsync to load parent entity lists
        if (fkPropsWithParents.Any())
        {
            sb.AppendLine($"    protected override async Task OnInitializedAsync() {{");
            foreach (var fkInfo in fkPropsWithParents)
            {
                sb.AppendLine($"        var {fkInfo.Rel.SourceEntityName.ToLower()}Result = await Helpers.GetOrPost<DataObjects.{fkInfo.Rel.SourceEntityName}FilterResult>(");
                sb.AppendLine($"            DataObjects.Endpoints.{projectName}.Get{fkInfo.Rel.SourceEntityName}s, new DataObjects.{fkInfo.Rel.SourceEntityName}Filter {{ PageSize = 1000 }}) ?? new();");
                sb.AppendLine($"        _{fkInfo.Rel.SourceEntityName.ToLower()}List = {fkInfo.Rel.SourceEntityName.ToLower()}Result.Records;");
            }
            sb.AppendLine($"    }}");
            sb.AppendLine();
        }

        sb.AppendLine($"    private async Task Save() => await OnSave.InvokeAsync(Item);");
        sb.AppendLine($"    private async Task Cancel() => await OnCancel.InvokeAsync();");
        sb.AppendLine($"}}");

        return sb.ToString();
    }

    // Backward compatibility overload
    private static string GenerateEditPage(DataObjects.EntityDefinition entity, DataObjects.EntityWizardOptions options, string projectName)
    {
        return GenerateEditPage(entity, new List<DataObjects.RelationshipDefinition>(), new List<DataObjects.EntityDefinition>(), options, projectName);
    }
    // ============================================================
    // LIST PAGE TEMPLATE (Relationship-Aware)
    // ============================================================

    private static string GenerateListPage(
        DataObjects.EntityDefinition entity,
        List<DataObjects.RelationshipDefinition> relationships,
        List<DataObjects.EntityDefinition> allEntities,
        DataObjects.EntityWizardOptions options,
        string projectName){
        var sb = new StringBuilder();
        var pkProp = entity.Properties.FirstOrDefault(p => p.IsPrimaryKey);
        var pkName = pkProp?.Name ?? $"{entity.Name}Id";

        var parentRels = GetParentRelationships(entity.Name, relationships);

        var displayProps = entity.Properties
            .Where(p => !p.IsPrimaryKey && !p.IsSystemField && p.Name != "Deleted")
            .Where(p => !parentRels.Any(r => r.ForeignKeyProperty == p.Name))
            .OrderBy(p => p.SortOrder)
            .Take(5)
            .ToList();

        sb.AppendLine($"@* {projectName}.App.{entity.PluralName}Page.razor - List page for {entity.Name} *@");
        sb.AppendLine($"@* Generated by Entity Builder Wizard *@");
        sb.AppendLine($"@page \"/{entity.PluralName}\"");
        sb.AppendLine($"@page \"/{{TenantCode}}/{entity.PluralName}\"");
        sb.AppendLine($"@implements IDisposable");
        sb.AppendLine($"@inject BlazorDataModel Model");
        sb.AppendLine($"@inject HttpClient Http");
        sb.AppendLine();
        sb.AppendLine($"@if (Model.Loaded && Model.LoggedIn && Model.View == _pageName) {{");
        sb.AppendLine($"    <div class=\"container-fluid\">");
        sb.AppendLine($"        <div class=\"@Model.StickyMenuClass\">");
        sb.AppendLine($"            <div class=\"d-flex justify-content-between align-items-center\">");
        sb.AppendLine($"                <h1 class=\"page-title mb-0\">");
        sb.AppendLine($"                    <i class=\"fa-solid fa-cubes me-2\"></i>{entity.PluralName}");
        sb.AppendLine($"                </h1>");
        if (!options.IsReadOnly)
        {
            sb.AppendLine($"                <button class=\"btn btn-primary\" @onclick=\"CreateNew\">");
            sb.AppendLine($"                    <i class=\"fa-solid fa-plus me-1\"></i>New {entity.Name}");
            sb.AppendLine($"                </button>");
        }
        sb.AppendLine($"            </div>");
        sb.AppendLine($"        </div>");
        sb.AppendLine();

        // Filter bar
        sb.AppendLine($"        <div class=\"card shadow-sm mb-3\">");
        sb.AppendLine($"            <div class=\"card-body py-2\">");
        sb.AppendLine($"                <div class=\"row g-2 align-items-center\">");
        sb.AppendLine($"                    <div class=\"col-md-3\">");
        sb.AppendLine($"                        <div class=\"input-group input-group-sm\">");
        sb.AppendLine($"                            <span class=\"input-group-text\"><i class=\"fa-solid fa-search\"></i></span>");
        sb.AppendLine($"                            <input type=\"text\" class=\"form-control\" placeholder=\"Search...\"");
        sb.AppendLine($"                                   @bind=\"_filter.Search\" @bind:event=\"oninput\" @bind:after=\"LoadData\" />");
        sb.AppendLine($"                        </div>");
        sb.AppendLine($"                    </div>");
        sb.AppendLine($"                    <div class=\"col-md-2\">");
        sb.AppendLine($"                        <select class=\"form-select form-select-sm\" @bind=\"_filter.PageSize\" @bind:after=\"LoadData\">");
        sb.AppendLine($"                            <option value=\"10\">10 per page</option>");
        sb.AppendLine($"                            <option value=\"25\">25 per page</option>");
        sb.AppendLine($"                            <option value=\"50\">50 per page</option>");
        sb.AppendLine($"                        </select>");
        sb.AppendLine($"                    </div>");
        sb.AppendLine($"                </div>");
        sb.AppendLine($"            </div>");
        sb.AppendLine($"        </div>");
        sb.AppendLine();

        // Data grid
        sb.AppendLine($"        @if (_loading) {{");
        sb.AppendLine($"            <div class=\"text-center py-5\"><i class=\"fa-solid fa-spinner fa-spin fa-2x\"></i></div>");
        sb.AppendLine($"        }} else if (_result.Records.Count == 0) {{");
        sb.AppendLine($"            <div class=\"text-center py-5 text-muted\">");
        sb.AppendLine($"                <i class=\"fa-solid fa-inbox fa-4x mb-3 opacity-50\"></i>");
        sb.AppendLine($"                <h4>No {entity.PluralName.ToLower()} found</h4>");
        sb.AppendLine($"            </div>");
        sb.AppendLine($"        }} else {{");
        sb.AppendLine($"            <div class=\"card shadow-sm\">");
        sb.AppendLine($"                <div class=\"table-responsive\">");
        sb.AppendLine($"                    <table class=\"table table-hover align-middle mb-0\">");
        sb.AppendLine($"                        <thead class=\"table-light\">");
        sb.AppendLine($"                            <tr>");
        foreach (var prop in displayProps)
        {
            sb.AppendLine($"                                <th>{prop.Name}</th>");
        }
        sb.AppendLine($"                                <th style=\"width: 100px;\">Actions</th>");
        sb.AppendLine($"                            </tr>");
        sb.AppendLine($"                        </thead>");
        sb.AppendLine($"                        <tbody>");
        sb.AppendLine($"                            @foreach (var item in _result.Records) {{");
        sb.AppendLine($"                                <tr>");
        foreach (var prop in displayProps)
        {
            var displayExpr = GetDisplayExpression(prop, entity.Enums);
            sb.AppendLine($"                                    <td>{displayExpr}</td>");
        }
        sb.AppendLine($"                                    <td>");
        sb.AppendLine($"                                        <div class=\"btn-group btn-group-sm\">");
        if (!options.IsReadOnly)
        {
            sb.AppendLine($"                                            <button class=\"btn btn-outline-primary\" @onclick=\"@(() => Edit(item))\"><i class=\"fa-solid fa-pen\"></i></button>");
            sb.AppendLine($"                                            <button class=\"btn btn-outline-danger\" @onclick=\"@(() => ConfirmDelete(item))\"><i class=\"fa-solid fa-trash\"></i></button>");
        }
        sb.AppendLine($"                                        </div>");
        sb.AppendLine($"                                    </td>");
        sb.AppendLine($"                                </tr>");
        sb.AppendLine($"                            }}");
        sb.AppendLine($"                        </tbody>");
        sb.AppendLine($"                    </table>");
        sb.AppendLine($"                </div>");
        sb.AppendLine($"            </div>");
        sb.AppendLine($"        }}");
        sb.AppendLine($"    </div>");

        if (!options.IsReadOnly)
        {
            sb.AppendLine();
            sb.AppendLine($"    @if (_showEdit) {{");
            sb.AppendLine($"        <{projectName}_App_Edit{entity.Name} Item=\"_editItem\" OnSave=\"SaveItem\" OnCancel=\"CloseEdit\" />");
            sb.AppendLine($"    }}");
            sb.AppendLine();
            sb.AppendLine($"    @* Delete Confirmation Modal *@");
            sb.AppendLine($"    @if (_showDeleteConfirm) {{");
            sb.AppendLine($"        <div class=\"modal fade show d-block\" tabindex=\"-1\" style=\"background: rgba(0,0,0,0.5);\">");
            sb.AppendLine($"            <div class=\"modal-dialog\">");
            sb.AppendLine($"                <div class=\"modal-content\">");
            sb.AppendLine($"                    <div class=\"modal-header bg-danger text-white\">");
            sb.AppendLine($"                        <h5 class=\"modal-title\"><i class=\"fa-solid fa-triangle-exclamation me-2\"></i>Confirm Delete</h5>");
            sb.AppendLine($"                        <button type=\"button\" class=\"btn-close btn-close-white\" @onclick=\"CancelDelete\"></button>");
            sb.AppendLine($"                    </div>");
            sb.AppendLine($"                    <div class=\"modal-body\">");
            sb.AppendLine($"                        <p>Are you sure you want to delete this {entity.Name.ToLower()}?</p>");
            sb.AppendLine($"                        <p class=\"text-muted small\">This action cannot be undone.</p>");
            sb.AppendLine($"                    </div>");
            sb.AppendLine($"                    <div class=\"modal-footer\">");
            sb.AppendLine($"                        <button type=\"button\" class=\"btn btn-secondary\" @onclick=\"CancelDelete\">@Helpers.ConfirmButtonTextCancel</button>");
            sb.AppendLine($"                        <button type=\"button\" class=\"btn btn-danger\" @onclick=\"DeleteConfirmed\">@Helpers.ConfirmButtonTextDelete</button>");
            sb.AppendLine($"                    </div>");
            sb.AppendLine($"                </div>");
            sb.AppendLine($"            </div>");
            sb.AppendLine($"        </div>");
            sb.AppendLine($"    }}");
        }

        sb.AppendLine($"}}");
        sb.AppendLine();

        // Code section
        sb.AppendLine($"@code {{");
        sb.AppendLine($"    private string _pageName = \"{entity.PluralName.ToLower()}\";");
        sb.AppendLine($"    private bool _loading = true;");
        sb.AppendLine($"    private DataObjects.{entity.Name}Filter _filter = new();");
        sb.AppendLine($"    private DataObjects.{entity.Name}FilterResult _result = new();");
        if (!options.IsReadOnly)
        {
            sb.AppendLine($"    private bool _showEdit = false;");
            sb.AppendLine($"    private DataObjects.{entity.Name} _editItem = new();");
            sb.AppendLine($"    private bool _showDeleteConfirm = false;");
            sb.AppendLine($"    private DataObjects.{entity.Name}? _deleteItem = null;");
        }
        sb.AppendLine();
        sb.AppendLine($"    public void Dispose() {{ Model.OnChange -= StateHasChanged; }}");
        sb.AppendLine();
        sb.AppendLine($"    protected override void OnInitialized() {{");
        sb.AppendLine($"        Model.View = _pageName;");
        sb.AppendLine($"        Model.OnChange += StateHasChanged;");
        sb.AppendLine($"    }}");
        sb.AppendLine();
        sb.AppendLine($"    protected override async Task OnAfterRenderAsync(bool firstRender) {{");
        sb.AppendLine($"        if (firstRender) await LoadData();");
        sb.AppendLine($"    }}");
        sb.AppendLine();
        sb.AppendLine($"    private async Task LoadData() {{");
        sb.AppendLine($"        _loading = true;");
        sb.AppendLine($"        StateHasChanged();");
        sb.AppendLine($"        _result = await Helpers.GetOrPost<DataObjects.{entity.Name}FilterResult>(");
        sb.AppendLine($"            DataObjects.Endpoints.{projectName}.Get{entity.PluralName}, _filter) ?? new();");
        sb.AppendLine($"        _loading = false;");
        sb.AppendLine($"        StateHasChanged();");
        sb.AppendLine($"    }}");

        if (!options.IsReadOnly)
        {
            sb.AppendLine();
            sb.AppendLine($"    private void CreateNew() {{ _editItem = new DataObjects.{entity.Name}(); _showEdit = true; }}");
            sb.AppendLine($"    private void Edit(DataObjects.{entity.Name} item) {{ _editItem = item; _showEdit = true; }}");
            sb.AppendLine($"    private void CloseEdit() {{ _showEdit = false; }}");
            sb.AppendLine();
            sb.AppendLine($"    private async Task SaveItem(DataObjects.{entity.Name} item) {{");
            sb.AppendLine($"        await Helpers.GetOrPost<DataObjects.{entity.Name}>(DataObjects.Endpoints.{projectName}.Save{entity.Name}, item);");
            sb.AppendLine($"        _showEdit = false;");
            sb.AppendLine($"        await LoadData();");
            sb.AppendLine($"    }}");
            sb.AppendLine();
            sb.AppendLine($"    private void ConfirmDelete(DataObjects.{entity.Name} item) {{");
            sb.AppendLine($"        _deleteItem = item;");
            sb.AppendLine($"        _showDeleteConfirm = true;");
            sb.AppendLine($"    }}");
            sb.AppendLine();
            sb.AppendLine($"    private void CancelDelete() {{");
            sb.AppendLine($"        _deleteItem = null;");
            sb.AppendLine($"        _showDeleteConfirm = false;");
            sb.AppendLine($"    }}");
            sb.AppendLine();
            sb.AppendLine($"    private async Task DeleteConfirmed() {{");
            sb.AppendLine($"        if (_deleteItem != null) {{");
            sb.AppendLine($"            await Helpers.GetOrPost<bool>(DataObjects.Endpoints.{projectName}.Delete{entity.Name}, _deleteItem.{pkName});");
            sb.AppendLine($"            _deleteItem = null;");
            sb.AppendLine($"            _showDeleteConfirm = false;");
            sb.AppendLine($"            await LoadData();");
            sb.AppendLine($"        }}");
            sb.AppendLine($"    }}");
        }

        sb.AppendLine($"}}");

        return sb.ToString();
    }

    // Backward compatibility overload
    private static string GenerateListPage(DataObjects.EntityDefinition entity, DataObjects.EntityWizardOptions options, string projectName)
    {
        return GenerateListPage(entity, new List<DataObjects.RelationshipDefinition>(), new List<DataObjects.EntityDefinition>(), options, projectName);
    }

    private static string GetDisplayExpression(DataObjects.PropertyDefinition prop, List<DataObjects.EnumDefinition> enums)
    {
        if (prop.Type == "bool")
        {
            return $"@if (item.{prop.Name}) {{ <i class=\"fa-solid fa-check text-success\"></i> }} else {{ <i class=\"fa-solid fa-minus text-muted\"></i> }}";
        }
        if (prop.Type == "DateTime")
        {
            return $"@item.{prop.Name}.ToString(\"MMM dd, yyyy\")";
        }
        if (prop.Type.StartsWith("enum:"))
        {
            return $"<span class=\"badge bg-secondary\">@item.{prop.Name}</span>";
        }
        return $"@item.{prop.Name}";
    }

    private static string GetFormField(DataObjects.PropertyDefinition prop, List<DataObjects.EnumDefinition> enums)
    {
        var sb = new StringBuilder();
        var bindExpr = $"@bind=\"Item.{prop.Name}\"";

        if (prop.Type == "bool")
        {
            sb.AppendLine($"                        <div class=\"form-check\">");
            sb.AppendLine($"                            <input type=\"checkbox\" class=\"form-check-input\" {bindExpr} />");
            sb.AppendLine($"                        </div>");
        }
        else if (prop.Type == "DateTime")
        {
            sb.AppendLine($"                        <input type=\"datetime-local\" class=\"form-control\" {bindExpr} />");
        }
        else if (prop.Type.StartsWith("enum:"))
        {
            var enumDef = enums.FirstOrDefault(e => e.Name == prop.EnumName);
            sb.AppendLine($"                        <select class=\"form-select\" {bindExpr}>");
            if (enumDef != null)
            {
                foreach (var val in enumDef.Values)
                {
                    sb.AppendLine($"                            <option value=\"{val}\">{val}</option>");
                }
            }
            sb.AppendLine($"                        </select>");
        }
        else if (prop.Type is "int" or "decimal" or "double" or "long")
        {
            sb.AppendLine($"                        <input type=\"number\" class=\"form-control\" {bindExpr} />");
        }
        else if (prop.MaxLength.HasValue && prop.MaxLength > 200)
        {
            sb.AppendLine($"                        <textarea class=\"form-control\" rows=\"3\" {bindExpr}></textarea>");
        }
        else
        {
            sb.AppendLine($"                        <input type=\"text\" class=\"form-control\" {bindExpr} />");
        }

        return sb.ToString();
    }
}

#endregion

// ---- EntityTemplates.Dashboard ----

#region EntityTemplates - Dashboard Generation
// ============================================================================
// ENTITY WIZARD - DASHBOARD PAGE TEMPLATES
// Generates dashboard pages with stats cards, recent feed, and status grid.
// Part of: EntityTemplates.App (partial)
// ============================================================================

public static partial class EntityTemplates
{
    private static string FormatColumnHeader(string column)
    {
        // Convert camelCase/PascalCase to Title Case with spaces
        var result = new StringBuilder();
        foreach (var c in column)
        {
            if (char.IsUpper(c) && result.Length > 0)
                result.Append(' ');
            result.Append(c);
        }
        return result.ToString();
    }

    // ============================================================
    // HELPER METHODS
    // ============================================================

    private static string FormatPeriodLabel(string period) => period switch
    {
        "Today" => "Today",
        "ThisWeek" => "This Week",
        "ThisMonth" => "This Month",
        "Yesterday" => "Yesterday",
        "LastWeek" => "Last Week",
        "LastMonth" => "Last Month",
        _ => period
    };
    // ============================================================
    // DASHBOARD PAGE GENERATION
    // ============================================================

    /// <summary>
    /// Generates a dashboard page from DashboardConfig.
    /// Used by FreeAudit template for GLBA compliance dashboard.
    /// </summary>
    public static string GenerateDashboardPage(
        DataObjects.DashboardConfig config,
        DataObjects.ExternalApiConfig? apiConfig,
        List<DataObjects.EntityDefinition> entities,
        string projectName){
        var sb = new StringBuilder();
        var pageName = config.PageName.ToLower();
        var controllerName = apiConfig?.ControllerName ?? "Dashboard";

        // File header
        sb.AppendLine($"@* {projectName}.App.{config.PageName}.razor - Dashboard page *@");
        sb.AppendLine($"@* Generated by Entity Builder Wizard *@");
        sb.AppendLine($"@page \"/{config.PageName}\"");
        sb.AppendLine($"@page \"/{{TenantCode}}/{config.PageName}\"");
        sb.AppendLine($"@implements IDisposable");
        sb.AppendLine($"@inject BlazorDataModel Model");
        sb.AppendLine($"@inject HttpClient Http");
        sb.AppendLine();

        // Main content wrapper
        sb.AppendLine($"@if (Model.Loaded && Model.LoggedIn && Model.View == _pageName) {{");
        sb.AppendLine($"    @if (_loading) {{");
        sb.AppendLine($"        <div class=\"container-fluid\">");
        sb.AppendLine($"            <h1 class=\"page-title\">");
        sb.AppendLine($"                <i class=\"fa-solid {config.Icon} me-2\"></i>{config.PageTitle}");
        sb.AppendLine($"            </h1>");
        sb.AppendLine($"            <div class=\"text-center py-5\"><i class=\"fa-solid fa-spinner fa-spin fa-2x\"></i></div>");
        sb.AppendLine($"        </div>");
        sb.AppendLine($"    }} else {{");
        sb.AppendLine($"        <div class=\"container-fluid\">");
        sb.AppendLine($"            <div class=\"@Model.StickyMenuClass\">");
        sb.AppendLine($"                <h1 class=\"page-title\">");
        sb.AppendLine($"                    <i class=\"fa-solid {config.Icon} me-2\"></i>{config.PageTitle}");
        sb.AppendLine($"                </h1>");
        sb.AppendLine($"            </div>");
        sb.AppendLine();

        // Stats Cards Row
        sb.AppendLine($"            @* Stats Cards *@");
        sb.AppendLine($"            <div class=\"row mb-4\">");

        foreach (var period in config.StatsPeriods)
        {
            var periodLower = period.Replace(" ", "");
            sb.AppendLine($"                <div class=\"col-md-4\">");
            sb.AppendLine($"                    <div class=\"card shadow-sm\">");
            sb.AppendLine($"                        <div class=\"card-body text-center\">");
            sb.AppendLine($"                            <h2 class=\"mb-0\">@_stats.{periodLower}.ToString(\"N0\")</h2>");
            sb.AppendLine($"                            <p class=\"text-muted mb-0\">{FormatPeriodLabel(period)}</p>");
            sb.AppendLine($"                        </div>");
            sb.AppendLine($"                    </div>");
            sb.AppendLine($"                </div>");
        }

        sb.AppendLine($"            </div>");
        sb.AppendLine();

        // Recent Events Section
        sb.AppendLine($"            @* Recent Events *@");
        sb.AppendLine($"            <div class=\"card shadow-sm mb-4\">");
        sb.AppendLine($"                <div class=\"card-header d-flex justify-content-between align-items-center\">");
        sb.AppendLine($"                    <h5 class=\"mb-0\">");
        sb.AppendLine($"                        <i class=\"fa-solid fa-clock-rotate-left me-2\"></i>Recent Events");
        sb.AppendLine($"                    </h5>");
        sb.AppendLine($"                    <a href=\"@Helpers.BuildUrl(\"{config.RecentFeedEntity}s\")\" class=\"btn btn-sm btn-outline-primary\">");
        sb.AppendLine($"                        View All <i class=\"fa-solid fa-arrow-right ms-1\"></i>");
        sb.AppendLine($"                    </a>");
        sb.AppendLine($"                </div>");
        sb.AppendLine($"                <div class=\"card-body p-0\">");
        sb.AppendLine($"                    <div class=\"table-responsive\">");
        sb.AppendLine($"                        <table class=\"table table-hover mb-0\">");
        sb.AppendLine($"                            <thead class=\"table-light\">");
        sb.AppendLine($"                                <tr>");

        foreach (var col in config.RecentFeedColumns)
        {
            sb.AppendLine($"                                    <th>{FormatColumnHeader(col)}</th>");
        }

        sb.AppendLine($"                                </tr>");
        sb.AppendLine($"                            </thead>");
        sb.AppendLine($"                            <tbody>");
        sb.AppendLine($"                                @foreach (var evt in _recentEvents) {{");
        sb.AppendLine($"                                    <tr>");

        foreach (var col in config.RecentFeedColumns)
        {
            var format = GetColumnFormat(col);
            sb.AppendLine($"                                        <td>@evt.{col}{format}</td>");
        }

        sb.AppendLine($"                                    </tr>");
        sb.AppendLine($"                                }}");
        sb.AppendLine($"                            </tbody>");
        sb.AppendLine($"                        </table>");
        sb.AppendLine($"                    </div>");
        sb.AppendLine($"                </div>");
        sb.AppendLine($"            </div>");
        sb.AppendLine();

        // Status Grid Section (if configured)
        if (!string.IsNullOrEmpty(config.StatusGridEntity))
        {
            sb.AppendLine($"            @* Source Systems Status *@");
            sb.AppendLine($"            <div class=\"card shadow-sm\">");
            sb.AppendLine($"                <div class=\"card-header d-flex justify-content-between align-items-center\">");
            sb.AppendLine($"                    <h5 class=\"mb-0\">");
            sb.AppendLine($"                        <i class=\"fa-solid fa-server me-2\"></i>Source Systems");
            sb.AppendLine($"                    </h5>");
            sb.AppendLine($"                    <a href=\"@Helpers.BuildUrl(\"{config.StatusGridEntity}s\")\" class=\"btn btn-sm btn-outline-primary\">");
            sb.AppendLine($"                        Manage <i class=\"fa-solid fa-gear ms-1\"></i>");
            sb.AppendLine($"                    </a>");
            sb.AppendLine($"                </div>");
            sb.AppendLine($"                <div class=\"card-body p-0\">");
            sb.AppendLine($"                    <div class=\"table-responsive\">");
            sb.AppendLine($"                        <table class=\"table table-hover mb-0\">");
            sb.AppendLine($"                            <thead class=\"table-light\">");
            sb.AppendLine($"                                <tr>");
            sb.AppendLine($"                                    <th>System</th>");
            sb.AppendLine($"                                    <th>Status</th>");

            if (!string.IsNullOrEmpty(config.StatusLastActivityProperty))
            {
                sb.AppendLine($"                                    <th>Last Event</th>");
            }

            if (!string.IsNullOrEmpty(config.StatusCountProperty))
            {
                sb.AppendLine($"                                    <th>Total Events</th>");
            }

            sb.AppendLine($"                                </tr>");
            sb.AppendLine($"                            </thead>");
            sb.AppendLine($"                            <tbody>");
            sb.AppendLine($"                                @foreach (var src in _sources) {{");
            sb.AppendLine($"                                    <tr>");
            sb.AppendLine($"                                        <td>@(src.DisplayName ?? src.Name)</td>");
            sb.AppendLine($"                                        <td>");
            sb.AppendLine($"                                            @if (src.{config.StatusActiveProperty}) {{");
            sb.AppendLine($"                                                <span class=\"badge bg-success\"><i class=\"fa-solid fa-circle me-1\"></i>Active</span>");
            sb.AppendLine($"                                            }} else {{");
            sb.AppendLine($"                                                <span class=\"badge bg-secondary\"><i class=\"fa-solid fa-circle me-1\"></i>Inactive</span>");
            sb.AppendLine($"                                            }}");
            sb.AppendLine($"                                        </td>");

            if (!string.IsNullOrEmpty(config.StatusLastActivityProperty))
            {
                sb.AppendLine($"                                        <td>@GetTimeAgo(src.{config.StatusLastActivityProperty})</td>");
            }

            if (!string.IsNullOrEmpty(config.StatusCountProperty))
            {
                sb.AppendLine($"                                        <td>@src.{config.StatusCountProperty}.ToString(\"N0\")</td>");
            }

            sb.AppendLine($"                                    </tr>");
            sb.AppendLine($"                                }}");
            sb.AppendLine($"                            </tbody>");
            sb.AppendLine($"                        </table>");
            sb.AppendLine($"                    </div>");
            sb.AppendLine($"                </div>");
            sb.AppendLine($"            </div>");
        }

        sb.AppendLine($"        </div>");
        sb.AppendLine($"    }}");
        sb.AppendLine($"}}");
        sb.AppendLine();

        // Code section
        sb.AppendLine($"@code {{");
        sb.AppendLine($"    [Parameter] public string? TenantCode {{ get; set; }}");
        sb.AppendLine();
        sb.AppendLine($"    private string _pageName = \"{pageName}\";");
        sb.AppendLine($"    private bool _loading = true;");
        sb.AppendLine($"    private bool _loadedData = false;");
        sb.AppendLine();
        sb.AppendLine($"    private DataObjects.{controllerName}Stats _stats = new();");
        sb.AppendLine($"    private List<DataObjects.{config.RecentFeedEntity}> _recentEvents = new();");

        if (!string.IsNullOrEmpty(config.StatusGridEntity))
        {
            sb.AppendLine($"    private List<DataObjects.{config.StatusGridEntity}> _sources = new();");
        }

        sb.AppendLine();
        sb.AppendLine($"    public void Dispose()");
        sb.AppendLine($"    {{");
        sb.AppendLine($"        Model.OnChange -= OnDataModelUpdated;");
        sb.AppendLine($"    }}");
        sb.AppendLine();
        sb.AppendLine($"    protected override void OnInitialized()");
        sb.AppendLine($"    {{");
        sb.AppendLine($"        Model.View = _pageName;");
        sb.AppendLine($"        Model.OnChange += OnDataModelUpdated;");
        sb.AppendLine($"    }}");
        sb.AppendLine();
        sb.AppendLine($"    protected override async Task OnAfterRenderAsync(bool firstRender)");
        sb.AppendLine($"    {{");
        sb.AppendLine($"        if (firstRender)");
        sb.AppendLine($"        {{");
        sb.AppendLine($"            Model.TenantCodeFromUrl = TenantCode;");
        sb.AppendLine($"        }}");
        sb.AppendLine();
        sb.AppendLine($"        if (Model.Loaded && Model.LoggedIn && !_loadedData)");
        sb.AppendLine($"        {{");
        sb.AppendLine($"            _loadedData = true;");
        sb.AppendLine($"            await LoadDataAsync();");
        sb.AppendLine($"        }}");
        sb.AppendLine($"    }}");
        sb.AppendLine();
        sb.AppendLine($"    private void OnDataModelUpdated()");
        sb.AppendLine($"    {{");
        sb.AppendLine($"        if (Model.View == _pageName)");
        sb.AppendLine($"        {{");
        sb.AppendLine($"            StateHasChanged();");
        sb.AppendLine($"        }}");
        sb.AppendLine($"    }}");
        sb.AppendLine();
        sb.AppendLine($"    private async Task LoadDataAsync()");
        sb.AppendLine($"    {{");
        sb.AppendLine($"        _loading = true;");
        sb.AppendLine($"        StateHasChanged();");
        sb.AppendLine();
        sb.AppendLine($"        try");
        sb.AppendLine($"        {{");

        var routePrefix = apiConfig?.RoutePrefix ?? "api/dashboard";

        sb.AppendLine($"            // Load all data in parallel");
        sb.AppendLine($"            var statsTask = Helpers.GetOrPost<DataObjects.{controllerName}Stats>(\"{routePrefix}/stats/summary\");");
        sb.AppendLine($"            var eventsTask = Helpers.GetOrPost<List<DataObjects.{config.RecentFeedEntity}>>(\"{routePrefix}/events/recent?limit={config.RecentFeedLimit}\");");

        if (!string.IsNullOrEmpty(config.StatusGridEntity))
        {
            sb.AppendLine($"            var sourcesTask = Helpers.GetOrPost<List<DataObjects.{config.StatusGridEntity}>>(\"{routePrefix}/sources/status\");");
            sb.AppendLine();
            sb.AppendLine($"            await Task.WhenAll(statsTask, eventsTask, sourcesTask);");
        }
        else
        {
            sb.AppendLine();
            sb.AppendLine($"            await Task.WhenAll(statsTask, eventsTask);");
        }

        sb.AppendLine();
        sb.AppendLine($"            _stats = await statsTask ?? new();");
        sb.AppendLine($"            _recentEvents = await eventsTask ?? new();");

        if (!string.IsNullOrEmpty(config.StatusGridEntity))
        {
            sb.AppendLine($"            _sources = await sourcesTask ?? new();");
        }

        sb.AppendLine($"        }}");
        sb.AppendLine($"        catch (Exception ex)");
        sb.AppendLine($"        {{");
        sb.AppendLine($"            Model.ErrorMessage($\"Error loading dashboard: {{ex.Message}}\");");
        sb.AppendLine($"        }}");
        sb.AppendLine();
        sb.AppendLine($"        _loading = false;");
        sb.AppendLine($"        StateHasChanged();");
        sb.AppendLine($"    }}");
        sb.AppendLine();

        // Time ago helper
        sb.AppendLine($"    private string GetTimeAgo(DateTime? dt)");
        sb.AppendLine($"    {{");
        sb.AppendLine($"        if (!dt.HasValue) return \"Never\";");
        sb.AppendLine($"        var diff = DateTime.UtcNow - dt.Value;");
        sb.AppendLine($"        if (diff.TotalMinutes < 1) return \"Just now\";");
        sb.AppendLine($"        if (diff.TotalMinutes < 60) return $\"{{(int)diff.TotalMinutes}} min ago\";");
        sb.AppendLine($"        if (diff.TotalHours < 24) return $\"{{(int)diff.TotalHours}} hours ago\";");
        sb.AppendLine($"        return $\"{{(int)diff.TotalDays}} days ago\";");
        sb.AppendLine($"    }}");
        sb.AppendLine($"}}");

        return sb.ToString();
    }

    private static string GetColumnFormat(string column)
    {
        // Add appropriate formatting based on column name
        if (column.Contains("At") || column.Contains("Date"))
            return ".ToString(\"g\")";
        return "";
    }
}

#endregion

// ---- EntityTemplates.ApiKeyAuth ----

#region EntityTemplates - API Key Auth Generation
// ============================================================================
// ENTITY WIZARD - API KEY AUTHENTICATION TEMPLATES
// Generates middleware and DataAccess for API key authentication.
// Part of: EntityTemplates.App (partial)
// ============================================================================

public static partial class EntityTemplates
{
    // ============================================================
    // API KEY DATA ACCESS GENERATION
    // ============================================================

    /// <summary>
    /// Generates DataAccess methods for API key validation and generation.
    /// </summary>
    public static string GenerateApiKeyDataAccess(
        DataObjects.ExternalApiConfig config,
        DataObjects.EntityDefinition apiKeyEntity,
        string projectName){
        var sb = new StringBuilder();
        var entityName = config.ApiKeyEntity;
        var keyProperty = config.ApiKeyProperty;

        // Add using statements and namespace
        sb.AppendLine($"using Microsoft.EntityFrameworkCore;");
        sb.AppendLine($"using System.Security.Cryptography;");
        sb.AppendLine($"using System.Text;");
        sb.AppendLine();
        sb.AppendLine($"namespace {projectName};");
        sb.AppendLine();
        sb.AppendLine($"// ============================================================================");
        sb.AppendLine($"// API KEY AUTHENTICATION DATA ACCESS");
        sb.AppendLine($"// ============================================================================");
        sb.AppendLine();

        // Generate the IDataAccess partial interface FIRST
        sb.AppendLine($"/// <summary>API Key authentication interface extensions.</summary>");
        sb.AppendLine($"public partial interface IDataAccess");
        sb.AppendLine($"{{");
        sb.AppendLine($"    /// <summary>Validates an API key and returns the associated source system.</summary>");
        sb.AppendLine($"    Task<DataObjects.{entityName}?> ValidateApiKeyAsync(string apiKey);");
        sb.AppendLine();
        sb.AppendLine($"    /// <summary>Generates a new API key for a source system.</summary>");
        sb.AppendLine($"    Task<string> GenerateApiKeyAsync(Guid {entityName.ToLower()}Id);");
        sb.AppendLine();
        sb.AppendLine($"    /// <summary>Gets all source systems for the dashboard.</summary>");
        sb.AppendLine($"    Task<List<DataObjects.{entityName}>> Get{entityName}sAsync();");
        sb.AppendLine($"}}");
        sb.AppendLine();

        // Now generate the DataAccess partial class implementation
        sb.AppendLine($"public partial class DataAccess");
        sb.AppendLine($"{{");
        sb.AppendLine($"    #region API Key Authentication");
        sb.AppendLine();

        // ValidateApiKey - use inline mapping instead of calling mapper method
        sb.AppendLine($"    /// <summary>");
        sb.AppendLine($"    /// Validates an API key and returns the associated source system.");
        sb.AppendLine($"    /// Returns null if key is invalid or source is inactive.");
        sb.AppendLine($"    /// </summary>");
        sb.AppendLine($"    public async Task<DataObjects.{entityName}?> ValidateApiKeyAsync(string apiKey)");
        sb.AppendLine($"    {{");
        sb.AppendLine($"        if (string.IsNullOrEmpty(apiKey))");
        sb.AppendLine($"            return null;");
        sb.AppendLine();
        sb.AppendLine($"        // Hash the provided key");
        sb.AppendLine($"        var hash = HashApiKey(apiKey);");
        sb.AppendLine();
        sb.AppendLine($"        // Look up by hash");
        sb.AppendLine($"        var record = await data.{entityName}s");
        sb.AppendLine($"            .FirstOrDefaultAsync(x => x.{keyProperty} == hash && x.IsActive);");
        sb.AppendLine();
        sb.AppendLine($"        if (record == null)");
        sb.AppendLine($"            return null;");
        sb.AppendLine();
        sb.AppendLine($"        // Return as DataObject with inline mapping");
        sb.AppendLine($"        return new DataObjects.{entityName}");
        sb.AppendLine($"        {{");
        // Map properties from entity
        foreach (var prop in apiKeyEntity.Properties.Where(p => !p.IsSystemField || p.IsPrimaryKey))
        {
            sb.AppendLine($"            {prop.Name} = record.{prop.Name},");
        }
        sb.AppendLine($"        }};");
        sb.AppendLine($"    }}");
        sb.AppendLine();

        // GenerateApiKey
        sb.AppendLine($"    /// <summary>");
        sb.AppendLine($"    /// Generates a new API key for a source system.");
        sb.AppendLine($"    /// Returns the plaintext key (show to user ONCE, then store only the hash).");
        sb.AppendLine($"    /// </summary>");
        sb.AppendLine($"    public async Task<string> GenerateApiKeyAsync(Guid {entityName.ToLower()}Id)");
        sb.AppendLine($"    {{");
        sb.AppendLine($"        var record = await data.{entityName}s");
        sb.AppendLine($"            .FirstOrDefaultAsync(x => x.{entityName}Id == {entityName.ToLower()}Id);");
        sb.AppendLine();
        sb.AppendLine($"        if (record == null)");
        sb.AppendLine($"            throw new InvalidOperationException(\"{entityName} not found\");");
        sb.AppendLine();
        sb.AppendLine($"        // Generate random 32-byte key and encode as Base64");
        sb.AppendLine($"        var bytes = new byte[32];");
        sb.AppendLine($"        RandomNumberGenerator.Fill(bytes);");
        sb.AppendLine($"        var plaintextKey = Convert.ToBase64String(bytes);");
        sb.AppendLine();
        sb.AppendLine($"        // Store only the hash");
        sb.AppendLine($"        record.{keyProperty} = HashApiKey(plaintextKey);");
        sb.AppendLine($"        await data.SaveChangesAsync();");
        sb.AppendLine();
        sb.AppendLine($"        // Return plaintext key (caller shows to user ONCE)");
        sb.AppendLine($"        return plaintextKey;");
        sb.AppendLine($"    }}");
        sb.AppendLine();

        // GetSourceSystems for dashboard
        sb.AppendLine($"    /// <summary>");
        sb.AppendLine($"    /// Gets all source systems for the dashboard.");
        sb.AppendLine($"    /// </summary>");
        sb.AppendLine($"    public async Task<List<DataObjects.{entityName}>> Get{entityName}sAsync()");
        sb.AppendLine($"    {{");
        sb.AppendLine($"        return await data.{entityName}s");
        sb.AppendLine($"            .Where(x => x.IsActive)");
        sb.AppendLine($"            .OrderBy(x => x.Name)");
        sb.AppendLine($"            .Select(x => new DataObjects.{entityName}");
        sb.AppendLine($"            {{");
        foreach (var prop in apiKeyEntity.Properties.Where(p => !p.IsSystemField || p.IsPrimaryKey))
        {
            sb.AppendLine($"                {prop.Name} = x.{prop.Name},");
        }
        sb.AppendLine($"            }})");
        sb.AppendLine($"            .ToListAsync();");
        sb.AppendLine($"    }}");
        sb.AppendLine();

        // HashApiKey helper
        sb.AppendLine($"    /// <summary>");
        sb.AppendLine($"    /// Hashes an API key using SHA256.");
        sb.AppendLine($"    /// </summary>");
        sb.AppendLine($"    private static string HashApiKey(string key)");
        sb.AppendLine($"    {{");
        sb.AppendLine($"        using var sha256 = SHA256.Create();");
        sb.AppendLine($"        var bytes = Encoding.UTF8.GetBytes(key);");
        sb.AppendLine($"        var hash = sha256.ComputeHash(bytes);");
        sb.AppendLine($"        return Convert.ToBase64String(hash);");
        sb.AppendLine($"    }}");
        sb.AppendLine();
        sb.AppendLine($"    #endregion");
        sb.AppendLine($"}}");

        return sb.ToString();
    }

    // ============================================================
    // EDIT PAGE SNIPPET FOR API KEY GENERATION UI
    // ============================================================

    /// <summary>
    /// Generates the API key generation section to add to the SourceSystem edit page.
    /// </summary>
    public static string GenerateApiKeyEditSection(DataObjects.ExternalApiConfig config, string projectName)
    {
        var sb = new StringBuilder();
        var entityName = config.ApiKeyEntity;

        sb.AppendLine($"@* API Key Generation Section - Add to Edit{entityName}.razor *@");
        sb.AppendLine();
        sb.AppendLine($"@if (!_isNew) {{");
        sb.AppendLine($"    <div class=\"card mb-3\">");
        sb.AppendLine($"        <div class=\"card-header\">");
        sb.AppendLine($"            <h6 class=\"mb-0\"><i class=\"fa-solid fa-key me-2\"></i>API Key</h6>");
        sb.AppendLine($"        </div>");
        sb.AppendLine($"        <div class=\"card-body\">");
        sb.AppendLine($"            @if (string.IsNullOrEmpty(_newApiKey)) {{");
        sb.AppendLine($"                <p class=\"text-muted mb-2\">");
        sb.AppendLine($"                    <i class=\"fa-solid fa-circle-info me-1\"></i>");
        sb.AppendLine($"                    API key is stored securely (hashed). Generate a new key if needed.");
        sb.AppendLine($"                </p>");
        sb.AppendLine($"                <button type=\"button\" class=\"btn btn-warning\" @onclick=\"GenerateNewApiKey\">");
        sb.AppendLine($"                    <i class=\"fa-solid fa-rotate me-1\"></i>Generate New API Key");
        sb.AppendLine($"                </button>");
        sb.AppendLine($"                <small class=\"text-danger d-block mt-2\">");
        sb.AppendLine($"                    Warning: This will invalidate the existing key!");
        sb.AppendLine($"                </small>");
        sb.AppendLine($"            }} else {{");
        sb.AppendLine($"                <div class=\"alert alert-success\">");
        sb.AppendLine($"                    <h6 class=\"alert-heading\"><i class=\"fa-solid fa-check-circle me-1\"></i>New API Key Generated</h6>");
        sb.AppendLine($"                    <p class=\"mb-2\">Copy this key now. <strong>It will not be shown again!</strong></p>");
        sb.AppendLine($"                    <div class=\"input-group\">");
        sb.AppendLine($"                        <input type=\"text\" class=\"form-control font-monospace\" value=\"@_newApiKey\" readonly />");
        sb.AppendLine($"                        <button class=\"btn btn-outline-secondary\" type=\"button\" @onclick=\"CopyApiKey\">");
        sb.AppendLine($"                            <i class=\"fa-solid fa-copy\"></i>");
        sb.AppendLine($"                        </button>");
        sb.AppendLine($"                    </div>");
        sb.AppendLine($"                </div>");
        sb.AppendLine($"            }}");
        sb.AppendLine($"        </div>");
        sb.AppendLine($"    </div>");
        sb.AppendLine($"}}");
        sb.AppendLine();
        sb.AppendLine($"@code {{");
        sb.AppendLine($"    private string _newApiKey = \"\";");
        sb.AppendLine();
        sb.AppendLine($"    private async Task GenerateNewApiKey()");
        sb.AppendLine($"    {{");
        sb.AppendLine($"        try");
        sb.AppendLine($"        {{");
        sb.AppendLine($"            var result = await Http.PostAsJsonAsync($\"api/{config.RoutePrefix}/sources/{{_item.{entityName}Id}}/generate-key\", new {{}});");
        sb.AppendLine($"            if (result.IsSuccessStatusCode)");
        sb.AppendLine($"            {{");
        sb.AppendLine($"                var response = await result.Content.ReadFromJsonAsync<GenerateKeyResponse>();");
        sb.AppendLine($"                _newApiKey = response?.ApiKey ?? \"\";");
        sb.AppendLine($"                StateHasChanged();");
        sb.AppendLine($"            }}");
        sb.AppendLine($"            else");
        sb.AppendLine($"            {{");
        sb.AppendLine($"                Model.ErrorMessage(\"Failed to generate API key\");");
        sb.AppendLine($"            }}");
        sb.AppendLine($"        }}");
        sb.AppendLine($"        catch (Exception ex)");
        sb.AppendLine($"        {{");
        sb.AppendLine($"            Model.ErrorMessage($\"Error: {{ex.Message}}\");");
        sb.AppendLine($"        }}");
        sb.AppendLine($"    }}");
        sb.AppendLine();
        sb.AppendLine($"    private async Task CopyApiKey()");
        sb.AppendLine($"    {{");
        sb.AppendLine($"        await JSRuntime.InvokeVoidAsync(\"navigator.clipboard.writeText\", _newApiKey);");
        sb.AppendLine($"        Model.SuccessMessage(\"API key copied to clipboard\");");
        sb.AppendLine($"    }}");
        sb.AppendLine();
        sb.AppendLine($"    private class GenerateKeyResponse");
        sb.AppendLine($"    {{");
        sb.AppendLine($"        public string ApiKey {{ get; set; }} = \"\";");
        sb.AppendLine($"    }}");
        sb.AppendLine($"}}");

        return sb.ToString();
    }
    // ============================================================
    // API KEY MIDDLEWARE GENERATION
    // ============================================================

    /// <summary>
    /// Generates API key authentication middleware.
    /// Validates Bearer token against hashed keys in database.
    /// </summary>
    public static string GenerateApiKeyMiddleware(DataObjects.ExternalApiConfig config, string projectName)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"using System.Security.Cryptography;");
        sb.AppendLine($"using System.Text;");
        sb.AppendLine($"using Microsoft.EntityFrameworkCore;");
        sb.AppendLine();
        sb.AppendLine($"namespace {projectName}.Middleware;");
        sb.AppendLine();
        sb.AppendLine($"// ============================================================================");
        sb.AppendLine($"// API KEY AUTHENTICATION MIDDLEWARE");
        sb.AppendLine($"// Validates Bearer token for external API endpoints.");
        sb.AppendLine($"// Generated by FreeManager for template: FreeAudit");
        sb.AppendLine($"// ============================================================================");
        sb.AppendLine();
        sb.AppendLine($"public class ApiKeyMiddleware");
        sb.AppendLine($"{{");
        sb.AppendLine($"    private readonly RequestDelegate _next;");
        sb.AppendLine();
        sb.AppendLine($"    public ApiKeyMiddleware(RequestDelegate next)");
        sb.AppendLine($"    {{");
        sb.AppendLine($"        _next = next;");
        sb.AppendLine($"    }}");
        sb.AppendLine();
        sb.AppendLine($"    public async Task InvokeAsync(HttpContext context, IDataAccess da)");
        sb.AppendLine($"    {{");
        sb.AppendLine($"        // Only apply to external API paths that need API key auth");
        sb.AppendLine($"        var path = context.Request.Path.Value ?? \"\";");
        sb.AppendLine($"        if (!path.StartsWith(\"/{config.RoutePrefix}/events\", StringComparison.OrdinalIgnoreCase))");
        sb.AppendLine($"        {{");
        sb.AppendLine($"            await _next(context);");
        sb.AppendLine($"            return;");
        sb.AppendLine($"        }}");
        sb.AppendLine();
        sb.AppendLine($"        // Extract API key from Authorization header");
        sb.AppendLine($"        if (!context.Request.Headers.TryGetValue(\"Authorization\", out var authHeader))");
        sb.AppendLine($"        {{");
        sb.AppendLine($"            await WriteUnauthorized(context, \"missing_api_key\", \"Authorization header required\");");
        sb.AppendLine($"            return;");
        sb.AppendLine($"        }}");
        sb.AppendLine();
        sb.AppendLine($"        var headerValue = authHeader.ToString();");
        sb.AppendLine($"        if (!headerValue.StartsWith(\"Bearer \", StringComparison.OrdinalIgnoreCase))");
        sb.AppendLine($"        {{");
        sb.AppendLine($"            await WriteUnauthorized(context, \"invalid_format\", \"Authorization header must use Bearer scheme\");");
        sb.AppendLine($"            return;");
        sb.AppendLine($"        }}");
        sb.AppendLine();
        sb.AppendLine($"        var apiKey = headerValue.Substring(7).Trim();");
        sb.AppendLine($"        if (string.IsNullOrEmpty(apiKey))");
        sb.AppendLine($"        {{");
        sb.AppendLine($"            await WriteUnauthorized(context, \"empty_api_key\", \"API key cannot be empty\");");
        sb.AppendLine($"            return;");
        sb.AppendLine($"        }}");
        sb.AppendLine();
        sb.AppendLine($"        // Validate API key");
        sb.AppendLine($"        var source = await da.ValidateApiKeyAsync(apiKey);");
        sb.AppendLine($"        if (source == null)");
        sb.AppendLine($"        {{");
        sb.AppendLine($"            await WriteUnauthorized(context, \"invalid_api_key\", \"API key is invalid or inactive\");");
        sb.AppendLine($"            return;");
        sb.AppendLine($"        }}");
        sb.AppendLine();
        sb.AppendLine($"        // Store source system in context for controller");
        sb.AppendLine($"        context.Items[\"SourceSystem\"] = source;");
        sb.AppendLine();
        sb.AppendLine($"        await _next(context);");
        sb.AppendLine($"    }}");
        sb.AppendLine();
        sb.AppendLine($"    private static async Task WriteUnauthorized(HttpContext context, string error, string message)");
        sb.AppendLine($"    {{");
        sb.AppendLine($"        context.Response.StatusCode = 401;");
        sb.AppendLine($"        context.Response.ContentType = \"application/json\";");
        sb.AppendLine($"        await context.Response.WriteAsJsonAsync(new {{ error, message }});");
        sb.AppendLine($"    }}");
        sb.AppendLine($"}}");
        sb.AppendLine();

        // Extension method for middleware registration
        sb.AppendLine($"// ============================================================================");
        sb.AppendLine($"// MIDDLEWARE EXTENSION");
        sb.AppendLine($"// ============================================================================");
        sb.AppendLine();
        sb.AppendLine($"public static class ApiKeyMiddlewareExtensions");
        sb.AppendLine($"{{");
        sb.AppendLine($"    public static IApplicationBuilder UseApiKeyAuth(this IApplicationBuilder builder)");
        sb.AppendLine($"    {{");
        sb.AppendLine($"        return builder.UseMiddleware<ApiKeyMiddleware>();");
        sb.AppendLine($"    }}");
        sb.AppendLine($"}}");

        return sb.ToString();
    }

    // ============================================================
    // PROGRAM.CS SNIPPET GENERATION
    // ============================================================

    /// <summary>
    /// Generates the snippet to add to Program.cs for middleware registration.
    /// </summary>
    public static string GenerateProgramCsSnippet(DataObjects.ExternalApiConfig config, string projectName)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"// ============================================================================");
        sb.AppendLine($"// ADD TO PROGRAM.CS - Middleware Registration");
        sb.AppendLine($"// ============================================================================");
        sb.AppendLine();
        sb.AppendLine($"// Add this using statement:");
        sb.AppendLine($"// using {projectName}.Middleware;");
        sb.AppendLine();
        sb.AppendLine($"// Add this line BEFORE app.UseAuthentication() and app.UseAuthorization():");
        sb.AppendLine($"// app.UseApiKeyAuth();");
        sb.AppendLine();
        sb.AppendLine($"// Full middleware order:");
        sb.AppendLine($"// app.UseRouting();");
        sb.AppendLine($"// app.UseApiKeyAuth();      // <-- Add this line");
        sb.AppendLine($"// app.UseAuthentication();");
        sb.AppendLine($"// app.UseAuthorization();");
        sb.AppendLine($"// app.MapControllers();");

        return sb.ToString();
    }
}

#endregion

// ---- EntityTemplates.ExternalApi ----

#region EntityTemplates - External API Generation
// ============================================================================
// ENTITY WIZARD - EXTERNAL API TEMPLATES
// Generates external API controller, middleware, and DataAccess for FreeAudit-style apps.
// Part of: EntityTemplates.App (partial)
// ============================================================================

public static partial class EntityTemplates
{
    // ============================================================
    // EXTERNAL API CONTROLLER GENERATION
    // ============================================================

    /// <summary>
    /// Generates an external API controller for receiving events from source systems.
    /// Used by FreeAudit template for GLBA compliance.
    /// </summary>
    public static string GenerateExternalApiController(
        DataObjects.ExternalApiConfig config,
        DataObjects.EntityDefinition eventEntity,
        DataObjects.EntityDefinition? apiKeyEntity,
        string projectName){
        var sb = new StringBuilder();
        var entityName = config.EntityName;
        var controllerName = config.ControllerName;
        var routePrefix = config.RoutePrefix;

        // File header
        sb.AppendLine("using Microsoft.AspNetCore.Mvc;");
        sb.AppendLine("using Microsoft.AspNetCore.Authorization;");
        sb.AppendLine();
        sb.AppendLine($"namespace {projectName}.Controllers;");
        sb.AppendLine();
        sb.AppendLine($"// ============================================================================");
        sb.AppendLine($"// {controllerName.ToUpper()} EXTERNAL API CONTROLLER");
        sb.AppendLine($"// Receives events from external source systems via API key authentication.");
        sb.AppendLine($"// Generated by FreeManager for template: FreeAudit");
        sb.AppendLine($"// ============================================================================");
        sb.AppendLine();

        sb.AppendLine($"[ApiController]");
        sb.AppendLine($"[Route(\"{routePrefix}\")]");
        sb.AppendLine($"public class {controllerName}Controller : ControllerBase");
        sb.AppendLine($"{{");
        sb.AppendLine($"    private readonly IDataAccess _da;");
        sb.AppendLine();
        sb.AppendLine($"    public {controllerName}Controller(IDataAccess da)");
        sb.AppendLine($"    {{");
        sb.AppendLine($"        _da = da;");
        sb.AppendLine($"    }}");
        sb.AppendLine();

        // POST /events - Single event (API key auth via middleware)
        sb.AppendLine($"    // ============================================================");
        sb.AppendLine($"    // EXTERNAL ENDPOINTS (API Key Auth via middleware)");
        sb.AppendLine($"    // ============================================================");
        sb.AppendLine();
        sb.AppendLine($"    /// <summary>");
        sb.AppendLine($"    /// Receive a single event from an external source system.");
        sb.AppendLine($"    /// Requires API key in Authorization header: Bearer {{api-key}}");
        sb.AppendLine($"    /// </summary>");
        sb.AppendLine($"    [HttpPost(\"events\")]");
        sb.AppendLine($"    public async Task<ActionResult<DataObjects.{controllerName}EventResponse>> PostEvent(");
        sb.AppendLine($"        [FromBody] DataObjects.{controllerName}EventRequest request)");
        sb.AppendLine($"    {{");
        sb.AppendLine($"        // Get source system from middleware (set by ApiKeyMiddleware)");
        sb.AppendLine($"        var source = HttpContext.Items[\"SourceSystem\"] as DataObjects.{config.ApiKeyEntity};");
        sb.AppendLine($"        if (source == null)");
        sb.AppendLine($"        {{");
        sb.AppendLine($"            return Unauthorized(new {{ error = \"invalid_api_key\", message = \"API key is invalid or inactive\" }});");
        sb.AppendLine($"        }}");
        sb.AppendLine();
        sb.AppendLine($"        var result = await _da.Process{controllerName}EventAsync(request, source.{config.ApiKeyEntity}Id);");
        sb.AppendLine();
        sb.AppendLine($"        return result.Status switch");
        sb.AppendLine($"        {{");
        sb.AppendLine($"            \"accepted\" => Created($\"/{routePrefix}/events/{{result.EventId}}\", result),");
        sb.AppendLine($"            \"duplicate\" => Conflict(result),");
        sb.AppendLine($"            _ => BadRequest(result)");
        sb.AppendLine($"        }};");
        sb.AppendLine($"    }}");
        sb.AppendLine();

        // POST /events/batch - Batch events
        if (config.AllowBatch)
        {
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// Receive a batch of events from an external source system.");
            sb.AppendLine($"    /// Maximum {config.BatchLimit} events per request.");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    [HttpPost(\"events/batch\")]");
            sb.AppendLine($"    public async Task<ActionResult<DataObjects.{controllerName}BatchResponse>> PostBatch(");
            sb.AppendLine($"        [FromBody] List<DataObjects.{controllerName}EventRequest> events)");
            sb.AppendLine($"    {{");
            sb.AppendLine($"        var source = HttpContext.Items[\"SourceSystem\"] as DataObjects.{config.ApiKeyEntity};");
            sb.AppendLine($"        if (source == null)");
            sb.AppendLine($"        {{");
            sb.AppendLine($"            return Unauthorized(new {{ error = \"invalid_api_key\", message = \"API key is invalid or inactive\" }});");
            sb.AppendLine($"        }}");
            sb.AppendLine();
            sb.AppendLine($"        if (events.Count > {config.BatchLimit})");
            sb.AppendLine($"        {{");
            sb.AppendLine($"            return BadRequest(new {{ error = \"batch_too_large\", message = \"Maximum {config.BatchLimit} events per batch\" }});");
            sb.AppendLine($"        }}");
            sb.AppendLine();
            sb.AppendLine($"        var result = await _da.Process{controllerName}BatchAsync(events, source.{config.ApiKeyEntity}Id);");
            sb.AppendLine($"        return Ok(result);");
            sb.AppendLine($"    }}");
            sb.AppendLine();
        }

        // Internal endpoints (user auth)
        sb.AppendLine($"    // ============================================================");
        sb.AppendLine($"    // INTERNAL ENDPOINTS (User Auth)");
        sb.AppendLine($"    // ============================================================");
        sb.AppendLine();

        // GET /stats/summary
        sb.AppendLine($"    /// <summary>");
        sb.AppendLine($"    /// Get summary statistics for the dashboard.");
        sb.AppendLine($"    /// </summary>");
        sb.AppendLine($"    [HttpGet(\"stats/summary\")]");
        sb.AppendLine($"    [Authorize]");
        sb.AppendLine($"    public async Task<ActionResult<DataObjects.{controllerName}Stats>> GetStats()");
        sb.AppendLine($"    {{");
        sb.AppendLine($"        var stats = await _da.Get{controllerName}StatsAsync();");
        sb.AppendLine($"        return Ok(stats);");
        sb.AppendLine($"    }}");
        sb.AppendLine();

        // GET /events/recent
        sb.AppendLine($"    /// <summary>");
        sb.AppendLine($"    /// Get recent events for the dashboard feed.");
        sb.AppendLine($"    /// </summary>");
        sb.AppendLine($"    [HttpGet(\"events/recent\")]");
        sb.AppendLine($"    [Authorize]");
        sb.AppendLine($"    public async Task<ActionResult<List<DataObjects.{entityName}>>> GetRecentEvents(");
        sb.AppendLine($"        [FromQuery] int limit = 50)");
        sb.AppendLine($"    {{");
        sb.AppendLine($"        var events = await _da.GetRecent{entityName}sAsync(Math.Min(limit, 100));");
        sb.AppendLine($"        return Ok(events);");
        sb.AppendLine($"    }}");
        sb.AppendLine();

        // GET /sources/status
        if (apiKeyEntity != null)
        {
            sb.AppendLine($"    /// <summary>");
            sb.AppendLine($"    /// Get source system status for the dashboard.");
            sb.AppendLine($"    /// </summary>");
            sb.AppendLine($"    [HttpGet(\"sources/status\")]");
            sb.AppendLine($"    [Authorize]");
            sb.AppendLine($"    public async Task<ActionResult<List<DataObjects.{config.ApiKeyEntity}>>> GetSourceStatus()");
            sb.AppendLine($"    {{");
            sb.AppendLine($"        var sources = await _da.Get{config.ApiKeyEntity}sAsync();");
            sb.AppendLine($"        return Ok(sources);");
            sb.AppendLine($"    }}");
        }

        sb.AppendLine($"}}");

        // DTOs are now generated in a separate file (GenerateExternalApiDataObjects)
        // NOT appended to the controller

        return sb.ToString();
    }

    // ============================================================
    // EXTERNAL API DATA ACCESS GENERATION
    // ============================================================

    /// <summary>
    /// Generates DataAccess methods for external API event processing.
    /// </summary>
    public static string GenerateExternalApiDataAccess(
        DataObjects.ExternalApiConfig config,
        DataObjects.EntityDefinition eventEntity,
        string projectName){
        var sb = new StringBuilder();
        var controllerName = config.ControllerName;
        var entityName = config.EntityName;

        // Add using statements and namespace
        sb.AppendLine($"using Microsoft.EntityFrameworkCore;");
        sb.AppendLine();
        sb.AppendLine($"namespace {projectName};");
        sb.AppendLine();
        sb.AppendLine($"// ============================================================================");
        sb.AppendLine($"// {controllerName.ToUpper()} EXTERNAL API DATA ACCESS");
        sb.AppendLine($"// ============================================================================");
        sb.AppendLine();

        // Generate the IDataAccess partial interface FIRST
        sb.AppendLine($"/// <summary>{controllerName} External API interface extensions.</summary>");
        sb.AppendLine($"public partial interface IDataAccess");
        sb.AppendLine($"{{");
        sb.AppendLine($"    /// <summary>Process a single event from external source.</summary>");
        sb.AppendLine($"    Task<DataObjects.{controllerName}EventResponse> Process{controllerName}EventAsync(DataObjects.{controllerName}EventRequest request, Guid sourceSystemId);");
        sb.AppendLine();

        if (config.AllowBatch)
        {
            sb.AppendLine($"    /// <summary>Process a batch of events from external source.</summary>");
            sb.AppendLine($"    Task<DataObjects.{controllerName}BatchResponse> Process{controllerName}BatchAsync(List<DataObjects.{controllerName}EventRequest> requests, Guid sourceSystemId);");
            sb.AppendLine();
        }

        sb.AppendLine($"    /// <summary>Get dashboard statistics.</summary>");
        sb.AppendLine($"    Task<DataObjects.{controllerName}Stats> Get{controllerName}StatsAsync();");
        sb.AppendLine();

        sb.AppendLine($"    /// <summary>Get recent events for dashboard feed.</summary>");
        sb.AppendLine($"    Task<List<DataObjects.{entityName}>> GetRecent{entityName}sAsync(int limit = 50);");
        sb.AppendLine($"}}");
        sb.AppendLine();

        // Now generate the DataAccess partial class implementation
        sb.AppendLine($"public partial class DataAccess");
        sb.AppendLine($"{{");
        sb.AppendLine($"    #region {controllerName} External API");
        sb.AppendLine();

        // ProcessEvent
        sb.AppendLine($"    /// <summary>Process a single event from external source.</summary>");
        sb.AppendLine($"    public async Task<DataObjects.{controllerName}EventResponse> Process{controllerName}EventAsync(");
        sb.AppendLine($"        DataObjects.{controllerName}EventRequest request, Guid sourceSystemId)");
        sb.AppendLine($"    {{");
        sb.AppendLine($"        var response = new DataObjects.{controllerName}EventResponse");
        sb.AppendLine($"        {{");
        sb.AppendLine($"            ReceivedAt = DateTime.UtcNow");
        sb.AppendLine($"        }};");
        sb.AppendLine();

        // Validation
        sb.AppendLine($"        // Validation");
        sb.AppendLine($"        if (string.IsNullOrWhiteSpace(request.SubjectId))");
        sb.AppendLine($"        {{");
        sb.AppendLine($"            response.Status = \"error\";");
        sb.AppendLine($"            response.Message = \"Missing required field: SubjectId\";");
        sb.AppendLine($"            return response;");
        sb.AppendLine($"        }}");
        sb.AppendLine();

        // Deduplication
        if (!string.IsNullOrEmpty(config.DedupeProperty))
        {
            sb.AppendLine($"        // Deduplication check");
            sb.AppendLine($"        if (!string.IsNullOrEmpty(request.{config.DedupeProperty}))");
            sb.AppendLine($"        {{");
            sb.AppendLine($"            var exists = await data.{eventEntity.PluralName}.AnyAsync(x =>");
            sb.AppendLine($"                x.SourceSystemId == sourceSystemId &&");
            sb.AppendLine($"                x.{config.DedupeProperty} == request.{config.DedupeProperty});");
            sb.AppendLine();
            sb.AppendLine($"            if (exists)");
            sb.AppendLine($"            {{");
            sb.AppendLine($"                response.Status = \"duplicate\";");
            sb.AppendLine($"                response.Message = \"Event with this {config.DedupeProperty} already exists\";");
            sb.AppendLine($"                return response;");
            sb.AppendLine($"            }}");
            sb.AppendLine($"        }}");
            sb.AppendLine();
        }

        // Create event
        sb.AppendLine($"        // Create event record");
        sb.AppendLine($"        var evt = new EFModels.EFModels.{entityName}Item");
        sb.AppendLine($"        {{");
        sb.AppendLine($"            {entityName}Id = Guid.NewGuid(),");
        sb.AppendLine($"            SourceSystemId = sourceSystemId,");
        sb.AppendLine($"            ReceivedAt = DateTime.UtcNow,");

        // Map request properties to entity
        foreach (var prop in eventEntity.Properties.Where(p =>
            !p.Name.EndsWith("Id") || p.Name == "SourceEventId" || p.Name == "SubjectId" || p.Name == "UserId"))
        {
            if (prop.Name.StartsWith("Created") || prop.Name.StartsWith("Modified") ||
                prop.Name == "TenantId" || prop.Name == "Deleted" || prop.Name == "ReceivedAt" ||
                prop.Name == "SourceSystemId")
                continue;

            sb.AppendLine($"            {prop.Name} = request.{prop.Name},");
        }

        sb.AppendLine($"        }};");
        sb.AppendLine();
        sb.AppendLine($"        data.{eventEntity.PluralName}.Add(evt);");
        sb.AppendLine();

        // Update source system stats
        sb.AppendLine($"        // Update source system stats");
        sb.AppendLine($"        await data.{config.ApiKeyEntity}s");
        sb.AppendLine($"            .Where(x => x.{config.ApiKeyEntity}Id == sourceSystemId)");
        sb.AppendLine($"            .ExecuteUpdateAsync(s => s");
        sb.AppendLine($"                .SetProperty(x => x.EventCount, x => x.EventCount + 1)");
        sb.AppendLine($"                .SetProperty(x => x.LastEventReceivedAt, DateTime.UtcNow));");
        sb.AppendLine();

        // Update stats entity if configured
        if (!string.IsNullOrEmpty(config.StatsEntity))
        {
            sb.AppendLine($"        // Update {config.StatsEntity} stats");
            sb.AppendLine($"        await Update{config.StatsEntity}StatsAsync(request.SubjectId);");
            sb.AppendLine();
        }

        sb.AppendLine($"        await data.SaveChangesAsync();");
        sb.AppendLine();
        sb.AppendLine($"        response.EventId = evt.{entityName}Id;");
        sb.AppendLine($"        response.Status = \"accepted\";");
        sb.AppendLine($"        return response;");
        sb.AppendLine($"    }}");
        sb.AppendLine();

        // ProcessBatch
        if (config.AllowBatch)
        {
            sb.AppendLine($"    /// <summary>Process a batch of events from external source.</summary>");
            sb.AppendLine($"    public async Task<DataObjects.{controllerName}BatchResponse> Process{controllerName}BatchAsync(");
            sb.AppendLine($"        List<DataObjects.{controllerName}EventRequest> requests, Guid sourceSystemId)");
            sb.AppendLine($"    {{");
            sb.AppendLine($"        var response = new DataObjects.{controllerName}BatchResponse();");
            sb.AppendLine();
            sb.AppendLine($"        for (int i = 0; i < requests.Count; i++)");
            sb.AppendLine($"        {{");
            sb.AppendLine($"            try");
            sb.AppendLine($"            {{");
            sb.AppendLine($"                var result = await Process{controllerName}EventAsync(requests[i], sourceSystemId);");
            sb.AppendLine($"                switch (result.Status)");
            sb.AppendLine($"                {{");
            sb.AppendLine($"                    case \"accepted\": response.Accepted++; break;");
            sb.AppendLine($"                    case \"duplicate\": response.Duplicate++; break;");
            sb.AppendLine($"                    default:");
            sb.AppendLine($"                        response.Rejected++;");
            sb.AppendLine($"                        response.Errors.Add(new DataObjects.{controllerName}BatchError {{ Index = i, Error = result.Message ?? \"Unknown error\" }});");
            sb.AppendLine($"                        break;");
            sb.AppendLine($"                }}");
            sb.AppendLine($"            }}");
            sb.AppendLine($"            catch (Exception ex)");
            sb.AppendLine($"            {{");
            sb.AppendLine($"                response.Rejected++;");
            sb.AppendLine($"                response.Errors.Add(new DataObjects.{controllerName}BatchError {{ Index = i, Error = ex.Message }});");
            sb.AppendLine($"            }}");
            sb.AppendLine($"        }}");
            sb.AppendLine();
            sb.AppendLine($"        return response;");
            sb.AppendLine($"    }}");
            sb.AppendLine();
        }

        // GetStats
        var dateProperty = config.StatsEntity != null ? "AccessedAt" : "ReceivedAt";
        sb.AppendLine($"    /// <summary>Get dashboard statistics.</summary>");
        sb.AppendLine($"    public async Task<DataObjects.{controllerName}Stats> Get{controllerName}StatsAsync()");
        sb.AppendLine($"    {{");
        sb.AppendLine($"        var now = DateTime.UtcNow;");
        sb.AppendLine($"        var todayStart = now.Date;");
        sb.AppendLine($"        var weekStart = now.Date.AddDays(-(int)now.DayOfWeek);");
        sb.AppendLine($"        var monthStart = new DateTime(now.Year, now.Month, 1);");
        sb.AppendLine();
        sb.AppendLine($"        return new DataObjects.{controllerName}Stats");
        sb.AppendLine($"        {{");
        sb.AppendLine($"            Today = await data.{eventEntity.PluralName}.CountAsync(x => x.{dateProperty} >= todayStart),");
        sb.AppendLine($"            ThisWeek = await data.{eventEntity.PluralName}.CountAsync(x => x.{dateProperty} >= weekStart),");
        sb.AppendLine($"            ThisMonth = await data.{eventEntity.PluralName}.CountAsync(x => x.{dateProperty} >= monthStart),");
        sb.AppendLine($"        }};");
        sb.AppendLine($"    }}");
        sb.AppendLine();

        // GetRecentEvents - use inline mapping instead of calling a mapper method
        sb.AppendLine($"    /// <summary>Get recent events for dashboard feed.</summary>");
        sb.AppendLine($"    public async Task<List<DataObjects.{entityName}>> GetRecent{entityName}sAsync(int limit = 50)");
        sb.AppendLine($"    {{");
        sb.AppendLine($"        return await data.{eventEntity.PluralName}");
        sb.AppendLine($"            .OrderByDescending(x => x.{dateProperty})");
        sb.AppendLine($"            .Take(limit)");
        sb.AppendLine($"            .Select(x => new DataObjects.{entityName}");
        sb.AppendLine($"            {{");
        foreach (var prop in eventEntity.Properties.Where(p => !p.IsSystemField || p.IsPrimaryKey))
        {
            sb.AppendLine($"                {prop.Name} = x.{prop.Name},");
        }
        sb.AppendLine($"            }})");
        sb.AppendLine($"            .ToListAsync();");
        sb.AppendLine($"    }}");
        sb.AppendLine();

        // UpdateStatsEntity helper
        if (!string.IsNullOrEmpty(config.StatsEntity))
        {
            sb.AppendLine($"    /// <summary>Update or create {config.StatsEntity} stats on event.</summary>");
            sb.AppendLine($"    private async Task Update{config.StatsEntity}StatsAsync(string subjectId)");
            sb.AppendLine($"    {{");
            sb.AppendLine($"        var subject = await data.{config.StatsEntity}s");
            sb.AppendLine($"            .FirstOrDefaultAsync(x => x.{config.StatsMatchProperty} == subjectId);");
            sb.AppendLine();
            sb.AppendLine($"        if (subject == null)");
            sb.AppendLine($"        {{");
            sb.AppendLine($"            subject = new EFModels.EFModels.{config.StatsEntity}Item");
            sb.AppendLine($"            {{");
            sb.AppendLine($"                {config.StatsEntity}Id = Guid.NewGuid(),");
            sb.AppendLine($"                {config.StatsMatchProperty} = subjectId,");
            sb.AppendLine($"                FirstAccessedAt = DateTime.UtcNow,");
            sb.AppendLine($"                TotalAccessCount = 0");
            sb.AppendLine($"            }};");
            sb.AppendLine($"            data.{config.StatsEntity}s.Add(subject);");
            sb.AppendLine($"        }}");
            sb.AppendLine();
            sb.AppendLine($"        subject.LastAccessedAt = DateTime.UtcNow;");
            sb.AppendLine($"        subject.TotalAccessCount++;");
            sb.AppendLine($"    }}");
        }

        sb.AppendLine();
        sb.AppendLine($"    #endregion");
        sb.AppendLine($"}}");

        return sb.ToString();
    }

    // ============================================================
    // EXTERNAL API DATAOBJECTS (Separate File)
    // ============================================================

    /// <summary>
    /// Generates a separate DataObjects file for External API DTOs.
    /// This should be called from GenerateAllFiles to create a separate file.
    /// </summary>
    public static string GenerateExternalApiDataObjects(
        DataObjects.ExternalApiConfig config,
        DataObjects.EntityDefinition eventEntity,
        string projectName){
        var sb = new StringBuilder();
        var controllerName = config.ControllerName;

        sb.AppendLine($"namespace {projectName};");
        sb.AppendLine();
        sb.AppendLine($"#region {controllerName} External API DataObjects");
        sb.AppendLine($"// ============================================================================");
        sb.AppendLine($"// {controllerName.ToUpper()} API DATA TRANSFER OBJECTS");
        sb.AppendLine($"// Generated by Entity Builder Wizard");
        sb.AppendLine($"// ============================================================================");
        sb.AppendLine();
        sb.AppendLine($"public partial class DataObjects");
        sb.AppendLine($"{{");

        // Event Request DTO
        sb.AppendLine($"    /// <summary>Incoming event from external source system.</summary>");
        sb.AppendLine($"    public class {controllerName}EventRequest");
        sb.AppendLine($"    {{");

        // Include relevant properties from event entity (excluding system fields)
        foreach (var prop in eventEntity.Properties.Where(p =>
            !p.Name.EndsWith("Id") || p.Name == "SourceEventId" || p.Name == "SubjectId" || p.Name == "UserId"))
        {
            if (prop.Name.StartsWith("Created") || prop.Name.StartsWith("Modified") ||
                prop.Name == "TenantId" || prop.Name == "Deleted" || prop.Name == "ReceivedAt")
                continue;

            var nullableMarker = prop.IsNullable || !prop.IsRequired ? "?" : "";
            var defaultValue = prop.Type == "string" ? " = string.Empty;" : "";
            if (prop.IsNullable || !prop.IsRequired) defaultValue = "";

            sb.AppendLine($"        public {prop.Type}{nullableMarker} {prop.Name} {{ get; set; }}{defaultValue}");
        }

        sb.AppendLine($"    }}");
        sb.AppendLine();

        // Event Response DTO
        sb.AppendLine($"    /// <summary>Response after processing an event.</summary>");
        sb.AppendLine($"    public class {controllerName}EventResponse");
        sb.AppendLine($"    {{");
        sb.AppendLine($"        public Guid? EventId {{ get; set; }}");
        sb.AppendLine($"        public DateTime ReceivedAt {{ get; set; }} = DateTime.UtcNow;");
        sb.AppendLine($"        public string Status {{ get; set; }} = string.Empty; // accepted, duplicate, error");
        sb.AppendLine($"        public string? Message {{ get; set; }}");
        sb.AppendLine($"    }}");
        sb.AppendLine();

        // Batch Response DTO
        if (config.AllowBatch)
        {
            sb.AppendLine($"    /// <summary>Response after processing a batch of events.</summary>");
            sb.AppendLine($"    public class {controllerName}BatchResponse");
            sb.AppendLine($"    {{");
            sb.AppendLine($"        public int Accepted {{ get; set; }}");
            sb.AppendLine($"        public int Rejected {{ get; set; }}");
            sb.AppendLine($"        public int Duplicate {{ get; set; }}");
            sb.AppendLine($"        public List<{controllerName}BatchError> Errors {{ get; set; }} = new();");
            sb.AppendLine($"    }}");
            sb.AppendLine();

            sb.AppendLine($"    /// <summary>Error detail for a single event in a batch.</summary>");
            sb.AppendLine($"    public class {controllerName}BatchError");
            sb.AppendLine($"    {{");
            sb.AppendLine($"        public int Index {{ get; set; }}");
            sb.AppendLine($"        public string Error {{ get; set; }} = string.Empty;");
            sb.AppendLine($"    }}");
            sb.AppendLine();
        }

        // Stats DTO
        sb.AppendLine($"    /// <summary>Dashboard statistics.</summary>");
        sb.AppendLine($"    public class {controllerName}Stats");
        sb.AppendLine($"    {{");
        sb.AppendLine($"        public long Today {{ get; set; }}");
        sb.AppendLine($"        public long ThisWeek {{ get; set; }}");
        sb.AppendLine($"        public long ThisMonth {{ get; set; }}");
        sb.AppendLine($"        public Dictionary<string, long> ByCategory {{ get; set; }} = new();");
        sb.AppendLine($"        public Dictionary<string, long> ByAccessType {{ get; set; }} = new();");
        sb.AppendLine($"    }}");

        sb.AppendLine($"}}");
        sb.AppendLine();
        sb.AppendLine($"#endregion");

        return sb.ToString();
    }
}

#endregion
