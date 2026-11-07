using Autodesk.Revit.UI;
using NUnit.Framework;

namespace FamilyBrowser.Revit.Tests;

[TestFixture]
public class AppStartupTests
{
    [Test]
    public void OnStartup_registers_ribbon_tab_and_dockable_pane(UIControlledApplication app, UIApplication uiapp)
    {
        var result = new App().OnStartup(app);

        Assert.That(result, Is.EqualTo(Result.Succeeded));
        Assert.That(App.Queue, Is.Not.Null);
        // Revit only instantiates panes registered after startup on the next UI build, so prove registration
        // by the duplicate-id rejection instead of GetDockablePane.
        Assert.That(() => app.RegisterDockablePane(PaneIds.Browser, "dup", new FamilyBrowser.Revit.UI.BrowserPane(
                new FamilyBrowser.Revit.UI.BrowserViewModel(
                    FamilyBrowser.Core.FamilyIndexStore.Default(), FamilyBrowser.Core.SourceListStore.Default(), _ => { }))),
            Throws.InstanceOf<Autodesk.Revit.Exceptions.ArgumentException>());
        Assert.That(app.GetRibbonPanels("DID").Select(p => p.Name), Does.Contain("DID"));
    }
}
