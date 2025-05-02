// ViewModels/MainViewModel.cs
using Nomad2.Services;
using System.Windows.Input;

namespace Nomad2.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly INavigationService _navigationService;
        private BaseViewModel _currentView;
        private string _username = "Keane";
        private string _userRole = "Test";
        private string _searchText;

        public MainViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
            _navigationService.CurrentViewChanged += OnCurrentViewChanged;
            NavigateCommand = new RelayCommand<string>(Navigate);
            Navigate("Dashboard"); // Set default view
        }

        public string Username
        {
            get => _username;
            set
            {
                _username = value;
                OnPropertyChanged();
            }
        }

        public string UserRole
        {
            get => _userRole;
            set
            {
                _userRole = value;
                OnPropertyChanged();
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
            }
        }

        public BaseViewModel CurrentView
        {
            get => _currentView;
            set
            {
                _currentView = value;
                OnPropertyChanged();
            }
        }

        public ICommand NavigateCommand { get; }

        private void Navigate(string destination)
        {
            _navigationService.NavigateTo(destination);
        }

        public void Dispose()
        {
            _navigationService.CurrentViewChanged -= OnCurrentViewChanged;
        }

        private void OnCurrentViewChanged(string viewName)
        {
            CurrentView = viewName switch
            {
                "Dashboard" => new DashboardViewModel(),
                "Customers" => new CustomersViewModel(),
                "Bikes" => new BikesViewModel(),
                "Rentals" => new RentalsViewModel(),
                "Payments" => new PaymentsViewModel(),
                "Returns" => new ReturnsViewModel(),
                "About" => new AboutViewModel(),
                "Settings" => new SettingsViewModel(),
                "Help" => new HelpViewModel(),
                _ => _currentView
            };
        }
    }
}