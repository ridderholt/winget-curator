# Winget Curator

A small interactive CLI (built with [Spectre.Console](https://spectreconsole.net/)) that helps
you capture and curate the list of apps installed on your Windows machine **before** a
domain-mandated reinstall, so you can restore them in one shot afterwards.

## What it does

1. Runs `winget export` to capture every app winget can manage/reinstall, and `winget list`
   to see everything installed locally (including apps winget can't manage).
2. Shows an interactive, source-grouped checklist (everything pre-checked) so you can uncheck
   anything you don't want to reinstall.
3. Writes out:
   - `curated-import.json` — a valid `winget import` file with just the apps you kept.
   - `manual-install-notes.txt` — apps winget saw installed locally but can't reinstall for
     you (no matching source package), so you don't forget them.

## Usage

Run it any time before a reinstall:

```powershell
cd C:\Privatespace\Repos\winget-curator
dotnet run
```

By default, output files are written to `.\winget-curator-output\` in the current directory.
Use `--output <dir>` to change that. Pass `--dry-run` to skip the interactive prompts and
just export everything (useful for a quick full backup without curating).

Controls in the checklists:
- `Space` toggles the highlighted item (or an entire source group if a group header is
  highlighted).
- `Enter` confirms your selection and moves to the next step.
- Type to filter/search within the list.

### After reinstalling Windows

Install the App Installer (winget) if it isn't already present, then run:

```powershell
winget import -i "C:\Privatespace\Repos\winget-curator\winget-curator-output\curated-import.json"
```

Then go through `manual-install-notes.txt` and install anything listed there by hand
(browser extensions, portable apps, installers winget doesn't know about, etc.).

### Re-curating later

You can re-run the tool before a *future* reinstall and start from your last curated list
instead of from scratch:

```powershell
dotnet run -- --reload "C:\Privatespace\Repos\winget-curator\winget-curator-output\curated-import.json"
```

This pre-applies your previous keep/remove choices onto the fresh export, so you only need to
adjust for what's changed.

## Notes

- `winget export` only includes apps winget can map to a known source (winget, msstore, etc).
  Manually installed software, some portable apps, and browser extensions won't show up there —
  that's what `manual-install-notes.txt` is for.
- Requires the Windows Package Manager (`winget`) to be installed and on `PATH`.
