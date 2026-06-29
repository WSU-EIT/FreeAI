using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace FreeCodeReorganizer.Core;

/// <summary>
/// Matches a file path against a list of user-supplied exclusion patterns. A pattern with no wildcard
/// is treated as a case-insensitive substring of the path (so "CRM.EFModels" excludes that whole
/// project folder); a pattern containing <c>*</c>, <c>?</c> or <c>**</c> is treated as a glob
/// (<c>**</c> spans directory separators, <c>*</c> stays within one segment). Forward and back slashes
/// are interchangeable. Used to keep, e.g., Entity Framework model projects (whose member order mirrors
/// the database) out of the reorganizer while still allowing the .editorconfig cleanup.
/// </summary>
public static class PathExclusion
{
    public static bool IsExcluded(string path, IReadOnlyList<string>? patterns)
    {
        if (patterns is null || patterns.Count == 0 || string.IsNullOrEmpty(path))
        {
            return false;
        }

        string norm = path.Replace('/', '\\');
        foreach (string raw in patterns)
        {
            string pattern = raw?.Trim() ?? string.Empty;
            if (pattern.Length == 0)
            {
                continue;
            }

            if (pattern.IndexOf('*') < 0 && pattern.IndexOf('?') < 0)
            {
                // Plain text: substring match (slashes normalized).
                if (norm.IndexOf(pattern.Replace('/', '\\'), StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            else if (GlobToRegex(pattern).IsMatch(norm))
            {
                return true;
            }
        }

        return false;
    }

    private static Regex GlobToRegex(string glob)
    {
        var sb = new StringBuilder();
        sb.Append(".*"); // allow the glob to match anywhere in the full path
        for (int i = 0; i < glob.Length; i++)
        {
            char c = glob[i];
            switch (c)
            {
                case '*':
                    if (i + 1 < glob.Length && glob[i + 1] == '*')
                    {
                        sb.Append(".*"); // ** spans separators
                        i++;
                    }
                    else
                    {
                        sb.Append("[^\\\\]*"); // * stays within a path segment
                    }

                    break;
                case '?':
                    sb.Append('.');
                    break;
                case '/':
                case '\\':
                    sb.Append("[\\\\/]");
                    break;
                default:
                    sb.Append(Regex.Escape(c.ToString()));
                    break;
            }
        }

        sb.Append(".*");
        return new Regex("^" + sb + "$", RegexOptions.IgnoreCase);
    }
}
