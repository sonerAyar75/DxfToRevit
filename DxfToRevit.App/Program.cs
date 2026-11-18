namespace DxfToRevit.App;

/// <summary>Entry point of the DxfToRevit desktop application.</summary>
static class Program
{
    /// <summary>Starts the WinForms application with <see cref="MainForm"/>.</summary>
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
