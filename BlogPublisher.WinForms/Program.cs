namespace BlogPublisher.WinForms;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        try
        {
            var repositoryRoot = RepositoryLocator.FindRoot(AppContext.BaseDirectory);
            Application.Run(new MainForm(repositoryRoot));
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                exception.Message,
                "Blog Publisher",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
