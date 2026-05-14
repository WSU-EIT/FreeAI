using Microsoft.EntityFrameworkCore;
using System.IO.Compression;
using System.Text;

namespace FreeManager;

#region Entity Wizard Project Persistence - DataAccess Methods
// ============================================================================
// ENTITY WIZARD PROJECT PERSISTENCE
// ============================================================================
// Related to: FreeManager.App.EntityWizard (Blazor component)
// Coordinator: DataAccess.App.FreeManager.cs
//
// Provides backend storage for the Entity Wizard UI, allowing users to:
//   • Save work-in-progress entity definitions
//   • Load previously saved projects
//   • Export generated code as downloadable ZIP
//
// METHODS IN THIS FILE:
// ┌────────────────────────────────────────────────────────────────────────┐
// │ CRUD OPERATIONS                                                        │
// │   FM_GetSavedProjects  - Filtered/paginated list of saved projects     │
// │   FM_GetSavedProject   - Get single project with full entity JSON      │
// │   FM_SaveWizardProject - Create or update saved project                │
// │   FM_DeleteSavedProject - Soft-delete a saved project                  │
// │                                                                        │
// │ EXPORT                                                                 │
// │   FM_DownloadProjectZip - Generate ZIP with all .App. files            │
// │                           └── Uses code generators to produce:         │
// │                               • DataObjects.App.{Name}.cs              │
// │                               • DataAccess.App.{Name}.cs               │
// │                               • DataController.App.{Name}.cs           │
// │                               • {Entity}Page.App.razor (per entity)    │
// │                               • EFModels/{Entity}.cs (per entity)      │
// └────────────────────────────────────────────────────────────────────────┘
//
// DATA MODEL:
//   FMSavedProject
//   ├── ProjectId, TenantId, Name, Description
//   ├── EntitiesJson     (serialized entity definitions)
//   ├── SettingsJson     (wizard UI state)
//   └── CreatedAt, UpdatedAt, Deleted, DeletedAt
//
// DEPENDENCIES:
//   EFModels: FMSavedProject (or Settings-based storage)
//   DataObjects: FMSavedProject, FMSavedProjectFilter, FMProjectZipResponse
// ============================================================================

public partial interface IDataAccess
{
    Task<DataObjects.FMSavedProjectFilterResult> FM_GetSavedProjects(DataObjects.FMSavedProjectFilter filter, DataObjects.User CurrentUser);
    Task<DataObjects.FMSavedProject?> FM_GetSavedProject(Guid projectId, DataObjects.User CurrentUser);
    Task<DataObjects.FMSavedProject> FM_SaveWizardProject(DataObjects.FMSaveWizardProjectRequest request, DataObjects.User CurrentUser);
    Task<DataObjects.BooleanResponse> FM_DeleteSavedProject(Guid projectId, DataObjects.User CurrentUser);
    Task<DataObjects.FMProjectZipResponse> FM_DownloadProjectZip(Guid projectId, DataObjects.User CurrentUser);
}

public partial class DataAccess
{
    public async Task<DataObjects.FMSavedProjectFilterResult> FM_GetSavedProjects(
        DataObjects.FMSavedProjectFilter filter,
        DataObjects.User CurrentUser)
    {
        var tenantId = CurrentUser.TenantId;
        var query = data.FMProjects.Where(p => p.TenantId == tenantId);

        if (!filter.IncludeDeleted)
            query = query.Where(p => !p.Deleted);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(search) ||
                                    p.DisplayName.ToLower().Contains(search) ||
                                    p.Description.ToLower().Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(filter.Status))
            query = query.Where(p => p.Status == filter.Status);

        var total = await query.CountAsync();

        query = filter.SortColumn switch
        {
            "Name" => filter.SortDescending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
            "CreatedAt" => filter.SortDescending ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt),
            "EntityCount" => filter.SortDescending ? query.OrderByDescending(p => p.EntityCount) : query.OrderBy(p => p.EntityCount),
            _ => filter.SortDescending ? query.OrderByDescending(p => p.UpdatedAt) : query.OrderBy(p => p.UpdatedAt)
        };

        var items = await query
            .Skip(filter.Skip)
            .Take(filter.PageSize)
            .Select(p => new DataObjects.FMSavedProject
            {
                FMProjectId = p.FMProjectId,
                Name = p.Name,
                DisplayName = p.DisplayName,
                Description = p.Description,
                Status = p.Status,
                EntityCount = p.EntityCount,
                RelationshipCount = p.RelationshipCount,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .ToListAsync();

        return new DataObjects.FMSavedProjectFilterResult
        {
            Records = items,
            TotalRecords = total,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<DataObjects.FMSavedProject?> FM_GetSavedProject(Guid projectId, DataObjects.User CurrentUser)
    {
        var tenantId = CurrentUser.TenantId;

        return await data.FMProjects
            .Where(p => p.FMProjectId == projectId && p.TenantId == tenantId && !p.Deleted)
            .Select(p => new DataObjects.FMSavedProject
            {
                FMProjectId = p.FMProjectId,
                Name = p.Name,
                DisplayName = p.DisplayName,
                Description = p.Description,
                Status = p.Status,
                EntityCount = p.EntityCount,
                RelationshipCount = p.RelationshipCount,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                EntityWizardStateJson = p.EntityWizardStateJson
            })
            .FirstOrDefaultAsync();
    }

    public async Task<DataObjects.FMSavedProject> FM_SaveWizardProject(
        DataObjects.FMSaveWizardProjectRequest request,
        DataObjects.User CurrentUser)
    {
        var tenantId = CurrentUser.TenantId;
        var userId = CurrentUser.UserId;
        var isNew = !request.FMProjectId.HasValue || request.FMProjectId == Guid.Empty;

        EFModels.EFModels.FMProject project;

        if (isNew)
        {
            project = new EFModels.EFModels.FMProject
            {
                FMProjectId = Guid.NewGuid(),
                TenantId = tenantId,
                Name = request.Name,
                DisplayName = request.DisplayName,
                Description = request.Description,
                Status = "Draft",
                EntityWizardStateJson = request.EntityWizardStateJson,
                EntityCount = request.EntityCount,
                RelationshipCount = request.RelationshipCount,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = userId
            };
            data.FMProjects.Add(project);
        }
        else
        {
            project = await data.FMProjects
                .FirstOrDefaultAsync(p => p.FMProjectId == request.FMProjectId!.Value
                                       && p.TenantId == tenantId
                                       && !p.Deleted)
                ?? throw new ArgumentException("Project not found");

            project.Name = request.Name;
            project.DisplayName = request.DisplayName;
            project.Description = request.Description;
            project.EntityWizardStateJson = request.EntityWizardStateJson;
            project.EntityCount = request.EntityCount;
            project.RelationshipCount = request.RelationshipCount;
            project.UpdatedAt = DateTime.UtcNow;
        }

        await data.SaveChangesAsync();

        return new DataObjects.FMSavedProject
        {
            FMProjectId = project.FMProjectId,
            Name = project.Name,
            DisplayName = project.DisplayName,
            Description = project.Description,
            Status = project.Status,
            EntityCount = project.EntityCount,
            RelationshipCount = project.RelationshipCount,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt
        };
    }

    public async Task<DataObjects.BooleanResponse> FM_DeleteSavedProject(Guid projectId, DataObjects.User CurrentUser)
    {
        var tenantId = CurrentUser.TenantId;
        var output = new DataObjects.BooleanResponse();

        var project = await data.FMProjects
            .FirstOrDefaultAsync(p => p.FMProjectId == projectId && p.TenantId == tenantId && !p.Deleted);

        if (project == null)
        {
            output.Messages.Add("Project not found");
            return output;
        }

        project.Deleted = true;
        project.DeletedAt = DateTime.UtcNow;
        project.UpdatedAt = DateTime.UtcNow;

        await data.SaveChangesAsync();
        output.Result = true;
        return output;
    }

    public async Task<DataObjects.FMProjectZipResponse> FM_DownloadProjectZip(Guid projectId, DataObjects.User CurrentUser)
    {
        var tenantId = CurrentUser.TenantId;
        var response = new DataObjects.FMProjectZipResponse();

        var project = await data.FMProjects
            .Where(p => p.FMProjectId == projectId && p.TenantId == tenantId && !p.Deleted)
            .FirstOrDefaultAsync();

        if (project == null)
        {
            response.ErrorMessage = "Project not found";
            return response;
        }

        try
        {
            // Parse wizard state to get generated files
            List<DataObjects.GeneratedFileInfo> generatedFiles = new();
            if (!string.IsNullOrEmpty(project.EntityWizardStateJson))
            {
                var state = System.Text.Json.JsonSerializer.Deserialize<DataObjects.EntityWizardState>(project.EntityWizardStateJson);
                if (state?.GeneratedFiles != null)
                {
                    generatedFiles = state.GeneratedFiles;
                }
            }

            // Also get files from the database (FMAppFiles)
            var dbFiles = await data.FMAppFiles
                .Where(f => f.FMProjectId == projectId && !f.Deleted)
                .Select(f => new { f.FilePath, f.FMAppFileId, f.CurrentVersion })
                .ToListAsync();

            using var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                int fileCount = 0;
                var projectName = project.Name;

                // Add generated code files from wizard state
                if (generatedFiles.Any())
                {
                    foreach (var file in generatedFiles)
                    {
                        var folderPath = GetFolderPath(file.FileType, projectName);
                        var fullPath = string.IsNullOrEmpty(folderPath)
                            ? file.FileName
                            : $"{folderPath}/{file.FileName}";

                        var entry = archive.CreateEntry(fullPath, CompressionLevel.Optimal);
                        using (var entryStream = entry.Open())
                        using (var writer = new StreamWriter(entryStream, Encoding.UTF8))
                        {
                            await writer.WriteAsync(file.Content ?? string.Empty);
                        }
                        fileCount++;
                    }
                }

                // Add files from database if wizard state has no files
                if (!generatedFiles.Any() && dbFiles.Any())
                {
                    foreach (var dbFile in dbFiles)
                    {
                        // Get the file content from the latest version
                        var version = await data.FMAppFileVersions
                            .Where(v => v.FMAppFileId == dbFile.FMAppFileId && v.Version == dbFile.CurrentVersion)
                            .FirstOrDefaultAsync();

                        if (version != null)
                        {
                            var entry = archive.CreateEntry(dbFile.FilePath, CompressionLevel.Optimal);
                            using (var entryStream = entry.Open())
                            using (var writer = new StreamWriter(entryStream, Encoding.UTF8))
                            {
                                await writer.WriteAsync(version.Content ?? string.Empty);
                            }
                            fileCount++;
                        }
                    }
                }

                // Add README
                var readmeContent = $@"================================================================================
{project.DisplayName ?? project.Name}
Entity Builder Wizard Export
================================================================================

Project Info:
- Name: {projectName}
- Entities: {project.EntityCount}
- Relationships: {project.RelationshipCount}
- Files: {fileCount}
- Created: {project.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC
- Updated: {project.UpdatedAt:yyyy-MM-dd HH:mm:ss} UTC

File Structure:
- {projectName}.EFModels/EFModels/*.cs     - Entity Framework models
- {projectName}.DataObjects/*.cs           - DTOs and filter classes
- {projectName}.DataAccess/*.cs            - Data access layer
- {projectName}/Controllers/*.cs           - API controllers
- {projectName}.Client/Pages/*.razor       - Blazor list pages
- {projectName}.Client/Shared/*.razor      - Blazor edit components

To Use These Files:
1. Copy files to corresponding folders in your {projectName} project
2. Register any new entities in EFDataModel.cs
3. Run database migration
4. Add menu items for new pages

================================================================================";

                var readmeEntry = archive.CreateEntry("README.txt", CompressionLevel.Optimal);
                using (var entryStream = readmeEntry.Open())
                using (var writer = new StreamWriter(entryStream, Encoding.UTF8))
                {
                    await writer.WriteAsync(readmeContent);
                }
                fileCount++;

                response.FileCount = fileCount;
            }

            var zipBytes = memoryStream.ToArray();

            response.Success = true;
            response.FileName = $"{project.Name}_export.zip";
            response.Base64Content = Convert.ToBase64String(zipBytes);
        }
        catch (Exception ex)
        {
            response.ErrorMessage = $"Error creating ZIP: {ex.Message}";
        }

        return response;
    }

    /// <summary>
    /// Maps file types to folder paths for proper project structure in ZIP.
    /// Uses the project name for folder prefixes (e.g., FreeGLBA.Client, FreeGLBA.DataAccess).
    /// </summary>
    private static string GetFolderPath(string fileType, string projectName) => fileType switch
    {
        "EFModel" or "EFDataModel" => $"{projectName}.EFModels/EFModels",
        "DataObjects" => $"{projectName}.DataObjects",
        "DataAccess" => $"{projectName}.DataAccess",
        "Controller" or "Middleware" => $"{projectName}/Controllers",
        "RazorPage" => $"{projectName}.Client/Pages",
        "RazorComponent" => $"{projectName}.Client/Shared/AppComponents",
        "Stylesheet" => $"{projectName}.Client/wwwroot/css",
        "HelpersApp" => $"{projectName}.Client",
        _ => projectName
    };
}

#endregion
