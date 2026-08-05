using WingetCurator.Models;

namespace WingetCurator.Services;

/// <summary>
/// Parses the fixed-width table text produced by `winget list`. Winget does not offer a
/// JSON output mode for `list`, so column boundaries are derived from the header row
/// (the position where each column name starts) and then used to slice every data row.
/// </summary>
public static class WingetListParser
{
    public static List<InstalledApp> Parse(string stdout)
    {
        var lines = stdout.Replace("\r\n", "\n").Split('\n');

        // Find the header line (contains "Name" and "Id" columns) and the separator line
        // made of dashes that follows it.
        int headerIndex = Array.FindIndex(lines, l => l.TrimStart().StartsWith("Name", StringComparison.Ordinal) && l.Contains("Id"));
        if (headerIndex < 0 || headerIndex + 1 >= lines.Length)
        {
            return new List<InstalledApp>();
        }

        var header = lines[headerIndex];
        var separator = lines[headerIndex + 1];
        if (!separator.Contains('-'))
        {
            return new List<InstalledApp>();
        }

        var columns = new[] { "Name", "Id", "Version", "Available", "Source" };
        var starts = new List<(string Name, int Start)>();
        foreach (var col in columns)
        {
            var idx = header.IndexOf(col, StringComparison.Ordinal);
            if (idx >= 0)
            {
                starts.Add((col, idx));
            }
        }
        starts.Sort((a, b) => a.Start.CompareTo(b.Start));

        var results = new List<InstalledApp>();

        for (int i = headerIndex + 2; i < lines.Length; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }
            // Winget prints trailing summary lines like "N upgrades available." - stop there.
            if (!starts.Any(s => s.Start < line.Length) && line.TrimStart().Length > 0 && !line.Contains(' '))
            {
                break;
            }

            var values = new Dictionary<string, string>();
            for (int c = 0; c < starts.Count; c++)
            {
                var (name, start) = starts[c];
                if (start >= line.Length)
                {
                    values[name] = string.Empty;
                    continue;
                }
                var end = c + 1 < starts.Count ? Math.Min(starts[c + 1].Start, line.Length) : line.Length;
                values[name] = line.Substring(start, Math.Max(0, end - start)).Trim();
            }

            if (!values.TryGetValue("Name", out var name2) || string.IsNullOrWhiteSpace(name2))
            {
                continue;
            }

            results.Add(new InstalledApp(
                Name: name2,
                Id: values.GetValueOrDefault("Id"),
                Version: values.GetValueOrDefault("Version"),
                Source: values.GetValueOrDefault("Source")));
        }

        return results;
    }
}
