using System;
using Fazor.UI;

namespace RTXLauncher.Fazor;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Run the Fazor-based RTX Launcher
        FazorApplication.RunPanel<MainWindow>();
    }
}
