using Nomad2.Models;
using Nomad2.Services;
using Nomad2.Validators;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Nomad2.ViewModels
{
    // view model for managing rental ops
    public class RentalDialogViewModel : BaseViewModel
    {
        // services and core dependencies
        private readonly ICustomerService _customerService;
        private readonly IBikeService _bikeService;
        private readonly IRentalService _rentalService;
        private readonly Window _dialog;
        private readonly Rental _rental;
        private readonly bool _isEdit;
        private string _errorMessage;
        private string _originalBikeId;
        private string _originalCustomerId;

        // search and selection state tracking
        private string _customerSearch;
        private string _bikeSearch;
        private Customer _selectedCustomer;
        private Bike _selectedBike;
        private DateTime _rentalDate;
        private bool _isCustomerSearchVisible;
        private bool _isBikeSearchVisible;

        // initializes rental dialog with required services and dat
        public RentalDialogViewModel(
                Rental rental,
                ICustomerService customerService,
                IBikeService bikeService,
                IRentalService rentalService,
                Window dialog,
                bool isEdit = false)
        {
            _rental = rental;
            _customerService = customerService;
            _bikeService = bikeService;
            _rentalService = rentalService;
            _dialog = dialog;
            _isEdit = isEdit;

            // Store original IDs if editing
            if (_isEdit)
            {
                if (rental.Bike != null)
                {
                    _originalBikeId = rental.BikeId;
                }
                if (rental.Customer != null)
                {
                    _originalCustomerId = rental.CustomerId;
                }
            }

            // initialize commands
            ToggleCustomerListCommand = new RelayCommand(ToggleCustomerList);
            ToggleBikeListCommand = new RelayCommand(ToggleBikeList);
            SaveCommand = new RelayCommand(Save, CanSave);
            CancelCommand = new RelayCommand(Cancel);

            // initialize collections and properties
            CustomerSearchResults = new ObservableCollection<Customer>();
            BikeSearchResults = new ObservableCollection<Bike>();

            RentalId = rental.RentalId;
            RentalDate = rental.RentalDate;

            // if editing, set the initial values
            if (_isEdit && rental.Customer != null && rental.Bike != null)
            {
                SelectedCustomer = rental.Customer;
                CustomerSearch = $"{rental.Customer.CustomerId} - {rental.Customer.Name}";

                SelectedBike = rental.Bike;
                BikeSearch = $"{rental.Bike.BikeId} - {rental.Bike.BikeModel}";
            }
            else
            {
                RentalDate = DateTime.Now;
                _rental.RentalStatus = "Active";
            }
        }

        // indicates if dialog is in edit or add mode
        public string DialogTitle => _isEdit ? "Edit Rental" : "Add Rental";

        #region Properties

        private string _rentalId;

        // rental identifier for database
        public string RentalId
        {
            get => _rentalId;
            set
            {
                _rentalId = value;
                OnPropertyChanged();
            }
        }

        // error message property with notification
        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                OnPropertyChanged();
            }
        }

        // customer search input with auto-update
        public string CustomerSearch
        {
            get => _customerSearch;
            set
            {
                _customerSearch = value;
                OnPropertyChanged();
                _ = SearchCustomers();
            }
        }

        // bike search input with auto-update
        public string BikeSearch
        {
            get => _bikeSearch;
            set
            {
                _bikeSearch = value;
                OnPropertyChanged();
                _ = SearchBikes();
            }
        }

        // selected customer with validation
        public Customer SelectedCustomer
        {
            get => _selectedCustomer;
            set
            {
                _selectedCustomer = value;
                OnPropertyChanged();
                if (value != null)
                {
                    IsCustomerSearchVisible = false;
                    CustomerSearch = $"{value.CustomerId} - {value.Name}";
                    // store the selection permanently
                    _rental.CustomerId = value.CustomerId;
                    _rental.Customer = value;
                }
                (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        // selected bike with validation
        public Bike SelectedBike
        {
            get => _selectedBike;
            set
            {
                _selectedBike = value;
                OnPropertyChanged();
                if (value != null)
                {
                    IsBikeSearchVisible = false;
                    BikeSearch = $"{value.BikeId} - {value.BikeModel}";
                    // store the selection permanently
                    _rental.BikeId = value.BikeId;
                    _rental.Bike = value;
                }
                (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        // rental date with validation
        public DateTime RentalDate
        {
            get => _rentalDate;
            set
            {
                _rentalDate = value;
                OnPropertyChanged();
                (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();  
            }
        }

        // controls customer search visibility
        public bool IsCustomerSearchVisible
        {
            get => _isCustomerSearchVisible;
            set
            {
                _isCustomerSearchVisible = value;
                OnPropertyChanged();
            }
        }

        // controls bike search visibility
        public bool IsBikeSearchVisible
        {
            get => _isBikeSearchVisible;
            set
            {
                _isBikeSearchVisible = value;
                OnPropertyChanged();
            }
        }

        // collections for search results and status options
        public ObservableCollection<Customer> CustomerSearchResults { get; }
        public ObservableCollection<Bike> BikeSearchResults { get; }

        #endregion

        #region Commands

        public ICommand ToggleCustomerListCommand { get; }
        public ICommand ToggleBikeListCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        #endregion

        #region Methods

        // searches customers based on input
        private async Task SearchCustomers()
        {
            if (string.IsNullOrWhiteSpace(CustomerSearch))
            {
                CustomerSearchResults.Clear();
                IsCustomerSearchVisible = false;
                return;
            }

            try
            {
                var (customers, _) = await _customerService.GetCustomersAsync(1, CustomerSearch, null);
                CustomerSearchResults.Clear();
                foreach (var customer in customers.Where(c =>
                    c.CustomerStatus.Equals("Active", StringComparison.OrdinalIgnoreCase) ||
                    c.CustomerStatus.Equals("Inactive", StringComparison.OrdinalIgnoreCase)))
                {
                    CustomerSearchResults.Add(customer);
                }
                IsCustomerSearchVisible = CustomerSearchResults.Any();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error searching customers: {ex.Message}");
            }
        }

        // searches bikes based on input
        private async Task SearchBikes()
        {
            if (string.IsNullOrWhiteSpace(BikeSearch))
            {
                BikeSearchResults.Clear();
                IsBikeSearchVisible = false;
                return;
            }

            try
            {
                var (bikes, _) = await _bikeService.GetBikesAsync(1, BikeSearch, null);
                BikeSearchResults.Clear();
                foreach (var bike in bikes.Where(b =>
                    b.BikeStatus.Equals("Available", StringComparison.OrdinalIgnoreCase)))
                {
                    BikeSearchResults.Add(bike);
                }
                IsBikeSearchVisible = BikeSearchResults.Any();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error searching bikes: {ex.Message}");
            }
        }

        // validates rental data before saving
        private bool CanSave()
        {
            // Update the rental object with current values
            _rental.RentalDate = RentalDate;

            var (isValid, errorMessage) = RentalValidator.ValidateRental(_rental);
            if (!isValid)
            {
                ErrorMessage = errorMessage;
                return false;
            }

            ErrorMessage = string.Empty;
            return true;
        }

        // saves rental and closes dialog
        private async void Save()
        {
            try
            {
                _rental.RentalDate = RentalDate;

                // validate customer eligibility for new rentals
                if (!_isEdit && !await _rentalService.IsCustomerEligibleForRental(_rental.CustomerId))
                {
                    MessageBox.Show(
                        "This customer already has 3 active rentals and cannot rent more bikes.",
                        "Rental Limit Reached",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                    return;
                }

                // validate customer eligibility when editing and changing customers
                if (_isEdit && _originalCustomerId != _rental.CustomerId)
                {
                    // get active rentals for the new customer
                    var activeRentals = await _rentalService.GetActiveRentalsByCustomerAsync(_rental.CustomerId);
                    if (activeRentals.Count >= 3)
                    {
                        MessageBox.Show(
                            "This customer already has 3 active rentals and cannot rent more bikes.",
                            "Rental Limit Reached",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning
                        );
                        return;
                    }
                }

                // validate bike availability - only for new rentals or when changing bikes
                if (!_isEdit || (_isEdit && _originalBikeId != _rental.BikeId))
                {
                    if (!await _rentalService.IsBikeAvailableForRental(_rental.BikeId))
                    {
                        MessageBox.Show(
                            "This bike is currently unavailable.",
                            "Unavailable",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning
                        );
                        return;
                    }
                }

                // Defensive: Ensure new rentals always get a new RentalId
                if (!_isEdit && string.IsNullOrWhiteSpace(_rental.RentalId))
                {
                    var lastId = await _rentalService.GetLastRentalIdAsync();
                    if (string.IsNullOrWhiteSpace(lastId) || lastId == "0000-0000")
                    {
                        _rental.RentalId = "0000-0001";
                    }
                    else
                    {
                        string[] parts = lastId.Split('-');
                        if (parts.Length == 2 && int.TryParse(parts[1], out int number))
                        {
                            _rental.RentalId = $"{parts[0]}-{(number + 1):D4}";
                        }
                        else
                        {
                            _rental.RentalId = "0000-0001";
                        }
                    }
                    RentalId = _rental.RentalId;
                }

                // 1. update the rental in the database first
                if (_isEdit)
                {
                    await _rentalService.UpdateRentalAsync(_rental);
                }

                // 2. update bike statuses (existing logic)
                if (_isEdit && _rental.Bike != null && _originalBikeId != _rental.BikeId)
                {
                    var originalBike = await _bikeService.GetBikeByIdAsync(_originalBikeId);
                    if (originalBike != null)
                    {
                        originalBike.BikeStatus = "Available";
                        await _bikeService.UpdateBikeAsync(originalBike);
                    }
                    _rental.Bike.BikeStatus = "Rented";
                    await _bikeService.UpdateBikeAsync(_rental.Bike);
                }
                else if (!_isEdit && _rental.Bike != null)
                {
                    _rental.Bike.BikeStatus = "Rented";
                    await _bikeService.UpdateBikeAsync(_rental.Bike);
                }

                // 3. update customer status
                if (_isEdit)
                {
                    // get the original customer if we're editing
                    var originalCustomer = await _customerService.GetCustomerByIdAsync(_originalCustomerId);
                    
                    // if were changing customers
                    if (originalCustomer != null && originalCustomer.CustomerId != _rental.Customer.CustomerId)
                    {
                        // check if original customer has any other active rentals
                        // this check happens AFTER the rental update, so it won't count the rental we just changed
                        var activeRentals = await _rentalService.GetActiveRentalsByCustomerAsync(originalCustomer.CustomerId);
                        if (!activeRentals.Any())
                        {
                            // If no other active rentals, set status to Inactive
                            originalCustomer.CustomerStatus = "Inactive";
                            await _customerService.UpdateCustomerAsync(originalCustomer);
                        }
                    }
                }

                // set the current customer to Active
                if (_rental.Customer != null)
                {
                    _rental.Customer.CustomerStatus = "Active";
                    await _customerService.UpdateCustomerAsync(_rental.Customer);
                }

                _dialog.DialogResult = true;
                _dialog.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving rental: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // cancels operation and closes dialog
        private void Cancel()
        {
            _dialog.DialogResult = false;
            _dialog.Close();
        }

        #endregion

        // loads and displays active customers
        private async Task ShowAvailableCustomers()
        {
            try
            {
                var customers = await _customerService.GetAllCustomersAsync();

                CustomerSearchResults.Clear();
                foreach (var customer in customers.Where(c =>
                    c.CustomerStatus.Equals("Active", StringComparison.OrdinalIgnoreCase) ||
                    c.CustomerStatus.Equals("Inactive", StringComparison.OrdinalIgnoreCase)))
                {
                    CustomerSearchResults.Add(customer);
                }

                IsCustomerSearchVisible = CustomerSearchResults.Any();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading customers: {ex.Message}");
            }
        }

        // loads and displays available bikes
        private async Task ShowAvailableBikes()
        {
            try
            {
                var bikes = await _bikeService.GetAllBikesAsync();

                BikeSearchResults.Clear();
                foreach (var bike in bikes.Where(b =>
                    b.BikeStatus.Equals("Available", StringComparison.OrdinalIgnoreCase)))
                {
                    BikeSearchResults.Add(bike);
                }

                IsBikeSearchVisible = BikeSearchResults.Any();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading bikes: {ex.Message}");
            }
        }

        // text for customer browse button
        private string _customerButtonText = "Browse";
        public string CustomerButtonText
        {
            get => _customerButtonText;
            set
            {
                _customerButtonText = value;
                OnPropertyChanged();
            }
        }

        // text for bike browse button
        private string _bikeButtonText = "Browse";
        public string BikeButtonText
        {
            get => _bikeButtonText;
            set
            {
                _bikeButtonText = value;
                OnPropertyChanged();
            }
        }

        private async void ToggleCustomerList()
        {
            if (IsCustomerSearchVisible)
            {
                // if list is visible, hide it
                IsCustomerSearchVisible = false;
                CustomerButtonText = "Browse";
                CustomerSearchResults.Clear();
            }
            else
            {
                // if list is hidden, show it and load data
                try
                {
                    var customers = await _customerService.GetAllCustomersAsync();

                    CustomerSearchResults.Clear();
                    foreach (var customer in customers.Where(c =>
                        c.CustomerStatus.Equals("Active", StringComparison.OrdinalIgnoreCase) ||
                        c.CustomerStatus.Equals("Inactive", StringComparison.OrdinalIgnoreCase)))
                    {
                        CustomerSearchResults.Add(customer);
                    }

                    IsCustomerSearchVisible = CustomerSearchResults.Any();
                    CustomerButtonText = "Close";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading customers: {ex.Message}");
                }
            }
        }

        private async void ToggleBikeList()
        {
            if (IsBikeSearchVisible)
            {
                // if list is visible, hide it
                IsBikeSearchVisible = false;
                BikeButtonText = "Browse";
                BikeSearchResults.Clear();
            }
            else
            {
                // if list is hidden, show it and load data
                try
                {
                    var bikes = await _bikeService.GetAllBikesAsync();

                    BikeSearchResults.Clear();
                    foreach (var bike in bikes.Where(b =>
                        b.BikeStatus.Equals("Available", StringComparison.OrdinalIgnoreCase)))
                    {
                        BikeSearchResults.Add(bike);
                    }

                    IsBikeSearchVisible = BikeSearchResults.Any();
                    BikeButtonText = "Close";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading bikes: {ex.Message}");
                }
            }
        }
    }
}