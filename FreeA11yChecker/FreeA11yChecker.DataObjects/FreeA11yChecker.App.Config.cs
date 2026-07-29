namespace FreeA11yChecker;

public partial interface IConfigurationHelper
{
    string ScanEncryptionKey { get; }
    bool ScanHeadless { get; }
    int ScanMaxConcurrency { get; }
    int ScanSettleDelayMs { get; }
    int ScanTimeoutMs { get; }
    string ScanUserAgent { get; }
    string ScanWcagLevel { get; }
}

public partial class ConfigurationHelper : IConfigurationHelper
{
    public string ScanEncryptionKey {
        get {
            return _loader.ScanEncryptionKey;
        }
    }

    public bool ScanHeadless {
        get {
            return _loader.ScanHeadless;
        }
    }

    public int ScanMaxConcurrency {
        get {
            return _loader.ScanMaxConcurrency;
        }
    }
    public int ScanSettleDelayMs {
        get {
            return _loader.ScanSettleDelayMs;
        }
    }

    public int ScanTimeoutMs {
        get {
            return _loader.ScanTimeoutMs;
        }
    }

    public string ScanUserAgent {
        get {
            return _loader.ScanUserAgent;
        }
    }

    public string ScanWcagLevel {
        get {
            return _loader.ScanWcagLevel;
        }
    }
}

public partial class ConfigurationHelperLoader
{
    public string ScanEncryptionKey { get; set; } = "";
    public bool ScanHeadless { get; set; } = true;
    public int ScanMaxConcurrency { get; set; } = 5;
    public int ScanSettleDelayMs { get; set; } = 5000;
    public int ScanTimeoutMs { get; set; } = 30000;
    public string ScanUserAgent { get; set; } = "";
    public string ScanWcagLevel { get; set; } = "wcag21aa";
}
