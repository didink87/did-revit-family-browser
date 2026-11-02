using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using FamilyBrowser.Core;

namespace FamilyBrowser.Revit;

public sealed class SourceOpenException(string message, Exception? inner = null) : Exception(message, inner);

/// <summary>A source project opened in the background. Never saved; closed on Dispose only if we opened it.</summary>
public sealed class SourceDocument : IDisposable
{
    private const int HostYear = 2026;

    public Document Document { get; }
    public bool OwnsDocument { get; }

    private SourceDocument(Document doc, bool owns) { Document = doc; OwnsDocument = owns; }

    public static SourceDocument Open(Application app, string path)
    {
        foreach (Document d in app.Documents)
            if (string.Equals(d.PathName, path, StringComparison.OrdinalIgnoreCase))
                return new SourceDocument(d, false);

        try
        {
            var info = BasicFileInfo.Extract(path);
            var year = RevitFileVersion.ParseYear(info.Format);
            if (!RevitFileVersion.CanOpen(year, HostYear))
                throw new SourceOpenException($"Dibuat dengan Revit {year}, lebih baru dari {HostYear}");

            var opts = new OpenOptions { Audit = false };
            if (info.IsWorkshared)
            {
                opts.DetachFromCentralOption = DetachFromCentralOption.DetachAndPreserveWorksets;
                opts.SetOpenWorksetsConfiguration(new WorksetConfiguration(WorksetConfigurationOption.CloseAllWorksets));
            }

            var doc = app.OpenDocumentFile(ModelPathUtils.ConvertUserVisiblePathToModelPath(path), opts);
            return new SourceDocument(doc, true);
        }
        catch (SourceOpenException) { throw; }
        catch (Exception ex) { throw new SourceOpenException(ex.Message, ex); }
    }

    public void Dispose()
    {
        if (OwnsDocument && Document.IsValidObject) Document.Close(false);
    }
}
