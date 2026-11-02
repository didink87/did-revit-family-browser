using Autodesk.Revit.UI;
using FamilyBrowser.Core;
using FamilyBrowser.Revit.UI;
using NUnit.Framework;

namespace FamilyBrowser.Revit.Tests;

[TestFixture]
public class BrowserViewModelTests
{
    private static (string dir, FamilyIndexStore store, SourceListStore sources) Arrange(Autodesk.Revit.ApplicationServices.Application app)
    {
        var dir = Fixtures.NewTempDir();
        var src = Path.Combine(dir, "proyek");
        Directory.CreateDirectory(Path.Combine(src, "sub"));
        var a = Fixtures.CreateProject(app, Path.Combine(src, "a.rvt"));
        File.Copy(a, Path.Combine(src, "sub", "b.rvt"));
        File.Copy(a, Path.Combine(src, "a.0001.rvt"));
        File.WriteAllText(Path.Combine(src, "rusak.rvt"), "bukan revit");
        var sources = new SourceListStore(Path.Combine(dir, "sources.json"));
        sources.Save(new[] { src });
        return (dir, new FamilyIndexStore(Path.Combine(dir, "data")), sources);
    }

    [Test]
    public void Rescan_fills_tiles_skips_backups_and_reports_failures(UIApplication uiapp)
    {
        var (_, store, sources) = Arrange(uiapp.Application);
        var vm = new BrowserViewModel(store, sources, job => job(uiapp));

        vm.Rescan(force: true);

        Assert.That(vm.Tiles.Count, Is.GreaterThan(0));
        Assert.That(vm.Sources.Select(s => s.Label), Is.EquivalentTo(new[] { "Semua", "a", "b" }));
        Assert.That(vm.Status, Does.EndWith("dari 3 proyek; 1 gagal"));
    }

    [Test]
    public void Search_and_category_filters_narrow_the_tiles(UIApplication uiapp)
    {
        var (_, store, sources) = Arrange(uiapp.Application);
        var vm = new BrowserViewModel(store, sources, job => job(uiapp));
        vm.Rescan(force: true);
        var all = vm.Tiles.Count;

        vm.SearchText = "zzzz-tidak-ada";
        Assert.That(vm.Tiles, Is.Empty);

        vm.SearchText = "";
        Assert.That(vm.Tiles.Count, Is.EqualTo(all));

        var cat = vm.Categories.First(c => c.Value != null);
        vm.SelectedCategory = cat;
        Assert.That(vm.Tiles.All(t => t.CategoryName == cat.Value), Is.True);
    }

    [Test]
    public void New_viewmodel_loads_from_cache_without_scanning(UIApplication uiapp)
    {
        var (_, store, sources) = Arrange(uiapp.Application);
        var first = new BrowserViewModel(store, sources, job => job(uiapp));
        first.Rescan(force: true);

        var second = new BrowserViewModel(store, sources, _ => throw new AssertionException("tidak boleh memindai"));

        Assert.That(second.Tiles.Count, Is.EqualTo(first.Tiles.Count));
        second.Rescan(force: false);
        Assert.That(second.Status, Does.EndWith("dari 3 proyek; 1 gagal"));
    }

    [Test]
    public void Pane_xaml_loads_and_binds_to_viewmodel(UIApplication uiapp)
    {
        var (_, store, sources) = Arrange(uiapp.Application);
        var vm = new BrowserViewModel(store, sources, job => job(uiapp));
        vm.Rescan(force: true);

        var pane = new BrowserPane(vm);

        Assert.That(pane.DataContext, Is.SameAs(vm));
    }
}
