using System.Configuration;
using System.Data;
using System.Windows;

namespace FileSystemExplorer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Global exception handling
            this.DispatcherUnhandledException += (sender, args) =>
            {
                MessageBox.Show($"An error occurred: {args.Exception.Message}",
                              "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
            };
        }
    }
}