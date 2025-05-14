// App.xaml.cs
using Nomad2.Services;
using Nomad2.ViewModels;
using System.Windows;

namespace Nomad2
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // initialize database
            var dbHelper = new DatabaseHelper();
            dbHelper.InitializeDatabase();

            INavigationService navigationService = new NavigationService();
            MainWindow = new MainWindow
            {
                DataContext = new MainViewModel(navigationService)
            };
            MainWindow.Show();
        }
    }
}