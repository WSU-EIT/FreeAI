// Shared: find the repo/solution root by walking up from a file to the nearest folder that holds a
// solution (.sln or the newer .slnx) or, failing that, a .git folder. Used by both repository commands.
namespace FreeCodeReorganizer.VS2026;

using System.IO;

internal static class RepoRoot
{
    public static string? Find(string startFile)
    {
        try
        {
            DirectoryInfo? dir = new FileInfo(startFile).Directory;
            DirectoryInfo? gitFallback = null;
            while (dir is not null)
            {
                if (dir.GetFiles("*.sln").Length > 0 || dir.GetFiles("*.slnx").Length > 0)
                {
                    return dir.FullName;
                }

                if (gitFallback is null && Directory.Exists(Path.Combine(dir.FullName, ".git")))
                {
                    gitFallback = dir;
                }

                dir = dir.Parent;
            }

            return gitFallback?.FullName;
        }
        catch
        {
            return null;
        }
    }
}
