namespace FreeA11yChecker;

public partial class DataObjects
{
    public class Site : ActionResponseObject
    {
        public DateTime Added { get; set; }
        public string? AddedBy { get; set; }
        public string BaseUrl { get; set; } = string.Empty;
        public List<SiteCredential> Credentials { get; set; } = new();
        public bool Deleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public bool Enabled { get; set; }
        public bool IsFreeCRMApp { get; set; }
        public int LastCriticalCount { get; set; }
        public DateTime LastModified { get; set; }
        public string? LastModifiedBy { get; set; }
        public DateTime? LastScanAt { get; set; }
        public Guid? LastScanRunId { get; set; }
        public string? LastScanStatus { get; set; }
        public int LastViolationCount { get; set; }
        public int MaxConcurrency { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<SitePage> Pages { get; set; } = new();
        public bool PublicVisible { get; set; }
        public string ScanScheduleCron { get; set; } = string.Empty;
        public Guid SiteId { get; set; }
        public Guid TenantId { get; set; }
    }

    public class SitePage : ActionResponseObject
    {
        public bool Enabled { get; set; }
        public bool IncludeInScan { get; set; }
        public string Path { get; set; } = string.Empty;
        public bool RequiresAuth { get; set; }
        public Guid SiteId { get; set; }
        public Guid SitePageId { get; set; }
        public int SortOrder { get; set; }
        public string Title { get; set; } = string.Empty;
    }

    public class SiteCredential : ActionResponseObject
    {
        public string AuthType { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string LoginUrl { get; set; } = string.Empty;
        public string PasswordEncrypted { get; set; } = string.Empty;
        public string PasswordSelector { get; set; } = string.Empty;
        public Guid SiteCredentialId { get; set; }
        public Guid SiteId { get; set; }
        public string SubmitSelector { get; set; } = string.Empty;
        public string TenantCode { get; set; } = string.Empty;
        public Guid TenantId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string UsernameSelector { get; set; } = string.Empty;
    }

    public class SiteChildFilter
    {
        public List<Guid> Ids { get; set; } = new();
        public Guid SiteId { get; set; }
    }

    public class DiscoverLinksFilter
    {
        public List<string> AdditionalUrls { get; set; } = new();
        public Guid SiteId { get; set; }
        public string Url { get; set; } = string.Empty;
    }

    public class DiscoveredLink
    {
        public bool IsInternal { get; set; }
        public string Path { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }

    public class DiscoverLinksResult
    {
        public string? ErrorMessage { get; set; }
        public List<DiscoveredLink> ExternalLinks { get; set; } = new();
        public List<DiscoveredLink> InternalLinks { get; set; } = new();
    }
}
