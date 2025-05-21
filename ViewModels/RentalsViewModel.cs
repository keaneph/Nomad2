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
    public class RentalsViewModel : BaseViewModel, ISearchable
    {
        private readonly IRentalService _rentalService;
        private readonly ICustomerService _customerService;
        private readonly IBikeService _bikeService;

        // Collections
        private ObservableCollection<Rental> _rentals;
        private ObservableCollection<SortOption<RentalSortOption>> _availableSortOptions;

        // Selected items and current states
        private Rental _selectedRental;
        private SortOption<RentalSortOption> _currentSortOption;
        private string _searchText;
        private int _currentPage = 1;
        private int _totalPages;
        private bool _isAscending = true;

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

            // Initialize sort options
            AvailableSortOptions = new ObservableCollection<SortOption<RentalSortOption>>
            {
                new SortOption<RentalSortOption> { DisplayName = "ID", Option = RentalSortOption.RentalId },
                new SortOption<RentalSortOption> { DisplayName = "Customer", Option = RentalSortOption.CustomerName },
                new SortOption<RentalSortOption> { DisplayName = "Bike", Option = RentalSortOption.BikeModel },
                new SortOption<RentalSortOption> { DisplayName = "Date", Option = RentalSortOption.RentalDate },
                new SortOption<RentalSortOption> { DisplayName = "Status", Option = RentalSortOption.RentalStatus }
            };

            CurrentSortOption = AvailableSortOptions[0];

            // Initialize commands
            CreateRentalCommand = new RelayCommand(ExecuteCreateRental);
            CompleteRentalCommand = new RelayCommand<Rental>(ExecuteCompleteRental);
            DeleteRentalCommand = new RelayCommand<Rental>(ExecuteDeleteRental);
            NextPageCommand = new RelayCommand(ExecuteNextPage, CanExecuteNextPage);
            PreviousPageCommand = new RelayCommand(ExecutePreviousPage, CanExecutePreviousPage);
            ToggleSortDirectionCommand = new RelayCommand(() => IsAscending = !IsAscending);

            // Load initial data
            _ = LoadRentals();
        }

        #region Properties

        public ObservableCollection<Rental> Rentals
        {
            get => _rentals;
            set
            {
                _rentals = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<SortOption<RentalSortOption>> AvailableSortOptions
        {
            get => _availableSortOptions;
            set
            {
                _availableSortOptions = value;
                OnPropertyChanged();
            }
        }

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

        public string CurrentPageDisplay =>
            $"Page {_currentPage} of {_totalPages}";

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
        public ICommand DeleteRentalCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand PreviousPageCommand { get; }
        public ICommand ToggleSortDirectionCommand { get; }

        #endregion

        #region Filtering

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

        public ObservableCollection<string> StatusFilters { get; } = new ObservableCollection<string>
        {
            "All",
            "Active",
            "Completed",
            "Overdue"
        };

        #endregion

        #region Command Methods

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
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading rentals: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanExecuteNextPage()
        {
            return _currentPage < _totalPages;
        }

        private bool CanExecutePreviousPage()
        {
            return _currentPage > 1;
        }

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

        public void UpdateSearch(string searchTerm)
        {
            SearchText = searchTerm;
        }

        #endregion
    }
}