using System.Windows;

namespace SystemMonitorApp
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Check if running as administrator
            var principal = new System.Security.Principal.WindowsPrincipal(
                System.Security.Principal.WindowsIdentity.GetCurrent());
            
            if (!principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator))
            {
                MessageBox.Show(
                    "För att få tillgång till all hårdvarudata rekommenderas att köra applikationen som administratör.\n\n" +
                    "Vissa fläkt- och temperaturdata kanske inte visas utan administratörsrättigheter.",
                    "Administratörsrättigheter",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
    }
} 