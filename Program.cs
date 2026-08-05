using Spectre.Console;
using WingetCurator.Models;
using WingetCurator.Services;
using WingetCurator.Ui;

AnsiConsole.Write(new FigletText("Winget Curator").Color(Color.Green));
AnsiConsole.MarkupLine("[grey]Curate your installed apps before a laptop reinstall.[/]\n");

string? reloadPath = GetArgValue(args, "--reload");
string outputDir = GetArgValue(args, "--output") ?? Path.Combine(Directory.GetCurrentDirectory(), "winget-curator-output");
bool dryRun = args.Contains("--dry-run", StringComparer.OrdinalIgnoreCase);
Directory.CreateDirectory(outputDir);

var runner = new WingetProcessRunner();

WingetPackageFile export;
List<InstalledApp> installed;

try
{
    export = await AnsiConsole.Status()
        .StartAsync("Running [green]winget export[/]...", async _ =>
        {
            var tempExportPath = Path.Combine(Path.GetTempPath(), $"winget-export-{Guid.NewGuid():N}.json");
            try
            {
                var json = await runner.ExportAsync(tempExportPath);
                return WingetJsonSerializer.ParseExport(json);
            }
            finally
            {
                if (File.Exists(tempExportPath))
                {
                    File.Delete(tempExportPath);
                }
            }
        });

    installed = await AnsiConsole.Status()
        .StartAsync("Running [green]winget list[/]...", async _ =>
        {
            var stdout = await runner.ListAsync();
            return WingetListParser.Parse(stdout);
        });
}
catch (WingetException ex)
{
    AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
    return 1;
}

var items = CurationService.Flatten(export);
AnsiConsole.MarkupLine($"[grey]Found {items.Count} winget-manageable packages across {export.Sources.Count} source(s).[/]\n");

if (reloadPath is not null)
{
    if (!File.Exists(reloadPath))
    {
        AnsiConsole.MarkupLine($"[red]--reload path not found:[/] {Markup.Escape(reloadPath)}");
        return 1;
    }

    var priorCurated = await WingetJsonSerializer.LoadExportFileAsync(reloadPath);
    var categoriesPath = Path.Combine(Path.GetDirectoryName(reloadPath) ?? ".", "curated-categories.json");
    var priorCategories = await WingetJsonSerializer.LoadCategoriesFileAsync(categoriesPath);

    CurationService.ApplyPriorSelection(items, priorCurated, priorCategories);
    AnsiConsole.MarkupLine($"[grey]Loaded prior selection from {Markup.Escape(reloadPath)}.[/]\n");
}

if (dryRun)
{
    AnsiConsole.MarkupLine("[grey]--dry-run: skipping interactive prompts, keeping everything as Occasional.[/]\n");
}
else
{
    CurationPrompts.PromptKeepOrRemove(items);
    CurationPrompts.PromptDailyVsOccasional(items);
}

var curatedFile = CurationService.BuildCuratedFile(export, items);
var categoriesFile = CurationService.BuildCategoriesFile(items);
var unmanaged = CurationService.FindUnmanagedApps(installed, export);

var importPath = Path.Combine(outputDir, "curated-import.json");
var categoriesOutPath = Path.Combine(outputDir, "curated-categories.json");
var notesPath = Path.Combine(outputDir, "manual-install-notes.txt");

await WingetJsonSerializer.SaveImportFileAsync(curatedFile, importPath);
await WingetJsonSerializer.SaveCategoriesFileAsync(categoriesFile, categoriesOutPath);

CurationPrompts.ShowUnmanagedApps(unmanaged);
await WriteManualNotesAsync(notesPath, unmanaged);

AnsiConsole.MarkupLine("\n[bold green]Done![/] Files written to:");
AnsiConsole.MarkupLine($"  [blue]{Markup.Escape(importPath)}[/]");
AnsiConsole.MarkupLine($"  [blue]{Markup.Escape(categoriesOutPath)}[/]");
AnsiConsole.MarkupLine($"  [blue]{Markup.Escape(notesPath)}[/]");
AnsiConsole.MarkupLine("\nAfter reinstalling Windows, restore your apps with:");
AnsiConsole.MarkupLine($"  [yellow]winget import -i \"{importPath}\"[/]");
AnsiConsole.MarkupLine("\nTo re-curate later (e.g. before a future reinstall), run:");
AnsiConsole.MarkupLine($"  [yellow]dotnet run -- --reload \"{importPath}\"[/]");

return 0;

static string? GetArgValue(string[] args, string name)
{
    for (int i = 0; i < args.Length - 1; i++)
    {
        if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
        {
            return args[i + 1];
        }
    }
    return null;
}

static async Task WriteManualNotesAsync(string path, List<InstalledApp> unmanaged)
{
    if (unmanaged.Count == 0)
    {
        await File.WriteAllTextAsync(path, "No unmanaged apps detected - everything was captured by winget export.\n");
        return;
    }

    var lines = new List<string>
    {
        "Apps detected locally that winget cannot manage/reinstall via `winget import`.",
        "Reinstall these manually after formatting:",
        string.Empty,
    };

    foreach (var app in unmanaged.OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase))
    {
        var idPart = string.IsNullOrWhiteSpace(app.Id) ? string.Empty : $" (Id: {app.Id})";
        var versionPart = string.IsNullOrWhiteSpace(app.Version) ? string.Empty : $" [version {app.Version}]";
        lines.Add($"- {app.Name}{idPart}{versionPart}");
    }

    await File.WriteAllLinesAsync(path, lines);
}
