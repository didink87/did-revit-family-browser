namespace FamilyBrowser.Core;

public sealed record FamilyEntry(
    string SourcePath,
    long ElementId,
    string FamilyName,
    string CategoryName,
    IReadOnlyList<string> TypeNames,
    string? PreviewFile);

public sealed record SourceIndex(
    string SourcePath,
    DateTime LastWriteUtc,
    long Length,
    IReadOnlyList<FamilyEntry> Families,
    string? Error);
