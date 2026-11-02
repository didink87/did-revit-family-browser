using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using FamilyBrowser.Core;

namespace FamilyBrowser.Revit;

public sealed record ImportResult(
    IReadOnlyList<string> Loaded,
    IReadOnlyList<string> Skipped,
    IReadOnlyList<string> Failed);

internal sealed class SkipExistingLoadOptions : IFamilyLoadOptions
{
    public bool OnFamilyFound(bool familyInUse, out bool overwriteParameterValues)
    {
        overwriteParameterValues = false;
        return false;
    }

    public bool OnSharedFamilyFound(Family sharedFamily, bool familyInUse, out FamilySource source, out bool overwriteParameterValues)
    {
        source = FamilySource.Family;
        overwriteParameterValues = false;
        return false;
    }
}

public static class FamilyImporter
{
    /// <summary>Loads the chosen families into <paramref name="target"/>. Must be called outside a transaction.</summary>
    public static ImportResult Import(Application app, Document target, IReadOnlyList<FamilyEntry> entries)
    {
        if (target.IsFamilyDocument) throw new ArgumentException("Buka dokumen proyek sebagai target");

        var loaded = new List<string>();
        var skipped = new List<string>();
        var failed = new List<string>();

        var existing = new FilteredElementCollector(target).OfClass(typeof(Family)).Cast<Family>()
            .Select(f => f.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var group in entries.GroupBy(e => e.SourcePath, StringComparer.OrdinalIgnoreCase))
        {
            SourceDocument src;
            try { src = SourceDocument.Open(app, group.Key); }
            catch (SourceOpenException ex)
            {
                failed.AddRange(group.Select(e => $"{e.FamilyName} — {ex.Message}"));
                continue;
            }

            using (src)
            {
                foreach (var entry in group)
                {
                    if (existing.Contains(entry.FamilyName))
                    {
                        skipped.Add($"{entry.FamilyName} — sudah ada di proyek");
                        continue;
                    }

                    var fam = Find(src.Document, entry);
                    if (fam is null)
                    {
                        failed.Add($"{entry.FamilyName} — tidak ditemukan di sumber, pindai ulang");
                        continue;
                    }

                    try
                    {
                        var famDoc = src.Document.EditFamily(fam);
                        try { famDoc.LoadFamily(target, new SkipExistingLoadOptions()); }
                        finally { famDoc.Close(false); }
                        existing.Add(entry.FamilyName);
                        loaded.Add(entry.FamilyName);
                    }
                    catch (Exception ex)
                    {
                        failed.Add($"{entry.FamilyName} — {ex.Message}");
                    }
                }
            }
        }

        return new ImportResult(loaded, skipped, failed);
    }

    private static Family? Find(Document doc, FamilyEntry entry)
    {
        if (doc.GetElement(new ElementId(entry.ElementId)) is Family byId
            && string.Equals(byId.Name, entry.FamilyName, StringComparison.OrdinalIgnoreCase))
            return byId;

        return new FilteredElementCollector(doc).OfClass(typeof(Family)).Cast<Family>()
            .FirstOrDefault(f => string.Equals(f.Name, entry.FamilyName, StringComparison.OrdinalIgnoreCase));
    }
}
