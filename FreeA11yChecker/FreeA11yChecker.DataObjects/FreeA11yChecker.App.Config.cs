namespace FreeA11yChecker;

public partial interface IConfigurationHelper
{
    int ScanSettleDelayMs { get; }
    int ScanTimeoutMs { get; }
    int ScanMaxConcurrency { get; }
    string ScanWcagLevel { get; }
    string ScanEncryptionKey { get; }
    bool ScanHeadless { get; }
    string ScanUserAgent { get; }
}

public partial class ConfigurationHelper : IConfigurationHelper
{
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

    public int ScanMaxConcurrency {
        get {
            return _loader.ScanMaxConcurrency;
        }
    }

    public string ScanWcagLevel {
        get {
            return _loader.ScanWcagLevel;
        }
    }

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

    public string ScanUserAgent {
        get {
            return _loader.ScanUserAgent;
        }
    }
}

public partial class ConfigurationHelperLoader
{
    public int ScanSettleDelayMs { get; set; } = 5000;
    public int ScanTimeoutMs { get; set; } = 30000;
    public int ScanMaxConcurrency { get; set; } = 5;
    public string ScanWcagLevel { get; set; } = "wcag21aa";
    public string ScanEncryptionKey { get; set; } = "";
    public bool ScanHeadless { get; set; } = true;
    public string ScanUserAgent { get; set; } = "";
}
