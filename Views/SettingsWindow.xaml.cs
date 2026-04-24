using System.Windows;
using SystemMonitorApp.ViewModels;

namespace SystemMonitorApp.Views
{
    /// <summary>
    /// Settings dialog — dark mode, teal accent.
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private readonly SettingsViewModel _vm;

        public SettingsWindow()
        {
            InitializeComponent();
            _vm = new SettingsViewModel();
            DataContext = _vm;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            _vm.Save();
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
