namespace FreeManager;

#region FreeManager Platform - API Endpoint Constants
// ┌─────────────────────────────────────────────────────────────────┐
// │           DataObjects.App.FreeManager.Endpoints.cs              │
// │                    API Endpoint Constants                        │
// ├─────────────────────────────────────────────────────────────────┤
// │ PROJECT ENDPOINTS (6)                                           │
// │   GetProjects, GetProject, CreateProject                        │
// │   UpdateProject, DeleteProject, ExportProject                   │
// ├─────────────────────────────────────────────────────────────────┤
// │ FILE ENDPOINTS (7)                                              │
// │   GetAppFiles, GetAppFile, SaveAppFile, CreateAppFile           │
// │   DeleteAppFile, GetFileVersions, GetFileVersion                │
// ├─────────────────────────────────────────────────────────────────┤
// │ BUILD ENDPOINTS (4)                                             │
// │   StartBuild, GetBuilds, GetBuild, DownloadArtifact             │
// ├─────────────────────────────────────────────────────────────────┤
// │ ENTITY WIZARD ENDPOINTS (6)                                     │
// │   GenerateEntityCode, GetSavedProjects, GetSavedProject         │
// │   SaveWizardProject, DeleteSavedProject, DownloadProjectZip     │
// └─────────────────────────────────────────────────────────────────┘
// Part of: DataObjects.App.FreeManager (partial)
// ============================================================================

public partial class DataObjects
{
    public static partial class Endpoints
    {
        /// <summary>
        /// FreeManager API endpoints for project, file, and build management.
        /// </summary>
        public static class FreeManager
        {
            // ============================================================
            // PROJECT ENDPOINTS
            // ============================================================

            /// <summary>Get all projects for tenant</summary>
            public const string GetProjects = "api/Data/FM_GetProjects";

            /// <summary>Get single project by ID</summary>
            public const string GetProject = "api/Data/FM_GetProject";

            /// <summary>Create new project</summary>
            public const string CreateProject = "api/Data/FM_CreateProject";

            /// <summary>Update project metadata</summary>
            public const string UpdateProject = "api/Data/FM_UpdateProject";

            /// <summary>Soft-delete project</summary>
            public const string DeleteProject = "api/Data/FM_DeleteProject";

            /// <summary>Export project as ZIP</summary>
            public const string ExportProject = "api/Data/FM_ExportProject";

            // ============================================================
            // FILE ENDPOINTS
            // ============================================================

            /// <summary>Get all files for project</summary>
            public const string GetAppFiles = "api/Data/FM_GetAppFiles";

            /// <summary>Get single file content</summary>
            public const string GetAppFile = "api/Data/FM_GetAppFile";

            /// <summary>Save file content (creates new version)</summary>
            public const string SaveAppFile = "api/Data/FM_SaveAppFile";

            /// <summary>Create new file in project</summary>
            public const string CreateAppFile = "api/Data/FM_CreateAppFile";

            /// <summary>Soft-delete file</summary>
            public const string DeleteAppFile = "api/Data/FM_DeleteAppFile";

            /// <summary>Get version history for file</summary>
            public const string GetFileVersions = "api/Data/FM_GetFileVersions";

            /// <summary>Get specific version content</summary>
            public const string GetFileVersion = "api/Data/FM_GetFileVersion";

            // ============================================================
            // BUILD ENDPOINTS (future use)
            // ============================================================

            /// <summary>Start new build</summary>
            public const string StartBuild = "api/Data/FM_StartBuild";

            /// <summary>Get all builds for project</summary>
            public const string GetBuilds = "api/Data/FM_GetBuilds";

            /// <summary>Get build details</summary>
            public const string GetBuild = "api/Data/FM_GetBuild";

            /// <summary>Download build artifact</summary>
            public const string DownloadArtifact = "api/Data/FM_DownloadArtifact";

            // ============================================================
            // ENTITY WIZARD ENDPOINTS
            // ============================================================

            /// <summary>Generate entity code from wizard state</summary>
            public const string GenerateEntityCode = "api/Data/FM_GenerateEntityCode";

            /// <summary>Get saved wizard projects (paginated)</summary>
            public const string GetSavedProjects = "api/Data/FM_GetSavedProjects";

            /// <summary>Get single wizard project with state JSON</summary>
            public const string GetSavedProject = "api/Data/FM_GetSavedProject";

            /// <summary>Save wizard project state</summary>
            public const string SaveWizardProject = "api/Data/FM_SaveWizardProject";

            /// <summary>Delete wizard project</summary>
            public const string DeleteSavedProject = "api/Data/FM_DeleteSavedProject";

            /// <summary>Download project as ZIP with generated files</summary>
            public const string DownloadProjectZip = "api/Data/FM_DownloadProjectZip";
        }
    }
}

#endregion
