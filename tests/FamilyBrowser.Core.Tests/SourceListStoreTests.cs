using FamilyBrowser.Core;

namespace FamilyBrowser.Core.Tests;

public sealed class SourceListStoreTests : IDisposable
{
    private readonly string dir = Path.Combine(Path.GetTempPath(), "fb-src-" + Guid.NewGuid().ToString("N"));
    private string File_ => Path.Combine(dir, "sources.json");

    public SourceListStoreTests() => Directory.CreateDirectory(dir);
    public void Dispose() => Directory.Delete(dir, true);

    [Fact]
    public void Load_missing_file_returns_empty() => Assert.Empty(new SourceListStore(File_).Load());

    [Fact]
    public void Load_corrupt_json_returns_empty()
    {
        File.WriteAllText(File_, "{ bukan json");
        Assert.Empty(new SourceListStore(File_).Load());
    }

    [Fact]
    public void Save_then_Load_preserves_order()
    {
        var s = new SourceListStore(File_);
        s.Save(new[] { @"C:\b", @"C:\a.rvt" });
        Assert.Equal(new[] { @"C:\b", @"C:\a.rvt" }, s.Load());
    }
}
