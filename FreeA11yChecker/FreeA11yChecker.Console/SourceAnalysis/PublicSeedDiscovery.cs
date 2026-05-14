using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml;

namespace FreeA11yChecker.Console.SourceAnalysis;

public static class PublicSeedDiscovery
{
    private const long MaxResponseBytes = 5L * 1024 * 1024;
    private const string UserAgent = "FreeA11yChecker/1.0 (compliance scanner)";
    private const string SitemapNamespace = "http://www.sitemaps.org/schemas/sitemap/0.9";

    public static async Task<List<string>> DiscoverPathsAsync(string baseUrl, HttpClient? http = null)
    {
        var results = new HashSet<string>(StringComparer.Ordinal);

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return results.ToList();
        }

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri))
        {
            return results.ToList();
        }

        var ownsHttp = http is null;
        var client = http ?? new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

        try
        {
            if (!client.DefaultRequestHeaders.UserAgent.Any())
            {
                try
                {
                    client.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
                }
                catch
                {
                    // ignore header set failure
                }
            }

            var origin = new Uri(baseUri.GetLeftPart(UriPartial.Authority));
            var sitemapUrls = new List<Uri>();

            // 1. Fetch robots.txt and extract Sitemap: lines
            var robotsUri = new Uri(origin, "/robots.txt");
            var robotsText = await TryGetStringAsync(client, robotsUri).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(robotsText))
            {
                foreach (var line in robotsText.Split('\n'))
                {
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith("Sitemap:", StringComparison.OrdinalIgnoreCase))
                    {
                        var value = trimmed.Substring("Sitemap:".Length).Trim();
                        if (Uri.TryCreate(value, UriKind.Absolute, out var sitemapUri))
                        {
                            sitemapUrls.Add(sitemapUri);
                        }
                    }
                }
            }

            // 2. Always try the default /sitemap.xml too
            sitemapUrls.Add(new Uri(origin, "/sitemap.xml"));

            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var sitemapUrl in sitemapUrls)
            {
                await ProcessSitemapAsync(client, sitemapUrl, baseUri, results, visited).ConfigureAwait(false);
            }
        }
        catch
        {
            // swallow – return what we have so far
        }
        finally
        {
            if (ownsHttp)
            {
                try { client.Dispose(); } catch { /* ignore */ }
            }
        }

        return results.ToList();
    }

    private static async Task ProcessSitemapAsync(
        HttpClient client,
        Uri sitemapUrl,
        Uri baseUri,
        HashSet<string> results,
        HashSet<string> visited)
    {
        if (!visited.Add(sitemapUrl.AbsoluteUri))
        {
            return;
        }

        var bytes = await TryGetBytesAsync(client, sitemapUrl).ConfigureAwait(false);
        if (bytes is null || bytes.Length == 0)
        {
            return;
        }

        Stream? stream = null;
        try
        {
            var isGz = sitemapUrl.AbsolutePath.EndsWith(".gz", StringComparison.OrdinalIgnoreCase)
                       || (bytes.Length >= 2 && bytes[0] == 0x1F && bytes[1] == 0x8B);

            stream = new MemoryStream(bytes, writable: false);
            if (isGz)
            {
                stream = new GZipStream(stream, CompressionMode.Decompress);
            }

            var nested = new List<Uri>();
            try
            {
                var readerSettings = new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Ignore,
                    IgnoreComments = true,
                    IgnoreWhitespace = true,
                    XmlResolver = null,
                    CloseInput = true,
                };

                using var reader = XmlReader.Create(stream, readerSettings);
                stream = null; // reader owns it now

                string? rootLocal = null;
                while (reader.Read())
                {
                    if (reader.NodeType != XmlNodeType.Element)
                    {
                        continue;
                    }

                    if (rootLocal is null)
                    {
                        rootLocal = reader.LocalName;
                    }

                    if (!IsLocElement(reader))
                    {
                        continue;
                    }

                    var value = reader.ReadElementContentAsString()?.Trim();
                    if (string.IsNullOrEmpty(value))
                    {
                        continue;
                    }

                    if (!Uri.TryCreate(value, UriKind.Absolute, out var locUri))
                    {
                        continue;
                    }

                    if (string.Equals(rootLocal, "sitemapindex", StringComparison.OrdinalIgnoreCase))
                    {
                        nested.Add(locUri);
                    }
                    else
                    {
                        if (string.Equals(locUri.Host, baseUri.Host, StringComparison.OrdinalIgnoreCase))
                        {
                            results.Add(locUri.AbsolutePath);
                        }
                    }
                }
            }
            catch
            {
                // ignore malformed XML
            }

            foreach (var nestedUrl in nested)
            {
                await ProcessSitemapAsync(client, nestedUrl, baseUri, results, visited).ConfigureAwait(false);
            }
        }
        catch
        {
            // ignore
        }
        finally
        {
            try { stream?.Dispose(); } catch { /* ignore */ }
        }
    }

    private static bool IsLocElement(XmlReader reader)
    {
        if (!string.Equals(reader.LocalName, "loc", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var ns = reader.NamespaceURI ?? string.Empty;
        return ns.Length == 0 || string.Equals(ns, SitemapNamespace, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<string?> TryGetStringAsync(HttpClient client, Uri uri)
    {
        var bytes = await TryGetBytesAsync(client, uri).ConfigureAwait(false);
        if (bytes is null)
        {
            return null;
        }

        try
        {
            return System.Text.Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return null;
        }
    }

    private static async Task<byte[]?> TryGetBytesAsync(HttpClient client, Uri uri)
    {
        try
        {
            using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            if (response.Content.Headers.ContentLength is long len && len > MaxResponseBytes)
            {
                return null;
            }

            using var source = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            using var buffer = new MemoryStream();
            var chunk = new byte[8192];
            long total = 0;
            int read;
            while ((read = await source.ReadAsync(chunk, 0, chunk.Length).ConfigureAwait(false)) > 0)
            {
                total += read;
                if (total > MaxResponseBytes)
                {
                    return null;
                }
                buffer.Write(chunk, 0, read);
            }

            return buffer.ToArray();
        }
        catch
        {
            return null;
        }
    }
}
