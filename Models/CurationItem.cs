namespace WingetCurator.Models;

/// <summary>
/// A single winget-manageable package flattened out of the export's source/package tree,
/// used as the unit of selection in the interactive prompts.
/// </summary>
public sealed class CurationItem
{
    public required string SourceName { get; init; }
    public required WingetPackage Package { get; init; }
    public bool Keep { get; set; } = true;

    public string DisplayLabel => string.IsNullOrWhiteSpace(Package.Version)
        ? Package.PackageIdentifier
        : $"{Package.PackageIdentifier} ({Package.Version})";
}
