using System.Windows.Controls;
using Autodesk.Revit.UI;

namespace FamilyBrowser.Revit.UI;

public partial class BrowserPane : Page, IDockablePaneProvider
{
    public BrowserPane(BrowserViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    public void SetupDockablePane(DockablePaneProviderData data)
    {
        data.FrameworkElement = this;
        data.InitialState = new DockablePaneState { DockPosition = DockPosition.Right };
    }
}
