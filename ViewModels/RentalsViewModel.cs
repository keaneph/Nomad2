using Nomad2.Models;
using Nomad2.Services;
using Nomad2.Sorting;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using Nomad2.Views;

namespace Nomad2.ViewModels
{
    // view model for managing rental operations and display
    public class RentalsViewModel : BaseViewModel, ISearchable
    {
        // services for data operations
        private readonly IRentalService _rentalService;
        private readonly ICustomerService _customerService;
        private readonly IBikeService _bikeService;

        // observable collections for data binding
        private ObservableCollection<Rental> _rentals;
        private ObservableCollection<SortOption<RentalSortOption>> _availableSortOptions;

        // state tracking fields
        private Rental _selectedRental;
        private SortOption<RentalSortOption> _currentSortOption;
        private string _searchText;
        private int _currentPage = 1;
        private int _totalPages;
        private bool _isAscending = true;

        // initializes rental management with required services
        public RentalsViewModel(
            IRentalService rentalService,
            ICustomerService customerService,
            IBikeService bikeService)
        {
            Title = "Rentals";
            Description = "Manage bike rentals";

            _rentalService = rentalService;
            _customerService = customerService;
            _bikeService = bikeService;

            Rentals = new ObservableCollection<Rental>();

            AvailableSortOptions = new ObservableCollection<SortOption<RentalSortOption>>
            {
                new SortOption<RentalSortOption> { DisplayName = "ID", Option = RentalSortOption.RentalId },
                new SortOption<RentalSortOption> { DisplayName = "Customer", Option = RentalSortOption.CustomerName },
                new SortOption<RentalSortOption> { DisplayName = "Bike", Option = RentalSortOption.BikeModel },
                new SortOption<RentalSortOption> { DisplayName = "Date", Option = RentalSortOption.RentalDate },
                new SortOption<RentalSortOption> { DisplayName = "Status", Option = RentalSortOption.RentalStatus }
            };

            CurrentSortOption = AvailableSortOptions[0];

            CreateRentalCommand = new RelayCommand(ExecuteCreateRental);
            CompleteRentalCommand = new RelayCommand<Rental>(ExecuteCompleteRental);
            DeleteRentalCommand = new RelayCommand<Rental>(ExecuteDeleteRental);
            EditRentalCommand = new RelayCommand<Rental>(ExecuteEditRental);
            NextPageCommand = new RelayCommand(ExecuteNextPage, CanExecuteNextPage);
            PreviousPageCommand = new RelayCommand(ExecutePreviousPage, CanExecutePreviousPage);
            ToggleSortDirectionCommand = new RelayCommand(() => IsAscending = !IsAscending);
            ClearCommand = new RelayCommand(() => ExecuteClear());

            _ = LoadRentals();
        }

        #region Properties

        // collection of rentals for display
        public ObservableCollection<Rental> Rentals
        {
            get => _rentals;
            set
            {
                _rentals = value;
                OnPropertyChanged();
            }
        }

        // available sorting options
        public ObservableCollection<SortOption<RentalSortOption>> AvailableSortOptions
        {
            get => _availableSortOptions;
            set
            {
                _availableSortOptions = value;
                OnPropertyChanged();
            }
        }

        // current sort option with auto-refresh
        public SortOption<RentalSortOption> CurrentSortOption
        {
            get => _currentSortOption;
            set
            {
                _currentSortOption = value;
                OnPropertyChanged();
                LoadRentals();
            }
        }

        // search text with auto-refresh
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                _currentPage = 1;
                LoadRentals();
            }
        }

        // handles rental editing and updates
        private async void ExecuteEditRental(Rental rental)
        {
            if (rental != null)
            {
                // create a clone of the rental for editing
                var rentalToEdit = new Rental
                {
                    RentalId = rental.RentalId,
                    CustomerId = rental.CustomerId,
                    BikeId = rental.BikeId,
                    RentalDate = rental.RentalDate,
                    RentalStatus = rental.RentalStatus,
                    Customer = rental.Customer,
                    Bike = rental.Bike
                };

                // opens dialog for editing
                var dialog = new RentalDialog(rentalToEdit, _customerService, _bikeService, true);
                if (dialog.ShowDialog() == true)
                {
                    try
                    {
                        // update rental in database
                        await _rentalService.UpdateRentalAsync(rentalToEdit);
                        await LoadRentals(); // Refresh the list
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error updating rental: {ex.Message}",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
        // sort direction with auto-refresh
        public bool IsAscending
        {
            get => _isAscending;
            set
            {
                _isAscending = value;
                OnPropertyChanged();
                LoadRentals();
            }
        }

        // current page information
        public string CurrentPageDisplay =>
            $"Page {_currentPage} of {_totalPages}";

        // currently selected rental
        public Rental SelectedRental
        {
            get => _selectedRental;
            set
            {
                _selectedRental = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Commands

        public ICommand CreateRentalCommand { get; }
        public ICommand CompleteRentalCommand { get; }
        public ICommand EditRentalCommand { get; }
        public ICommand DeleteRentalCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand PreviousPageCommand { get; }
        public ICommand ToggleSortDirectionCommand { get; }
        public ICommand ClearCommand { get; }

        #endregion

        #region Filtering

        // current status filter
        private string _selectedStatusFilter = "All";
        public string SelectedStatusFilter
        {
            get => _selectedStatusFilter;
            set
            {
                _selectedStatusFilter = value;
                OnPropertyChanged();
                LoadRentals();
            }
        }

        // available status filter options
        public ObservableCollection<string> StatusFilters { get; } = new ObservableCollection<string>
        {
            "All",
            "Active",
            "Completed",
            "Overdue"
        };

        #endregion

        #region Command Methods

        // creates new rental with validation
        private async void ExecuteCreateRental()
        {
            try
            {
                var newRental = new Rental
                {
                    RentalId = await GenerateNewRentalId(),
                    RentalDate = DateTime.Now,
                    RentalStatus = "Active"
                };

                // Open dialog for new rental
                var dialog = new RentalDialog(newRental, _customerService, _bikeService);
                if (dialog.ShowDialog() == true)
                {
                    // Validate bike availability
                    if (!await _rentalService.IsBikeAvailableForRental(newRental.BikeId))
                    {
                        MessageBox.Show("This bike is currently unavailable.",
                            "Unavailable", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Validate customer eligibility
                    if (!await _rentalService.IsCustomerEligibleForRental(newRental.CustomerId))
                    {
                        MessageBox.Show("This customer has reached their rental limit.",
                            "Ineligible", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    await _rentalService.AddRentalAsync(newRental);
                    await LoadRentals();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating rental: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // marks rental as completed
        private async void ExecuteCompleteRental(Rental rental)
        {
            if (rental != null)
            {
                var result = MessageBox.Show(
                    "Are you sure you want to complete this rental?",
                    "Confirm Completion",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        rental.RentalStatus = "Completed";
                        await _rentalService.UpdateRentalAsync(rental);
                        await LoadRentals();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error completing rental: {ex.Message}",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        // removes rental from database
        private async void ExecuteDeleteRental(Rental rental)
        {
            if (rental != null)
            {
                var result = MessageBox.Show(
                    "Are you sure you want to delete this rental?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        await _rentalService.DeleteRentalAsync(rental.RentalId);
                        await LoadRentals();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting rental: {ex.Message}",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private async void ExecuteClear()
        {
            var result = MessageBox.Show(
                "Are you sure you want to delete ALL rentals? This action cannot be undone.",
                "Confirm Delete All",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    bool success = await _rentalService.ClearAllRentalsAsync();

                    if (success)
                    {
                        Rentals.Clear();
                        _currentPage = 1;
                        _totalPages = 0;
                        OnPropertyChanged(nameof(CurrentPageDisplay));

                        MessageBox.Show("All rentals have been deleted successfully.",
                            "Success",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Failed to delete all rentals.",
                            "Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred: {ex.Message}",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        // navigation 
        private void ExecuteNextPage()
        {
            if (_currentPage < _totalPages)
            {
                _currentPage++;
                LoadRentals();
            }
        }

        private void ExecutePreviousPage()
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                LoadRentals();
            }
        }

        #endregion

        #region Helper Methods

        // loads rentals with current filters and sorting
        private async Task LoadRentals()
        {
            try
            {
                var sortOption = new SortOption<RentalSortOption>
                {
                    Option = CurrentSortOption.Option,
                    IsAscending = IsAscending
                };

                var (rentals, totalCount) = await _rentalService.GetRentalsAsync(
                    _currentPage, SearchText, sortOption);

                // Apply status filter if not "All"
                if (SelectedStatusFilter != "All")
                {
                    rentals = rentals.Where(r => r.RentalStatus == SelectedStatusFilter).ToList();
                }

                Rentals.Clear();
                foreach (var rental in rentals)
                {
                    Rentals.Add(rental);
                }

                _totalPages = (int)Math.Ceiling(totalCount / (double)_rentalService.PageSize);
                OnPropertyChanged(nameof(CurrentPageDisplay));
                
                // raise CanExecuteChanged for pagination commands
                (NextPageCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (PreviousPageCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading rentals: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // checks if next page is available
        private bool CanExecuteNextPage()
        {
            return _currentPage < _totalPages;
        }

        private bool CanExecutePreviousPage()
        {
            return _currentPage > 1;
        }

        // generates unique rental id
        private async Task<string> GenerateNewRentalId()
        {
            string lastId = await _rentalService.GetLastRentalIdAsync();

            if (string.IsNullOrEmpty(lastId))
            {
                return "0000-0001";
            }

            string[] parts = lastId.Split('-');
            if (parts.Length == 2 && int.TryParse(parts[1], out int lastNumber))
            {
                int newNumber = lastNumber + 1;
                return $"0000-{newNumber:D4}";
            }

            return "0000-0001";
        }

        // updates search text and refreshes
        public void UpdateSearch(string searchTerm)
        {
            SearchText = searchTerm;
        }

        // updates the page size and reloads rentals if it changes
        public void UpdatePageSize(int newSize)
        {
            if (_rentalService.PageSize != newSize)
            {
                _rentalService.PageSize = newSize;
                _ = LoadRentals(); // using _ = to handle the async call
            }
        }
    }
   #endregion
}
