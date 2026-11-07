# did-revit-family-browser

An open source Revit family browser, free for all.

Add-in for **Revit 2026** that reads other `.rvt` projects in the background and lists the families inside them,
so you do not have to open each project one by one. A dockable pane shows each family with its preview image,
name, category, source project and number of types. Pick families and load them into the active project.

## Features

- Add single `.rvt` files or whole folders (scanned recursively). Revit backup files (`*.0001.rvt`) are ignored.
- Source projects are opened without a window and never saved. Workshared models are opened detached with all worksets closed.
- Scan results (including preview images) are cached; only changed files are rescanned.
- Search by family or type name, filter by category and by project.
- Load selected families into the active project. Families that already exist are skipped, never overwritten.

## Install

```
dotnet build src/FamilyBrowser.Revit -p:InstallAddin=true
```

This copies `FamilyBrowser.addin` to `%AppData%\Autodesk\Revit\Addins\2026\`. Restart Revit, choose **Always Load**,
then open the **DID** tab -> **DID** panel -> **Browser**. (The DID panel is shared with Sheet Studio and Face View.)

## Build and test

Requires the .NET 8 SDK and Revit 2026 (the Revit tests start their own Revit 2026 instance and close it again).

```
dotnet test FamilyBrowser.sln
```

## Not supported

Standalone `.rfa` files, in-place families and system families (wall/floor types).

## Layout

- `src/FamilyBrowser.Core` - source discovery, index cache, filtering (no Revit dependency)
- `src/FamilyBrowser.Revit` - Revit add-in: scanner, importer, ribbon, dockable pane
- `tests/` - xunit tests for Core, NUnit tests that run inside Revit
- `docs/superpowers/` - design spec and implementation plan
