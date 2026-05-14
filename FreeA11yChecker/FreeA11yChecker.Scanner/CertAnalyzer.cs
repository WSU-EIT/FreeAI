using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace FreeA11yChecker.Scanner;

using FreeA11yChecker.Scanner.Models;

/// <summary>
/// Extracts SSL/TLS certificate information from HTTPS endpoints.
/// </summary>
public static class CertAnalyzer
{
    /// <summary>
    /// Connect to the given URL's host via TLS, extract certificate details,
    /// and return structured CertInfo. Returns null if the URL is not HTTPS
    /// or if the connection fails.
    /// </summary>
    public static CertInfo? AnalyzeCert(string url)
    {
        Uri uri;
        try
        {
            uri = new Uri(url);
        }
        catch
        {
            return null;
        }

        if (uri.Scheme != "https")
            return null;

        try
        {
            var host = uri.Host;
            var port = uri.Port > 0 ? uri.Port : 443;

            using var tcp = new TcpClient();
            tcp.ConnectAsync(host, port).GetAwaiter().GetResult();

            using var ssl = new SslStream(tcp.GetStream(), false, (_, _, _, _) => true);
            ssl.AuthenticateAsClient(host);

            var cert2 = new X509Certificate2(ssl.RemoteCertificate!);

            var certInfo = new CertInfo
            {
                Subject = cert2.Subject,
                Issuer = cert2.Issuer,
                Expiry = cert2.NotAfter
            };

            // Extract Subject Alternative Names
            var sanExtension = cert2.Extensions["2.5.29.17"];
            if (sanExtension != null)
            {
                var sanData = new AsnEncodedData("2.5.29.17", sanExtension.RawData);
                var sanString = sanData.Format(multiLine: true);

                foreach (var line in sanString.Split('\n', '\r'))
                {
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith("DNS Name=", StringComparison.OrdinalIgnoreCase))
                    {
                        var dns = trimmed["DNS Name=".Length..].Trim();
                        if (!string.IsNullOrWhiteSpace(dns))
                            certInfo.SubjectAlternativeNames.Add(dns);
                    }
                }

                certInfo.SubjectAlternativeNames.Sort(StringComparer.OrdinalIgnoreCase);
            }

            cert2.Dispose();
            return certInfo;
        }
        catch
        {
            return null;
        }
    }
}
