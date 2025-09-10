using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace SystemMonitorApp
{
    public partial class SplashWindow : Window
    {
        private DispatcherTimer _loadingTimer;
        private int _loadingStep = 0;
        private string[] _loadingMessages = new string[]
        {
            "Initialiserar hårdvaruövervakare...",
            "Laddar systemsensorer...",
            "Konfigurerar hårdvarudetektering...",
            "Förbereder användargränssnitt...",
            "Nästan klar..."
        };

        public SplashWindow()
        {
            InitializeComponent();
            StartLoadingSequence();
        }

        private void StartLoadingSequence()
        {
            _loadingTimer = new DispatcherTimer();
            _loadingTimer.Interval = TimeSpan.FromMilliseconds(800);
            _loadingTimer.Tick += LoadingTimer_Tick;
            _loadingTimer.Start();
        }

        private void LoadingTimer_Tick(object sender, EventArgs e)
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
            
            // Use async method to prevent blocking
            Task.Run(async () =>
            {
                // Small delay to ensure main window is fully visible
                await Task.Delay(100);
                
                // Close splash on UI thread with immediate effect
                await Dispatcher.InvokeAsync(() =>
                {
                    // Simple fade out that doesn't block
                    var fadeOut = new System.Windows.Media.Animation.DoubleAnimation
                    {
                        From = 1,
                        To = 0,
                        Duration = TimeSpan.FromMilliseconds(200),
                        FillBehavior = System.Windows.Media.Animation.FillBehavior.Stop
                    };

                    fadeOut.Completed += (s, e) =>
                    {
                        Task.Run(() => Dispatcher.InvokeAsync(() => this.Close()));
                    };
                    
                    this.BeginAnimation(UIElement.OpacityProperty, fadeOut);
                });
            });
        }
    }
}