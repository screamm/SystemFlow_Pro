using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace SystemMonitorApp
{
    public partial class SplashWindow : Window
    {
        private DispatcherTimer? _loadingTimer;
        private int _loadingStep;
        private readonly string[] _loadingMessages =
        {
            "Initializing hardware monitor...",
            "Loading system sensors...",
            "Configuring hardware detection...",
            "Preparing user interface...",
            "Almost ready..."
        };

        public SplashWindow()
        {
            InitializeComponent();
            SetVersionText();
            StartLoadingSequence();
        }

        private void SetVersionText()
        {
            try
            {
                var version = Assembly.GetExecutingAssembly()
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                    ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString(3);
                if (!string.IsNullOrEmpty(version))
                {
                    // Strip commit hash appended by SourceLink (+abc1234...)
                    int plus = version.IndexOf('+');
                    if (plus > 0) version = version.Substring(0, plus);
                    VersionText.Text = $"v{version}";
                }
            }
            catch { /* default "v1.1.0" in XAML */ }
        }

        private void StartLoadingSequence()
        {
            _loadingTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(800)
            };
            _loadingTimer.Tick += LoadingTimer_Tick;
            _loadingTimer.Start();
        }

        private void LoadingTimer_Tick(object? sender, EventArgs e)
        {
            if (_loadingStep < _loadingMessages.Length - 1)
            {
                _loadingStep++;
                LoadingText.Text = _loadingMessages[_loadingStep];
            }
        }

        public void CloseSplash()
        {
            _loadingTimer?.Stop();

            // Simple fade out — we're already on the UI thread (called via Dispatcher.InvokeAsync from App).
            var fadeOut = new System.Windows.Media.Animation.DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(200),
                FillBehavior = System.Windows.Media.Animation.FillBehavior.Stop
            };
            fadeOut.Completed += (_, _) =>
            {
                try { Close(); }
                catch (InvalidOperationException) { /* already closed */ }
            };
            BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }
    }
}
