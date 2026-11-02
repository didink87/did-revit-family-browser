# Family Browser — Design Spec

**Tanggal:** 2026-11-02
**Permintaan user:** Add-in Revit 2026.2 yang membaca proyek lain untuk mengambil family dari setiap proyek tanpa membuka satu per satu; ada panel dengan preview family, nama, dan kategori family.

## Keputusan

1. **Target:** Revit 2026 (2026.2 memakai API 2026), .NET 8, C# add-in (bukan Dynamo/pyRevit).
2. **Sumber:** user menambahkan file `.rvt` atau folder (dipindai rekursif). Daftar sumber disimpan di `%APPDATA%\FamilyBrowser\sources.json`. File backup Revit (`*.0001.rvt`) dan file non-`.rvt` diabaikan.
3. **Membaca proyek tanpa membuka di UI:** `Application.OpenDocumentFile` di background (tanpa jendela). Model workshared dibuka *detach & preserve worksets* dengan semua workset tertutup; sumber **tidak pernah disimpan** (`Close(false)`). File yang sedang terbuka di sesi Revit dipakai langsung dan tidak ditutup. File dari versi Revit lebih baru dari 2026 dilewati dengan pesan.
4. **Yang diambil:** family loadable yang bisa di-edit (`!IsInPlace && IsEditable`). Per family: nama, kategori, nama-nama type, proyek sumber, preview PNG 128×128 dari type pertama (`ElementType.GetPreviewImage`).
5. **Cache/index:** hasil pindai disimpan di `%LOCALAPPDATA%\FamilyBrowser\index\<hash>.json` + `previews\<hash>\*.png`. File hanya dipindai ulang bila `LastWriteTimeUtc` atau ukuran berubah, atau user menekan "Pindai Ulang". Gagal pindai juga dicatat (dengan pesan error) agar tidak diulang terus.
6. **UI:** Ribbon tab **Family Browser** → panel **Library** → tombol **Browser** yang membuka *dockable pane*. Pane berisi: tombol *Tambah File*, *Tambah Folder*, *Pindai Ulang*; kotak cari (nama family / nama type); filter **Kategori** dan **Proyek**; grid tile (preview, nama family, kategori, nama proyek, jumlah type); pilih banyak; tombol **Muat ke Proyek**; baris status. Bahasa UI: Indonesia.
7. **Mengambil family:** family terpilih dimuat ke dokumen proyek aktif via `EditFamily` → `LoadFamily`. Family dengan nama yang sudah ada di proyek target **dilewati, tidak ditimpa**, dan dilaporkan.
8. **Responsif:** pemindaian berjalan satu file per `ExternalEvent` sehingga status di pane ter-update antar file.

## Di luar lingkup
Family in-place, system family (wall/floor type), family dokumen `.rfa` lepas, menimpa family yang sudah ada, multi-versi Revit.
