using System.Diagnostics;
using System.Text;

namespace BlogPublisher.WinForms;

internal static class PublisherRunner
{
    public static async Task<(bool Success, string Output)> PublishAsync(string repositoryRoot, string sourcePath)
    {
        var scriptPath = Path.Combine(repositoryRoot, "tools", "publish-post.ps1");
        var startInfo = new ProcessStartInfo
        {
            FileName = Path.Combine(Environment.SystemDirectory, "WindowsPowerShell", "v1.0", "powershell.exe"),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = repositoryRoot
        };

        startInfo.ArgumentList.Add("-NoProfile");
        startInfo.ArgumentList.Add("-ExecutionPolicy");
        startInfo.ArgumentList.Add("Bypass");
        startInfo.ArgumentList.Add("-File");
        startInfo.ArgumentList.Add(scriptPath);
        startInfo.ArgumentList.Add("-Source");
        startInfo.ArgumentList.Add(sourcePath);
        startInfo.ArgumentList.Add("-RepositoryRoot");
        startInfo.ArgumentList.Add(repositoryRoot);

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Unable to start PowerShell.");
        var standardOutput = process.StandardOutput.ReadToEndAsync();
        var standardError = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        var output = new StringBuilder();
        output.Append(await standardOutput);
        var error = await standardError;
        if (!string.IsNullOrWhiteSpace(error))
        {
            output.AppendLine();
            output.Append(error);
        }

        return (process.ExitCode == 0, output.ToString().Trim());
    }
}
