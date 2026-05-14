using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace FreeManager;

#region FreeManager Platform - DataAccess Methods (Coordinator)
// ============================================================================
// FREEMANAGER PLATFORM EXTENSION - DATAACCESS COORDINATOR
// ============================================================================
//
// This is the coordinator file for FreeManager DataAccess methods.
// All methods are split into feature-specific partial files:
//
//   DataAccess.App.FreeManager.cs (this file)
//       │
//       ├── DataAccess.App.FreeManager.Projects.cs
//       │       └── FM_GetProjects, FM_GetProject, FM_CreateProject,
//       │           FM_UpdateProject, FM_DeleteProject
//       │
//       ├── DataAccess.App.FreeManager.Files.cs
//       │       └── FM_GetAppFiles, FM_GetAppFile, FM_SaveAppFile,
//       │           FM_CreateAppFile, FM_DeleteAppFile, FM_GetFileVersions,
//       │           FM_GetFileVersion
//       │
//       ├── DataAccess.App.FreeManager.Builds.cs
//       │       └── FM_StartBuild, FM_GetBuilds, FM_GetBuild,
//       │           FM_ExportProjectAsZip, FM_ComputeHash
//       │
//       ├── DataAccess.App.FreeManager.Templates.cs
//       │       └── FM_GetProjectTemplateFiles
//       │           ├── Skeleton: DataObjects, DataAccess, Controller, GlobalSettings
//       │           ├── Starter:  + Component, Page (Settings-based storage)
//       │           └── FullCrud: + EFModel, DbContext (database entity)
//       │
//       └── DataAccess.App.EntityWizardPersistence.cs
//               └── FM_GetSavedProjects, FM_GetSavedProject, FM_SaveWizardProject,
//                   FM_DeleteSavedProject, FM_DownloadProjectZip
//
// ARCHITECTURE:
// ┌─────────────────────────────────────────────────────────────────────────┐
// │                           FreeManager Platform                          │
// ├─────────────────────────────────────────────────────────────────────────┤
// │  Controller Layer          DataAccess Layer           EFModels Layer   │
// │  ┌─────────────────┐      ┌─────────────────┐      ┌─────────────────┐ │
// │  │ DataController  │ ──── │  IDataAccess    │ ──── │  FMProject      │ │
// │  │ .App.FreeManager│      │  (this file)    │      │  FMAppFile      │ │
// │  └─────────────────┘      └─────────────────┘      │  FMAppFileVer   │ │
// │          │                        │                │  FMBuild        │ │
// │          ▼                        ▼                └─────────────────┘ │
// │  ┌─────────────────┐      ┌─────────────────┐                          │
// │  │ Blazor Client   │      │  DataObjects    │                          │
// │  │ FreeManager.App │ ◄─── │  FM* DTOs       │                          │
// │  └─────────────────┘      └─────────────────┘                          │
// └─────────────────────────────────────────────────────────────────────────┘
//
// NOT part of stock FreeCRM - this is FreeManager-specific functionality.
// ============================================================================

/// <summary>
/// FreeManager DataAccess interface - defines all business logic methods.
/// </summary>
public partial interface IDataAccess
{
    // Projects
    Task<List<DataObjects.FMProjectInfo>> FM_GetProjects(DataObjects.User CurrentUser);
    Task<DataObjects.FMProjectInfo?> FM_GetProject(Guid projectId, DataObjects.User CurrentUser);
    Task<DataObjects.FMProjectInfo> FM_CreateProject(DataObjects.FMCreateProjectRequest request, DataObjects.User CurrentUser);
    Task<DataObjects.BooleanResponse> FM_UpdateProject(DataObjects.FMUpdateProjectRequest request, DataObjects.User CurrentUser);
    Task<DataObjects.BooleanResponse> FM_DeleteProject(Guid projectId, DataObjects.User CurrentUser);
    Task<byte[]?> FM_ExportProjectAsZip(Guid projectId, DataObjects.User CurrentUser);

    // Files
    Task<List<DataObjects.FMAppFileInfo>> FM_GetAppFiles(Guid projectId, DataObjects.User CurrentUser);
    Task<DataObjects.FMAppFileContent?> FM_GetAppFile(Guid fileId, DataObjects.User CurrentUser);
    Task<DataObjects.FMSaveFileResponse> FM_SaveAppFile(DataObjects.FMSaveFileRequest request, DataObjects.User CurrentUser);
    Task<DataObjects.FMAppFileInfo?> FM_CreateAppFile(DataObjects.FMCreateFileRequest request, DataObjects.User CurrentUser);
    Task<DataObjects.BooleanResponse> FM_DeleteAppFile(Guid fileId, DataObjects.User CurrentUser);
    Task<List<DataObjects.FMFileVersionInfo>> FM_GetFileVersions(Guid fileId, DataObjects.User CurrentUser);
    Task<DataObjects.FMAppFileContent?> FM_GetFileVersion(Guid versionId, DataObjects.User CurrentUser);

    // Builds
    Task<DataObjects.FMBuildInfo> FM_StartBuild(DataObjects.FMStartBuildRequest request, DataObjects.User CurrentUser);
    Task<List<DataObjects.FMBuildInfo>> FM_GetBuilds(Guid projectId, DataObjects.User CurrentUser);
    Task<DataObjects.FMBuildDetailInfo?> FM_GetBuild(Guid buildId, DataObjects.User CurrentUser);
}

/// <summary>
/// FreeManager DataAccess implementation - coordinator for partial classes.
/// See partial files listed above for actual implementations.
/// </summary>
public partial class DataAccess
{
    // All method implementations are in partial files.
}

#endregion
