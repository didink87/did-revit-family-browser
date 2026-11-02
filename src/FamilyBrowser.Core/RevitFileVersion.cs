using System.Text.RegularExpressions;

namespace FamilyBrowser.Core;

public static class RevitFileVersion
{
    private static readonly Regex Year = new(@"20\d\d", RegexOptions.Compiled);

    public static int? ParseYear(string? format)
    {
        if (string.IsNullOrWhiteSpace(format)) return null;
        var m = Year.Match(format);
        return m.Success ? int.Parse(m.Value) : null;
    }

    public static bool CanOpen(int? fileYear, int hostYear) => fileYear is null || fileYear <= hostYear;
}
