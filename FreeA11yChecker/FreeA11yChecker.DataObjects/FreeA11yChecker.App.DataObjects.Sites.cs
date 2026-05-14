namespace FreeA11yChecker;

public partial class DataObjects
{
    public class Site : ActionResponseObject
    {
        public Guid SiteId { get; set; }
        public Guid TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = string.Empty;
        public bool IsFreeCRMApp { get; set; }
        public string ScanScheduleCron { get; set; } = string.Empty;
        public int MaxConcurrency { get; set; }
        public bool Enabled { get; set; }
        public bool PublicVisible { get; set; }
        public Guid? LastScanRunId { get; set; }
        public DateTime? LastScanAt { get; set; }
        public string? LastScanStatus { get; set; }
        public int LastViolationCount { get; set; }
        public int LastCriticalCount { get; set; }
        public DateTime Added { get; set; }
        public string? AddedBy { get; set; }
        public DateTime LastModified { get; set; }
        public string? LastModifiedBy { get; set; }
        public bool Deleted { get; set; }
        public DateTime? DeletedAt { get; set; }
        public List<SitePage> Pages { get; set; } = new();
        public List<SiteCredential> Credentials { get; set; } = new();
    }

    public class SitePage : ActionResponseObject
    {
        public Guid SitePageId { get; set; }
        public Guid SiteId { get; set; }
        public string Path { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public bool RequiresAuth { get; set; }
        public bool Enabled { get; set; }
        public bool IncludeInScan { get; set; }
        public int SortOrder { get; set; }
    }

    public class SiteCredential : ActionResponseObject
    {
        public Guid SiteCredentialId { get; set; }
        public Guid SiteId { get; set; }
        public Guid TenantId { get; set; }
        public string Label { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string PasswordEncrypted { get; set; } = string.Empty;
        public string AuthType { get; set; } = string.Empty;
        public string TenantCode { get; set; } = string.Empty;
        public string LoginUrl { get; set; } = string.Empty;
        public string UsernameSelector { get; set; } = string.Empty;
        public string PasswordSelector { get; set; } = string.Empty;
        public string SubmitSelector { get; set; } = string.Empty;
    }

    public class SiteChildFilter
    {
        public List<Guid> Ids { get; set; } = new();
        public Guid SiteId { get; set; }
    }

    public class DiscoverLinksFilter
    {
        public Guid SiteId { get; set; }
        public string Url { get; set; } = string.Empty;
        public List<string> AdditionalUrls { get; set; } = new();
    }

    public class DiscoveredLink
    {
        public string Url { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public bool IsInternal { get; set; }
    }

    public class DiscoverLinksResult
    {
        public List<DiscoveredLink> InternalLinks { get; set; } = new();
        public List<DiscoveredLink> ExternalLinks { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }
}
