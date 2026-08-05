using System.Text.Json.Serialization;

namespace WingetCurator.Models;

/// <summary>
/// Sidecar file mapping PackageIdentifier -> category (Daily/Occasional).
/// Purely for the user's own reference; winget import ignores this file.
/// </summary>
public sealed class CuratedCategoriesFile
{
    [JsonPropertyName("GeneratedDate")]
    public string? GeneratedDate { get; set; }

    [JsonPropertyName("Categories")]
    public Dictionary<string, string> Categories { get; set; } = new();
}
