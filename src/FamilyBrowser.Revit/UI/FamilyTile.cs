using System.ComponentModel;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FamilyBrowser.Core;

namespace FamilyBrowser.Revit.UI;

public sealed class FamilyTile : INotifyPropertyChanged
{
    private bool isSelected;

    public FamilyTile(FamilyEntry entry)
    {
        Entry = entry;
        Preview = LoadPreview(entry.PreviewFile);
    }

    public FamilyEntry Entry { get; }
    public string FamilyName => Entry.FamilyName;
    public string CategoryName => Entry.CategoryName;
    public string SourceName => Path.GetFileNameWithoutExtension(Entry.SourcePath);
    public string TypeCountText => $"{Entry.TypeNames.Count} type";
    public ImageSource? Preview { get; }

    public bool IsSelected
    {
        get => isSelected;
        set
        {
            if (isSelected == value) return;
            isSelected = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private static ImageSource? LoadPreview(string? file)
    {
        if (string.IsNullOrEmpty(file) || !File.Exists(file)) return null;
        try
        {
            var img = new BitmapImage();
            img.BeginInit();
            img.UriSource = new Uri(file);
            img.CacheOption = BitmapCacheOption.OnLoad; // do not keep the PNG locked
            img.EndInit();
            img.Freeze();
            return img;
        }
        catch (Exception) { return null; }
    }
}
