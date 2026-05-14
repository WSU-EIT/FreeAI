/*
    Purpose: Parses a raw repository URL into platform, owner, repo, and base API URL.
    Rules:
      - github.com      → GitHub    (api.github.com)
      - gitlab.com      → GitLab    (gitlab.com/api/v4)
      - bitbucket.org   → Bitbucket (api.bitbucket.org)
      - anything else   → requires platformOverride, otherwise throws
      - GitHubEnterprise → {host}/api/v3
      - GitLab self-hosted → {host}/api/v4
      - Gitea           → {host}/api/v1
    Security: BaseApiUrl is credential-free and safe to display or log.
*/
namespace FreeBlazorExtended.GitRepoBrowser;

public static class GitPlatformDetector
{
    /// <summary>
    /// Parses <paramref name="rawUrl"/> and returns the resolved platform details.
    /// </summary>
    /// <param name="rawUrl">A full repository URL (e.g. https://github.com/owner/repo).</param>
    /// <param name="platformOverride">
    /// Force a specific platform. Required for self-hosted servers that cannot be identified by hostname alone.
    /// </param>
    /// <exception cref="ArgumentException">Thrown when the URL is invalid or the platform cannot be determined.</exception>
    public static (GitPlatform Platform, string Owner, string Repo, string BaseApiUrl) Detect(
        string rawUrl,
        GitPlatform? platformOverride = null)
    {
        if (string.IsNullOrWhiteSpace(rawUrl))
            throw new ArgumentException("Repository URL cannot be empty.", nameof(rawUrl));

        if (!Uri.TryCreate(rawUrl.Trim(), UriKind.Absolute, out var uri))
            throw new ArgumentException($"'{rawUrl}' is not a valid absolute URL.", nameof(rawUrl));

        var host = uri.Host.ToLowerInvariant();

        // Parse owner + repo from path segments (strip .git suffix)
        var segments = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 2)
            throw new ArgumentException(
                "URL must contain at least an owner and repository segment (e.g. /owner/repo).",
                nameof(rawUrl));

        var owner = segments[0];
        var repo  = segments[1].EndsWith(".git", StringComparison.OrdinalIgnoreCase)
                    ? segments[1][..^4]
                    : segments[1];

        GitPlatform platform;
        string baseApiUrl;

        if (platformOverride.HasValue && platformOverride.Value != GitPlatform.Unknown)
        {
            platform   = platformOverride.Value;
            baseApiUrl = BuildBaseApiUrl(platform, host, uri.Scheme);
        }
        else
        {
            platform = host switch
            {
                "github.com"    => GitPlatform.GitHub,
                "gitlab.com"    => GitPlatform.GitLab,
                "bitbucket.org" => GitPlatform.Bitbucket,
                "dev.azure.com" => GitPlatform.AzureDevOps,
                var h when h.EndsWith(".visualstudio.com") => GitPlatform.AzureDevOps,
                _               => GitPlatform.Unknown
            };

            if (platform == GitPlatform.Unknown)
                throw new ArgumentException(
                    $"Cannot determine platform from host '{host}'. " +
                    "Select a platform override (GitHubEnterprise, GitLab, Gitea, etc.) for self-hosted servers.",
                    nameof(rawUrl));

            baseApiUrl = BuildBaseApiUrl(platform, host, uri.Scheme);
        }

        return (platform, owner, repo, baseApiUrl);
    }

    private static string BuildBaseApiUrl(GitPlatform platform, string host, string scheme) =>
        platform switch
        {
            // Cloud platforms — canonical well-known API roots
            GitPlatform.GitHub    => "https://api.github.com",
            GitPlatform.GitLab    => host == "gitlab.com"
                                         ? "https://gitlab.com/api/v4"
                                         : $"{scheme}://{host}/api/v4",
            GitPlatform.Bitbucket => "https://api.bitbucket.org",

            // Self-hosted / enterprise — version prefix baked into BaseApiUrl
            GitPlatform.GitHubEnterprise => $"https://{host}/api/v3",
            GitPlatform.Gitea            => $"{scheme}://{host}/api/v1",
            GitPlatform.AzureDevOps      => $"https://dev.azure.com",

            _ => throw new ArgumentException($"Cannot build API URL for unsupported platform '{platform}'.")
        };
}
