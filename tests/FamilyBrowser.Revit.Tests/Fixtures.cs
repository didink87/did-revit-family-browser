using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;

namespace FamilyBrowser.Revit.Tests;

internal static class Fixtures
{
    private const string Template = @"C:\ProgramData\Autodesk\RVT 2026\Templates\English\DefaultMetric.rte";

    public static string NewTempDir()
    {
        var d = Path.Combine(Path.GetTempPath(), "FamilyBrowserTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(d);
        return d;
    }

    /// <summary>Creates a saved project (optionally a workshared central) from the default template and closes it.</summary>
    public static string CreateProject(Application app, string path, bool workshared = false)
    {
        var doc = File.Exists(Template) ? app.NewProjectDocument(Template) : app.NewProjectDocument(app.DefaultProjectTemplate);
        try
        {
            if (workshared)
            {
                doc.EnableWorksharing("Shared Levels and Grids", "Workset1");
                var opts = new SaveAsOptions();
                opts.SetWorksharingOptions(new WorksharingSaveAsOptions { SaveAsCentral = true });
                doc.SaveAs(path, opts);
            }
            else doc.SaveAs(path);
        }
        finally { doc.Close(false); }
        return path;
    }

    public static int EditableLoadableFamilyCount(Document doc) =>
        new FilteredElementCollector(doc).OfClass(typeof(Family)).Cast<Family>().Count(f => !f.IsInPlace && f.IsEditable);
}
