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

            // Create services only when needed for RentalsViewModel
            IRentalService rentalService = null;
            ICustomerService customerService = null;
            IBikeService bikeService = null;

            if (viewName == "Rentals")
            {
                rentalService = new RentalService();
                customerService = new CustomerService();
                bikeService = new BikeService();
            }

            CurrentView = viewName switch
            {
                "Dashboard" => new DashboardViewModel(),
                "Customers" => new CustomersViewModel(),
                "Bikes" => new BikesViewModel(),
                "Rentals" => new RentalsViewModel(rentalService, customerService, bikeService),
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

// to all that will contribteute to this project:

// service classes (BikeService, CustomerService)
// these are classes that handle business logic and data operations
// they act as a middle layer between da UI and database
// example: BikeService handles all bike-related operations like adding/updating/deleting bikes/fetching bike data

// interfaces (IBikeService)
// like a contract that defines what methods a class must implement
// helps with dependency injection and testing
// example: IBikeService ensures any bike service must have methods like GetBikeByIdAsync, AddBikeAsync, etc.


// public async Task<Bike> GetBikeByIdAsync(string id)
// async: tells C# this method can run asynchronously
// Task: Represents work being done in the background
// await: Waits for async operation without blocking the main thread
// good for database operations that might take time
// keeps your app responsive while waiting for data


// string searchTerm = "";
// SortOption<BikeSortOption> sortOption = null;
// allows filtering data based on user input
// flexible sorting options (ascending/descending)
// works across multiple fields (id, name, status, etc.)


// public class StatusToColorConverter : IValueConverter
// transforms data for UI display
// example: Converts status text to colors
// helps with UI presentation

// Create (Add)
// Read (Get)
// Update
// Delete
// Each service implements these basic operations

// var (isValid, errorMessage) = BikeValidator.ValidateBike(bike);
// checks data before database operations
// ensures data integrity
// provides error messages
// this forms a typical modern application structure where:

// UI calls service methods
// services handle business logic
// services communicate with database
// async operations keep everything responsive
// interfaces make code maintainable
// converters help with UI presentation
// validation ensures data quality