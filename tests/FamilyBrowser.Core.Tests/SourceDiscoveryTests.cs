using FamilyBrowser.Core;

namespace FamilyBrowser.Core.Tests;

public sealed class SourceDiscoveryTests : IDisposable
{
    private readonly string dir = Path.Combine(Path.GetTempPath(), "fb-disc-" + Guid.NewGuid().ToString("N"));

    public SourceDiscoveryTests()
    {
        Directory.CreateDirectory(Path.Combine(dir, "sub"));
        foreach (var f in new[] { "A.rvt", Path.Combine("sub", "B.RVT"), "A.0001.rvt", "C.rfa", "notes.txt" })
            File.WriteAllText(Path.Combine(dir, f), "x");
    }

    public void Dispose() => Directory.Delete(dir, true);

    [Fact]
    public void Folder_returns_rvt_recursively_without_backups()
    {
        var r = SourceDiscovery.FindProjects(new[] { dir });
        Assert.Equal(new[] { Path.Combine(dir, "A.rvt"), Path.Combine(dir, "sub", "B.RVT") }, r);
    }

    [Fact]
    public void File_and_its_folder_given_together_are_deduplicated()
        => Assert.Single(SourceDiscovery.FindProjects(new[] { Path.Combine(dir, "sub", "B.RVT"), Path.Combine(dir, "SUB") }));

    [Fact]
    public void Missing_root_is_ignored()
        => Assert.Empty(SourceDiscovery.FindProjects(new[] { Path.Combine(dir, "nope") }));

    [Fact]
    public void Backup_given_directly_is_ignored()
        => Assert.Empty(SourceDiscovery.FindProjects(new[] { Path.Combine(dir, "A.0001.rvt") }));
}
