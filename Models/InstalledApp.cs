namespace WingetCurator.Models;

/// <summary>
/// A row parsed from `winget list` table output. Used to detect apps that winget
/// knows about locally but that `winget export` could not map to an installable
/// source package (no Id, or Source is empty) — these need manual reinstall notes.
/// </summary>
public sealed record InstalledApp(string Name, string? Id, string? Version, string? Source);
