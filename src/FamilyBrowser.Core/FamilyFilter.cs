namespace FamilyBrowser.Core;

public sealed record FamilyQuery(string? Text, string? Category, string? SourcePath);

public static class FamilyFilter
{
    private static readonly StringComparer Cmp = StringComparer.OrdinalIgnoreCase;

    public static IReadOnlyList<FamilyEntry> Apply(IEnumerable<FamilyEntry> all, FamilyQuery q)
    {
        var text = q.Text?.Trim();
        return all
            .Where(e => q.Category is null || Cmp.Equals(e.CategoryName, q.Category))
            .Where(e => q.SourcePath is null || Cmp.Equals(e.SourcePath, q.SourcePath))
            .Where(e => string.IsNullOrEmpty(text)
                || e.FamilyName.Contains(text, StringComparison.OrdinalIgnoreCase)
                || e.TypeNames.Any(t => t.Contains(text, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(e => e.CategoryName, Cmp)
            .ThenBy(e => e.FamilyName, Cmp)
            .ThenBy(e => e.SourcePath, Cmp)
            .ToList();
    }

    public static IReadOnlyList<string> Categories(IEnumerable<FamilyEntry> all) =>
        all.Select(e => e.CategoryName).Distinct(Cmp).OrderBy(c => c, Cmp).ToList();
}
