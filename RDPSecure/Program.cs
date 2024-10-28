using Microsoft.Extensions.DependencyInjection;
using RDPSecure.Services;
using RDPSecure.Logging;

namespace RDPSecure;

static class Program
{
    [STAThread]
    static void Main()
    {
        try
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Application Error: {ex.Message}\n\nDetails: {ex.StackTrace}",
                "RDPSecure Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}