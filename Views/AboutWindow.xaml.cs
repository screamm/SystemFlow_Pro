using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Navigation;

namespace SystemMonitorApp.Views;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();

        var assembly = Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version?.ToString() ?? "unknown";
        VersionText.Text = $"v{version}";

        BuildDateText.Text = $"Built: {GetBuildDate(assembly):yyyy-MM-dd}";
    }

    private static DateTime GetBuildDate(Assembly assembly)
    {
        // In single-file publish Assembly.Location is empty. Use AppContext.BaseDirectory
        // and probe for the running executable there instead.
        try
        {
            var baseDir = AppContext.BaseDirectory;
            if (!string.IsNullOrEmpty(baseDir))
            {
                var exe = Path.Combine(baseDir, "SystemFlow-Pro.exe");
                if (File.Exists(exe))
                    return File.GetLastWriteTime(exe);
            }
        }
        catch { /* fall through */ }

        // Fallback: attempt legacy Location (works in non-single-file debug builds).
        // IL3000 warning suppressed: we explicitly handle empty-Location case.
#pragma warning disable IL3000
        try
        {
            var location = assembly.Location;
            if (!string.IsNullOrEmpty(location) && File.Exists(location))
                return File.GetLastWriteTime(location);
        }
        catch { /* fall through */ }
#pragma warning restore IL3000

        return DateTime.Now;
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true
            });
        }
        catch
        {
            // ignore launch failures silently — About dialog must not crash
        }
        e.Handled = true;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
