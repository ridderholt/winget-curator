using Spectre.Console;
using WingetCurator.Models;

namespace WingetCurator.Ui;

/// <summary>
/// Interactive prompts for curating the winget export: keep/remove selection.
/// </summary>
public static class CurationPrompts
{
    /// <summary>
    /// Shows a searchable, source-grouped multi-select with everything pre-checked.
    /// Unchecking an item marks it for removal from the curated import file.
    /// </summary>
    public static void PromptKeepOrRemove(List<CurationItem> items)
    {
        if (items.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No packages found in the export - nothing to curate.[/]");
            return;
        }

        var prompt = new MultiSelectionPrompt<CurationItem>()
            .Title("Uncheck anything you [red]do not[/] want to reinstall. Everything starts checked.")
            .Required(false)
            .PageSize(20)
            .MoreChoicesText("[grey](Move up/down to reveal more apps, <space> to toggle, <enter> to confirm)[/]")
            .InstructionsText("[grey](Press <space> to toggle an app, <enter> to accept)[/]")
            .UseConverter(i => i.DisplayLabel);

        foreach (var group in items.GroupBy(i => i.SourceName).OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase))
        {
            prompt.AddChoiceGroup(
                new CurationItem { SourceName = group.Key, Package = new WingetPackage { PackageIdentifier = $"[bold]{group.Key}[/]" } },
                group);
        }

        // Pre-check everything by default.
        foreach (var item in items)
        {
            prompt.Select(item);
        }

        var selected = AnsiConsole.Prompt(prompt);
        var selectedSet = new HashSet<CurationItem>(selected);

        foreach (var item in items)
        {
            item.Keep = selectedSet.Contains(item);
        }

        var keptCount = items.Count(i => i.Keep);
        AnsiConsole.MarkupLine($"[green]Keeping {keptCount} of {items.Count} apps.[/]");
    }

    public static void ShowUnmanagedApps(List<InstalledApp> unmanaged)
    {
        if (unmanaged.Count == 0)
        {
            return;
        }

        AnsiConsole.MarkupLine("[yellow]The following apps were detected locally but can't be captured by `winget import` " +
                                "(no matching source package). You'll need to reinstall these manually:[/]");

        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn("Name");
        table.AddColumn("Id");
        table.AddColumn("Version");

        foreach (var app in unmanaged.OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase))
        {
            table.AddRow(app.Name, app.Id ?? "-", app.Version ?? "-");
        }

        AnsiConsole.Write(table);
    }
}
