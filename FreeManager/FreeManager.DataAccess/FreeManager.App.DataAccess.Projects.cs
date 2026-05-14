using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace FreeManager;

#region FreeManager Platform - Project DataAccess Methods
// ============================================================================
// FREEMANAGER PROJECT METHODS
// ============================================================================
// Part of: DataAccess.App.FreeManager (partial)
// Coordinator: DataAccess.App.FreeManager.cs
//
// METHODS IN THIS FILE:
// ┌────────────────────────────────────────────────────────────────────────┐
// │ PROJECT CRUD                                                           │
// │   FM_GetProjects    - List all projects for tenant (newest first)      │
// │                       └── Includes LastBuild info for each project     │
// │   FM_GetProject     - Get single project with full metadata            │
// │   FM_CreateProject  - Create project with template files               │
// │                       ├── Validates name (C# identifier rules)         │
// │                       ├── Checks for duplicate names in tenant         │
// │                       └── Calls FM_CreateDefaultAppFiles               │
// │   FM_UpdateProject  - Update display name, description, status         │
// │   FM_DeleteProject  - Soft-delete project                              │
// │                                                                        │
// │ INTERNAL HELPERS                                                       │
// │   FM_CreateDefaultAppFiles - Creates template files for new project    │
// │                              └── Uses FM_GetProjectTemplateFiles       │
// └────────────────────────────────────────────────────────────────────────┘
//
// PROJECT TEMPLATES (via FM_CreateDefaultAppFiles):
//   ┌─────────────┬──────────────────────────────────────────────────────┐
//   │ Empty       │ No files                                             │
//   │ Skeleton    │ DataObjects, DataAccess, Controller, GlobalSettings  │
//   │ Starter     │ Skeleton + Component, Page (Settings-based storage)  │
//   │ FullCrud    │ Starter + EFModel, DbContext (database entity)       │
//   └─────────────┴──────────────────────────────────────────────────────┘
//
// DEPENDENCIES:
//   EFModels: FMProject, FMBuild, FMAppFile, FMAppFileVersion
//   DataObjects: FMProjectInfo, FMCreateProjectRequest, FMUpdateProjectRequest
//   Templates: FM_GetProjectTemplateFiles (in Templates.cs)
// ============================================================================

public partial class DataAccess
{
    // ============================================================
    // PROJECT METHODS
    // ============================================================

    public async Task<List<DataObjects.FMProjectInfo>> FM_GetProjects(DataObjects.User CurrentUser)
    {
        var tenantId = CurrentUser.TenantId;

        var projects = await data.FMProjects
            .Where(p => p.TenantId == tenantId && !p.Deleted)
            .OrderByDescending(p => p.UpdatedAt)
            .Select(p => new DataObjects.FMProjectInfo
            {
                Id = p.FMProjectId,
                Name = p.Name,
                DisplayName = p.DisplayName,
                Description = p.Description,
                IncludedModules = string.IsNullOrEmpty(p.IncludedModules)
                    ? new List<string>()
                    : p.IncludedModules.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
                Status = p.Status,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                EntityCount = p.EntityCount,
                RelationshipCount = p.RelationshipCount,
                FileCount = p.AppFiles.Count(f => !f.Deleted),
                BuildCount = p.Builds.Count()
            })
            .ToListAsync();

        // Load last build for each project
        foreach (var project in projects)
        {
            var lastBuild = await data.FMBuilds
                .Where(b => b.FMProjectId == project.Id)
                .OrderByDescending(b => b.BuildNumber)
                .Select(b => new DataObjects.FMBuildInfo
                {
                    Id = b.FMBuildId,
                    ProjectId = b.FMProjectId,
                    BuildNumber = b.BuildNumber,
                    Status = b.Status,
                    CreatedAt = b.CreatedAt,
                    StartedAt = b.StartedAt,
                    CompletedAt = b.CompletedAt,
                    ArtifactSizeBytes = b.ArtifactSizeBytes,
                    ErrorMessage = b.ErrorMessage
                })
                .FirstOrDefaultAsync();

            project.LastBuild = lastBuild;
        }

        return projects;
    }

    public async Task<DataObjects.FMProjectInfo?> FM_GetProject(Guid projectId, DataObjects.User CurrentUser)
    {
        var tenantId = CurrentUser.TenantId;

        var project = await data.FMProjects
            .Where(p => p.FMProjectId == projectId && p.TenantId == tenantId && !p.Deleted)
            .Select(p => new DataObjects.FMProjectInfo
            {
                Id = p.FMProjectId,
                Name = p.Name,
                DisplayName = p.DisplayName,
                Description = p.Description,
                IncludedModules = string.IsNullOrEmpty(p.IncludedModules)
                    ? new List<string>()
                    : p.IncludedModules.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
                Status = p.Status,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                FileCount = p.AppFiles.Count(f => !f.Deleted),
                BuildCount = p.Builds.Count()
            })
            .FirstOrDefaultAsync();

        if (project != null)
        {
            project.LastBuild = await data.FMBuilds
                .Where(b => b.FMProjectId == projectId)
                .OrderByDescending(b => b.BuildNumber)
                .Select(b => new DataObjects.FMBuildInfo
                {
                    Id = b.FMBuildId,
                    ProjectId = b.FMProjectId,
                    BuildNumber = b.BuildNumber,
                    Status = b.Status,
                    CreatedAt = b.CreatedAt,
                    StartedAt = b.StartedAt,
                    CompletedAt = b.CompletedAt,
                    ArtifactSizeBytes = b.ArtifactSizeBytes,
                    ErrorMessage = b.ErrorMessage
                })
                .FirstOrDefaultAsync();
        }

        return project;
    }

    public async Task<DataObjects.FMProjectInfo> FM_CreateProject(DataObjects.FMCreateProjectRequest request, DataObjects.User CurrentUser)
    {
        var tenantId = CurrentUser.TenantId;
        var userId = CurrentUser.UserId;

        // Validate project name (must be valid C# identifier)
        if (!Regex.IsMatch(request.Name, @"^[A-Za-z][A-Za-z0-9]*$"))
        {
            throw new ArgumentException("Project name must start with a letter and contain only letters and numbers.");
        }

        // Check for duplicate name in tenant
        var exists = await data.FMProjects
            .AnyAsync(p => p.TenantId == tenantId
                        && p.Name.ToLower() == request.Name.ToLower()
                        && !p.Deleted);

        if (exists)
        {
            throw new ArgumentException($"A project named '{request.Name}' already exists.");
        }

        // Create project
        var project = new EFModels.EFModels.FMProject
        {
            TenantId = tenantId,
            Name = request.Name,
            DisplayName = string.IsNullOrEmpty(request.DisplayName) ? request.Name : request.DisplayName,
            Description = request.Description ?? string.Empty,
            IncludedModules = string.Join(",", request.IncludedModules ?? new List<string>()),
            Status = "Active",
            CreatedBy = userId
        };

        data.FMProjects.Add(project);
        await data.SaveChangesAsync();

        // Create template files based on selected template
        await FM_CreateDefaultAppFiles(project, request.Template, CurrentUser);

        return new DataObjects.FMProjectInfo
        {
            Id = project.FMProjectId,
            Name = project.Name,
            DisplayName = project.DisplayName,
            Description = project.Description,
            IncludedModules = string.IsNullOrEmpty(project.IncludedModules)
                ? new List<string>()
                : project.IncludedModules.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
            Status = project.Status,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt,
            FileCount = 0,
            BuildCount = 0
        };
    }

    public async Task<DataObjects.BooleanResponse> FM_UpdateProject(DataObjects.FMUpdateProjectRequest request, DataObjects.User CurrentUser)
    {
        var output = new DataObjects.BooleanResponse();
        var tenantId = CurrentUser.TenantId;

        var project = await data.FMProjects
            .FirstOrDefaultAsync(p => p.FMProjectId == request.Id
                                   && p.TenantId == tenantId
                                   && !p.Deleted);

        if (project == null)
        {
            output.Messages.Add("Project not found");
            return output;
        }

        project.DisplayName = request.DisplayName;
        project.Description = request.Description;
        if (!string.IsNullOrEmpty(request.Status))
        {
            project.Status = request.Status;
        }
        project.UpdatedAt = DateTime.UtcNow;

        await data.SaveChangesAsync();

        output.Result = true;
        return output;
    }

    public async Task<DataObjects.BooleanResponse> FM_DeleteProject(Guid projectId, DataObjects.User CurrentUser)
    {
        var output = new DataObjects.BooleanResponse();
        var tenantId = CurrentUser.TenantId;

        var project = await data.FMProjects
            .FirstOrDefaultAsync(p => p.FMProjectId == projectId
                                   && p.TenantId == tenantId
                                   && !p.Deleted);

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

    /// <summary>
    /// Creates default .App. files for a new project based on selected template.
    /// </summary>
    private async Task FM_CreateDefaultAppFiles(EFModels.EFModels.FMProject project, DataObjects.FMProjectTemplate template, DataObjects.User CurrentUser)
    {
        var userId = CurrentUser.UserId;

        var templateFiles = FM_GetProjectTemplateFiles(template, project.Name);

        foreach (var (fileName, fileType, content) in templateFiles)
        {
            var file = new EFModels.EFModels.FMAppFile
            {
                FMProjectId = project.FMProjectId,
                FilePath = fileName,
                FileType = fileType,
                CurrentVersion = 1
            };

            data.FMAppFiles.Add(file);
            await data.SaveChangesAsync();

            var version = new EFModels.EFModels.FMAppFileVersion
            {
                FMAppFileId = file.FMAppFileId,
                Version = 1,
                Content = content,
                ContentHash = FM_ComputeHash(content),
                CreatedBy = userId,
                Comment = "Initial file created from template"
            };

            data.FMAppFileVersions.Add(version);
        }

        await data.SaveChangesAsync();
    }
}

#endregion
