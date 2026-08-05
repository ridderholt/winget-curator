using System.Text.Json.Serialization;

namespace WingetCurator.Models;

/// <summary>
/// Root object matching the schema produced by `winget export` and consumed by `winget import`.
/// See https://aka.ms/winget-packages.schema.2.0.json
/// </summary>
public sealed class WingetPackageFile
{
    [JsonPropertyName("$schema")]
    public string Schema { get; set; } = "https://aka.ms/winget-packages.schema.2.0.json";

    [JsonPropertyName("CreationDate")]
    public string? CreationDate { get; set; }

    [JsonPropertyName("Sources")]
    public List<WingetSource> Sources { get; set; } = new();

    [JsonPropertyName("WinGetVersion")]
    public string? WinGetVersion { get; set; }
}

public sealed class WingetSource
{
    [JsonPropertyName("SourceDetails")]
    public WingetSourceDetails SourceDetails { get; set; } = new();

    [JsonPropertyName("Packages")]
    public List<WingetPackage> Packages { get; set; } = new();
}

public sealed class WingetSourceDetails
{
    [JsonPropertyName("Argument")]
    public string? Argument { get; set; }

    [JsonPropertyName("Identifier")]
    public string? Identifier { get; set; }

    [JsonPropertyName("Name")]
    public string? Name { get; set; }

    [JsonPropertyName("Type")]
    public string? Type { get; set; }
}

public sealed class WingetPackage
{
    [JsonPropertyName("PackageIdentifier")]
    public string PackageIdentifier { get; set; } = string.Empty;

    [JsonPropertyName("Version")]
    public string? Version { get; set; }
}
