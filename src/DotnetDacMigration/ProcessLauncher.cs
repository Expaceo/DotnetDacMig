using System.Diagnostics;

namespace DotnetDacMigration;

/// <summary>
/// helper to launch a program and returns stdout/stderr/exitcode.
/// </summary>
internal static class ProcessLauncher
{
    public static async Task<(string Stdout, string Stderr, int ExitCode)> Run(string fileName, string arguments, string workingDir = null)
    {
        var processInfo = new ProcessStartInfo();
        processInfo.FileName = fileName;
        processInfo.Arguments = arguments;
        processInfo.RedirectStandardOutput = true;
        processInfo.RedirectStandardError = true;
        processInfo.UseShellExecute = false;
        processInfo.WorkingDirectory = workingDir ?? Environment.CurrentDirectory;

        var process = new Process();
        process.StartInfo = processInfo;

        try
        {
            if (process.Start())
            {
                var stdout = await process.StandardOutput.ReadToEndAsync();
                var stderr = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();
                return (stdout, stderr, process.ExitCode);
            }
            else
            {
                throw new InvalidOperationException($"{fileName} not found.");
            }
        }
        finally
        {
            process.Dispose();
        }
    }
}
