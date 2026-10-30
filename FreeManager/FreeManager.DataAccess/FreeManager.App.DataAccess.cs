using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace FreeManager;

// ---- DataAccess.Projects ----

#region FreeManager Platform - Project DataAccess Methods
// ============================================================================
// FREEMANAGER PROJECT METHODS
// ============================================================================
// Part of: DataAccess.App.FreeManager (partial)
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
//   Templates: FM_GetProjectTemplateFiles (in the Templates section of FreeManager.App.DataAccess.cs)
// ============================================================================

public partial interface IDataAccess
{
    Task<DataObjects.FMProjectInfo> FM_CreateProject(DataObjects.FMCreateProjectRequest request, DataObjects.User CurrentUser);
    Task<DataObjects.BooleanResponse> FM_DeleteProject(Guid projectId, DataObjects.User CurrentUser);
    Task<byte[]?> FM_ExportProjectAsZip(Guid projectId, DataObjects.User CurrentUser);
    Task<DataObjects.FMProjectInfo?> FM_GetProject(Guid projectId, DataObjects.User CurrentUser);
    Task<List<DataObjects.FMProjectInfo>> FM_GetProjects(DataObjects.User CurrentUser);
    Task<DataObjects.BooleanResponse> FM_UpdateProject(DataObjects.FMUpdateProjectRequest request, DataObjects.User CurrentUser);
}

public partial class DataAccess
{
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
}

#endregion

// ---- DataAccess.Files ----

#region FreeManager Platform - File DataAccess Methods
// ============================================================================
// FREEMANAGER FILE METHODS
// ============================================================================
// Part of: DataAccess.App.FreeManager (partial)
//
// METHODS IN THIS FILE:
// ┌────────────────────────────────────────────────────────────────────────┐
// │ FILE CRUD                                                              │
// │   FM_GetAppFiles    - List all files in project (by type, then path)   │
// │   FM_GetAppFile     - Get file with latest version content             │
// │   FM_CreateAppFile  - Create new file with initial version             │
// │   FM_DeleteAppFile  - Soft-delete a file                               │
// │                                                                        │
// │ FILE VERSIONING                                                        │
// │   FM_SaveAppFile    - Save with optimistic concurrency + versioning    │
// │                       ├── Checks ExpectedVersion (concurrency)         │
// │                       ├── Computes ContentHash (change detection)      │
// │                       └── Creates new FMAppFileVersion record          │
// │   FM_GetFileVersions - List version history for a file                 │
// │   FM_GetFileVersion  - Get specific version content (for restore)      │
// └────────────────────────────────────────────────────────────────────────┘
//
// DATA FLOW:
//   FMProject (1) ──┬── (*) FMAppFile ──── (*) FMAppFileVersion
//                   │         │                    │
//                   │         ├── FilePath         ├── Version (1, 2, 3...)
//                   │         ├── FileType         ├── Content
//                   │         └── CurrentVersion   └── ContentHash
//                   │
//   All operations verify TenantId through FMProject for security.
//
// DEPENDENCIES:
//   EFModels: FMAppFile, FMAppFileVersion, FMProject
//   DataObjects: FMAppFileInfo, FMAppFileContent, FMSaveFileRequest/Response
// ============================================================================

public partial interface IDataAccess
{
    Task<DataObjects.FMAppFileInfo?> FM_CreateAppFile(DataObjects.FMCreateFileRequest request, DataObjects.User CurrentUser);
    Task<DataObjects.BooleanResponse> FM_DeleteAppFile(Guid fileId, DataObjects.User CurrentUser);
    Task<DataObjects.FMAppFileContent?> FM_GetAppFile(Guid fileId, DataObjects.User CurrentUser);
    Task<List<DataObjects.FMAppFileInfo>> FM_GetAppFiles(Guid projectId, DataObjects.User CurrentUser);
    Task<DataObjects.FMAppFileContent?> FM_GetFileVersion(Guid versionId, DataObjects.User CurrentUser);
    Task<List<DataObjects.FMFileVersionInfo>> FM_GetFileVersions(Guid fileId, DataObjects.User CurrentUser);
    Task<DataObjects.FMSaveFileResponse> FM_SaveAppFile(DataObjects.FMSaveFileRequest request, DataObjects.User CurrentUser);
}

public partial class DataAccess
{
    public async Task<DataObjects.FMAppFileInfo?> FM_CreateAppFile(DataObjects.FMCreateFileRequest request, DataObjects.User CurrentUser)
    {
        var tenantId = CurrentUser.TenantId;
        var userId = CurrentUser.UserId;

        var project = await data.FMProjects
            .FirstOrDefaultAsync(p => p.FMProjectId == request.ProjectId
                                   && p.TenantId == tenantId
                                   && !p.Deleted);

        if (project == null) return null;

        var exists = await data.FMAppFiles
            .AnyAsync(f => f.FMProjectId == request.ProjectId
                        && f.FilePath.ToLower() == request.FilePath.ToLower()
                        && !f.Deleted);

        if (exists)
        {
            throw new ArgumentException($"A file named '{request.FilePath}' already exists in this project.");
        }

        var file = new EFModels.EFModels.FMAppFile
        {
            FMProjectId = request.ProjectId,
            FilePath = request.FilePath,
            FileType = request.FileType,
            CurrentVersion = 1
        };

        data.FMAppFiles.Add(file);
        await data.SaveChangesAsync();

        var version = new EFModels.EFModels.FMAppFileVersion
        {
            FMAppFileId = file.FMAppFileId,
            Version = 1,
            Content = request.Content ?? string.Empty,
            ContentHash = FM_ComputeHash(request.Content ?? string.Empty),
            CreatedBy = userId,
            Comment = "File created"
        };

        data.FMAppFileVersions.Add(version);
        project.UpdatedAt = DateTime.UtcNow;

        await data.SaveChangesAsync();

        return new DataObjects.FMAppFileInfo
        {
            Id = file.FMAppFileId,
            ProjectId = file.FMProjectId,
            FilePath = file.FilePath,
            FileType = file.FileType,
            CurrentVersion = file.CurrentVersion,
            UpdatedAt = file.UpdatedAt
        };
    }

    public async Task<DataObjects.BooleanResponse> FM_DeleteAppFile(Guid fileId, DataObjects.User CurrentUser)
    {
        var output = new DataObjects.BooleanResponse();
        var tenantId = CurrentUser.TenantId;

        var file = await data.FMAppFiles
            .FirstOrDefaultAsync(f => f.FMAppFileId == fileId && !f.Deleted);

        if (file == null)
        {
            output.Messages.Add("File not found");
            return output;
        }

        var projectBelongsToTenant = await data.FMProjects
            .AnyAsync(p => p.FMProjectId == file.FMProjectId && p.TenantId == tenantId && !p.Deleted);

        if (!projectBelongsToTenant)
        {
            output.Messages.Add("File not found");
            return output;
        }

        file.Deleted = true;
        file.DeletedAt = DateTime.UtcNow;
        file.UpdatedAt = DateTime.UtcNow;

        var project = await data.FMProjects.FindAsync(file.FMProjectId);
        if (project != null)
        {
            project.UpdatedAt = DateTime.UtcNow;
        }

        await data.SaveChangesAsync();

        output.Result = true;
        return output;
    }

    public async Task<DataObjects.FMAppFileContent?> FM_GetAppFile(Guid fileId, DataObjects.User CurrentUser)
    {
        var tenantId = CurrentUser.TenantId;

        var file = await data.FMAppFiles
            .Where(f => f.FMAppFileId == fileId && !f.Deleted)
            .FirstOrDefaultAsync();

        if (file == null) return null;

        var projectBelongsToTenant = await data.FMProjects
            .AnyAsync(p => p.FMProjectId == file.FMProjectId && p.TenantId == tenantId && !p.Deleted);

        if (!projectBelongsToTenant) return null;

        var latestVersion = await data.FMAppFileVersions
            .Where(v => v.FMAppFileId == fileId)
            .OrderByDescending(v => v.Version)
            .FirstOrDefaultAsync();

        return new DataObjects.FMAppFileContent
        {
            Id = file.FMAppFileId,
            ProjectId = file.FMProjectId,
            FilePath = file.FilePath,
            FileType = file.FileType,
            Content = latestVersion?.Content ?? string.Empty,
            Version = file.CurrentVersion,
            UpdatedAt = file.UpdatedAt
        };
    }
    // ============================================================
    // FILE METHODS
    // ============================================================

    public async Task<List<DataObjects.FMAppFileInfo>> FM_GetAppFiles(Guid projectId, DataObjects.User CurrentUser)
    {
        var tenantId = CurrentUser.TenantId;

        // Verify project belongs to tenant
        var projectExists = await data.FMProjects
            .AnyAsync(p => p.FMProjectId == projectId && p.TenantId == tenantId && !p.Deleted);

        if (!projectExists) return new List<DataObjects.FMAppFileInfo>();

        var files = await data.FMAppFiles
            .Where(f => f.FMProjectId == projectId && !f.Deleted)
            .OrderBy(f => f.FileType)
            .ThenBy(f => f.FilePath)
            .Select(f => new DataObjects.FMAppFileInfo
            {
                Id = f.FMAppFileId,
                ProjectId = f.FMProjectId,
                FilePath = f.FilePath,
                FileType = f.FileType,
                CurrentVersion = f.CurrentVersion,
                UpdatedAt = f.UpdatedAt
            })
            .ToListAsync();

        return files;
    }

    public async Task<DataObjects.FMAppFileContent?> FM_GetFileVersion(Guid versionId, DataObjects.User CurrentUser)
    {
        var tenantId = CurrentUser.TenantId;

        var version = await data.FMAppFileVersions
            .Include(v => v.AppFile)
            .ThenInclude(f => f!.Project)
            .Where(v => v.FMAppFileVersionId == versionId
                     && v.AppFile != null
                     && v.AppFile.Project != null
                     && v.AppFile.Project.TenantId == tenantId)
            .FirstOrDefaultAsync();

        if (version?.AppFile == null) return null;

        return new DataObjects.FMAppFileContent
        {
            Id = version.AppFile.FMAppFileId,
            ProjectId = version.AppFile.FMProjectId,
            FilePath = version.AppFile.FilePath,
            FileType = version.AppFile.FileType,
            Content = version.Content,
            Version = version.Version,
            UpdatedAt = version.CreatedAt
        };
    }

    public async Task<List<DataObjects.FMFileVersionInfo>> FM_GetFileVersions(Guid fileId, DataObjects.User CurrentUser)
    {
        var tenantId = CurrentUser.TenantId;

        var file = await data.FMAppFiles
            .FirstOrDefaultAsync(f => f.FMAppFileId == fileId && !f.Deleted);

        if (file == null) return new List<DataObjects.FMFileVersionInfo>();

        var projectBelongsToTenant = await data.FMProjects
            .AnyAsync(p => p.FMProjectId == file.FMProjectId && p.TenantId == tenantId && !p.Deleted);

        if (!projectBelongsToTenant) return new List<DataObjects.FMFileVersionInfo>();

        var versions = await data.FMAppFileVersions
            .Where(v => v.FMAppFileId == fileId)
            .OrderByDescending(v => v.Version)
            .Select(v => new DataObjects.FMFileVersionInfo
            {
                Id = v.FMAppFileVersionId,
                Version = v.Version,
                CreatedAt = v.CreatedAt,
                Comment = v.Comment,
                CreatedByName = string.Empty
            })
            .ToListAsync();

        return versions;
    }

    public async Task<DataObjects.FMSaveFileResponse> FM_SaveAppFile(DataObjects.FMSaveFileRequest request, DataObjects.User CurrentUser)
    {
        var response = new DataObjects.FMSaveFileResponse();
        var tenantId = CurrentUser.TenantId;
        var userId = CurrentUser.UserId;

        var file = await data.FMAppFiles
            .FirstOrDefaultAsync(f => f.FMAppFileId == request.FileId && !f.Deleted);

        if (file == null)
        {
            response.Success = false;
            response.Message = "File not found";
            return response;
        }

        var projectBelongsToTenant = await data.FMProjects
            .AnyAsync(p => p.FMProjectId == file.FMProjectId && p.TenantId == tenantId && !p.Deleted);

        if (!projectBelongsToTenant)
        {
            response.Success = false;
            response.Message = "File not found";
            return response;
        }

        // Optimistic concurrency check
        if (file.CurrentVersion != request.ExpectedVersion)
        {
            response.Message = $"Version conflict. Expected v{request.ExpectedVersion}, but file is at v{file.CurrentVersion}. Please refresh and try again.";
            return response;
        }

        // Check if content actually changed
        var contentHash = FM_ComputeHash(request.Content);
        var lastVersion = await data.FMAppFileVersions
            .Where(v => v.FMAppFileId == file.FMAppFileId)
            .OrderByDescending(v => v.Version)
            .FirstOrDefaultAsync();

        if (lastVersion != null && lastVersion.ContentHash == contentHash)
        {
            response.Success = true;
            response.NewVersion = file.CurrentVersion;
            response.Message = "No changes detected";
            return response;
        }

        // Create new version
        var newVersion = file.CurrentVersion + 1;

        var version = new EFModels.EFModels.FMAppFileVersion
        {
            FMAppFileId = file.FMAppFileId,
            Version = newVersion,
            Content = request.Content,
            ContentHash = contentHash,
            CreatedBy = userId,
            Comment = request.Comment ?? string.Empty
        };

        data.FMAppFileVersions.Add(version);

        file.CurrentVersion = newVersion;
        file.UpdatedAt = DateTime.UtcNow;

        var project = await data.FMProjects.FindAsync(file.FMProjectId);
        if (project != null)
        {
            project.UpdatedAt = DateTime.UtcNow;
        }

        await data.SaveChangesAsync();

        response.Success = true;
        response.NewVersion = newVersion;
        response.Message = $"Saved as version {newVersion}";

        return response;
    }
}

#endregion

// ---- DataAccess.Builds ----

#region FreeManager Platform - Build & Export DataAccess Methods
// ============================================================================
// FREEMANAGER BUILD & EXPORT METHODS
// ============================================================================
// Part of: DataAccess.App.FreeManager (partial)
//
// METHODS IN THIS FILE:
// ┌────────────────────────────────────────────────────────────────────────┐
// │ BUILD METHODS                                                          │
// │   FM_StartBuild     - Creates new build, auto-increments build number  │
// │   FM_GetBuilds      - Lists all builds for a project (newest first)    │
// │   FM_GetBuild       - Gets build details including log output          │
// │                                                                        │
// │ EXPORT METHODS                                                         │
// │   FM_ExportProjectAsZip - Exports project files as ZIP with structure: │
// │                                                                        │
// │       {ProjectName}.zip                                                │
// │       ├── FreeManager.DataObjects/{files}                              │
// │       ├── FreeManager.DataAccess/{files}                               │
// │       ├── FreeManager/Controllers/{files}                              │
// │       ├── FreeManager.Client/Shared/AppComponents/{files}              │
// │       ├── FreeManager.Client/Pages/{files}                             │
// │       ├── FreeManager.EFModels/EFModels/{files}                        │
// │       └── README.txt                                                   │
// │                                                                        │
// │ HELPER METHODS                                                         │
// │   FM_GetExportPath  - Maps FileType to folder path                     │
// │   FM_GetExportReadme - Generates README with install instructions      │
// │   FM_ComputeHash    - SHA256 hash for change detection                 │
// └────────────────────────────────────────────────────────────────────────┘
//
// DEPENDENCIES:
//   EFModels: FMBuild, FMProject, FMAppFile, FMAppFileVersion
//   DataObjects: FMBuildInfo, FMBuildDetailInfo, FMStartBuildRequest
// ============================================================================

public partial interface IDataAccess
{
    Task<DataObjects.FMBuildDetailInfo?> FM_GetBuild(Guid buildId, DataObjects.User CurrentUser);
    Task<List<DataObjects.FMBuildInfo>> FM_GetBuilds(Guid projectId, DataObjects.User CurrentUser);
    Task<DataObjects.FMBuildInfo> FM_StartBuild(DataObjects.FMStartBuildRequest request, DataObjects.User CurrentUser);
}

public partial class DataAccess
{
    // ============================================================
    // HELPER METHODS
    // ============================================================

    /// <summary>
    /// Computes SHA256 hash of content for change detection.
    /// </summary>
    private static string FM_ComputeHash(string content)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }

    // ============================================================
    // EXPORT METHODS
    // ============================================================

    /// <summary>
    /// Exports all project files as a ZIP with correct folder structure.
    /// </summary>
    public async Task<byte[]?> FM_ExportProjectAsZip(Guid projectId, DataObjects.User CurrentUser)
    {
        var tenantId = CurrentUser.TenantId;

        var project = await data.FMProjects
            .Include(p => p.AppFiles.Where(f => !f.Deleted))
            .FirstOrDefaultAsync(p => p.FMProjectId == projectId
                                   && p.TenantId == tenantId
                                   && !p.Deleted);

        if (project == null) return null;

        List<(string FilePath, string Content)> files = new();

        foreach (var file in project.AppFiles)
        {
            var latestVersion = await data.FMAppFileVersions
                .Where(v => v.FMAppFileId == file.FMAppFileId)
                .OrderByDescending(v => v.Version)
                .FirstOrDefaultAsync();

            if (latestVersion != null)
            {
                string path = FM_GetExportPath(file.FileType, file.FilePath, project.Name);
                files.Add((path, latestVersion.Content));
            }
        }

        if (files.Count == 0) return null;

        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            foreach (var (filePath, content) in files)
            {
                var entry = archive.CreateEntry(filePath, CompressionLevel.Optimal);
                using var entryStream = entry.Open();
                using var writer = new StreamWriter(entryStream, Encoding.UTF8);
                await writer.WriteAsync(content);
            }

            var readmeEntry = archive.CreateEntry("README.txt", CompressionLevel.Optimal);
            using var readmeStream = readmeEntry.Open();
            using var readmeWriter = new StreamWriter(readmeStream, Encoding.UTF8);
            await readmeWriter.WriteAsync(FM_GetExportReadme(project.Name, project.DisplayName, files.Count));
        }

        return memoryStream.ToArray();
    }

    public async Task<DataObjects.FMBuildDetailInfo?> FM_GetBuild(Guid buildId, DataObjects.User CurrentUser)
    {
        var tenantId = CurrentUser.TenantId;

        var build = await data.FMBuilds
            .Include(b => b.Project)
            .Where(b => b.FMBuildId == buildId
                     && b.Project != null
                     && b.Project.TenantId == tenantId)
            .Select(b => new DataObjects.FMBuildDetailInfo
            {
                Id = b.FMBuildId,
                ProjectId = b.FMProjectId,
                BuildNumber = b.BuildNumber,
                Status = b.Status,
                CreatedAt = b.CreatedAt,
                StartedAt = b.StartedAt,
                CompletedAt = b.CompletedAt,
                ArtifactSizeBytes = b.ArtifactSizeBytes,
                ErrorMessage = b.ErrorMessage,
                LogOutput = b.LogOutput
            })
            .FirstOrDefaultAsync();

        return build;
    }

    public async Task<List<DataObjects.FMBuildInfo>> FM_GetBuilds(Guid projectId, DataObjects.User CurrentUser)
    {
        var tenantId = CurrentUser.TenantId;

        var projectExists = await data.FMProjects
            .AnyAsync(p => p.FMProjectId == projectId && p.TenantId == tenantId && !p.Deleted);

        if (!projectExists) return new List<DataObjects.FMBuildInfo>();

        var builds = await data.FMBuilds
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
            .ToListAsync();

        return builds;
    }

    /// <summary>
    /// Maps file type to export path in project folder structure.
    /// Uses the project name for folder prefixes (e.g., FreeGLBA.Client, FreeGLBA.DataAccess).
    /// </summary>
    private static string FM_GetExportPath(string fileType, string fileName, string projectName)
    {
        string baseName = fileName;
        if (!baseName.Contains(".App.") && !fileType.Equals("EFModel") && !fileType.Equals("Stylesheet"))
        {
            int extIndex = baseName.LastIndexOf('.');
            if (extIndex > 0)
            {
                baseName = baseName.Substring(0, extIndex) + ".App" + baseName.Substring(extIndex);
            }
        }

        return fileType switch
        {
            DataObjects.FMFileTypes.DataObjects => $"{projectName}.DataObjects/{baseName}",
            DataObjects.FMFileTypes.DataAccess => $"{projectName}.DataAccess/{baseName}",
            DataObjects.FMFileTypes.Controller => $"{projectName}/Controllers/{baseName}",
            DataObjects.FMFileTypes.RazorComponent => $"{projectName}.Client/Shared/AppComponents/{baseName}",
            DataObjects.FMFileTypes.RazorPage => $"{projectName}.Client/Pages/{baseName}",
            DataObjects.FMFileTypes.Stylesheet => $"{projectName}.Client/wwwroot/css/{baseName}",
            DataObjects.FMFileTypes.GlobalSettings => $"{projectName}.DataObjects/{baseName}",
            DataObjects.FMFileTypes.HelpersApp => $"{projectName}.Client/{baseName}",
            DataObjects.FMFileTypes.EFModel => $"{projectName}.EFModels/EFModels/{baseName}",
            DataObjects.FMFileTypes.EFDataModel => $"{projectName}.EFModels/EFModels/{baseName}",
            DataObjects.FMFileTypes.Utilities => $"{projectName}.DataAccess/{baseName}",
            _ => $"{projectName}/{baseName}"
        };
    }

    /// <summary>
    /// Generates README content for the export ZIP.
    /// </summary>
    private static string FM_GetExportReadme(string projectName, string displayName, int fileCount) => $@"
================================================================================
{displayName} ({projectName})
Exported from FreeManager
================================================================================

This ZIP contains {fileCount} .App. extension file(s) for your project.

INSTALLATION:
1. Clone or download the latest FreeCRM from GitHub
2. Fork it to create your {projectName} project (or use FreeTools.ForkCRM)
3. Extract this ZIP on top of your {projectName} folder
4. Files will be placed in their correct locations:
   - {projectName}.DataObjects/     -> DataObjects extensions
   - {projectName}.DataAccess/      -> DataAccess extensions
   - {projectName}/Controllers/     -> API controller extensions
   - {projectName}.Client/          -> Blazor components and styles
   - {projectName}.EFModels/        -> Entity Framework models

5. Run: dotnet build
6. Run: dotnet ef database update (if you added entities)
7. Run: dotnet run

Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
================================================================================
";
    // ============================================================
    // BUILD METHODS
    // ============================================================

    public async Task<DataObjects.FMBuildInfo> FM_StartBuild(DataObjects.FMStartBuildRequest request, DataObjects.User CurrentUser)
    {
        var tenantId = CurrentUser.TenantId;
        var userId = CurrentUser.UserId;

        var project = await data.FMProjects
            .Include(p => p.Builds)
            .FirstOrDefaultAsync(p => p.FMProjectId == request.ProjectId
                                   && p.TenantId == tenantId
                                   && !p.Deleted);

        if (project == null)
        {
            throw new ArgumentException("Project not found");
        }

        var buildNumber = (project.Builds.Any() ? project.Builds.Max(b => b.BuildNumber) : 0) + 1;

        var build = new EFModels.EFModels.FMBuild
        {
            FMProjectId = project.FMProjectId,
            BuildNumber = buildNumber,
            Status = "Queued",
            CreatedBy = userId
        };

        data.FMBuilds.Add(build);
        await data.SaveChangesAsync();

        return new DataObjects.FMBuildInfo
        {
            Id = build.FMBuildId,
            ProjectId = build.FMProjectId,
            BuildNumber = build.BuildNumber,
            Status = build.Status,
            CreatedAt = build.CreatedAt
        };
    }
}

#endregion

// ---- DataAccess.EntityWizardPersistence ----

#region Entity Wizard Project Persistence - DataAccess Methods
// ============================================================================
// ENTITY WIZARD PROJECT PERSISTENCE
// ============================================================================
// Related to: FreeManager.App.EntityWizard (Blazor component)
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
    Task<DataObjects.BooleanResponse> FM_DeleteSavedProject(Guid projectId, DataObjects.User CurrentUser);
    Task<DataObjects.FMProjectZipResponse> FM_DownloadProjectZip(Guid projectId, DataObjects.User CurrentUser);
    Task<DataObjects.FMSavedProject?> FM_GetSavedProject(Guid projectId, DataObjects.User CurrentUser);
    Task<DataObjects.FMSavedProjectFilterResult> FM_GetSavedProjects(DataObjects.FMSavedProjectFilter filter, DataObjects.User CurrentUser);
    Task<DataObjects.FMSavedProject> FM_SaveWizardProject(DataObjects.FMSaveWizardProjectRequest request, DataObjects.User CurrentUser);
}

public partial class DataAccess
{
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
    public async Task<DataObjects.FMSavedProjectFilterResult> FM_GetSavedProjects(
        DataObjects.FMSavedProjectFilter filter,
        DataObjects.User CurrentUser){
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

    public async Task<DataObjects.FMSavedProject> FM_SaveWizardProject(
        DataObjects.FMSaveWizardProjectRequest request,
        DataObjects.User CurrentUser){
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

// ---- DataAccess.Templates ----

#region FreeManager Platform - Project Template Generators
// ============================================================================
// FREEMANAGER PROJECT TEMPLATES
// ============================================================================
// Part of: DataAccess.App.FreeManager (partial)
//
// Code generators that produce .App. files for new FreeManager projects.
// Called by FM_CreateProject when a template is selected.
//
// TEMPLATE TYPES:
// ┌────────────────────────────────────────────────────────────────────────┐
// │                                                                        │
// │  EMPTY        → No files generated                                     │
// │                                                                        │
// │  SKELETON     → Minimal scaffolds with placeholder comments            │
// │                 ├── DataObjects.App.{Name}.cs                          │
// │                 ├── DataAccess.App.{Name}.cs                           │
// │                 ├── DataController.App.{Name}.cs                       │
// │                 └── GlobalSettings.App.{Name}.cs                       │
// │                                                                        │
// │  STARTER      → Working example using Settings table (no migrations)   │
// │                 ├── All Skeleton files (with implementations)          │
// │                 ├── {Name}.App.razor          (card-based list view)   │
// │                 └── {Name}Page.App.razor      (routed page)            │
// │                                                                        │
// │  FULLCRUD     → Database-backed entity with EF Core                    │
// │                 ├── All Starter files                                  │
// │                 ├── {Name}Item.cs             (EF entity model)        │
// │                 └── EFDataModel.App.{Name}.cs (DbSet registration)     │
// │                                                                        │
// └────────────────────────────────────────────────────────────────────────┘
//
// GENERATED CODE FEATURES:
//   • Tenant isolation (TenantId filtering)
//   • Soft delete (Deleted, DeletedAt fields)
//   • Audit fields (CreatedAt, UpdatedAt, CreatedBy)
//   • REST API endpoints with [Authorize]
//   • Blazor UI with modal add/edit
//
// NOTE: This file is 900+ lines. Consider splitting into:
//   • Templates.Skeleton section
//   • Templates.Starter section
//   • Templates.FullCrud section
// ============================================================================

public partial class DataAccess
{
    private static string FM_GetFullCrudController(string name) => $@"using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreeManager.Server.Controllers;

#region {name} API Endpoints
// ============================================================================
// {name.ToUpper()} PROJECT - FULL CRUD TEMPLATE
// REST API endpoints with full CRUD operations.
// ============================================================================

public partial class DataController
{{
    [HttpGet]
    [Authorize]
    [Route($""~/{{DataObjects.Endpoints.{name}.GetItems}}"")]
    public async Task<ActionResult<List<DataObjects.{name}Item>>> {name}_GetItems()
    {{
        return await da.{name}_GetItems(CurrentUser);
    }}

    [HttpGet]
    [Authorize]
    [Route($""~/{{DataObjects.Endpoints.{name}.GetItem}}"")]
    public async Task<ActionResult<DataObjects.{name}Item?>> {name}_GetItem([FromQuery] Guid itemId)
    {{
        return await da.{name}_GetItem(itemId, CurrentUser);
    }}

    [HttpPost]
    [Authorize]
    [Route($""~/{{DataObjects.Endpoints.{name}.SaveItem}}"")]
    public async Task<ActionResult<DataObjects.{name}Item?>> {name}_SaveItem([FromBody] DataObjects.{name}Item item)
    {{
        return await da.{name}_SaveItem(item, CurrentUser);
    }}

    [HttpDelete]
    [Authorize]
    [Route($""~/{{DataObjects.Endpoints.{name}.DeleteItem}}"")]
    public async Task<ActionResult<DataObjects.BooleanResponse>> {name}_DeleteItem([FromQuery] Guid itemId)
    {{
        return await da.{name}_DeleteItem(itemId, CurrentUser);
    }}
}}

#endregion
";

    private static string FM_GetFullCrudDataAccess(string name) => $@"using Microsoft.EntityFrameworkCore;

namespace FreeManager;

#region {name} DataAccess
// ============================================================================
// {name.ToUpper()} PROJECT - FULL CRUD TEMPLATE
// Business logic with EF Core database operations.
// ============================================================================

public partial interface IDataAccess
{{
    Task<List<DataObjects.{name}Item>> {name}_GetItems(DataObjects.User CurrentUser);
    Task<DataObjects.{name}Item?> {name}_GetItem(Guid itemId, DataObjects.User CurrentUser);
    Task<DataObjects.{name}Item?> {name}_SaveItem(DataObjects.{name}SaveRequest request, DataObjects.User CurrentUser);
    Task<DataObjects.BooleanResponse> {name}_DeleteItem(Guid itemId, DataObjects.User CurrentUser);
}}

public partial class DataAccess
{{
    public async Task<List<DataObjects.{name}Item>> {name}_GetItems(DataObjects.User CurrentUser)
    {{
        Guid tenantId = CurrentUser.TenantId;

        List<DataObjects.{name}Item> output = await data.{name}Items
            .Where(x => x.TenantId == tenantId && !x.Deleted)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new DataObjects.{name}Item {{
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                IsComplete = x.IsComplete,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                CompletedAt = x.CompletedAt
            }})
            .ToListAsync();

        return output;
    }}

    public async Task<DataObjects.{name}Item?> {name}_GetItem(Guid itemId, DataObjects.User CurrentUser)
    {{
        Guid tenantId = CurrentUser.TenantId;

        EFModels.EFModels.{name}Item? entity = await data.{name}Items
            .FirstOrDefaultAsync(x => x.Id == itemId && x.TenantId == tenantId && !x.Deleted);

        if (entity == null) return null;

        return new DataObjects.{name}Item {{
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            IsComplete = entity.IsComplete,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            CompletedAt = entity.CompletedAt
        }};
    }}

    public async Task<DataObjects.{name}Item?> {name}_SaveItem(DataObjects.{name}SaveRequest request, DataObjects.User CurrentUser)
    {{
        Guid tenantId = CurrentUser.TenantId;
        EFModels.EFModels.{name}Item entity;

        if (request.Id.HasValue && request.Id != Guid.Empty) {{
            // Update existing
            entity = await data.{name}Items
                .FirstOrDefaultAsync(x => x.Id == request.Id.Value && x.TenantId == tenantId && !x.Deleted)
                ?? new EFModels.EFModels.{name}Item {{ TenantId = tenantId }};

            entity.Name = request.Name;
            entity.Description = request.Description;
            entity.UpdatedAt = DateTime.UtcNow;

            if (request.IsComplete && !entity.IsComplete) {{
                entity.CompletedAt = DateTime.UtcNow;
            }} else if (!request.IsComplete) {{
                entity.CompletedAt = null;
            }}
            entity.IsComplete = request.IsComplete;

            if (entity.Id == Guid.Empty) {{
                entity.Id = Guid.NewGuid();
                entity.CreatedAt = DateTime.UtcNow;
                entity.CreatedBy = CurrentUser.UserId;
                data.{name}Items.Add(entity);
            }}
        }} else {{
            // Create new
            entity = new EFModels.EFModels.{name}Item {{
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Name = request.Name,
                Description = request.Description,
                IsComplete = request.IsComplete,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = CurrentUser.UserId
            }};
            data.{name}Items.Add(entity);
        }}

        await data.SaveChangesAsync();

        return new DataObjects.{name}Item {{
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            IsComplete = entity.IsComplete,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            CompletedAt = entity.CompletedAt
        }};
    }}

    public async Task<DataObjects.BooleanResponse> {name}_DeleteItem(Guid itemId, DataObjects.User CurrentUser)
    {{
        var output = new DataObjects.BooleanResponse();
        var tenantId = CurrentUser.TenantId;

        var entity = await data.{name}Items
            .FirstOrDefaultAsync(x => x.Id == itemId && x.TenantId == tenantId && !x.Deleted);

        if (entity == null) {{
            output.Messages.Add(""Item not found"");
            return output;
        }}

        entity.Deleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        await data.SaveChangesAsync();

        output.Result = true;
        return output;
    }}
}}

#endregion
";

    // ============================================================
    // FULL CRUD TEMPLATES (EF Entity based)
    // ============================================================

    private static string FM_GetFullCrudDataObjects(string name) => $@"namespace FreeManager;

#region {name} DataObjects
// ===========================================================================
// {name.ToUpper()} PROJECT - FULL CRUD TEMPLATE
// DTOs for database-backed CRUD operations.
// ===========================================================================

public partial class DataObjects
{{
    public static partial class Endpoints
    {{
        public static class {name}
        {{
            public const string GetItems = ""api/Data/{name}_GetItems"";
            public const string GetItem = ""api/Data/{name}_GetItem"";
            public const string SaveItem = ""api/Data/{name}_SaveItem"";
            public const string DeleteItem = ""api/Data/{name}_DeleteItem"";
        }}
    }}

    /// <summary>
    /// {name} item DTO for API responses.
    /// </summary>
    public class {name}Item
    {{
        public Guid Id {{ get; set; }}
        public string Name {{ get; set; }} = string.Empty;
        public string Description {{ get; set; }} = string.Empty;
        public bool IsComplete {{ get; set; }}
        public DateTime CreatedAt {{ get; set; }}
        public DateTime UpdatedAt {{ get; set; }}
        public DateTime? CompletedAt {{ get; set; }}
    }}

    /// <summary>
    /// Request to save an item.
    /// </summary>
    public class {name}SaveRequest
    {{
        public Guid? Id {{ get; set; }}
        public string Name {{ get; set; }} = string.Empty;
        public string Description {{ get; set; }} = string.Empty;
        public bool IsComplete {{ get; set; }}
    }}
}}

#endregion
";

    private static string FM_GetFullCrudDbContext(string name) => $@"using Microsoft.EntityFrameworkCore;

namespace FreeManager.EFModels.EFModels;

#region {name} DbContext Extension
// ============================================================================
// {name.ToUpper()} PROJECT - FULL CRUD TEMPLATE
// DbSet registration for EF Core.
// ============================================================================

public partial class EFDataModel
{{
    public virtual DbSet<{name}Item> {name}Items {{ get; set; }} = null!;
}}

#endregion
";

    private static string FM_GetFullCrudEntity(string name) => $@"using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FreeManager.EFModels.EFModels;

#region {name} Entity
// ============================================================================
// {name.ToUpper()} PROJECT - FULL CRUD TEMPLATE
// Entity Framework model with tenant isolation and soft delete.
// ============================================================================

[Table(""{name}Items"")]
public class {name}Item
{{
    [Key]
    public Guid Id {{ get; set; }} = Guid.NewGuid();

    [Required]
    public Guid TenantId {{ get; set; }}

    [MaxLength(200)]
    public string Name {{ get; set; }} = string.Empty;

    public string Description {{ get; set; }} = string.Empty;

    public bool IsComplete {{ get; set; }} = false;

    public DateTime CreatedAt {{ get; set; }} = DateTime.UtcNow;
    public DateTime UpdatedAt {{ get; set; }} = DateTime.UtcNow;
    public DateTime? CompletedAt {{ get; set; }}

    public Guid? CreatedBy {{ get; set; }}

    public bool Deleted {{ get; set; }} = false;
    public DateTime? DeletedAt {{ get; set; }}

    // Navigation
    public virtual Tenant? Tenant {{ get; set; }}
}}

#endregion
";
    // ============================================================
    // PROJECT TEMPLATE ROUTING
    // ============================================================

    /// <summary>
    /// Gets template files based on project template type.
    /// </summary>
    private static List<(string FileName, string FileType, string Content)> FM_GetProjectTemplateFiles(
        DataObjects.FMProjectTemplate template,
        string projectName){
        List<(string, string, string)> files = new();

        switch (template)
        {
            case DataObjects.FMProjectTemplate.Empty:
                break;

            case DataObjects.FMProjectTemplate.Skeleton:
                files.Add(($"DataObjects.App.{projectName}.cs", DataObjects.FMFileTypes.DataObjects, FM_GetSkeletonDataObjects(projectName)));
                files.Add(($"DataAccess.App.{projectName}.cs", DataObjects.FMFileTypes.DataAccess, FM_GetSkeletonDataAccess(projectName)));
                files.Add(($"DataController.App.{projectName}.cs", DataObjects.FMFileTypes.Controller, FM_GetSkeletonController(projectName)));
                files.Add(($"GlobalSettings.App.{projectName}.cs", DataObjects.FMFileTypes.GlobalSettings, FM_GetSkeletonGlobalSettings(projectName)));
                break;

            case DataObjects.FMProjectTemplate.Starter:
                files.Add(($"DataObjects.App.{projectName}.cs", DataObjects.FMFileTypes.DataObjects, FM_GetStarterDataObjects(projectName)));
                files.Add(($"DataAccess.App.{projectName}.cs", DataObjects.FMFileTypes.DataAccess, FM_GetStarterDataAccess(projectName)));
                files.Add(($"DataController.App.{projectName}.cs", DataObjects.FMFileTypes.Controller, FM_GetStarterController(projectName)));
                files.Add(($"GlobalSettings.App.{projectName}.cs", DataObjects.FMFileTypes.GlobalSettings, FM_GetStarterGlobalSettings(projectName)));
                files.Add(($"{projectName}.App.razor", DataObjects.FMFileTypes.RazorComponent, FM_GetStarterComponent(projectName)));
                files.Add(($"{projectName}Page.App.razor", DataObjects.FMFileTypes.RazorPage, FM_GetStarterPage(projectName)));
                break;

            case DataObjects.FMProjectTemplate.FullCrud:
                files.Add(($"DataObjects.App.{projectName}.cs", DataObjects.FMFileTypes.DataObjects, FM_GetFullCrudDataObjects(projectName)));
                files.Add(($"DataAccess.App.{projectName}.cs", DataObjects.FMFileTypes.DataAccess, FM_GetFullCrudDataAccess(projectName)));
                files.Add(($"DataController.App.{projectName}.cs", DataObjects.FMFileTypes.Controller, FM_GetFullCrudController(projectName)));
                files.Add(($"GlobalSettings.App.{projectName}.cs", DataObjects.FMFileTypes.GlobalSettings, FM_GetStarterGlobalSettings(projectName)));
                files.Add(($"{projectName}.App.razor", DataObjects.FMFileTypes.RazorComponent, FM_GetStarterComponent(projectName)));
                files.Add(($"{projectName}Page.App.razor", DataObjects.FMFileTypes.RazorPage, FM_GetStarterPage(projectName)));
                files.Add(($"{projectName}Item.cs", DataObjects.FMFileTypes.EFModel, FM_GetFullCrudEntity(projectName)));
                files.Add(($"EFDataModel.App.{projectName}.cs", DataObjects.FMFileTypes.EFDataModel, FM_GetFullCrudDbContext(projectName)));
                break;
        }

        return files;
    }

    private static string FM_GetSkeletonController(string name) => $@"using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreeManager.Server.Controllers;

#region {name} API Endpoints
// ============================================================================
// {name.ToUpper()} PROJECT
// Add your API endpoints here.
// ============================================================================

public partial class DataController
{{
    // Add your endpoints here
    // [HttpGet]
    // [Authorize]
    // [Route(""~/api/Data/{name}_GetItems"")]
    // public async Task<ActionResult<List<DataObjects.{name}Item>>> {name}_GetItems() {{ }}
}}

#endregion
";

    private static string FM_GetSkeletonDataAccess(string name) => $@"namespace FreeManager;

#region {name} DataAccess
// ============================================================================
// {name.ToUpper()} PROJECT
// Add your business logic methods here.
// ============================================================================

public partial interface IDataAccess
{{
    // Define your method signatures here
    // Task<List<DataObjects.{name}Item>> {name}_GetItems(DataObjects.User CurrentUser);
}}

public partial class DataAccess
{{
    // Implement your methods here
}}

#endregion
";

    // ============================================================
    // SKELETON TEMPLATES
    // ============================================================

    private static string FM_GetSkeletonDataObjects(string name) => $@"namespace FreeManager;

#region {name} DataObjects
// ============================================================================
// {name.ToUpper()} PROJECT
// Add your DTOs and models here.
// ============================================================================

public partial class DataObjects
{{
    public static partial class Endpoints
    {{
        public static class {name}
        {{
            // Define your API endpoints here
            // public const string GetItems = ""api/Data/{name}_GetItems"";
        }}
    }}

    // Add your DTOs here
    // public class {name}Item {{ }}
}}

#endregion
";

    private static string FM_GetSkeletonGlobalSettings(string name) => $@"namespace FreeManager;

#region {name} Settings
// ============================================================================
// {name.ToUpper()} PROJECT
// Add your app configuration here.
// ============================================================================

public static partial class GlobalSettings
{{
    public static class {name}
    {{
        public static string AppName {{ get; set; }} = ""{name}"";
        public static string Version {{ get; set; }} = ""1.0.0"";
    }}
}}

#endregion
";

    private static string FM_GetStarterComponent(string name) => $@"@implements IDisposable
@inject BlazorDataModel Model
@inject HttpClient Http

@* ============================================================================
   {name} Component - STARTER TEMPLATE
   Card-based list view with add/edit/delete functionality.
   ============================================================================ *@

@if (Model.Loaded && Model.LoggedIn) {{
    <div class=""container-fluid"">
        <h1 class=""page-title"">
            <i class=""fa-solid fa-list-check me-2""></i>
            {name} Items
        </h1>

        <div class=""mb-3"">
            <button class=""btn btn-success"" @onclick=""ShowAddModal"">
                <i class=""fa-solid fa-plus me-1""></i>
                Add Item
            </button>
        </div>

        @if (_loading) {{
            <LoadingMessage />
        }} else {{
            @if (_items.Count == 0) {{
                <div class=""alert alert-info"">
                    <i class=""fa-solid fa-info-circle me-2""></i>
                    No items yet. Click ""Add Item"" to create your first one!
                </div>
            }} else {{
                <div class=""row"">
                    @foreach (var item in _items) {{
                        <div class=""col-md-4 mb-3"">
                            <div class=""card @(item.IsComplete ? ""border-success"" : """")"">
                                <div class=""card-body"">
                                    <h5 class=""card-title"">
                                        @if (item.IsComplete) {{
                                            <i class=""fa-solid fa-check-circle text-success me-1""></i>
                                        }}
                                        @item.Name
                                    </h5>
                                    <p class=""card-text text-muted"">@item.Description</p>
                                    <div class=""d-flex justify-content-between align-items-center"">
                                        <small class=""text-muted"">@item.CreatedAt.ToString(""MMM dd, yyyy"")</small>
                                        <div class=""btn-group btn-group-sm"">
                                            <button class=""btn btn-outline-primary"" @onclick=""() => ShowEditModal(item)"">
                                                <i class=""fa-solid fa-edit""></i>
                                            </button>
                                            <button class=""btn btn-outline-danger"" @onclick=""() => DeleteItem(item)"">
                                                <i class=""fa-solid fa-trash""></i>
                                            </button>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    }}
                </div>
            }}
        }}
    </div>

    @* Add/Edit Modal *@
    @if (_showModal) {{
        <div class=""modal fade show d-block"" style=""background-color: rgba(0,0,0,0.5);"">
            <div class=""modal-dialog"">
                <div class=""modal-content"">
                    <div class=""modal-header"">
                        <h5 class=""modal-title"">@(_editingItem.Id == null ? ""Add"" : ""Edit"") Item</h5>
                        <button type=""button"" class=""btn-close"" @onclick=""CloseModal""></button>
                    </div>
                    <div class=""modal-body"">
                        <div class=""mb-3"">
                            <label class=""form-label"">Name</label>
                            <input type=""text"" class=""form-control"" @bind=""_editingItem.Name"" />
                        </div>
                        <div class=""mb-3"">
                            <label class=""form-label"">Description</label>
                            <textarea class=""form-control"" rows=""3"" @bind=""_editingItem.Description""></textarea>
                        </div>
                        <div class=""form-check"">
                            <input type=""checkbox"" class=""form-check-input"" id=""isComplete"" @bind=""_editingItem.IsComplete"" />
                            <label class=""form-check-label"" for=""isComplete"">Mark as complete</label>
                        </div>
                    </div>
                    <div class=""modal-footer"">
                        <button class=""btn btn-secondary"" @onclick=""CloseModal"">Cancel</button>
                        <button class=""btn btn-primary"" @onclick=""SaveItem"" disabled=""@_saving"">
                            @if (_saving) {{
                                <i class=""fa-solid fa-spinner fa-spin me-1""></i>
                            }}
                            Save
                        </button>
                    </div>
                </div>
            </div>
        </div>
    }}
}}

@code {{
    private bool _loading = true;
    private bool _loadedData = false;
    private bool _saving = false;
    private bool _showModal = false;
    private List<DataObjects.{name}Item> _items = new();
    private DataObjects.{name}SaveRequest _editingItem = new();

    public void Dispose()
    {{
        Model.OnChange -= StateHasChanged;
    }}

    protected override void OnInitialized()
    {{
        Model.OnChange += StateHasChanged;
    }}

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {{
        if (Model.Loaded && Model.LoggedIn && !_loadedData) {{
            _loadedData = true;
            await LoadData();
        }}
    }}

    private async Task LoadData()
    {{
        try {{
            _items = await Http.GetFromJsonAsync<List<DataObjects.{name}Item>>(
                DataObjects.Endpoints.{name}.GetItems) ?? new();
        }} catch (Exception ex) {{
            await Helpers.ConsoleLog($""Error loading items: {{ex.Message}}"");
        }}
        _loading = false;
        StateHasChanged();
    }}

    private void ShowAddModal()
    {{
        _editingItem = new DataObjects.{name}SaveRequest();
        _showModal = true;
    }}

    private void ShowEditModal(DataObjects.{name}Item item)
    {{
        _editingItem = new DataObjects.{name}SaveRequest {{
            Id = item.Id,
            Name = item.Name,
            Description = item.Description,
            IsComplete = item.IsComplete
        }};
        _showModal = true;
    }}

    private void CloseModal()
    {{
        _showModal = false;
        _editingItem = new();
    }}

    private async Task SaveItem()
    {{
        if (string.IsNullOrWhiteSpace(_editingItem.Name)) return;

        _saving = true;
        StateHasChanged();

        try {{
            await Helpers.GetOrPost<DataObjects.{name}Item>(
                DataObjects.Endpoints.{name}.SaveItem, _editingItem);
            await LoadData();
            CloseModal();
        }} catch (Exception ex) {{
            await Helpers.ConsoleLog($""Error saving item: {{ex.Message}}"");
        }}

        _saving = false;
        StateHasChanged();
    }}

    private async Task DeleteItem(DataObjects.{name}Item item)
    {{
        try {{
            await Http.DeleteAsync($""{{DataObjects.Endpoints.{name}.DeleteItem}}?itemId={{item.Id}}"");
            await LoadData();
        }} catch (Exception ex) {{
            await Helpers.ConsoleLog($""Error deleting item: {{ex.Message}}"");
        }}
    }}
}}
";

    private static string FM_GetStarterController(string name) => $@"using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreeManager.Server.Controllers;

#region {name} API Endpoints
// ============================================================================
// {name.ToUpper()} PROJECT - STARTER TEMPLATE
// REST API endpoints for {name} items.
// ============================================================================

public partial class DataController
{{
    [HttpGet]
    [Authorize]
    [Route($""~/{{DataObjects.Endpoints.{name}.GetItems}}"")]
    public async Task<ActionResult<List<DataObjects.{name}Item>>> {name}_GetItems()
    {{
        return await da.{name}_GetItems(CurrentUser);
    }}

    [HttpPost]
    [Authorize]
    [Route($""~/{{DataObjects.Endpoints.{name}.SaveItem}}"")]
    public async Task<ActionResult<DataObjects.{name}Item?>> {name}_SaveItem([FromBody] DataObjects.{name}SaveRequest request)
    {{
        return await da.{name}_SaveItem(request, CurrentUser);
    }}

    [HttpDelete]
    [Authorize]
    [Route($""~/{{DataObjects.Endpoints.{name}.DeleteItem}}"")]
    public async Task<ActionResult<DataObjects.BooleanResponse>> {name}_DeleteItem([FromQuery] Guid itemId)
    {{
        return await da.{name}_DeleteItem(itemId, CurrentUser);
    }}
}}

#endregion
";

    private static string FM_GetStarterDataAccess(string name) => $@"using System.Text.Json;

namespace FreeManager;

#region {name} DataAccess
// ============================================================================
// {name.ToUpper()} PROJECT - STARTER TEMPLATE
// Business logic using Settings table for JSON storage.
// ============================================================================

public partial interface IDataAccess
{{
    Task<List<DataObjects.{name}Item>> {name}_GetItems(DataObjects.User CurrentUser);
    Task<DataObjects.{name}Item?> {name}_SaveItem(DataObjects.{name}SaveRequest request, DataObjects.User CurrentUser);
    Task<DataObjects.BooleanResponse> {name}_DeleteItem(Guid itemId, DataObjects.User CurrentUser);
}}

public partial class DataAccess
{{
    private const string {name}SettingsKey = ""{name}_Items"";

    public async Task<List<DataObjects.{name}Item>> {name}_GetItems(DataObjects.User CurrentUser)
    {{
        var items = await {name}_LoadItems(CurrentUser.TenantId);
        return items.OrderByDescending(x => x.CreatedAt).ToList();
    }}

    public async Task<DataObjects.{name}Item?> {name}_SaveItem(DataObjects.{name}SaveRequest request, DataObjects.User CurrentUser)
    {{
        List<DataObjects.{name}Item> items = await {name}_LoadItems(CurrentUser.TenantId);
        DataObjects.{name}Item item;

        if (request.Id.HasValue && request.Id != Guid.Empty) {{
            // Update existing
            item = items.FirstOrDefault(x => x.Id == request.Id.Value) ?? new DataObjects.{name}Item();
            item.Name = request.Name;
            item.Description = request.Description;

            if (request.IsComplete && !item.IsComplete) {{
                item.CompletedAt = DateTime.UtcNow;
            }} else if (!request.IsComplete) {{
                item.CompletedAt = null;
            }}
            item.IsComplete = request.IsComplete;

            if (!items.Any(x => x.Id == item.Id)) {{
                items.Add(item);
            }}
        }} else {{
            // Create new
            item = new DataObjects.{name}Item {{
                Id = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                IsComplete = request.IsComplete,
                CreatedAt = DateTime.UtcNow
            }};
            items.Add(item);
        }}

        await {name}_SaveItems(items, CurrentUser.TenantId);
        return item;
    }}

    public async Task<DataObjects.BooleanResponse> {name}_DeleteItem(Guid itemId, DataObjects.User CurrentUser)
    {{
        DataObjects.BooleanResponse output = new();
        List<DataObjects.{name}Item> items = await {name}_LoadItems(CurrentUser.TenantId);

        int removed = items.RemoveAll(x => x.Id == itemId);
        if (removed > 0) {{
            await {name}_SaveItems(items, CurrentUser.TenantId);
            output.Result = true;
        }} else {{
            output.Messages.Add(""Item not found"");
        }}

        return output;
    }}

    private async Task<List<DataObjects.{name}Item>> {name}_LoadItems(Guid tenantId)
    {{
        DataObjects.Setting? setting = await GetSetting({name}SettingsKey, tenantId);
        if (setting == null || string.IsNullOrEmpty(setting.Value)) {{
            return new List<DataObjects.{name}Item>();
        }}
        return JsonSerializer.Deserialize<List<DataObjects.{name}Item>>(setting.Value) ?? new();
    }}

    private async Task {name}_SaveItems(List<DataObjects.{name}Item> items, Guid tenantId)
    {{
        string json = JsonSerializer.Serialize(items);
        await SaveSetting({name}SettingsKey, json, tenantId);
    }}
}}

#endregion
";

    // ============================================================
    // STARTER TEMPLATES (Working example with Settings storage)
    // ============================================================

    private static string FM_GetStarterDataObjects(string name) => $@"using System.Text.Json.Serialization;

namespace FreeManager;

#region {name} DataObjects
// ============================================================================
// {name.ToUpper()} PROJECT - STARTER TEMPLATE
// This template provides a working Items list stored in the Settings table.
// No database migration required!
// ============================================================================

public partial class DataObjects
{{
    public static partial class Endpoints
    {{
        public static class {name}
        {{
            public const string GetItems = ""api/Data/{name}_GetItems"";
            public const string SaveItem = ""api/Data/{name}_SaveItem"";
            public const string DeleteItem = ""api/Data/{name}_DeleteItem"";
        }}
    }}

    /// <summary>
    /// {name} item - stored as JSON in Settings table.
    /// </summary>
    public class {name}Item
    {{
        public Guid Id {{ get; set; }} = Guid.NewGuid();
        public string Name {{ get; set; }} = string.Empty;
        public string Description {{ get; set; }} = string.Empty;
        public bool IsComplete {{ get; set; }} = false;
        public DateTime CreatedAt {{ get; set; }} = DateTime.UtcNow;
        public DateTime? CompletedAt {{ get; set; }}
    }}

    /// <summary>
    /// Request to save an item.
    /// </summary>
    public class {name}SaveRequest
    {{
        public Guid? Id {{ get; set; }}
        public string Name {{ get; set; }} = string.Empty;
        public string Description {{ get; set; }} = string.Empty;
        public bool IsComplete {{ get; set; }} = false;
    }}
}}

#endregion
";

    private static string FM_GetStarterGlobalSettings(string name) => $@"namespace FreeManager;

#region {name} Settings
// ============================================================================
// {name.ToUpper()} PROJECT - STARTER TEMPLATE
// App configuration and constants.
// ============================================================================

public static partial class GlobalSettings
{{
    public static class {name}
    {{
        public static string AppName {{ get; set; }} = ""{name}"";
        public static string Version {{ get; set; }} = ""1.0.0"";
        public static string Description {{ get; set; }} = ""A {name} application built with FreeManager"";
    }}
}}

#endregion
";

    private static string FM_GetStarterPage(string name) => $@"@page ""/{name}""
@page ""/{{TenantCode}}/{name}""
@inject BlazorDataModel Model
@implements IDisposable

@* ============================================================================
   {name} Page - STARTER TEMPLATE
   Routed page that hosts the {name} component.
   ============================================================================ *@

@if (Model.Loaded && Model.LoggedIn && Model.View == _pageName) {{
    <{name}_App />
}}

@code {{
    [Parameter] public string? TenantCode {{ get; set; }}

    protected bool _loadedData = false;
    protected string _pageName = ""{name.ToLower()}"";

    public void Dispose()
    {{
        Model.OnChange -= OnDataModelUpdated;
    }}

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {{
        if (firstRender) {{
            Model.TenantCodeFromUrl = TenantCode;
        }}

        if (Model.Loaded) {{
            if (Model.LoggedIn) {{
                if (!_loadedData) {{
                    _loadedData = true;
                    await Helpers.ValidateUrl(TenantCode, true);
                }}
            }} else {{
                Helpers.NavigateToLogin();
            }}
        }}
    }}

    protected void OnDataModelUpdated()
    {{
        if (Model.View == _pageName) {{
            StateHasChanged();
        }}
    }}

    protected override void OnInitialized()
    {{
        Model.View = _pageName;
        Model.OnChange += StateHasChanged;
    }}
}}
";
}

#endregion

// ---- DataAccess.Language ----

// FreeManager language tags, called from one line in AppLanguage in DataAccess.App.cs.

public partial class DataAccess
{
    private Dictionary<string, string> FreeManagerAppLanguage => new Dictionary<string, string> {
        { "AppBuilder", "App Builder" },
        { "Dashboard", "Dashboard" },
        { "EntityWizard", "Entity Wizard" },
        { "SetupWizardBuilder", "Setup Wizard" },
        { "TemplateGallery", "Templates" },
    };
}
