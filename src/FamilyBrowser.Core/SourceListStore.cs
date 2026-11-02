using System.Text.Json;

namespace FamilyBrowser.Core;

public sealed class SourceListStore(string filePath)
{
    public static SourceListStore Default() => new(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FamilyBrowser", "sources.json"));

    public IReadOnlyList<string> Load()
    {
        try
        {
            if (!File.Exists(filePath)) return Array.Empty<string>();
            return JsonSerializer.Deserialize<List<string>>(File.ReadAllText(filePath)) ?? new List<string>();
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            return Array.Empty<string>();
        }
    }

    public void Save(IEnumerable<string> roots)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        File.WriteAllText(filePath, JsonSerializer.Serialize(roots.ToList(), new JsonSerializerOptions { WriteIndented = true }));
    }
}
