namespace FreeA11yChecker;

// Use this file as a place to put any application-specific data objects.

public partial class DataObjects
{
    public partial class SignalRUpdateType
    {
        public const string ScanQueued = "ScanQueued";
        public const string ScanStarted = "ScanStarted";
        public const string ScanProgress = "ScanProgress";
        public const string ScanComplete = "ScanComplete";
        public const string ScanFailed = "ScanFailed";
        public const string ScanLog = "ScanLog";
    }

    public partial class BlazorDataModelLoader
    {
        /// <summary>
        /// Site summaries loaded during GetBlazorDataModelApp for the current tenant.
        /// </summary>
        public List<DataObjects.Site> SiteList { get; set; } = new List<DataObjects.Site>();
    }

    public partial class User
    {
        //public string? MyCustomUserProperty { get; set; }
    }

    //public class YourClass
    //{
    //    public string? YourProperty { get; set; }
    //}
}