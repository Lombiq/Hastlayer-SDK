using System;
using System.Windows.Forms;

namespace Hast.Samples.Kpz;

internal static class Program
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(defaultValue: false);

        using var form = new ChartForm();
        Application.Run(form);
    }
}
