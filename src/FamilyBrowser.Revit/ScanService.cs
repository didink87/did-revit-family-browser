using Autodesk.Revit.ApplicationServices;
using FamilyBrowser.Core;

namespace FamilyBrowser.Revit;

public static class ScanService
{
    /// <summary>Scans one source project and always saves the result (failures are stored as <see cref="SourceIndex.Error"/>).</summary>
    public static SourceIndex ScanSource(Application app, FamilyIndexStore store, string path)
    {
        var (written, length) = FamilyIndexStore.Stamp(path);
        SourceIndex index;
        try
        {
            using var src = SourceDocument.Open(app, path);
            var families = FamilyScanner.Scan(src.Document, path, store.PreviewDir(path));
            index = new SourceIndex(path, written, length, families, null);
        }
        catch (Exception ex)
        {
            index = new SourceIndex(path, written, length, Array.Empty<FamilyEntry>(), ex.Message);
        }
        store.Save(index);
        return index;
    }
}
