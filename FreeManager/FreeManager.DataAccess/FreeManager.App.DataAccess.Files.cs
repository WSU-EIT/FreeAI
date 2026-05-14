using Microsoft.EntityFrameworkCore;

namespace FreeManager;

#region FreeManager Platform - File DataAccess Methods
// ============================================================================
// FREEMANAGER FILE METHODS
// ============================================================================
// Part of: DataAccess.App.FreeManager (partial)
// Coordinator: DataAccess.App.FreeManager.cs
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

public partial class DataAccess
{
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
}

#endregion
