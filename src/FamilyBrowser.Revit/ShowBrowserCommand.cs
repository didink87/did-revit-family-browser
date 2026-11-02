using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace FamilyBrowser.Revit;

[Transaction(TransactionMode.Manual)]
public sealed class ShowBrowserCommand : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        commandData.Application.GetDockablePane(PaneIds.Browser).Show();
        return Result.Succeeded;
    }
}
