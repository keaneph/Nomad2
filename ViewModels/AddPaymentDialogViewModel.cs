using System;
using System.Windows.Input;
using Nomad2.Models;
using Nomad2.Services;
using System.Threading.Tasks;
using System.Windows;
using System.Collections.ObjectModel;
using System.Linq;
using Nomad2.Views;

namespace Nomad2.ViewModels
{
    public class AddPaymentDialogViewModel : BaseViewModel
    {
        private readonly Window _dialog;
        private readonly IRentalService _rentalService;
        private readonly ICustomerService _customerService;
        private readonly IBikeService _bikeService;
        private string _errorMessage;

        // search and selection state tracking
        private string _customerSearch;
        private string _bikeSearch;
        private Customer _selectedCustomer;
        private Bike _selectedBike;
        private bool _isCustomerSearchVisible;
        private bool _isBikeSearchVisible;
        private bool _suppressCustomerSearch = false;
        private bool _suppressBikeSearch = false;

        public AddPaymentDialogViewModel(Window dialog)
        {
            _dialog = dialog ?? throw new ArgumentNullException(nameof(dialog));
            _rentalService = new RentalService();
            _customerService = new CustomerService();
            _bikeService = new BikeService();

            // initialize collections
            CustomerSearchResults = new ObservableCollection<Customer>();
            BikeSearchResults = new ObservableCollection<Bike>();

            // initialize commands
            ToggleCustomerListCommand = new RelayCommand(ToggleCustomerList);
            ToggleBikeListCommand = new RelayCommand(ToggleBikeList);
            ContinueCommand = new RelayCommand(async () => await ExecuteContinue(), CanExecuteContinue);
            CancelCommand = new RelayCommand(ExecuteCancel);
        }

        public string DialogTitle => "Add Payment";

        #region Properties

        public string CustomerSearch
        {
            get => _customerSearch;
            set
            {
                _customerSearch = value;
                OnPropertyChanged();
                if (!_suppressCustomerSearch)
                {
                    _ = SearchCustomers();
                }
            }
        }

        public string BikeSearch
        {
            get => _bikeSearch;
            set
            {
                _bikeSearch = value;
                OnPropertyChanged();
                if (!_suppressBikeSearch)
                {
                    _ = SearchBikes();
                }
            }
        }

        public Customer SelectedCustomer
        {
            get => _selectedCustomer;
            set
            {
                if (_selectedCustomer != value)
                {
                    _selectedCustomer = value;
                    OnPropertyChanged();
                    if (value != null)
                    {
                        IsCustomerSearchVisible = false;
                        _suppressCustomerSearch = true;
                        CustomerSearch = $"{value.CustomerId} - {value.Name}";
                        _suppressCustomerSearch = false;
                        // clear bike selection when customer changes
                        SelectedBike = null;
                        BikeSearch = string.Empty;
                        BikeSearchResults.Clear();
                        ErrorMessage = string.Empty;
                    }
                    else
                    {
                        ErrorMessage = "Please select a customer";
                    }
                    (ContinueCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

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
                    _suppressBikeSearch = true;
                    BikeSearch = $"{value.BikeId} - {value.BikeModel}";
                    _suppressBikeSearch = false;
                }
                (ContinueCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public bool IsCustomerSearchVisible
        {
            get => _isCustomerSearchVisible;
            set
            {
                _isCustomerSearchVisible = value;
                OnPropertyChanged();
            }
        }

        public bool IsBikeSearchVisible
        {
            get => _isBikeSearchVisible;
            set
            {
                _isBikeSearchVisible = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<Customer> CustomerSearchResults { get; }
        public ObservableCollection<Bike> BikeSearchResults { get; }

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

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Commands

        public ICommand ToggleCustomerListCommand { get; }
        public ICommand ToggleBikeListCommand { get; }
        public ICommand ContinueCommand { get; }
        public ICommand CancelCommand { get; }

        #endregion

        #region Methods

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
                foreach (var customer in customers.Where(c => c.CustomerStatus == "Active"))
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

        private async Task SearchBikes()
        {
            if (string.IsNullOrWhiteSpace(BikeSearch) || SelectedCustomer == null)
            {
                BikeSearchResults.Clear();
                IsBikeSearchVisible = false;
                return;
            }

            try
            {
                // get active rentals for the selected customer
                var activeRentals = await _rentalService.GetActiveRentalsByCustomerAsync(SelectedCustomer.CustomerId);
                BikeSearchResults.Clear();

                foreach (var rental in activeRentals)
                {
                    if (rental.Bike != null && 
                        rental.Bike.BikeModel.ToLower().Contains(BikeSearch.ToLower()))
                    {
                        BikeSearchResults.Add(rental.Bike);
                    }
                }
                IsBikeSearchVisible = BikeSearchResults.Any();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error searching bikes: {ex.Message}");
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
                    foreach (var customer in customers.Where(c => c.CustomerStatus == "Active"))
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
            else if (SelectedCustomer != null)
            {
                // if list is hidden and customer is selected, show it and load data
                try
                {
                    var activeRentals = await _rentalService.GetActiveRentalsByCustomerAsync(SelectedCustomer.CustomerId);
                    BikeSearchResults.Clear();
                    foreach (var rental in activeRentals)
                    {
                        if (rental.Bike != null)
                        {
                            BikeSearchResults.Add(rental.Bike);
                        }
                    }
                    IsBikeSearchVisible = BikeSearchResults.Any();
                    BikeButtonText = "Close";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading bikes: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("Please select a customer first", "No Customer Selected", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private bool CanExecuteContinue()
        {
            if (SelectedCustomer == null)
            {
                ErrorMessage = "Please select a customer";
                return false;
            }

            if (SelectedBike == null)
            {
                ErrorMessage = "Please select a bike";
                return false;
            }

            ErrorMessage = string.Empty;
            return true;
        }

        private async Task ExecuteContinue()
        {
            if (CanExecuteContinue())
            {
                // find the rental for the selected customer and bike
                var activeRentals = await _rentalService.GetActiveRentalsByCustomerAsync(SelectedCustomer.CustomerId);
                var rental = activeRentals.FirstOrDefault(r => r.BikeId == SelectedBike.BikeId);

                if (rental != null)
                {
                    var returnDialog = new ReturnDialog(rental);
                    _dialog.DialogResult = returnDialog.ShowDialog();
                    _dialog.Close();
                }
                else
                {
                    ErrorMessage = "No active rental found for the selected customer and bike";
                }
            }
        }

        private void ExecuteCancel()
        {
            _dialog.DialogResult = false;
            _dialog.Close();
        }

        #endregion
    }
} 
