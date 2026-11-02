using FamilyBrowser.Core;

namespace FamilyBrowser.Core.Tests;

public class RevitFileVersionTests
{
    [Theory]
    [InlineData("2026", 2026)]
    [InlineData("Autodesk Revit 2024", 2024)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void ParseYear(string? f, int? y) => Assert.Equal(y, RevitFileVersion.ParseYear(f));

    [Theory]
    [InlineData(2027, false)]
    [InlineData(2026, true)]
    [InlineData(2023, true)]
    [InlineData(null, true)]
    public void CanOpen_on_2026(int? y, bool ok) => Assert.Equal(ok, RevitFileVersion.CanOpen(y, 2026));
}
