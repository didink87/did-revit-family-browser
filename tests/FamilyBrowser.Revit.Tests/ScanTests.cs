using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using FamilyBrowser.Core;
using NUnit.Framework;

namespace FamilyBrowser.Revit.Tests;

[TestFixture]
public class ScanTests
{
    private static bool IsOpen(Application app, string path) =>
        app.Documents.Cast<Document>().Any(d => d.PathName.Equals(path, StringComparison.OrdinalIgnoreCase));

    [Test]
    public void Scan_returns_every_editable_loadable_family_with_category_and_preview(Application app)
    {
        var dir = Fixtures.NewTempDir();
        var project = Fixtures.CreateProject(app, Path.Combine(dir, "a.rvt"));
        var store = new FamilyIndexStore(Path.Combine(dir, "data"));

        var probe = app.OpenDocumentFile(project);
        var expected = Fixtures.EditableLoadableFamilyCount(probe);
        probe.Close(false);

        var idx = ScanService.ScanSource(app, store, project);

        Assert.That(idx.Error, Is.Null);
        Assert.That(idx.Families.Count, Is.EqualTo(expected).And.GreaterThan(0));
        Assert.That(idx.Families.All(f => f.CategoryName != "" && f.TypeNames.Count > 0));
        Assert.That(idx.Families.Count(f => f.PreviewFile != null && File.Exists(f.PreviewFile)), Is.GreaterThan(0));
        Assert.That(IsOpen(app, project), Is.False);
    }

    [Test]
    public void Already_open_document_is_reused_and_left_open(Application app)
    {
        var dir = Fixtures.NewTempDir();
        var project = Fixtures.CreateProject(app, Path.Combine(dir, "a.rvt"));
        var store = new FamilyIndexStore(Path.Combine(dir, "data"));
        var open = app.OpenDocumentFile(project);
        try
        {
            var idx = ScanService.ScanSource(app, store, project);
            Assert.That(idx.Error, Is.Null);
            Assert.That(open.IsValidObject, Is.True);
        }
        finally { open.Close(false); }
    }

    [Test]
    public void Workshared_central_is_scanned_detached_and_not_modified(Application app)
    {
        var dir = Fixtures.NewTempDir();
        var central = Fixtures.CreateProject(app, Path.Combine(dir, "central.rvt"), workshared: true);
        var store = new FamilyIndexStore(Path.Combine(dir, "data"));
        var before = File.GetLastWriteTimeUtc(central);

        var idx = ScanService.ScanSource(app, store, central);

        Assert.That(idx.Error, Is.Null);
        Assert.That(idx.Families, Is.Not.Empty);
        Assert.That(File.GetLastWriteTimeUtc(central), Is.EqualTo(before));
    }

    [Test]
    public void Corrupt_file_records_error_and_is_current(Application app)
    {
        var dir = Fixtures.NewTempDir();
        var bad = Path.Combine(dir, "rusak.rvt");
        File.WriteAllText(bad, "bukan revit");
        var store = new FamilyIndexStore(Path.Combine(dir, "data"));

        var idx = ScanService.ScanSource(app, store, bad);

        Assert.That(idx.Error, Is.Not.Null.And.Not.Empty);
        Assert.That(store.IsCurrent(bad), Is.True);
    }
}
