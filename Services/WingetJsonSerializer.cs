using System.Text.Json;
using WingetCurator.Models;

namespace WingetCurator.Services;

/// <summary>
/// Loads and saves the winget import/export JSON format.
/// </summary>
public static class WingetJsonSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
    };

    public static WingetPackageFile ParseExport(string json)
    {
        var file = JsonSerializer.Deserialize<WingetPackageFile>(json, Options);
        if (file is null)
        {
            throw new InvalidOperationException("Failed to parse winget export JSON: result was null.");
        }
        return file;
    }

    public static async Task<WingetPackageFile> LoadExportFileAsync(string path, CancellationToken ct = default)
    {
        var json = await File.ReadAllTextAsync(path, ct).ConfigureAwait(false);
        return ParseExport(json);
    }

    public static async Task SaveImportFileAsync(WingetPackageFile file, string path, CancellationToken ct = default)
    {
        file.CreationDate ??= DateTimeOffset.Now.ToString("O");
        var json = JsonSerializer.Serialize(file, Options);
        await File.WriteAllTextAsync(path, json, ct).ConfigureAwait(false);
    }

}
