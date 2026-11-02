using FamilyBrowser.Core;

namespace FamilyBrowser.Core.Tests;

public class FamilyFilterTests
{
    private static FamilyEntry E(string src, string name, string cat, params string[] types) =>
        new(src, 1, name, cat, types, null);

    private readonly FamilyEntry[] all =
    {
        E(@"C:\P\A.rvt", "Pintu Tunggal", "Doors", "900x2100"),
        E(@"C:\P\B.rvt", "Jendela Geser", "Windows", "1200"),
        E(@"C:\P\B.rvt", "pintu ganda", "Doors", "1800"),
    };

    private static string[] Names(IEnumerable<FamilyEntry> e) => e.Select(x => x.FamilyName).ToArray();

    [Fact]
    public void Text_matches_family_name_case_insensitive()
        => Assert.Equal(new[] { "pintu ganda", "Pintu Tunggal" }, Names(FamilyFilter.Apply(all, new("PINTU", null, null))));

    [Fact]
    public void Text_matches_type_name()
        => Assert.Equal(new[] { "Pintu Tunggal" }, Names(FamilyFilter.Apply(all, new("2100", null, null))));

    [Fact]
    public void Category_and_source_combine()
        => Assert.Equal(new[] { "pintu ganda" }, Names(FamilyFilter.Apply(all, new(null, "Doors", @"c:\p\b.RVT"))));

    [Fact]
    public void Empty_query_returns_all_sorted_by_category_then_name()
        => Assert.Equal(new[] { "pintu ganda", "Pintu Tunggal", "Jendela Geser" }, Names(FamilyFilter.Apply(all, new("  ", null, null))));

    [Fact]
    public void Categories_are_distinct_sorted()
        => Assert.Equal(new[] { "Doors", "Windows" }, FamilyFilter.Categories(all));
}
