namespace BlogPublisher.WinForms;

internal static class RepositoryLocator
{
    public static string FindRoot(string applicationDirectory)
    {
        foreach (var startDirectory in new[] { applicationDirectory, Environment.CurrentDirectory })
        {
            var directory = new DirectoryInfo(Path.GetFullPath(startDirectory));
            while (directory is not null)
            {
                if (Directory.Exists(Path.Combine(directory.FullName, "writing")) &&
                    Directory.Exists(Path.Combine(directory.FullName, "_posts")) &&
                    File.Exists(Path.Combine(directory.FullName, "tools", "publish-post.ps1")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }
        }

        throw new InvalidOperationException(
            "Blog repository not found. Put BlogPublisher.exe in the repository root or one of its subfolders. " +
            "The repository must contain writing, _posts, and tools\\publish-post.ps1.");
    }
}
