using System.Diagnostics;

namespace WingetCurator.Services;

/// <summary>
/// Thin wrapper around invoking the `winget` executable as a subprocess, with
/// consistent error handling (missing binary, non-zero exit codes, timeouts).
/// </summary>
public sealed class WingetProcessRunner
{
    public sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError);

    /// <summary>
    /// Runs `winget export` to the given file path and returns the raw JSON contents.
    /// </summary>
    public async Task<string> ExportAsync(string outputJsonPath, CancellationToken ct = default)
    {
        var args = $"export -o \"{outputJsonPath}\" --accept-source-agreements --include-versions";
        var result = await RunAsync(args, ct).ConfigureAwait(false);

        if (!File.Exists(outputJsonPath))
        {
            throw new WingetException(
                $"`winget export` did not produce an output file. Exit code: {result.ExitCode}.\n" +
                $"stdout: {result.StandardOutput}\nstderr: {result.StandardError}");
        }

        return await File.ReadAllTextAsync(outputJsonPath, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Runs `winget list` and returns the raw stdout table text for parsing.
    /// </summary>
    public async Task<string> ListAsync(CancellationToken ct = default)
    {
        var result = await RunAsync("list --accept-source-agreements", ct).ConfigureAwait(false);

        if (result.ExitCode != 0 && string.IsNullOrWhiteSpace(result.StandardOutput))
        {
            throw new WingetException(
                $"`winget list` failed. Exit code: {result.ExitCode}.\nstderr: {result.StandardError}");
        }

        return result.StandardOutput;
    }

    private static async Task<ProcessResult> RunAsync(string arguments, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "winget",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = new Process { StartInfo = psi };

        try
        {
            process.Start();
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            throw new WingetException(
                "Could not find or start 'winget'. Make sure the App Installer / Windows Package Manager " +
                "is installed and available on PATH.", ex);
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
        var stderrTask = process.StandardError.ReadToEndAsync(ct);

        await process.WaitForExitAsync(ct).ConfigureAwait(false);

        var stdout = await stdoutTask.ConfigureAwait(false);
        var stderr = await stderrTask.ConfigureAwait(false);

        return new ProcessResult(process.ExitCode, stdout, stderr);
    }
}

public sealed class WingetException : Exception
{
    public WingetException(string message) : base(message) { }
    public WingetException(string message, Exception inner) : base(message, inner) { }
}
