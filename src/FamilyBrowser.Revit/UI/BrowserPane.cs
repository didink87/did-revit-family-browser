using System.Windows.Controls;
using Autodesk.Revit.UI;

namespace FamilyBrowser.Revit.UI;

/// <summary>Placeholder replaced by the XAML pane in Task 7.</summary>
public sealed class BrowserPane : Page, IDockablePaneProvider
{
    public void SetupDockablePane(DockablePaneProviderData data)
    {
        data.FrameworkElement = this;
        data.InitialState = new DockablePaneState { DockPosition = DockPosition.Right };
    }
}
