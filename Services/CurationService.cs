using WingetCurator.Models;

namespace WingetCurator.Services;

/// <summary>
/// Bridges the winget export tree and the flat list of selectable <see cref="CurationItem"/>
/// used by the interactive prompts, and rebuilds a filtered export file from a selection.
/// </summary>
public static class CurationService
{
    public static List<CurationItem> Flatten(WingetPackageFile export)
    {
        var items = new List<CurationItem>();
        foreach (var source in export.Sources)
        {
            var sourceName = source.SourceDetails.Name ?? "Unknown source";
            foreach (var pkg in source.Packages)
            {
                items.Add(new CurationItem { SourceName = sourceName, Package = pkg });
            }
        }
        return items.OrderBy(i => i.SourceName, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(i => i.Package.PackageIdentifier, StringComparer.OrdinalIgnoreCase)
                     .ToList();
    }

    /// <summary>
    /// Rebuilds a winget-import-compatible file containing only the kept items, preserving
    /// the original SourceDetails for each source group.
    /// </summary>
    public static WingetPackageFile BuildCuratedFile(WingetPackageFile originalExport, List<CurationItem> items)
    {
        var kept = items.Where(i => i.Keep).ToList();
        var result = new WingetPackageFile
        {
            WinGetVersion = originalExport.WinGetVersion,
            CreationDate = DateTimeOffset.Now.ToString("O"),
        };

        foreach (var source in originalExport.Sources)
        {
            var sourceName = source.SourceDetails.Name ?? "Unknown source";
            var keptForSource = kept
                .Where(i => string.Equals(i.SourceName, sourceName, StringComparison.OrdinalIgnoreCase))
                .Select(i => i.Package)
                .ToList();

            if (keptForSource.Count > 0)
            {
                result.Sources.Add(new WingetSource
                {
                    SourceDetails = source.SourceDetails,
                    Packages = keptForSource,
                });
            }
        }

        return result;
    }

    public static CuratedCategoriesFile BuildCategoriesFile(List<CurationItem> items)
    {
        var file = new CuratedCategoriesFile();
        foreach (var item in items.Where(i => i.Keep))
        {
            file.Categories[item.Package.PackageIdentifier] = item.Category.ToString();
        }
        return file;
    }

    /// <summary>
    /// Finds apps `winget list` reports locally that have no matching PackageIdentifier in
    /// the export (i.e. winget cannot manage/reinstall them via `winget import`), so the user
    /// gets a reminder to reinstall them manually.
    /// </summary>
    public static List<InstalledApp> FindUnmanagedApps(List<InstalledApp> installed, WingetPackageFile export)
    {
        var exportedIds = new HashSet<string>(
            export.Sources.SelectMany(s => s.Packages).Select(p => p.PackageIdentifier),
            StringComparer.OrdinalIgnoreCase);

        return installed
            .Where(app => string.IsNullOrWhiteSpace(app.Id)
                          || string.IsNullOrWhiteSpace(app.Source)
                          || !exportedIds.Contains(app.Id))
            .ToList();
    }

    /// <summary>
    /// Applies a previously saved curated file + categories sidecar onto a freshly flattened
    /// full item list, so the user can re-edit a prior selection instead of starting over.
    /// Items not present in the prior curated file default to Keep=false (since they weren't
    /// part of the last curated set) unless they simply weren't installed before.
    /// </summary>
    public static void ApplyPriorSelection(List<CurationItem> freshItems, WingetPackageFile priorCurated, CuratedCategoriesFile? priorCategories)
    {
        var priorIds = new HashSet<string>(
            priorCurated.Sources.SelectMany(s => s.Packages).Select(p => p.PackageIdentifier),
            StringComparer.OrdinalIgnoreCase);

        foreach (var item in freshItems)
        {
            item.Keep = priorIds.Contains(item.Package.PackageIdentifier);

            if (priorCategories is not null &&
                priorCategories.Categories.TryGetValue(item.Package.PackageIdentifier, out var categoryName) &&
                Enum.TryParse<AppCategory>(categoryName, ignoreCase: true, out var category))
            {
                item.Category = category;
            }
        }
    }
}
