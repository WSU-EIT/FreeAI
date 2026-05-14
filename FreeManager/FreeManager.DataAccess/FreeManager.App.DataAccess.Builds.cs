using Microsoft.EntityFrameworkCore;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace FreeManager;

#region FreeManager Platform - Build & Export DataAccess Methods
// ============================================================================
// FREEMANAGER BUILD & EXPORT METHODS
// ============================================================================
// Part of: DataAccess.App.FreeManager (partial)
// Coordinator: DataAccess.App.FreeManager.cs
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

public partial class DataAccess
{
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
}

#endregion
