using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace FamilyBrowser.Core;

public sealed class FamilyIndexStore(string rootDir)
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public static FamilyIndexStore Default() => new(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FamilyBrowser"));

    public static (DateTime, long) Stamp(string sourcePath)
    {
        var f = new FileInfo(sourcePath);
        return (f.LastWriteTimeUtc, f.Length);
    }

    private static string Hash(string sourcePath) =>
        Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(sourcePath.ToLowerInvariant()))).ToLowerInvariant();

    private string IndexFile(string sourcePath) => Path.Combine(rootDir, "index", Hash(sourcePath) + ".json");

    public string PreviewDir(string sourcePath)
    {
        var d = Path.Combine(rootDir, "previews", Hash(sourcePath));
        Directory.CreateDirectory(d);
        return d;
    }

    public void Save(SourceIndex index)
    {
        var file = IndexFile(index.SourcePath);
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        var tmp = file + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(index, Json));
        File.Move(tmp, file, overwrite: true);
    }

    public SourceIndex? Load(string sourcePath)
    {
        try
        {
            var file = IndexFile(sourcePath);
            return File.Exists(file) ? JsonSerializer.Deserialize<SourceIndex>(File.ReadAllText(file)) : null;
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            return null;
        }
    }

    public bool IsCurrent(string sourcePath)
    {
        var idx = Load(sourcePath);
        if (idx is null || !File.Exists(sourcePath)) return false;
        var (w, l) = Stamp(sourcePath);
        return idx.LastWriteUtc == w && idx.Length == l;
    }
}
