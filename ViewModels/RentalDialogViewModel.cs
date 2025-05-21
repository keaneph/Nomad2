using Nomad2.Models;
using Nomad2.Services;
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

    // services and core dependencies
    {
        private readonly ICustomerService _customerService;
        private readonly IBikeService _bikeService;
        private readonly Window _dialog;
        private readonly Rental _rental;
        private readonly bool _isEdit;

        // search and selection state tracking
        private string _customerSearch;
        private string _bikeSearch;
        private Customer _selectedCustomer;
        private Bike _selectedBike;
        private DateTime _rentalDate;
        private string _rentalStatus;
        private bool _isCustomerSearchVisible;
        private bool _isBikeSearchVisible;

        // initializes rental dialog with required services and dat
        public RentalDialogViewModel(
                Rental rental,
                ICustomerService customerService,
                IBikeService bikeService,
                Window dialog,
                bool isEdit = false)
        {
            _rental = rental;
            _customerService = customerService;
            _bikeService = bikeService;
            _dialog = dialog;
            _isEdit = isEdit;

            // initialize commands
            ToggleCustomerListCommand = new RelayCommand(ToggleCustomerList);
            ToggleBikeListCommand = new RelayCommand(ToggleBikeList);
            SaveCommand = new RelayCommand(Save, CanSave);
            CancelCommand = new RelayCommand(Cancel);

            // initialize collections and properties
            CustomerSearchResults = new ObservableCollection<Customer>();
            BikeSearchResults = new ObservableCollection<Bike>();
            AvailableStatuses = new ObservableCollection<string> { "Active", "Completed", "Overdue" };

            RentalId = rental.RentalId;
            RentalDate = rental.RentalDate;
            RentalStatus = rental.RentalStatus;

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
                RentalStatus = "Active";
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


        // current status of rental
        public string RentalStatus
        {
            get => _rentalStatus;
            set
            {
                _rentalStatus = value;
                OnPropertyChanged();
                (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();  // Add this line
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
        public ObservableCollection<string> AvailableStatuses { get; }


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
                foreach (var customer in customers)
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
                foreach (var bike in bikes)
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
            bool customerValid = _rental.CustomerId != null && _rental.Customer != null;
            bool bikeValid = _rental.BikeId != null && _rental.Bike != null;
            bool statusValid = !string.IsNullOrWhiteSpace(_rental.RentalStatus);
            bool dateValid = _rental.RentalDate >= DateTime.Today;
            bool idValid = !string.IsNullOrWhiteSpace(_rental.RentalId);

            return customerValid && bikeValid && statusValid && dateValid && idValid;
        }


        // saves rental and closes dialog
        private void Save()
        {
            _rental.RentalDate = RentalDate;
            _rental.RentalStatus = RentalStatus;

            _dialog.DialogResult = true;
            _dialog.Close();
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
                    c.CustomerStatus.Equals("Active", StringComparison.OrdinalIgnoreCase)))
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
                        c.CustomerStatus.Equals("Active", StringComparison.OrdinalIgnoreCase)))
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
                // If list is visible, hide it
                IsBikeSearchVisible = false;
                BikeButtonText = "Browse";
                BikeSearchResults.Clear();
            }
            else
            {
                // If list is hidden, show it and load data
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