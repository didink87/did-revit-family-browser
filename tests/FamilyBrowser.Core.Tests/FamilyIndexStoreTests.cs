using FamilyBrowser.Core;

namespace FamilyBrowser.Core.Tests;

public sealed class FamilyIndexStoreTests : IDisposable
{
    private readonly string dir = Path.Combine(Path.GetTempPath(), "fb-idx-" + Guid.NewGuid().ToString("N"));
    private readonly string project;
    private readonly FamilyIndexStore store;

    public FamilyIndexStoreTests()
    {
        Directory.CreateDirectory(dir);
        project = Path.Combine(dir, "A.rvt");
        File.WriteAllText(project, "x");
        store = new FamilyIndexStore(Path.Combine(dir, "data"));
    }

    public void Dispose() => Directory.Delete(dir, true);

    private SourceIndex Make(string? error = null, IReadOnlyList<FamilyEntry>? fams = null)
    {
        var (w, l) = FamilyIndexStore.Stamp(project);
        return new SourceIndex(project, w, l, fams ?? Array.Empty<FamilyEntry>(), error);
    }

    [Fact]
    public void Save_then_Load_roundtrips_entries_and_error()
    {
        var e = new FamilyEntry(project, 42, "Pintu", "Doors", new[] { "900", "1000" }, "p.png");
        store.Save(Make("rusak", new[] { e }));
        var back = store.Load(project)!;
        Assert.Equal("rusak", back.Error);
        Assert.Equal(new[] { "900", "1000" }, back.Families[0].TypeNames);
        Assert.Equal(42, back.Families[0].ElementId);
    }

    [Fact]
    public void IsCurrent_false_when_no_index() => Assert.False(store.IsCurrent(project));

    [Fact]
    public void IsCurrent_true_after_save_with_current_stamp()
    {
        store.Save(Make());
        Assert.True(store.IsCurrent(project));
    }

    [Fact]
    public void IsCurrent_false_after_file_modified()
    {
        store.Save(Make());
        File.SetLastWriteTimeUtc(project, DateTime.UtcNow.AddMinutes(1));
        Assert.False(store.IsCurrent(project));
    }

    [Fact]
    public void IsCurrent_true_for_saved_error_index_until_file_changes()
    {
        store.Save(Make("rusak"));
        Assert.True(store.IsCurrent(project));
        File.WriteAllText(project, "xyz");
        Assert.False(store.IsCurrent(project));
    }

    [Fact]
    public void Index_file_name_is_case_insensitive_on_path()
    {
        store.Save(Make());
        Assert.NotNull(store.Load(project.ToUpperInvariant()));
    }
}
