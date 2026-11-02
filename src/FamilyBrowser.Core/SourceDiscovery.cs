using System.Text.RegularExpressions;

namespace FamilyBrowser.Core;

public static class SourceDiscovery
{
    private static readonly Regex Backup = new(@"\.\d{4}\.rvt$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static IReadOnlyList<string> FindProjects(IEnumerable<string> roots)
    {
        var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var options = new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true };

        foreach (var root in roots)
        {
            if (File.Exists(root)) Add(found, root);
            else if (Directory.Exists(root))
                foreach (var f in Directory.EnumerateFiles(root, "*", options)) Add(found, f);
        }

        return found.OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static void Add(HashSet<string> set, string path)
    {
        if (!path.EndsWith(".rvt", StringComparison.OrdinalIgnoreCase) || Backup.IsMatch(path)) return;
        set.Add(Path.GetFullPath(path));
    }
}
