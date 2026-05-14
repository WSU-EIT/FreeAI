namespace FreeManager;

#region EntityTemplates - Entity Wizard Code Generation (Coordinator)
// ============================================================================
// ENTITY BUILDER WIZARD - CODE GENERATION TEMPLATES
// This is the coordinator file for Entity Wizard template generation.
// All generators are split into feature-specific partial files:
//
// - FreeManager.App.EntityTemplates.EFModel.cs      → EF Model generation
// - FreeManager.App.EntityTemplates.DataObjects.cs  → DTOs, filters, enums
// - FreeManager.App.EntityTemplates.DataAccess.cs   → Data access layer
// - FreeManager.App.EntityTemplates.Controller.cs   → REST API endpoints
// - FreeManager.App.EntityTemplates.RazorPages.cs   → List and Edit Blazor pages
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
        List<DataObjects.EntityDefinition> existingEntities)
    {
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

    // ============================================================
    // RELATIONSHIP HELPERS
    // ============================================================

    /// <summary>
    /// Get relationships where this entity is the child (has FK to parent).
    /// </summary>
    private static List<DataObjects.RelationshipDefinition> GetParentRelationships(
        string entityName,
        List<DataObjects.RelationshipDefinition> relationships)
    {
        return relationships
            .Where(r => r.TargetEntityName == entityName && r.SourceEntityName != entityName)
            .ToList();
    }

    /// <summary>
    /// Get relationships where this entity is the parent (has children).
    /// </summary>
    private static List<DataObjects.RelationshipDefinition> GetChildRelationships(
        string entityName,
        List<DataObjects.RelationshipDefinition> relationships)
    {
        return relationships
            .Where(r => r.SourceEntityName == entityName &&
                       (r.Type == DataObjects.RelationshipType.OneToMany ||
                        r.Type == DataObjects.RelationshipType.ManyToMany))
            .ToList();
    }
}

#endregion
