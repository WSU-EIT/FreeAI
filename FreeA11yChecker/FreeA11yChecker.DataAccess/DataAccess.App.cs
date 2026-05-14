namespace FreeA11yChecker;

// Use this file as a place to put any application-specific data access methods.

public partial interface IDataAccess
{
    Task<DataObjects.BooleanResponse> ProcessBackgroundTasksApp(Guid TenantId, long Iteration);
    DataObjects.BooleanResponse YourMethod();
}

public partial class DataAccess
{
    /// <summary>
    /// Use this area to add your custom language tags for your app, or to override built-in language tags (eg: AppTitle).
    /// </summary>
    private Dictionary<string, string> AppLanguage {
        get {
            return new Dictionary<string, string>
            {
                //{ "AppTitle", "My Custom App Title" },
                //{ "YourLanguageItem", "Your Language Item" },
            };
        }
    }

    private void DataAccessAppInit()
    {
        // Add any app-specific initialization logic here.
    }

    /// <summary>
    /// Use this method to delete any pending records for your app-specific tables.
    /// Return true if everything was deleted successfully.
    /// Otherwise, return false and include any error messages in the Messages property.
    /// </summary>
    private async Task<DataObjects.BooleanResponse> DeleteAllPendingDeletedRecordsApp(Guid TenantId, DateTime OlderThan)
    {
        await Task.Delay(0); // Simulate a delay since this method has to be async. This can be removed once you implement your await logic.

        var output = new DataObjects.BooleanResponse();

        output.Result = true;

        return output;
    }

    /// <summary>
    /// Use this method to immediately delete any app-specific records.
    /// Return true if the item was deleted successfully.
    /// Otherwise, return false and include any error messages in the Messages property.
    /// </summary>
    private async Task<DataObjects.BooleanResponse> DeleteRecordImmediatelyApp(string? Type, Guid RecordId, DataObjects.User CurrentUser)
    {
        await Task.Delay(0); // Simulate a delay since this method has to be async. This can be removed once you implement your await logic.

        var output = new DataObjects.BooleanResponse();

        if (!String.IsNullOrWhiteSpace(Type)) {
            switch (Type.ToLower()) {
                case "mytype":
                    //output = await DeleteMyItemType(RecordId, CurrentUser);
                    break;
            }
        }

        // Remove this line once you implement your logic.
        output.Result = true;

        return output;
    }

    /// <summary>
    /// Called from various delete methods to delete any app-specific records before continuing with the delete process.
    /// </summary>
    /// <param name="Rec">The EF record object.</param>
    /// <param name="CurrentUser">The User object for the current user, if loaded.</param>
    /// <returns>A BooleanResponse object.</returns>
    private async Task<DataObjects.BooleanResponse> DeleteRecordsApp(object Rec, DataObjects.User? CurrentUser = null)
    {
        await Task.Delay(0); // Simulate a delay since this method has to be async. This can be removed once you implement your await logic.

        var output = new DataObjects.BooleanResponse();

        try {

            if (Rec is EFModels.EFModels.Department) {
                EFModels.EFModels.Department? rec = Rec as EFModels.EFModels.Department;
                if (rec != null) {
                    // Remove any related records.
                }
            }

            if (Rec is EFModels.EFModels.DepartmentGroup) {
                EFModels.EFModels.DepartmentGroup? rec = Rec as EFModels.EFModels.DepartmentGroup;
                if (rec != null) {
                    // Remove any related records.
                }
            }

            // Cascade delete scans and related data when a site is deleted.
            if (Rec is EFModels.EFModels.Site) {
                EFModels.EFModels.Site? rec = Rec as EFModels.EFModels.Site;
                if (rec != null) {
                    // Get all scan runs for this site.
                    List<Guid> scanRunIds = await data.ScanRuns
                        .Where(x => x.SiteId == rec.SiteId)
                        .Select(x => x.ScanRunId)
                        .ToListAsync();

                    if (scanRunIds.Any()) {
                        // Delete violations via page scan results.
                        List<Guid> pageResultIds = await data.PageScanResults
                            .Where(x => scanRunIds.Contains(x.ScanRunId))
                            .Select(x => x.PageScanResultId)
                            .ToListAsync();

                        if (pageResultIds.Any()) {
                            data.A11yViolations.RemoveRange(
                                data.A11yViolations.Where(x => pageResultIds.Contains(x.PageScanResultId)));
                            await data.SaveChangesAsync();
                        }

                        // Delete page scan results.
                        data.PageScanResults.RemoveRange(
                            data.PageScanResults.Where(x => scanRunIds.Contains(x.ScanRunId)));
                        await data.SaveChangesAsync();

                        // Delete scan runs.
                        data.ScanRuns.RemoveRange(
                            data.ScanRuns.Where(x => x.SiteId == rec.SiteId));
                        await data.SaveChangesAsync();
                    }

                    // Delete manual check results.
                    data.ManualCheckResults.RemoveRange(
                        data.ManualCheckResults.Where(x => x.SiteId == rec.SiteId));
                    await data.SaveChangesAsync();
                }
            }

            if (Rec is EFModels.EFModels.Tenant) {
                EFModels.EFModels.Tenant? rec = Rec as EFModels.EFModels.Tenant;
                if (rec != null) {
                    // Implement your app-specific tenant deletion logic here to remove records from tables that are specific to your app.
                    // Example:
                    // data.MyTable.RemoveRange(data.MyTable.Where(x => x.TenantId == TenantId));
                    // await data.SaveChangesAsync();
                }
            }

            if (Rec is EFModels.EFModels.User) {
                var rec = Rec as EFModels.EFModels.User;
                if (rec != null) {
                    // Remove any related records.
                }
            }

            if (Rec is EFModels.EFModels.UserGroup) {
                var rec = Rec as EFModels.EFModels.UserGroup;
                if (rec != null) {
                    // Remove any related records.
                }
            }
        } catch (Exception ex) {
            output.Messages.Add("An Error Occurred in DeleteUserApp");
            output.Messages.AddRange(RecurseException(ex));
        }

        output.Result = output.Messages.Count == 0;

        return output;
    }

    /// <summary>
    /// Called by the GetApplicationSettings method to load any app-specific settings into the ApplicationSettings object.
    /// </summary>
    /// <param name="settings">The ApplicationSettings object.</param>
    /// <returns>The object with any updates for your app.</returns>
    private DataObjects.ApplicationSettings GetApplicationSettingsApp(DataObjects.ApplicationSettings settings)
    {
        // Add any app-specific settings here.

        return settings;
    }

    /// <summary>
    /// Called by the AppSettings property get load any app-specific settings into the ApplicationSettingsUpdate object.
    /// </summary>
    /// <param name="settings">The current ApplicationSettingsUpdate object.</param>
    /// <returns>The object with any updates from your app.</returns>
    private DataObjects.ApplicationSettingsUpdate GetApplicationSettingsUpdateApp(DataObjects.ApplicationSettingsUpdate settings)
    {
        // Any any app-specific settings for the ApplicationSettingsUpdate object here.
        return settings;
    }

    /// <summary>
    /// Called by the main GetBlazorDataModel method to load any app-specific data into the Blazor data model.
    /// Loads site summaries for the current user's tenant.
    /// </summary>
    /// <param name="blazorDataModelLoader">The BlazorDataModelLoader object being populated.</param>
    /// <param name="CurrentUser">The User object for the current user, if loaded.</param>
    /// <returns>The updated BlazorDataModelLoader.</returns>
    private async Task<DataObjects.BlazorDataModelLoader> GetBlazorDataModelApp(DataObjects.BlazorDataModelLoader blazorDataModelLoader, DataObjects.User? CurrentUser = null)
    {
        DataObjects.BlazorDataModelLoader output = blazorDataModelLoader;

        try {
            if (CurrentUser != null && CurrentUser.TenantId != Guid.Empty) {
                // Load site list for the current tenant so the dashboard can show site summaries.
                List<DataObjects.Site> sites = await GetSites(null, CurrentUser.TenantId, CurrentUser);
                output.SiteList = sites;
            }
        } catch (Exception ex) {
            Console.WriteLine("Error in GetBlazorDataModelApp: " + RecurseExceptionAsString(ex));
        }

        return output;
    }

    /// <summary>
    /// This method is called to add any app-specific deleted record counts to the output.
    /// </summary>
    private async Task<DataObjects.DeletedRecordCounts> GetDeletedRecordCountsApp(Guid TenantId, DataObjects.DeletedRecordCounts deletedRecordCounts)
    {
        await Task.Delay(0); // Simulate a delay since this method has to be async. This can be removed once you implement your await logic.

        var output = deletedRecordCounts;

        // Do any lookups for your app-specific deleted record counts here and add them to the output.
        // output.MyCount = await data.MyTable.CountAsync(x => x.TenantId == TenantId && x.Deleted == true);

        return output;
    }

    /// <summary>
    /// This method is called to add any app-specific filter columns to the filter output.
    /// </summary>
    /// <param name="Type">The filter type (eg: Users, Invoices, etc.)</param>
    /// <param name="Position">The position in the column orders (see calling code for details.)</param>
    /// <param name="CurrentUser">The current user object, if one exists</param>
    /// <returns>A list of FilterColumn objects</returns>
    private List<DataObjects.FilterColumn> GetFilterColumnsApp(string Type, string Position, DataObjects.Language Language, DataObjects.User? CurrentUser = null)
    {
        var output = new List<DataObjects.FilterColumn>();
        // Add any app-specific filter columns here.
        // Example:
        // if (Type.ToLower() == "users" && Position.ToLower() == "username") {
        //     output.Add(new DataObjects.FilterColumn { Name = "MyColumn", Type = "string", Title = "My Column", Placeholder = "My Column", Width = 150 });
        // }
        return output;
    }

    /// <summary>
    /// This method is called to add any app-specific deleted records to the output.
    /// </summary>
    private async Task<DataObjects.DeletedRecords> GetDeletedRecordsApp(Guid TenantId, DataObjects.DeletedRecords deletedRecords)
    {
        await Task.Delay(0); // Simulate a delay since this method has to be async. This can be removed once you implement your await logic.

        var output = deletedRecords;

        // Do any lookups for your app-specific deleted records here and add them to the output.

        return output;
    }

    /// <summary>
    /// This is called by various Get methods to map any app-specific fields from the EF model to the data object.
    /// </summary>
    /// <param name="Rec">The EF record object.</param>
    /// <param name="DataObject">The DataObjects object being updated.</param>
    /// <param name="CurrentUser">The current user object, if one exists.</param>
    private void GetDataApp(object Rec, object DataObject, DataObjects.User? CurrentUser = null)
    {
        try {

            if (Rec is EFModels.EFModels.Department && DataObject is DataObjects.Department) {
                var rec = Rec as EFModels.EFModels.Department;
                var department = DataObject as DataObjects.Department;

                if (rec != null && department != null) {
                    //department.Property = rec.Property;
                }

                return;
            }

            if (Rec is EFModels.EFModels.DepartmentGroup && DataObject is DataObjects.DepartmentGroup) {
                var rec = Rec as EFModels.EFModels.DepartmentGroup;
                var departmentGroup = DataObject as DataObjects.DepartmentGroup;

                if (rec != null && departmentGroup != null) {
                    //departmentGroup.Property = rec.Property;
                }

                return;
            }

            if (Rec is EFModels.EFModels.User && DataObject is DataObjects.ActiveUser) {
                var rec = Rec as EFModels.EFModels.User;
                var activeUser = DataObject as DataObjects.ActiveUser;

                if (rec != null && activeUser != null) {
                    //activeUser.Property = rec.Property;
                }

                return;
            }

            if (Rec is EFModels.EFModels.User && DataObject is DataObjects.User) {
                var rec = Rec as EFModels.EFModels.User;
                var user = DataObject as DataObjects.User;

                if (rec != null && user != null) {
                    //user.Property = rec.Property;
                }

                return;
            }

            if (Rec is EFModels.EFModels.User && DataObject is DataObjects.UserAccount) {
                var rec = Rec as EFModels.EFModels.User;
                var userAccount = DataObject as DataObjects.UserAccount;

                if (rec != null && userAccount != null) {
                    //userAccount.Property = rec.Property;
                }

                return;
            }

            if (Rec is EFModels.EFModels.User && DataObject is DataObjects.UserListing) {
                var rec = Rec as EFModels.EFModels.User;
                var userListing = DataObject as DataObjects.UserListing;

                if (rec != null && userListing != null) {
                    //userListing.Property = rec.Property;
                }

                return;
            }

            if (Rec is EFModels.EFModels.UserGroup && DataObject is DataObjects.UserGroup) {
                var rec = Rec as EFModels.EFModels.UserGroup;
                var userGroup = DataObject as DataObjects.UserGroup;

                if (rec != null && userGroup != null) {
                    //userGroup.Property = rec.Property;
                }

                return;
            }
        } catch { }
    }

    /// <summary>
    /// Called by the background processor to process any app-specific background tasks.
    /// Prunes old scan runs every 5 minutes (Iteration modulo 30, assuming 10-second intervals).
    /// </summary>
    /// <param name="TenantId">The tenant id being processed.</param>
    /// <param name="Iteration">The iteration counter from the background processor.</param>
    /// <returns>A BooleanResponse object.</returns>
    public async Task<DataObjects.BooleanResponse> ProcessBackgroundTasksApp(Guid TenantId, long Iteration)
    {
        DataObjects.BooleanResponse output = new DataObjects.BooleanResponse();
        output.Result = true;

        try {
            // Every 5 minutes (modulo 30 at 10-second intervals), prune old scans.
            if (Iteration % 30 == 0) {
                List<DataObjects.Site> sites = await GetSites(null, TenantId);
                foreach (DataObjects.Site site in sites) {
                    // Keep the 20 most recent scan runs per site.
                    int pruned = await PruneOldScans(site.SiteId, 20);
                    if (pruned > 0) {
                        Console.WriteLine("Pruned " + pruned + " old scan(s) for site " + site.Name);
                    }
                }
            }
        } catch (Exception ex) {
            output.Messages.Add("Error in ProcessBackgroundTasksApp");
            output.Messages.AddRange(RecurseException(ex));
            output.Result = false;
        }

        return output;
    }

    /// <summary>
    /// Called by the main SaveApplicationSettings method to save any app-specific settings from the ApplicationSettings object.
    /// </summary>
    /// <param name="settings">The ApplicationSettings object.</param>
    /// <param name="CurrentUser">The User object for the current user.</param>
    /// <returns>The updated ApplicationSettings object.</returns>
    private async Task<DataObjects.ApplicationSettings> SaveApplicationSettingsApp(DataObjects.ApplicationSettings settings, DataObjects.User CurrentUser)
    {
        await Task.Delay(0); // Simulate a delay since this method has to be async. This can be removed once you implement your await logic.

        // Add any app-specific settings here.
        return settings;
    }

    /// <summary>
    /// This is called by various Save methods to map any app-specific fields from the data object to the EF model object.
    /// </summary>
    /// <param name="Rec">The EF record object being updated.</param>
    /// <param name="DataObject">The DataObjects object.</param>
    /// <param name="CurrentUser">The current user object, if one exists.</param>
    private void SaveDataApp(object Rec, object DataObject, DataObjects.User? CurrentUser = null)
    {
        try {

            if (Rec is EFModels.EFModels.Department && DataObject is DataObjects.Department) {
                var rec = Rec as EFModels.EFModels.Department;
                var department = DataObject as DataObjects.Department;

                if (rec != null && department != null) {
                    //rec.Property = department.Property;
                }

                return;
            }

            if (Rec is EFModels.EFModels.DepartmentGroup && DataObject is DataObjects.DepartmentGroup) {
                var rec = Rec as EFModels.EFModels.DepartmentGroup;
                var departmentGroup = DataObject as DataObjects.DepartmentGroup;

                if (rec != null && departmentGroup != null) {
                    //rec.Property = departmentGroup.Property;
                }

                return;
            }

            if (Rec is EFModels.EFModels.FileStorage && DataObject is DataObjects.FileStorage) {
                var rec = Rec as EFModels.EFModels.FileStorage;
                var fileStorage = DataObject as DataObjects.FileStorage;

                if (rec != null && fileStorage != null) {
                    //rec.Property = fileStorage.Property;
                }
            }

            if (Rec is EFModels.EFModels.User && DataObject is DataObjects.User) {
                var rec = Rec as EFModels.EFModels.User;
                var user = DataObject as DataObjects.User;

                if (rec != null && user != null) {
                    //rec.Property = user.Property;
                }

                return;
            }

            if (Rec is EFModels.EFModels.UserGroup && DataObject is DataObjects.UserGroup) {
                var rec = Rec as EFModels.EFModels.UserGroup;
                var userGroup = DataObject as DataObjects.UserGroup;

                if (rec != null && userGroup != null) {
                    //rec.Property = userGroup.Property;
                }

                return;
            }
        } catch { }
    }

    /// <summary>
    /// Called by the main method to get filtered users to apply any app-specific sorting.
    /// </summary>
    /// <param name="recs">The current records.</param>
    /// <param name="SortBy">The sort field.</param>
    /// <param name="Ascending">The boolean that indicates if we are sorting ascending.</param>
    /// <returns></returns>
    private IQueryable<EFModels.EFModels.User>? SortUsersApp(IQueryable<EFModels.EFModels.User>? recs, string SortBy, bool Ascending)
    {
        if (recs != null) {

            switch (SortBy.ToUpper()) {
                //case "PROPERTY":
                //    recs = Ascending
                //        ? recs.OrderBy(x => x.PROPERTY).ThenBy(x => x.FirstName).ThenBy(x => x.LastName)
                //        : recs.OrderBy(x => x.PROPERTY).ThenBy(x => x.FirstName).ThenBy(x => x.LastName);
                //    break;

                default:
                    break;
            }
        }

        return recs;
    }

    /// <summary>
    /// This method is called to undelete any app-specific records.
    /// </summary>
    private async Task<DataObjects.BooleanResponse> UndeleteRecordApp(string? Type, Guid RecordId, DataObjects.User CurrentUser)
    {
        await Task.Delay(0); // Simulate a delay since this method has to be async. This can be removed once you implement your await logic.

        var output = new DataObjects.BooleanResponse();

        try {
            if (!String.IsNullOrWhiteSpace(Type)) {
                object? obj = null;
                bool sendSignalRUpdate = false;

                switch (Type.ToLower()) {
                    case "this":
                        // Add code to undelete the record of type "this" here.
                        output.Result = true;
                        sendSignalRUpdate = true;
                        break;

                    default:
                        output.Messages.Add("Invalid Delete Record Type '" + Type + "'");
                        break;
                }

                if (sendSignalRUpdate) {
                    await SignalRUpdate(new DataObjects.SignalRUpdate {
                        TenantId = CurrentUser.TenantId,
                        ItemId = RecordId,
                        UpdateType = DataObjects.SignalRUpdateType.Undelete,
                        Message = Type,
                        Object = obj,
                        UserId = CurrentUserId(CurrentUser),
                    });
                }
            }
        } catch (Exception ex) {
            output.Messages.Add("Error Undeleting '" + Type + "' " + RecordId.ToString() + " - " + RecurseExceptionAsString(ex));
        }

        // Do any app-specific undelete logic here. See the main UndeleteRecord method for an example.
        output.Result = true; // Remove this line once you implement your logic.

        return output;
    }

    public DataObjects.BooleanResponse YourMethod()
    {
        return new DataObjects.BooleanResponse { Result = true, Messages = new List<string> { "Your Messages" } };
    }
}