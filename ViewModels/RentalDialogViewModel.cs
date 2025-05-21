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
    public class RentalDialogViewModel : BaseViewModel
    {
        private readonly ICustomerService _customerService;
        private readonly IBikeService _bikeService;
        private readonly Window _dialog;
        private readonly Rental _rental;

        // Properties for binding
        private string _customerSearch;
        private string _bikeSearch;
        private Customer _selectedCustomer;
        private Bike _selectedBike;
        private DateTime _rentalDate;
        private string _rentalStatus;
        private bool _isCustomerSearchVisible;
        private bool _isBikeSearchVisible;

        public RentalDialogViewModel(
                Rental rental,
                ICustomerService customerService,
                IBikeService bikeService,
                Window dialog)
        {
            _rental = rental;
            _customerService = customerService;
            _bikeService = bikeService;
            _dialog = dialog;

            // Initialize commands
            ShowAvailableCustomersCommand = new RelayCommand(async () => await ShowAvailableCustomers());
            ShowAvailableBikesCommand = new RelayCommand(async () =>
            {
                MessageBox.Show("ShowAvailableBikesCommand executed");
                await ShowAvailableBikes();
            });
            SaveCommand = new RelayCommand(Save, CanSave);
            CancelCommand = new RelayCommand(Cancel);
            DebugStateCommand = new RelayCommand(DebugState);

            // Initialize collections
            CustomerSearchResults = new ObservableCollection<Customer>();
            BikeSearchResults = new ObservableCollection<Bike>();
            AvailableStatuses = new ObservableCollection<string> { "Active", "Completed", "Overdue" };

            // Initialize properties
            RentalId = rental.RentalId;
            RentalDate = DateTime.Now; // Set to current date and time
            RentalStatus = "Active"; // Set default status
        }

        #region Properties

        private string _rentalId;
        public string RentalId
        {
            get => _rentalId;
            set
            {
                _rentalId = value;
                OnPropertyChanged();
            }
        }

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
                    // Store the selection permanently
                    _rental.CustomerId = value.CustomerId;
                    _rental.Customer = value;
                }
                (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
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
                    BikeSearch = $"{value.BikeId} - {value.BikeModel}";
                    // Store the selection permanently
                    _rental.BikeId = value.BikeId;
                    _rental.Bike = value;
                }
                (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        private void DebugState()
        {
            MessageBox.Show($"Current State:\n" +
                           $"Customer: {_selectedCustomer != null} ({_selectedCustomer?.CustomerId})\n" +
                           $"Bike: {_selectedBike != null} ({_selectedBike?.BikeId})\n" +
                           $"Status: {RentalStatus}\n" +
                           $"Date Valid: {RentalDate >= DateTime.Today}\n" +
                           $"RentalId: {RentalId}");
        }

        public DateTime RentalDate
        {
            get => _rentalDate;
            set
            {
                _rentalDate = value;
                OnPropertyChanged();
                (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();  // Add this line
            }
        }

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
        public ObservableCollection<string> AvailableStatuses { get; }


        #endregion

        #region Commands

        public ICommand DebugStateCommand { get; }
        public ICommand ShowAvailableCustomersCommand { get; }
        public ICommand ShowAvailableBikesCommand { get; }
        public ICommand SaveCommand { get; }
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

        private bool CanSave()
        {
            bool customerValid = _rental.CustomerId != null && _rental.Customer != null;
            bool bikeValid = _rental.BikeId != null && _rental.Bike != null;
            bool statusValid = !string.IsNullOrWhiteSpace(_rental.RentalStatus);
            bool dateValid = _rental.RentalDate >= DateTime.Today;
            bool idValid = !string.IsNullOrWhiteSpace(_rental.RentalId);

            return customerValid && bikeValid && statusValid && dateValid && idValid;
        }

        private void Save()
        {
            // We don't need to set these again because we already set them when selecting
            // _rental.CustomerId = SelectedCustomer.CustomerId;  // Remove this line
            // _rental.BikeId = SelectedBike.BikeId;            // Remove this line

            // These should already be set, but let's make sure
            _rental.RentalDate = RentalDate;
            _rental.RentalStatus = RentalStatus;

            _dialog.DialogResult = true;
            _dialog.Close();
        }

        private void Cancel()
        {
            _dialog.DialogResult = false;
            _dialog.Close();
        }

        #endregion

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
    }
}