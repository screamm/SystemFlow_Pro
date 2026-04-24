using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using SystemMonitorApp.Services;

namespace SystemMonitorApp
{
    public partial class App : Application
    {
        private const int MinSplashMs = 800;

        private SplashWindow? _splashWindow;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            RegisterGlobalExceptionHandlers();
            Logger.Info($"SystemFlow Pro starting. Version={GetAssemblyVersion()}, OS={Environment.OSVersion}, .NET={Environment.Version}");
            _ = SettingsService.Current; // trigger static init so settings load before any consumers

            _splashWindow = new SplashWindow();
            _splashWindow.Show();

            _ = StartMainWindowAsync();
        }

        private async Task StartMainWindowAsync()
        {
            var splashStarted = DateTime.UtcNow;
            MainWindow? mainWindow = null;
            bool startupFailed = false;

            try
            {
                // Create window on UI thread (minimal — constructor does nothing heavy).
                await Dispatcher.InvokeAsync(() =>
                {
                    mainWindow = new MainWindow();
                    MainWindow = mainWindow;
                });

                if (mainWindow == null)
                    throw new InvalidOperationException("MainWindow creation returned null");

                // Heavy init happens on background thread now — splash can stay interactive.
                await mainWindow.InitializeAsync();

                // Ensure splash is visible at least MinSplashMs so it doesn't flash.
                var elapsed = (DateTime.UtcNow - splashStarted).TotalMilliseconds;
                if (elapsed < MinSplashMs)
                    await Task.Delay(MinSplashMs - (int)elapsed);

                await Dispatcher.InvokeAsync(() =>
                {
                    if (SettingsService.Current.StartMinimized)
                        mainWindow.WindowState = WindowState.Minimized;
                    mainWindow.Show();
                });

                // Tiny settle time so the main window renders before splash fades.
                await Task.Delay(150);

                Logger.Info($"Main window shown successfully (total startup {((DateTime.UtcNow - splashStarted).TotalMilliseconds):F0}ms)");

                // Fire-and-forget update check. Non-blocking; failures are silent.
                _ = CheckForUpdatesInBackground();
            }
            catch (Exception ex)
            {
                startupFailed = true;
                Logger.Error("Startup sequence failed", ex);
                await Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show(
                        $"An error occurred during startup of SystemFlow Pro.\n\n{ex.Message}\n\nSee the log in %APPDATA%\\SystemFlow Pro\\logs for details.",
                        "Startup Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                });
            }
            finally
            {
                // Splash MUST close regardless of success/failure — otherwise user sees both.
                try
                {
                    await Dispatcher.InvokeAsync(() => _splashWindow?.CloseSplash());
                }
                catch (Exception ex) { Logger.Warn("Splash close failed", ex); }

                if (startupFailed)
                    await Dispatcher.InvokeAsync(() => Shutdown(1));
            }
        }

        private async Task CheckForUpdatesInBackground()
        {
            try
            {
                // Wait a moment so the user sees the UI before any notification pops.
                await Task.Delay(TimeSpan.FromSeconds(8));
                var info = await UpdateChecker.CheckAsync();
                if (info == null) return;

                await Dispatcher.InvokeAsync(() =>
                {
                    var result = MessageBox.Show(
                        $"New version available: {info.LatestVersion}\n\nDo you want to open the release page?",
                        "SystemFlow Pro — update available",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes && !string.IsNullOrEmpty(info.ReleaseUrl))
                    {
                        try
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = info.ReleaseUrl,
                                UseShellExecute = true
                            });
                        }
                        catch (Exception ex) { Logger.Warn("Open release URL failed", ex); }
                    }
                });
            }
            catch (Exception ex)
            {
                Logger.Info($"Update check background task failed (silent): {ex.Message}");
            }
        }

        private void RegisterGlobalExceptionHandlers()
        {
            DispatcherUnhandledException += (s, e) =>
            {
                Logger.Error("Unhandled UI exception", e.Exception);
                try
                {
                    MessageBox.Show(
                        $"An unexpected error occurred.\n\n{e.Exception.Message}\n\n" +
                        "The error has been saved to the log:\n%APPDATA%\\SystemFlow Pro\\logs",
                        "SystemFlow Pro",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
                catch { }
                e.Handled = true;
            };

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                Logger.Error("Unobserved task exception", e.Exception);
                e.SetObserved();
            };

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                    Logger.Error($"AppDomain unhandled exception (terminating={e.IsTerminating})", ex);
                else
                    Logger.Error($"AppDomain unhandled non-exception (terminating={e.IsTerminating})");
                Logger.Flush();
            };
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Logger.Info($"SystemFlow Pro exiting. Code={e.ApplicationExitCode}");
            Logger.Flush();
            Logger.Shutdown();
            base.OnExit(e);
        }

        private static string GetAssemblyVersion()
        {
            try
            {
                return System.Reflection.Assembly.GetExecutingAssembly()
                    .GetName().Version?.ToString() ?? "unknown";
            }
            catch { return "unknown"; }
        }
    }
}
