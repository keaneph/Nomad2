// ViewModels/MainViewModel.cs
using Nomad2.Services;
using System.Windows.Input;

namespace Nomad2.ViewModels
{
    // handles navigation and user information
    public class MainViewModel : BaseViewModel
    {
        // service for handling navigation between different views, calls the interface
        private readonly INavigationService _navigationService;

        // holds the currently displayed view model
        private BaseViewModel _currentView;

        // default values still doesnt know how to implement this
        //FIXME: future implementation
        private string _username = "Keane";
        private string _userRole = "Test";

        // search text for the search bar
        //FIXME: searchbox full implementation
        private string _searchText;

        // constructor initializes navigation and sets up initial view
        public MainViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
            // subscribe to navigation service's view change event
            _navigationService.CurrentViewChanged += OnCurrentViewChanged;

            // initialize navigation command
            NavigateCommand = new RelayCommand<string>(Execute_Navigate);


            // sets dashboard as the default view
            Execute_Navigate("Dashboard"); 
        }


        // not yet implemented: username, userrole, and searchtext.
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
                // [ropagate search to current view
                PropagateSearch();
            }
        }

        // method to propagate search text to the current view
        private void PropagateSearch()
        {
            // cast the CurrentView to ISearchable if it implements the interface
            if (CurrentView is ISearchable searchableView)
            {
                searchableView.UpdateSearch(SearchText);
            }
        }

        // property for current view with change notifications
        public BaseViewModel CurrentView
        {
            get => _currentView;
            set
            {
                _currentView = value;
                OnPropertyChanged();
            }
        }

        // command to handle the navigation of views
        public ICommand NavigateCommand { get; }


        // executes navigation to specified destination
        private void Execute_Navigate(string destination)
        {
            if (destination != null)
            {
                _navigationService.NavigateTo(destination);
            }
        }


        // wanted to clean up any resources or event handlers when the view model is disposed
        // doesnt know how to implement this yet
        // got this from an example and a tip
        public void Dispose()
        {
            _navigationService.CurrentViewChanged -= OnCurrentViewChanged;
        }

        // event handler for view changes, creates appropriate view model based on destination
        private void OnCurrentViewChanged(string viewName)
        {
            var previousView = CurrentView;
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

            // Propagate current search term to new view if it's searchable
            if (!string.IsNullOrEmpty(SearchText) && CurrentView is ISearchable searchable)
            {
                searchable.UpdateSearch(SearchText);
            }
        }


    }
}