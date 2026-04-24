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

        BuildDateText.Text = $"Byggd: {GetBuildDate(assembly):yyyy-MM-dd}";
    }

    private static DateTime GetBuildDate(Assembly assembly)
    {
        try
        {
            var location = assembly.Location;
            if (!string.IsNullOrEmpty(location) && File.Exists(location))
                return File.GetLastWriteTime(location);
        }
        catch
        {
            // fall through
        }
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
