namespace FreeA11yChecker.Client;

public partial class BlazorDataModel
{
    private List<string> _MyValues = new List<string>();
    private List<DataObjects.Site> _SiteList = new List<DataObjects.Site>();
    private Dictionary<Guid, DataObjects.ScanProgress> _ScanStatuses = new Dictionary<Guid, DataObjects.ScanProgress>();

    private bool HaveDeletedRecordsApp {
        get {
            bool output = false;

            // Check your app-specific deleted records here.
            //if (DeletedRecordCounts.MyValue > 0 ) {
            //    output = true;
            //}

            return output;
        }
    }

    public bool MyCustomDataModelMethod()
    {
        return true;
    }

    /// <summary>
    /// An example of implementing a custom property in your data model.
    /// </summary>
    public List<string> MyValues {
        get {
            return _MyValues;
        }

        set {
            if (!ObjectsAreEqual(_MyValues, value)) {
                _MyValues = value;
                _ModelUpdated = DateTime.UtcNow;
                NotifyDataChanged();
            }
        }
    }

    /// <summary>
    /// The list of configured sites for the current tenant.
    /// Loaded by GetBlazorDataModelApp and used by the dashboard and scan pages.
    /// </summary>
    public List<DataObjects.Site> SiteList {
        get {
            return _SiteList;
        }

        set {
            if (!ObjectsAreEqual(_SiteList, value)) {
                _SiteList = value;
                _ModelUpdated = DateTime.UtcNow;
                NotifyDataChanged();
            }
        }
    }

    /// <summary>
    /// Tracks active scan progress by SiteId. Updated by the SignalR handler in Helpers.App.cs
    /// and read by the TriggerScan and ScanDashboard pages.
    /// </summary>
    public Dictionary<Guid, DataObjects.ScanProgress> ScanStatuses {
        get {
            return _ScanStatuses;
        }

        set {
            if (!ObjectsAreEqual(_ScanStatuses, value)) {
                _ScanStatuses = value;
                _ModelUpdated = DateTime.UtcNow;
                NotifyDataChanged();
            }
        }
    }

    /// <summary>
    /// Set this option to true if you wish to make sure all Blazor plugins are precompiled during page load.
    /// If this is set to false then any components that have not yet been cached will take some time to load
    /// in the interface while they are being compiled and cached.
    /// </summary>
    public bool PrecompileBlazorPlugins {
        get {
            return false;
        }
    }
}
