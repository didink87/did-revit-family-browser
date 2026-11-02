using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using FamilyBrowser.Core;
using NUnit.Framework;

namespace FamilyBrowser.Revit.Tests;

[TestFixture]
public class ImportTests
{
    private static Document NewTarget(Application app) => app.NewProjectDocument(UnitSystem.Metric);

    private static HashSet<string> FamilyNames(Document d) =>
        new FilteredElementCollector(d).OfClass(typeof(Family)).Cast<Family>().Select(f => f.Name).ToHashSet();

    private static (string dir, string a, string b, IReadOnlyList<FamilyEntry> ea, IReadOnlyList<FamilyEntry> eb) Sources(Application app)
    {
        var dir = Fixtures.NewTempDir();
        var a = Fixtures.CreateProject(app, Path.Combine(dir, "a.rvt"));
        var b = Path.Combine(dir, "b.rvt");
        File.Copy(a, b);
        var store = new FamilyIndexStore(Path.Combine(dir, "data"));
        return (dir, a, b, ScanService.ScanSource(app, store, a).Families, ScanService.ScanSource(app, store, b).Families);
    }

    [Test]
    public void Selected_families_are_loaded_into_target(Application app)
    {
        var (_, _, _, ea, _) = Sources(app);
        var target = NewTarget(app);
        try
        {
            var pick = ea.Take(2).ToList();
            var r = FamilyImporter.Import(app, target, pick);
            Assert.That(r.Loaded, Is.EquivalentTo(pick.Select(e => e.FamilyName)));
            Assert.That(FamilyNames(target), Is.SupersetOf(pick.Select(e => e.FamilyName)));
        }
        finally { target.Close(false); }
    }

    [Test]
    public void Existing_family_is_skipped_not_overwritten(Application app)
    {
        var (_, _, _, ea, _) = Sources(app);
        var target = NewTarget(app);
        try
        {
            FamilyImporter.Import(app, target, ea.Take(1).ToList());
            var r = FamilyImporter.Import(app, target, ea.Take(1).ToList());
            Assert.That(r.Loaded, Is.Empty);
            Assert.That(r.Skipped.Single(), Does.EndWith("sudah ada di proyek"));
        }
        finally { target.Close(false); }
    }

    [Test]
    public void Same_name_from_two_sources_loads_once(Application app)
    {
        var (_, _, _, ea, eb) = Sources(app);
        var target = NewTarget(app);
        try
        {
            var r = FamilyImporter.Import(app, target, new[] { ea[0], eb.First(e => e.FamilyName == ea[0].FamilyName) });
            Assert.That(r.Loaded.Count, Is.EqualTo(1));
            Assert.That(r.Skipped.Count, Is.EqualTo(1));
        }
        finally { target.Close(false); }
    }

    [Test]
    public void Missing_source_file_reports_failed(Application app)
    {
        var (_, _, _, ea, _) = Sources(app);
        var target = NewTarget(app);
        try
        {
            var gone = ea[0] with { SourcePath = @"C:\tidak\ada.rvt" };
            Assert.That(FamilyImporter.Import(app, target, new[] { gone }).Failed, Has.Count.EqualTo(1));
        }
        finally { target.Close(false); }
    }

    [Test]
    public void Source_file_is_closed_after_import(Application app)
    {
        var (_, a, _, ea, _) = Sources(app);
        var target = NewTarget(app);
        try
        {
            FamilyImporter.Import(app, target, ea.Take(1).ToList());
            Assert.That(app.Documents.Cast<Document>().Any(d => d.PathName.Equals(a, StringComparison.OrdinalIgnoreCase)), Is.False);
        }
        finally { target.Close(false); }
    }
}
