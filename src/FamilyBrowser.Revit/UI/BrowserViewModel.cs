using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Autodesk.Revit.UI;
using FamilyBrowser.Core;
using Microsoft.Win32;

namespace FamilyBrowser.Revit.UI;

public sealed record Choice(string Label, string? Value);

public sealed class BrowserViewModel : INotifyPropertyChanged
{
    private static readonly Choice All = new("Semua", null);

    private readonly FamilyIndexStore store;
    private readonly SourceListStore sourceList;
    private readonly Action<Action<UIApplication>> enqueue;

    private List<FamilyEntry> entries = new();
    private string searchText = "";
    private Choice selectedCategory = All;
    private Choice selectedSource = All;
    private string status = "";
    private string errorDetails = "";
    private bool refreshing;
    private int totalFiles;
    private int failedFiles;

    public BrowserViewModel(FamilyIndexStore store, SourceListStore sourceList, Action<Action<UIApplication>> enqueue)
    {
        this.store = store;
        this.sourceList = sourceList;
        this.enqueue = enqueue;

        AddFilesCommand = new RelayCommand(AddFiles);
        AddFolderCommand = new RelayCommand(AddFolder);
        RescanCommand = new RelayCommand(() => Rescan(force: true));
        LoadSelectedCommand = new RelayCommand(LoadSelected);

        Reload();
        Status = Summary();
    }

    public ObservableCollection<FamilyTile> Tiles { get; } = new();
    public ObservableCollection<Choice> Categories { get; } = new() { All };
    public ObservableCollection<Choice> Sources { get; } = new() { All };

    public ICommand AddFilesCommand { get; }
    public ICommand AddFolderCommand { get; }
    public ICommand RescanCommand { get; }
    public ICommand LoadSelectedCommand { get; }

    public string SearchText { get => searchText; set { searchText = value ?? ""; Raise(nameof(SearchText)); RefreshTiles(); } }
    public Choice SelectedCategory { get => selectedCategory; set { selectedCategory = value ?? All; Raise(nameof(SelectedCategory)); RefreshTiles(); } }
    public Choice SelectedSource { get => selectedSource; set { selectedSource = value ?? All; Raise(nameof(SelectedSource)); RefreshTiles(); } }
    public string Status { get => status; set { status = value; Raise(nameof(Status)); } }
    public string ErrorDetails { get => errorDetails; private set { errorDetails = value; Raise(nameof(ErrorDetails)); } }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void Raise(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    /// <summary>Rebuilds everything from the cached index files; needs no Revit API.</summary>
    public void Reload()
    {
        var files = SourceDiscovery.FindProjects(sourceList.Load());
        var indexes = files.Select(store.Load).OfType<SourceIndex>().ToList();
        entries = indexes.SelectMany(i => i.Families).ToList();

        var errors = indexes.Where(i => i.Error != null).Select(i => $"{Path.GetFileName(i.SourcePath)}: {i.Error}").ToList();
        ErrorDetails = string.Join(Environment.NewLine, errors);
        totalFiles = files.Count;
        failedFiles = errors.Count;

        refreshing = true;
        try
        {
            Replace(Categories, FamilyFilter.Categories(entries).Select(c => new Choice(c, c)), SelectedCategory, v => SelectedCategory = v);
            Replace(Sources,
                entries.Select(e => e.SourcePath).Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                    .Select(p => new Choice(Path.GetFileNameWithoutExtension(p), p)),
                SelectedSource, v => SelectedSource = v);
        }
        finally { refreshing = false; }
        RefreshTiles();
    }

    private string Summary() => $"{entries.Count} family dari {totalFiles} proyek; {failedFiles} gagal";

    private static void Replace(ObservableCollection<Choice> target, IEnumerable<Choice> items, Choice current, Action<Choice> setCurrent)
    {
        target.Clear();
        target.Add(All);
        foreach (var i in items) target.Add(i);
        setCurrent(target.FirstOrDefault(c => c.Value == current.Value) ?? All);
    }

    private void RefreshTiles()
    {
        if (refreshing) return;
        var keep = Tiles.Where(t => t.IsSelected).Select(t => (t.Entry.SourcePath, t.Entry.ElementId)).ToHashSet();
        Tiles.Clear();
        foreach (var e in FamilyFilter.Apply(entries, new FamilyQuery(SearchText, SelectedCategory.Value, SelectedSource.Value)))
            Tiles.Add(new FamilyTile(e) { IsSelected = keep.Contains((e.SourcePath, e.ElementId)) });
    }

    /// <summary>Scans stale sources (all of them when forced), one queued job per file so the status can repaint.</summary>
    public void Rescan(bool force)
    {
        var files = SourceDiscovery.FindProjects(sourceList.Load());
        var pending = files.Where(f => force || !store.IsCurrent(f)).ToList();
        if (pending.Count == 0)
        {
            Reload();
            Status = Summary();
            return;
        }

        var done = 0;
        foreach (var path in pending)
        {
            enqueue(uiapp =>
            {
                Status = $"Memindai {done + 1}/{pending.Count}: {Path.GetFileName(path)}";
                ScanService.ScanSource(uiapp.Application, store, path);
                done++;
                Reload();
                Status = done == pending.Count ? Summary() : $"Memindai {done}/{pending.Count}";
            });
        }
    }

    public void ReportFailure(Exception ex) => Status = "Gagal: " + ex.Message;

    private void AddFiles()
    {
        var dlg = new OpenFileDialog { Filter = "Revit Project (*.rvt)|*.rvt", Multiselect = true };
        if (dlg.ShowDialog() == true) AddRoots(dlg.FileNames);
    }

    private void AddFolder()
    {
        var dlg = new OpenFolderDialog();
        if (dlg.ShowDialog() == true) AddRoots(new[] { dlg.FolderName });
    }

    public void AddRoots(IEnumerable<string> roots)
    {
        var merged = sourceList.Load().Concat(roots).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        sourceList.Save(merged);
        Rescan(force: false);
    }

    private void LoadSelected()
    {
        var picked = Tiles.Where(t => t.IsSelected).Select(t => t.Entry).ToList();
        if (picked.Count == 0) { Status = "Pilih family terlebih dahulu"; return; }

        enqueue(uiapp =>
        {
            var doc = uiapp.ActiveUIDocument?.Document;
            if (doc is null || doc.IsFamilyDocument) { Status = "Buka dokumen proyek terlebih dahulu"; return; }

            var r = FamilyImporter.Import(uiapp.Application, doc, picked);
            Status = $"Dimuat {r.Loaded.Count}, dilewati {r.Skipped.Count}, gagal {r.Failed.Count}";
            if (r.Skipped.Count + r.Failed.Count > 0)
                TaskDialog.Show("Family Browser", string.Join(Environment.NewLine, r.Skipped.Concat(r.Failed)));
        });
    }
}
