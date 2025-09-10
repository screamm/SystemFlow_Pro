using System.Threading.Tasks;
using System.Windows;

namespace SystemMonitorApp
{
    public partial class App : Application
    {
        private SplashWindow _splashWindow;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Show splash screen immediately
            _splashWindow = new SplashWindow();
            _splashWindow.Show();
            
            // Load main application asynchronously
            Task.Run(async () =>
            {
                // Ensure splash shows for at least 2 seconds
                await Task.Delay(2000);
                
                // Create main window on background thread (non-UI operations only)
                MainWindow mainWindow = null;
                
                // Create the window on UI thread but don't show it yet
                await Dispatcher.InvokeAsync(() =>
                {
                    mainWindow = new MainWindow();
                    MainWindow = mainWindow;
                });
                
                // Let the splash screen continue animating while we do admin check
                bool showAdminMessage = false;
                
                // Check admin status on background thread
                await Task.Run(() =>
                {
                    var principal = new System.Security.Principal.WindowsPrincipal(
                        System.Security.Principal.WindowsIdentity.GetCurrent());
                    showAdminMessage = !principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
                });
                
                // Small delay to ensure smooth transition
                await Task.Delay(300);
                
                // Show main window on UI thread (minimal operation)
                await Dispatcher.InvokeAsync(() => mainWindow.Show());
                
                // Small delay to let main window render
                await Task.Delay(200);
                
                // Close splash screen on UI thread
                await Dispatcher.InvokeAsync(() => _splashWindow.CloseSplash());
                
                // Show admin message after splash is fully closed
                if (showAdminMessage)
                {
                    await Task.Delay(600);
                    await Dispatcher.InvokeAsync(() =>
                    {
                        MessageBox.Show(
                            "För att få tillgång till all hårdvarudata rekommenderas att köra applikationen som administratör.\n\n" +
                            "Vissa fläkt- och temperaturdata kanske inte visas utan administratörsrättigheter.",
                            "Administratörsrättigheter",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    });
                }
            });
        }
    }
} 