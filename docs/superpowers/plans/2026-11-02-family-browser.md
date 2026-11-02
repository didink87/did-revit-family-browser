# Family Browser Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add-in Revit 2026 yang memindai banyak file `.rvt` di background, menampilkan family-nya (preview, nama, kategori) di dockable pane, dan memuat family terpilih ke proyek aktif.

**Architecture:** `FamilyBrowser.Core` (net8.0, tanpa referensi Revit) berisi penemuan file sumber, index/cache JSON, dan filter — diuji xunit. `FamilyBrowser.Revit` (net8.0-windows, WPF) berisi pembuka dokumen sumber, scanner, importer, antrean `ExternalEvent`, ribbon dan dockable pane — diuji dengan ricaun.RevitTest pada Revit 2026 yang diluncurkan dan ditutup sendiri oleh test.

**Tech Stack:** C# 12, .NET 8, Nice3point.Revit.Api 2026.*, WPF, System.Text.Json, xunit 2.5.3, NUnit 3.13.3 + ricaun.RevitTest.TestAdapter 1.11.1.

**Spec:** `docs/superpowers/specs/2026-11-02-family-browser-design.md`

## Global Constraints

- Repo: `D:\Library\AI\Plugin Revit\Family Browser`, solution `FamilyBrowser.sln`, namespace root `FamilyBrowser`.
- `Directory.Build.props` sama dengan Library Baja: `LangVersion 12`, `Nullable enable`, `ImplicitUsings enable`, `TreatWarningsAsErrors true`.
- Revit API: `Nice3point.Revit.Api.RevitAPI` / `RevitAPIUI` versi `2026.*`, `ExcludeAssets="runtime"`.
- **Jangan pernah** attach, menutup, atau `Stop-Process` Revit milik user; test Revit memakai metadata `NUnit.Open=true`, `NUnit.Close=true`, `NUnit.Version=2026`.
- Dokumen sumber **tidak pernah disimpan**: selalu `Close(false)`; dokumen yang sudah terbuka di sesi tidak ditutup.
- Lokasi data: sumber `%APPDATA%\FamilyBrowser\sources.json`; index `%LOCALAPPDATA%\FamilyBrowser\index\<sha1>.json`; preview `%LOCALAPPDATA%\FamilyBrowser\previews\<sha1>\<elementId>.png` (sha1 = hex huruf kecil dari full path huruf kecil).
- Teks UI dalam Bahasa Indonesia, persis: tab `Family Browser`, panel `Library`, tombol `Browser`, `Tambah File`, `Tambah Folder`, `Pindai Ulang`, `Muat ke Proyek`, label filter `Kategori`, `Proyek`.
- Kategori kosong ditampilkan sebagai `(Tanpa kategori)`. Ukuran preview 128×128.
- DockablePane GUID: `7E1C5A2B-3F4D-4B8E-9A61-2D0C8F5B9E47`.

## Review Focus

1. **Folder berisi file backup** (`Gedung.0001.rvt`) → tidak ikut dipindai. Test di Task 1.
2. **Sumber sedang terbuka di sesi Revit** (mis. dokumen aktif) → dipakai langsung, tidak dibuka ulang (API akan melempar exception), dan tetap terbuka setelah pindai. Test di Task 4.
3. **Model central workshared** → dibuka detached, worksets tertutup, tidak disimpan; `LastWriteTimeUtc` file central tidak berubah setelah pindai. Test di Task 4.
4. **File rusak / versi lebih baru / terkunci** di tengah antrean → file itu tercatat error, file berikutnya tetap dipindai, index error tidak dipindai ulang sampai file berubah. Test di Task 2 (staleness) dan Task 4 (error terekam).
5. **Family sudah ada di proyek target, atau dua family bernama sama dari dua proyek dipilih bersamaan** → yang kedua dilewati dan dilaporkan, tidak menimpa. Test di Task 5.

---

### Task 1: Scaffold solution + penemuan file sumber

**Files:**
- Create: `Directory.Build.props`, `FamilyBrowser.sln`, `.gitignore` (bin/, obj/, *.user, .vs/)
- Create: `src/FamilyBrowser.Core/FamilyBrowser.Core.csproj` (net8.0)
- Create: `src/FamilyBrowser.Core/SourceDiscovery.cs`
- Create: `tests/FamilyBrowser.Core.Tests/FamilyBrowser.Core.Tests.csproj` (salin paket dari `LibraryBaja.Core.Tests.csproj`)
- Test: `tests/FamilyBrowser.Core.Tests/SourceDiscoveryTests.cs`

**Interfaces:**
- Produces: `public static class SourceDiscovery { public static IReadOnlyList<string> FindProjects(IEnumerable<string> roots); }` — root bisa file atau folder (rekursif). Hasil: full path, distinct case-insensitive, urut `StringComparer.OrdinalIgnoreCase`. Abaikan root yang tidak ada, file non-`.rvt` (case-insensitive), dan backup yang cocok regex `\.\d{4}\.rvt$`.

- [ ] **Step 1:** `git init`, buat props/sln/.gitignore/csproj, `dotnet sln add` kedua proyek.
- [ ] **Step 2: Tulis test gagal** — buat folder temp berisi `A.rvt`, `sub/B.RVT`, `A.0001.rvt`, `C.rfa`, `notes.txt`:

```csharp
[Fact] public void Folder_returns_rvt_recursively_without_backups()
{
    var r = SourceDiscovery.FindProjects(new[] { dir });
    Assert.Equal(new[] { Path.Combine(dir, "A.rvt"), Path.Combine(dir, "sub", "B.RVT") }, r);
}
[Fact] public void File_and_its_folder_given_together_are_deduplicated()
    => Assert.Single(SourceDiscovery.FindProjects(new[] { Path.Combine(dir, "sub", "B.RVT"), Path.Combine(dir, "SUB") }));
[Fact] public void Missing_root_is_ignored()
    => Assert.Empty(SourceDiscovery.FindProjects(new[] { Path.Combine(dir, "nope") }));
[Fact] public void Backup_given_directly_is_ignored()
    => Assert.Empty(SourceDiscovery.FindProjects(new[] { Path.Combine(dir, "A.0001.rvt") }));
```

- [ ] **Step 3:** `dotnet test tests/FamilyBrowser.Core.Tests` → FAIL (`SourceDiscovery` tidak ada).
- [ ] **Step 4:** Implement `SourceDiscovery.FindProjects` (`Directory.EnumerateFiles(..., "*", SearchOption.AllDirectories)` lalu filter ekstensi; `IOException`/`UnauthorizedAccessException` di subfolder dilewati via `EnumerationOptions { IgnoreInaccessible = true, RecurseSubdirectories = true }`).
- [ ] **Step 5:** `dotnet test tests/FamilyBrowser.Core.Tests` → PASS.
- [ ] **Step 6:** `git add -A && git commit -m "feat: scaffold solution and source discovery"`

---

### Task 2: Model data, index store, daftar sumber, cek versi

**Files:**
- Create: `src/FamilyBrowser.Core/FamilyEntry.cs`, `FamilyIndexStore.cs`, `SourceListStore.cs`, `RevitFileVersion.cs`
- Test: `tests/FamilyBrowser.Core.Tests/FamilyIndexStoreTests.cs`, `SourceListStoreTests.cs`, `RevitFileVersionTests.cs`

**Interfaces:**
- Produces:
  - `public sealed record FamilyEntry(string SourcePath, long ElementId, string FamilyName, string CategoryName, IReadOnlyList<string> TypeNames, string? PreviewFile);`
  - `public sealed record SourceIndex(string SourcePath, DateTime LastWriteUtc, long Length, IReadOnlyList<FamilyEntry> Families, string? Error);`
  - `public sealed class FamilyIndexStore(string rootDir)` dengan `void Save(SourceIndex index)`, `SourceIndex? Load(string sourcePath)`, `bool IsCurrent(string sourcePath)`, `string PreviewDir(string sourcePath)` (membuat folder), `static FamilyIndexStore Default()` (pakai `%LOCALAPPDATA%\FamilyBrowser`), `static (DateTime, long) Stamp(string sourcePath)`.
  - `public sealed class SourceListStore(string filePath)` dengan `IReadOnlyList<string> Load()` (file tidak ada/JSON rusak → kosong) dan `void Save(IEnumerable<string> roots)`; `static SourceListStore Default()` (`%APPDATA%\FamilyBrowser\sources.json`).
  - `public static class RevitFileVersion { int? ParseYear(string? format); bool CanOpen(int? fileYear, int hostYear); }` — `ParseYear` mengambil 4 digit pertama `20\d\d` dari string `BasicFileInfo.Format` (contoh `"2026"`, `"Autodesk Revit 2024"`); `CanOpen` = `fileYear is null || fileYear <= hostYear`.

- [ ] **Step 1: Tulis test gagal**

```csharp
// FamilyIndexStoreTests
[Fact] public void Save_then_Load_roundtrips_entries_and_error()        // Families[0].TypeNames & Error sama
[Fact] public void IsCurrent_false_when_no_index()
[Fact] public void IsCurrent_true_after_save_with_current_stamp()      // Save(new SourceIndex(p, stamp.Item1, stamp.Item2, [], null))
[Fact] public void IsCurrent_false_after_file_modified()               // File.SetLastWriteTimeUtc(p, +1 menit)
[Fact] public void IsCurrent_true_for_saved_error_index_until_file_changes() // Error = "rusak"
[Fact] public void Index_file_name_is_case_insensitive_on_path()       // Save dgn "C:\X\A.rvt", Load dgn "c:\x\a.RVT" → not null
// SourceListStoreTests
[Fact] public void Load_missing_file_returns_empty()
[Fact] public void Load_corrupt_json_returns_empty()
[Fact] public void Save_then_Load_preserves_order()
// RevitFileVersionTests
[Theory, InlineData("2026", 2026), InlineData("Autodesk Revit 2024", 2024), InlineData("", null), InlineData(null, null)]
public void ParseYear(string? f, int? y) => Assert.Equal(y, RevitFileVersion.ParseYear(f));
[Theory, InlineData(2027, false), InlineData(2026, true), InlineData(2023, true), InlineData(null, true)]
public void CanOpen_on_2026(int? y, bool ok) => Assert.Equal(ok, RevitFileVersion.CanOpen(y, 2026));
```

- [ ] **Step 2:** `dotnet test tests/FamilyBrowser.Core.Tests` → FAIL (tipe belum ada).
- [ ] **Step 3:** Implement. Nama file index = `SHA1(path.ToLowerInvariant())` hex; JSON via `System.Text.Json` (indented). Tulis atomik: tulis ke `.tmp` lalu `File.Move(overwrite: true)`.
- [ ] **Step 4:** `dotnet test tests/FamilyBrowser.Core.Tests` → PASS.
- [ ] **Step 5:** `git commit -am "feat: family index store, source list and version check"` (sertakan file baru dengan `git add -A`).

---

### Task 3: Filter & daftar kategori

**Files:**
- Create: `src/FamilyBrowser.Core/FamilyFilter.cs`
- Test: `tests/FamilyBrowser.Core.Tests/FamilyFilterTests.cs`

**Interfaces:**
- Consumes: `FamilyEntry` (Task 2).
- Produces:
  - `public sealed record FamilyQuery(string? Text, string? Category, string? SourcePath);`
  - `public static class FamilyFilter { IReadOnlyList<FamilyEntry> Apply(IEnumerable<FamilyEntry> all, FamilyQuery q); IReadOnlyList<string> Categories(IEnumerable<FamilyEntry> all); }`
  - Aturan: `Text` kosong/whitespace = tanpa filter; cocok bila *contains* case-insensitive pada `FamilyName` atau salah satu `TypeNames`. `Category`/`SourcePath` null = semua; `SourcePath` dibandingkan case-insensitive. Urutan hasil: `CategoryName`, lalu `FamilyName`, lalu `SourcePath` (semua `OrdinalIgnoreCase`). `Categories` distinct & terurut.

- [ ] **Step 1: Tulis test gagal** — data: `Pintu Tunggal`(Doors, type "900x2100", A.rvt), `Jendela Geser`(Windows, B.rvt), `pintu ganda`(Doors, B.rvt).

```csharp
[Fact] public void Text_matches_family_name_case_insensitive()
    => Assert.Equal(new[]{"pintu ganda","Pintu Tunggal"}, Names(FamilyFilter.Apply(all, new("PINTU", null, null))));
[Fact] public void Text_matches_type_name()
    => Assert.Equal(new[]{"Pintu Tunggal"}, Names(FamilyFilter.Apply(all, new("2100", null, null))));
[Fact] public void Category_and_source_combine()
    => Assert.Equal(new[]{"pintu ganda"}, Names(FamilyFilter.Apply(all, new(null, "Doors", @"c:\p\b.RVT"))));
[Fact] public void Empty_query_returns_all_sorted_by_category_then_name()
    => Assert.Equal(new[]{"pintu ganda","Pintu Tunggal","Jendela Geser"}, Names(FamilyFilter.Apply(all, new("  ", null, null))));
[Fact] public void Categories_are_distinct_sorted()
    => Assert.Equal(new[]{"Doors","Windows"}, FamilyFilter.Categories(all));
```

- [ ] **Step 2:** `dotnet test` → FAIL.
- [ ] **Step 3:** Implement `FamilyFilter` dengan LINQ.
- [ ] **Step 4:** `dotnet test` → PASS.
- [ ] **Step 5:** `git add -A && git commit -m "feat: family filter"`

---

### Task 4: Membuka sumber di background + scanner family & preview

**Files:**
- Create: `src/FamilyBrowser.Revit/FamilyBrowser.Revit.csproj` (salin pola `LibraryBaja.Revit.csproj`: net8.0-windows, `UseWPF`, `RevitVersion 2026`, referensi Core)
- Create: `src/FamilyBrowser.Revit/SourceDocument.cs`, `FamilyScanner.cs`, `ScanService.cs`
- Create: `tests/FamilyBrowser.Revit.Tests/FamilyBrowser.Revit.Tests.csproj` (salin pola `LibraryBaja.Revit.Tests.csproj` **tanpa** referensi SteelConnections, termasuk metadata `NUnit.Open/Close/Version` dan `RepoRoot`)
- Create: `tests/FamilyBrowser.Revit.Tests/Fixtures.cs`
- Test: `tests/FamilyBrowser.Revit.Tests/ScanTests.cs`

**Interfaces:**
- Consumes: `FamilyEntry`, `SourceIndex`, `FamilyIndexStore`, `RevitFileVersion` (Task 2).
- Produces:
  - `public sealed class SourceOpenException(string message, Exception? inner = null) : Exception`
  - `public sealed class SourceDocument : IDisposable { Document Document { get; } bool OwnsDocument { get; } static SourceDocument Open(Application app, string path); }` — jika ada `app.Documents` dengan `PathName` sama (case-insensitive) → pakai, `OwnsDocument=false`. Jika tidak: `BasicFileInfo.Extract(path)`; `!RevitFileVersion.CanOpen(ParseYear(info.Format), 2026)` → `SourceOpenException("Dibuat dengan Revit {tahun}, lebih baru dari 2026")`. `OpenOptions { Audit = false }`; bila `info.IsWorkshared` → `DetachFromCentralOption = DetachAndPreserveWorksets` dan `SetOpenWorksetsConfiguration(new WorksetConfiguration(WorksetConfigurationOption.CloseAllWorksets))`. `app.OpenDocumentFile(ModelPathUtils.ConvertUserVisiblePathToModelPath(path), opts)`; exception API dibungkus `SourceOpenException(ex.Message, ex)`. `Dispose` → `Document.Close(false)` hanya bila `OwnsDocument`.
  - `public static class FamilyScanner { IReadOnlyList<FamilyEntry> Scan(Document doc, string sourcePath, string previewDir); }` — `FilteredElementCollector(doc).OfClass(typeof(Family))`, ambil `!IsInPlace && IsEditable`; `CategoryName = FamilyCategory?.Name ?? "(Tanpa kategori)"`; `TypeNames` = nama `GetFamilySymbolIds()` terurut; preview dari symbol pertama `GetPreviewImage(new System.Drawing.Size(128,128))` disimpan `previewDir\<Id.Value>.png`, null → `PreviewFile = null`. (Tambahkan `PackageReference System.Drawing.Common 8.0.*` bila `Bitmap.Save` butuh.)
  - `public static class ScanService { SourceIndex ScanSource(Application app, FamilyIndexStore store, string path); }` — stamp diambil **sebelum** membuka; `SourceOpenException`/exception lain → `SourceIndex` dengan `Families=[]`, `Error=pesan`; hasil selalu di-`Save`.

- [ ] **Step 1: Fixture** — `Fixtures.CreateProject(Application app, string path, bool workshared)`: `app.NewProjectDocument(@"C:\ProgramData\Autodesk\RVT 2026\Templates\English\DefaultMetric.rte")` (bila path itu tidak ada, pakai `app.DefaultProjectTemplate`); bila `workshared` → `doc.EnableWorksharing("Shared Levels and Grids", "Workset1")` dan `SaveAs` dengan `WorksharingSaveAsOptions { SaveAsCentral = true }`; tutup. Folder output di `Path.GetTempPath()\FamilyBrowserTests\<guid>`.
- [ ] **Step 2: Tulis test gagal** (`[TestFixture]`, `Application app` via ricaun `[OneTimeSetUp] Setup(UIApplication uiapp)`):

```csharp
[Test] public void Scan_returns_every_editable_loadable_family_with_category_and_preview()
{   // expected = hitung langsung dari doc fixture: Family !IsInPlace && IsEditable
    var idx = ScanService.ScanSource(app, store, projectPath);
    Assert.That(idx.Error, Is.Null);
    Assert.That(idx.Families.Count, Is.EqualTo(expected).And.GreaterThan(0));
    Assert.That(idx.Families.All(f => f.CategoryName != "" && f.TypeNames.Count > 0));
    Assert.That(idx.Families.Count(f => f.PreviewFile != null && File.Exists(f.PreviewFile)), Is.GreaterThan(0));
    Assert.That(app.Documents.Cast<Document>().Any(d => d.PathName.Equals(projectPath, StringComparison.OrdinalIgnoreCase)), Is.False); // ditutup
}
[Test] public void Already_open_document_is_reused_and_left_open()
{   var open = app.OpenDocumentFile(projectPath);
    var idx = ScanService.ScanSource(app, store, projectPath);
    Assert.That(idx.Error, Is.Null); Assert.That(open.IsValidObject, Is.True); open.Close(false); }
[Test] public void Workshared_central_is_scanned_detached_and_not_modified()
{   var before = File.GetLastWriteTimeUtc(centralPath);
    var idx = ScanService.ScanSource(app, store, centralPath);
    Assert.That(idx.Error, Is.Null); Assert.That(idx.Families, Is.Not.Empty);
    Assert.That(File.GetLastWriteTimeUtc(centralPath), Is.EqualTo(before)); }
[Test] public void Corrupt_file_records_error_and_is_current()
{   File.WriteAllText(badPath, "bukan revit"); // badPath = "...\rusak.rvt"
    var idx = ScanService.ScanSource(app, store, badPath);
    Assert.That(idx.Error, Is.Not.Null.And.Not.Empty); Assert.That(store.IsCurrent(badPath), Is.True); }
```

- [ ] **Step 3:** Build tests: `dotnet build tests/FamilyBrowser.Revit.Tests` → FAIL (tipe belum ada).
- [ ] **Step 4:** Implement `SourceDocument`, `FamilyScanner`, `ScanService`.
- [ ] **Step 5:** `dotnet test tests/FamilyBrowser.Revit.Tests --filter ScanTests` (meluncurkan Revit 2026 baru & menutupnya) → 4 PASS.
- [ ] **Step 6:** `git add -A && git commit -m "feat: background source opening and family scanner"`

---

### Task 5: Memuat family terpilih ke proyek aktif

**Files:**
- Create: `src/FamilyBrowser.Revit/FamilyImporter.cs`
- Test: `tests/FamilyBrowser.Revit.Tests/ImportTests.cs`

**Interfaces:**
- Consumes: `SourceDocument.Open` (Task 4), `FamilyEntry` (Task 2), `Fixtures.CreateProject` (Task 4).
- Produces:
  - `public sealed record ImportResult(IReadOnlyList<string> Loaded, IReadOnlyList<string> Skipped, IReadOnlyList<string> Failed);` — `Skipped`/`Failed` berformat `"<FamilyName> — <alasan>"`.
  - `public static class FamilyImporter { ImportResult Import(Application app, Document target, IReadOnlyList<FamilyEntry> entries); }` — `target.IsFamilyDocument` → `ArgumentException("Buka dokumen proyek sebagai target")`. Dikelompokkan per `SourcePath`; nama family yang sudah ada di target (atau sudah dimuat lebih awal di panggilan yang sama) → Skipped `"sudah ada di proyek"`. Family dicari via `ElementId(entry.ElementId)`, fallback nama; tidak ketemu → Failed `"tidak ditemukan di sumber, pindai ulang"`. Sumber gagal dibuka → setiap entry sumber itu Failed dengan pesan `SourceOpenException`. Muat: `var fd = src.EditFamily(fam); fd.LoadFamily(target, new SkipExistingLoadOptions()); fd.Close(false);` dengan `SkipExistingLoadOptions : IFamilyLoadOptions` mengembalikan `false` di kedua callback. Dipanggil di luar transaksi.

- [ ] **Step 1: Tulis test gagal** — fixture: proyek sumber A (template), target = `app.NewProjectDocument(UnitSystem.Metric)` (proyek kosong). Ambil entries via `ScanService.ScanSource`.

```csharp
[Test] public void Selected_families_are_loaded_into_target()
{   var pick = entriesA.Take(2).ToList();
    var r = FamilyImporter.Import(app, target, pick);
    Assert.That(r.Loaded, Is.EquivalentTo(pick.Select(e => e.FamilyName)));
    Assert.That(FamilyNames(target), Is.SupersetOf(pick.Select(e => e.FamilyName))); }
[Test] public void Existing_family_is_skipped_not_overwritten()
{   FamilyImporter.Import(app, target, entriesA.Take(1).ToList());
    var r = FamilyImporter.Import(app, target, entriesA.Take(1).ToList());
    Assert.That(r.Loaded, Is.Empty); Assert.That(r.Skipped.Single(), Does.EndWith("sudah ada di proyek")); }
[Test] public void Same_name_from_two_sources_loads_once()
{   // proyek B = salinan A (File.Copy), dipindai terpisah
    var r = FamilyImporter.Import(app, target, new[]{ entriesA[0], entriesB.First(e => e.FamilyName == entriesA[0].FamilyName) });
    Assert.That(r.Loaded.Count, Is.EqualTo(1)); Assert.That(r.Skipped.Count, Is.EqualTo(1)); }
[Test] public void Missing_source_file_reports_failed()
{   var gone = entriesA[0] with { SourcePath = @"C:\tidak\ada.rvt" };
    Assert.That(FamilyImporter.Import(app, target, new[]{ gone }).Failed, Has.Count.EqualTo(1)); }
[Test] public void Source_file_is_closed_after_import()   // tidak ada app.Documents dgn PathName sumber A
```

- [ ] **Step 2:** `dotnet build tests/FamilyBrowser.Revit.Tests` → FAIL.
- [ ] **Step 3:** Implement `FamilyImporter` + `SkipExistingLoadOptions`.
- [ ] **Step 4:** `dotnet test tests/FamilyBrowser.Revit.Tests --filter ImportTests` → 5 PASS.
- [ ] **Step 5:** `git add -A && git commit -m "feat: load selected families into active project"`

---

### Task 6: Aplikasi Revit — ribbon, antrean ExternalEvent, manifest

**Files:**
- Create: `src/FamilyBrowser.Revit/App.cs`, `RevitTaskQueue.cs`, `ShowBrowserCommand.cs`, `PaneIds.cs`
- Create: `src/FamilyBrowser.Revit/FamilyBrowser.addin`
- Create: `src/FamilyBrowser.Revit/Resources/browser32.png`, `browser16.png` (ikon sederhana, `EmbeddedResource`)
- Modify: `src/FamilyBrowser.Revit/FamilyBrowser.Revit.csproj` — target `AfterBuild` (Debug saja) menyalin `FamilyBrowser.addin` ke `$(AppData)\Autodesk\Revit\Addins\2026\` dengan `<Assembly>` menunjuk ke `$(TargetPath)` (ganti token `__ASSEMBLY__` saat salin).
- Test: `tests/FamilyBrowser.Revit.Tests/TaskQueueTests.cs`

**Interfaces:**
- Consumes: —
- Produces:
  - `public static class PaneIds { public static readonly DockablePaneId Browser = new(new Guid("7E1C5A2B-3F4D-4B8E-9A61-2D0C8F5B9E47")); }`
  - `public sealed class RevitTaskQueue { RevitTaskQueue(); void Enqueue(Action<UIApplication> work); int Pending { get; } event Action<Exception>? Failed; }` — satu `ExternalEvent`; `Execute` menjalankan **satu** item lalu `Raise()` lagi jika masih ada antrean; exception item ditangkap → `Failed`, antrean lanjut. Internal `void RunNext(UIApplication app)` (`InternalsVisibleTo` tests) agar bisa diuji tanpa event.
  - `App : IExternalApplication` — `OnStartup`: `CreateRibbonTab("Family Browser")`, panel `Library`, `PushButtonData("Browser", "Browser", asm, typeof(ShowBrowserCommand).FullName)`; buat `RevitTaskQueue` statis `App.Queue`; `RegisterDockablePane(PaneIds.Browser, "Family Browser", new BrowserPane(...))` (BrowserPane dari Task 7 — di task ini pakai `Page` placeholder kosong yang mengimplementasi `IDockablePaneProvider`, diganti di Task 7).
  - `ShowBrowserCommand : IExternalCommand` → `uiapp.GetDockablePane(PaneIds.Browser).Show()`.

- [ ] **Step 1: Tulis test gagal**

```csharp
[Test] public void RunNext_executes_one_item_in_order()
{   var q = new RevitTaskQueue(); var log = new List<int>();
    q.Enqueue(_ => log.Add(1)); q.Enqueue(_ => log.Add(2));
    q.RunNext(uiapp); Assert.That(log, Is.EqualTo(new[]{1})); Assert.That(q.Pending, Is.EqualTo(1)); }
[Test] public void Failing_item_raises_Failed_and_queue_continues()
{   var q = new RevitTaskQueue(); Exception? seen = null; var ran = false;
    q.Failed += e => seen = e; q.Enqueue(_ => throw new InvalidOperationException("x")); q.Enqueue(_ => ran = true);
    q.RunNext(uiapp); q.RunNext(uiapp);
    Assert.That(seen, Is.TypeOf<InvalidOperationException>()); Assert.That(ran, Is.True); }
```

- [ ] **Step 2:** `dotnet build tests/FamilyBrowser.Revit.Tests` → FAIL.
- [ ] **Step 3:** Implement `RevitTaskQueue`, `PaneIds`, `App`, `ShowBrowserCommand`, manifest (`AddInId` GUID baru, `VendorId` `DIDF`) dan target salin.
- [ ] **Step 4:** `dotnet test tests/FamilyBrowser.Revit.Tests --filter TaskQueueTests` → 2 PASS.
- [ ] **Step 5: Verifikasi manual** — `dotnet build src/FamilyBrowser.Revit -c Debug`; luncurkan **instance Revit 2026 baru** (jangan pakai instance kerja user); tab `Family Browser` → `Library` → `Browser` muncul dan membuka pane kosong. Tutup instance itu.
- [ ] **Step 6:** `git add -A && git commit -m "feat: ribbon, task queue and add-in manifest"`

---

### Task 7: Dockable pane — grid preview, filter, pindai, muat

**Files:**
- Create: `src/FamilyBrowser.Revit/UI/BrowserPane.xaml` + `.xaml.cs` (`Page`, `IDockablePaneProvider`, `DockPosition.Right`)
- Create: `src/FamilyBrowser.Revit/UI/BrowserViewModel.cs`, `UI/FamilyTile.cs`, `UI/RelayCommand.cs`
- Modify: `src/FamilyBrowser.Revit/App.cs` — daftarkan `BrowserPane` asli.

**Interfaces:**
- Consumes: `SourceDiscovery.FindProjects` (T1); `FamilyIndexStore`, `SourceListStore` (T2); `FamilyFilter`, `FamilyQuery` (T3); `ScanService.ScanSource` (T4); `FamilyImporter.Import`, `ImportResult` (T5); `App.Queue.Enqueue` (T6).
- Produces:
  - `public sealed class FamilyTile(FamilyEntry entry)` — `Entry`, `FamilyName`, `CategoryName`, `SourceName` (= `Path.GetFileNameWithoutExtension(SourcePath)`), `TypeCountText` (`"{n} type"`), `ImageSource? Preview` (`BitmapImage` dengan `CacheOption.OnLoad` agar file PNG tidak terkunci; null → template menampilkan nama kategori di kotak abu-abu), `bool IsSelected`.
  - `public sealed class BrowserViewModel : INotifyPropertyChanged` — properti `SearchText`, `SelectedCategory` (item pertama `"Semua"` = null), `SelectedSource` (item pertama `"Semua"`), `ObservableCollection<FamilyTile> Tiles`, `Categories`, `Sources`, `Status`; perintah `AddFilesCommand` (`OpenFileDialog`, filter `Revit Project (*.rvt)|*.rvt`, multi), `AddFolderCommand` (`Microsoft.Win32.OpenFolderDialog` .NET 8), `RescanCommand`, `LoadSelectedCommand`. Perubahan filter → `FamilyFilter.Apply` ulang (tanpa Revit API).
  - Alur pindai: roots dari `SourceListStore` → `SourceDiscovery.FindProjects` → untuk tiap file yang `!store.IsCurrent(path)` (atau semua jika *Pindai Ulang*) enqueue satu job `ScanService.ScanSource`; setelah tiap job, di `Dispatcher` perbarui `Status` = `"Memindai {i}/{n}: {nama file}"` dan tile. Saat pane dibuka, index yang sudah current langsung dimuat tanpa membuka Revit doc. Akhir: `"{jumlah family} family dari {jumlah file} proyek; {e} gagal"` dan tooltip daftar error.
  - Alur muat: entry dari tile terpilih → enqueue `FamilyImporter.Import(app, uiapp.ActiveUIDocument.Document, entries)`; tidak ada dokumen aktif atau target family doc → Status `"Buka dokumen proyek terlebih dahulu"`; hasil → Status `"Dimuat {L}, dilewati {S}, gagal {F}"` + `TaskDialog` berisi rincian Skipped/Failed bila ada.
  - Layout XAML: baris atas tombol `Tambah File`, `Tambah Folder`, `Pindai Ulang`; baris kedua `TextBox` cari + `ComboBox` `Kategori` + `ComboBox` `Proyek`; tengah `ListBox` `SelectionMode=Extended` dengan `ItemsPanel` `WrapPanel` dan `VirtualizingPanel.IsVirtualizing=True`, tile 140 lebar: gambar 128×128, `FamilyName` tebal (wrap), `CategoryName`, `SourceName · TypeCountText` abu-abu; bawah tombol `Muat ke Proyek` + `TextBlock` status.

- [ ] **Step 1:** Implement `FamilyTile`, `RelayCommand`, `BrowserViewModel`, `BrowserPane`; ganti placeholder di `App`.
- [ ] **Step 2:** `dotnet build FamilyBrowser.sln -c Debug` → sukses tanpa warning; `dotnet test tests/FamilyBrowser.Core.Tests` → PASS.
- [ ] **Step 3: Verifikasi manual di instance Revit 2026 baru** (siapkan folder berisi ≥2 proyek, 1 file backup `*.0001.rvt`, 1 file `.rvt` rusak, 1 model central):
  - `Tambah Folder` → status berjalan per file, tile muncul dengan preview, nama, kategori; file backup tidak muncul di filter `Proyek`; file rusak tercatat gagal, pemindaian lanjut.
  - Ketik di kotak cari, ganti `Kategori` dan `Proyek` → grid tersaring.
  - Tutup & buka ulang Revit → pane terisi dari cache tanpa memindai ulang (status langsung final).
  - Pilih 3 tile → `Muat ke Proyek` di proyek aktif → family muncul di Project Browser; ulangi → semua "dilewati".
  - `File.GetLastWriteTimeUtc` model central tidak berubah.
- [ ] **Step 4:** `git add -A && git commit -m "feat: family browser dockable pane"`
