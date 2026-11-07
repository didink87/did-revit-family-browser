using System.Reflection;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;
using FamilyBrowser.Core;
using FamilyBrowser.Revit.UI;

namespace FamilyBrowser.Revit;

public sealed class App : IExternalApplication
{
    // Shared with Sheet Studio and Face View: one "DID" tab, one "DID" panel.
    private const string TabName = "DID";
    private const string PanelName = "DID";

    /// <summary>Created in OnStartup; every Revit API call from the pane goes through it.</summary>
    public static RevitTaskQueue Queue { get; private set; } = null!;

    public Result OnStartup(UIControlledApplication application)
    {
        Queue = new RevitTaskQueue();

        try { application.CreateRibbonTab(TabName); } catch (Autodesk.Revit.Exceptions.ArgumentException) { /* tab already created by another DID add-in */ }
        var panel = application.GetRibbonPanels(TabName).FirstOrDefault(p => p.Name == PanelName)
                    ?? application.CreateRibbonPanel(TabName, PanelName);
        var asm = Assembly.GetExecutingAssembly();
        var button = new PushButtonData("Browser", "Browser", asm.Location, typeof(ShowBrowserCommand).FullName)
        {
            LargeImage = LoadIcon(asm, "browser32.png"),
            Image = LoadIcon(asm, "browser16.png"),
            ToolTip = "Buka panel Family Browser",
        };
        panel.AddItem(button);

        var viewModel = new BrowserViewModel(FamilyIndexStore.Default(), SourceListStore.Default(), Queue.Enqueue);
        Queue.Failed += viewModel.ReportFailure;
        application.RegisterDockablePane(PaneIds.Browser, "Family Browser", new BrowserPane(viewModel));
        return Result.Succeeded;
    }

    public Result OnShutdown(UIControlledApplication application) => Result.Succeeded;

    private static BitmapImage LoadIcon(Assembly asm, string name)
    {
        using var s = asm.GetManifestResourceStream($"FamilyBrowser.Revit.Resources.{name}")!;
        var img = new BitmapImage();
        img.BeginInit();
        img.StreamSource = s;
        img.CacheOption = BitmapCacheOption.OnLoad;
        img.EndInit();
        img.Freeze();
        return img;
    }
}
