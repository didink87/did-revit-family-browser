using System.Drawing;
using System.Drawing.Imaging;
using Autodesk.Revit.DB;
using FamilyBrowser.Core;

namespace FamilyBrowser.Revit;

public static class FamilyScanner
{
    public const string NoCategory = "(Tanpa kategori)";

    public static IReadOnlyList<FamilyEntry> Scan(Document doc, string sourcePath, string previewDir)
    {
        var result = new List<FamilyEntry>();
        foreach (var fam in new FilteredElementCollector(doc).OfClass(typeof(Family)).Cast<Family>())
        {
            if (fam.IsInPlace || !fam.IsEditable) continue;

            var symbols = fam.GetFamilySymbolIds().Select(id => doc.GetElement(id)).OfType<FamilySymbol>()
                .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase).ToList();

            result.Add(new FamilyEntry(
                sourcePath,
                fam.Id.Value,
                fam.Name,
                fam.FamilyCategory?.Name ?? NoCategory,
                symbols.Select(s => s.Name).ToList(),
                SavePreview(symbols.FirstOrDefault(), previewDir, fam.Id.Value)));
        }
        return result;
    }

    private static string? SavePreview(FamilySymbol? symbol, string dir, long familyId)
    {
        if (symbol is null) return null;
        try
        {
            using var bmp = symbol.GetPreviewImage(new Size(128, 128));
            if (bmp is null) return null;
            var file = Path.Combine(dir, familyId + ".png");
            bmp.Save(file, ImageFormat.Png);
            return file;
        }
        catch (Exception) { return null; }
    }
}
