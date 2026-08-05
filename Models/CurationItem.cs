namespace WingetCurator.Models;

/// <summary>
/// Category tag the user assigns to a kept package purely for their own reference.
/// Not consumed by `winget import` itself.
/// </summary>
public enum AppCategory
{
    Daily,
    Occasional
}

/// <summary>
/// A single winget-manageable package flattened out of the export's source/package tree,
/// used as the unit of selection in the interactive prompts.
/// </summary>
public sealed class CurationItem
{
    public required string SourceName { get; init; }
    public required WingetPackage Package { get; init; }
    public bool Keep { get; set; } = true;
    public AppCategory Category { get; set; } = AppCategory.Occasional;

    public string DisplayLabel => string.IsNullOrWhiteSpace(Package.Version)
        ? Package.PackageIdentifier
        : $"{Package.PackageIdentifier} ({Package.Version})";
}
