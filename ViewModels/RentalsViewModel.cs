﻿using Nomad2.Models;
using Nomad2.Services;
using Nomad2.Sorting;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using Nomad2.Views;
using System.Linq;

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
            NavigateToCustomerCommand = new RelayCommand<Customer>(ExecuteNavigateToCustomer);
            NavigateToBikeCommand = new RelayCommand<Bike>(ExecuteNavigateToBike);
            NavigateToReturnCommand = new RelayCommand<Rental>(ExecuteNavigateToReturn);
            ProcessPaymentCommand = new RelayCommand<Rental>(ExecuteProcessPayment);
            NavigateToPaymentsCommand = new RelayCommand<Rental>(ExecuteNavigateToPayments);

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
                var dialog = new RentalDialog(rentalToEdit, _customerService, _bikeService, _rentalService, true);
                if (dialog.ShowDialog() == true)
                {
                    try
                    {
                        // update rental in database
                        await _rentalService.UpdateRentalAsync(rentalToEdit);
                        await LoadRentals(); // refresh the list
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
        public ICommand NavigateToCustomerCommand { get; }
        public ICommand NavigateToBikeCommand { get; }
        public ICommand NavigateToReturnCommand { get; }
        public ICommand ProcessPaymentCommand { get; }
        public ICommand NavigateToPaymentsCommand { get; }

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
            "Completed"
        };

        #endregion

        #region Command Methods

        private void ExecuteNavigateToCustomer(Customer customer)
        {
            if (customer != null)
            {
                // navigate to CustomersView and select the customer
                var mainViewModel = Application.Current.MainWindow.DataContext as MainViewModel;
                if (mainViewModel != null)
                {
                    mainViewModel.NavigateCommand.Execute("Customers");
                    // the CustomersViewModel will handle selecting the customer
                    var customersViewModel = mainViewModel.CurrentView as CustomersViewModel;
                    customersViewModel?.SelectCustomer(customer);
                }
            }
        }

        private void ExecuteNavigateToBike(Bike bike)
        {
            if (bike != null)
            {
                // navigate to BikeView and select the bike
                var mainViewModel = Application.Current.MainWindow.DataContext as MainViewModel;
                if (mainViewModel != null)
                {
                    mainViewModel.NavigateCommand.Execute("Bikes");
                    // the BikesViewModel will handle selecting the bike
                    var bikesViewModel = mainViewModel.CurrentView as BikesViewModel;
                    bikesViewModel?.SelectBike(bike);
                }
            }
        }

        private void ExecuteNavigateToReturn(Rental rental)
        {
            if (rental != null)
            {
                // navigate to ReturnView and select the rental
                var mainViewModel = Application.Current.MainWindow.DataContext as MainViewModel;
                if (mainViewModel != null)
                {
                    mainViewModel.NavigateCommand.Execute("Returns");
                    // the ReturnsViewModel will handle selecting the rental
                    var returnsViewModel = mainViewModel.CurrentView as ReturnsViewModel;
                    returnsViewModel?.SelectRental(rental);
                }
            }
        }

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

                // open dialog for new rental
                var dialog = new RentalDialog(newRental, _customerService, _bikeService, _rentalService);
                if (dialog.ShowDialog() == true)
                {
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
                var dialog = new ReturnDialog(rental);
                if (dialog.ShowDialog() == true)
                {
                    try
                    {
                        // Check if customer has any more active rentals
                        var activeRentals = await _rentalService.GetActiveRentalsByCustomerAsync(rental.CustomerId);
                        if (activeRentals.Count == 0)
                        {
                            var customer = await _customerService.GetCustomerByIdAsync(rental.CustomerId);
                            if (customer != null)
                            {
                                customer.CustomerStatus = "Inactive";
                                await _customerService.UpdateCustomerAsync(customer);
                            }
                        }
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
                        // check if customer has any more active rentals
                        var activeRentals = await _rentalService.GetActiveRentalsByCustomerAsync(rental.CustomerId);
                        if (activeRentals.Count == 0)
                        {
                            var customer = await _customerService.GetCustomerByIdAsync(rental.CustomerId);
                            if (customer != null)
                            {
                                customer.CustomerStatus = "Inactive";
                                await _customerService.UpdateCustomerAsync(customer);
                            }
                        }
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
                    await _rentalService.ClearAllRentalsAsync();
                    await LoadRentals(); // refresh the list after clearing
                    MessageBox.Show("All rentals have been deleted successfully.",
                        "Success",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                catch (InvalidOperationException ex)
                {
                    MessageBox.Show(ex.Message,
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
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

        private async void ExecuteProcessPayment(Rental rental)
        {
            if (rental != null)
            {
                var dialog = new PaymentDialog(rental, false); // partial payment mode
                dialog.Owner = Application.Current.MainWindow;
                if (dialog.ShowDialog() == true)
                {
                    await LoadRentals();
                }
            }
        }

        private void ExecuteNavigateToPayments(Rental rental)
        {
            if (rental != null)
            {
                var mainViewModel = Application.Current.MainWindow.DataContext as MainViewModel;
                if (mainViewModel != null)
                {
                    mainViewModel.NavigateCommand.Execute("Payments");
                    // Select the payment for this rental in PaymentsViewModel
                    var paymentsViewModel = mainViewModel.CurrentView as PaymentsViewModel;
                    paymentsViewModel?.SelectPaymentByRentalId(rental.RentalId);
                }
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
                var paymentService = new PaymentService();
                var allPayments = await paymentService.GetAllPaymentsAsync();
                foreach (var rental in rentals)
                {
                    var paymentsForRental = allPayments.Where(p => p.RentalId == rental.RentalId).ToList();
                    string status = "Unpaid";
                    if (paymentsForRental.Any(p => p.PaymentStatus == "Paid"))
                        status = "Paid";
                    else if (paymentsForRental.Any(p => p.PaymentStatus == "Pending"))
                        status = "Pending";
                    // Create a synthetic Payment object for status display
                    rental.Payment = new Payment { PaymentStatus = status };
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
