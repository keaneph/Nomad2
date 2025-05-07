using Nomad2.Models;
using Nomad2.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using Nomad2.Views;

namespace Nomad2.ViewModels
{
    public class CustomersViewModel : BaseViewModel
    {
        private readonly ICustomerService _customerService;
        private ObservableCollection<Customer> _customers;
        private string _searchText;
        private int _currentPage = 1;
        private int _totalPages;
        private string _selectedSortOption;
        private string _currentPageDisplay;
        private bool _isDialogOpen;
        private Customer _selectedCustomer;

        public CustomersViewModel()
        {
            Title = "Customers";
            Description = "Manage customer records";

            _customerService = new CustomerService();
            Customers = new ObservableCollection<Customer>();
            SortOptions = new ObservableCollection<string> { "Name", "Date", "Status" };
            SelectedSortOption = "Name";

            // Initialize Commands
            AddCustomerCommand = new RelayCommand(ExecuteAddCustomer);
            EditCustomerCommand = new RelayCommand<Customer>(ExecuteEditCustomer);
            DeleteCustomerCommand = new RelayCommand<Customer>(ExecuteDeleteCustomer);
            ClearCommand = new RelayCommand(() => ExecuteClear());
            NextPageCommand = new RelayCommand(ExecuteNextPage, CanExecuteNextPage);
            PreviousPageCommand = new RelayCommand(ExecutePreviousPage, CanExecutePreviousPage);

            // Load initial data
            _ = LoadCustomers(); // Fire and forget, but better to handle properly
        }

        #region Properties

        public ObservableCollection<Customer> Customers
        {
            get => _customers;
            set
            {
                _customers = value;
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
                _currentPage = 1; // Reset to first page when searching
                LoadCustomers();
            }
        }

        public ObservableCollection<string> SortOptions { get; }

        public string SelectedSortOption
        {
            get => _selectedSortOption;
            set
            {
                _selectedSortOption = value;
                OnPropertyChanged();
                LoadCustomers();
            }
        }

        public string CurrentPageDisplay
        {
            get => $"Page {_currentPage} of {_totalPages}";
            set
            {
                _currentPageDisplay = value;
                OnPropertyChanged();
            }
        }

        public bool IsDialogOpen
        {
            get => _isDialogOpen;
            set
            {
                _isDialogOpen = value;
                OnPropertyChanged();
            }
        }

        public Customer SelectedCustomer
        {
            get => _selectedCustomer;
            set
            {
                _selectedCustomer = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Commands

        public ICommand AddCustomerCommand { get; }
        public ICommand EditCustomerCommand { get; }
        public ICommand DeleteCustomerCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand PreviousPageCommand { get; }

        #endregion

        #region Command Methods

        private async void ExecuteAddCustomer()
        {
            var newCustomer = new Customer
            {
                CustomerId = GenerateNewCustomerId(),
                RegistrationDate = DateTime.Now,
                CustomerStatus = "Active"
            };

            var dialog = new CustomerDialog(newCustomer, false);
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    await _customerService.AddCustomerAsync(newCustomer);
                    LoadCustomers();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error adding customer: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExecuteEditCustomer(Customer customer)
        {
            if (customer != null)
            {
                var dialog = new CustomerDialog(customer, true);
                if (dialog.ShowDialog() == true)
                {
                    // Refresh the customer list after successful edit
                    LoadCustomers();
                }
            }
        }

        private async void ExecuteDeleteCustomer(Customer customer)
        {
            if (customer == null) return;

            var result = MessageBox.Show(
                "Are you sure you want to delete this customer?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _customerService.DeleteCustomerAsync(customer.CustomerId);
                    LoadCustomers();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting customer: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void ExecuteClear()
        {
            // Show confirmation dialog
            var result = MessageBox.Show(
                "Are you sure you want to delete ALL customers? This action cannot be undone.",
                "Confirm Delete All",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // Delete all records from database
                    bool success = await _customerService.ClearAllCustomersAsync();

                    if (success)
                    {
                        // Clear the local collection
                        Customers.Clear();
                        _currentPage = 1;
                        _totalPages = 0;
                        OnPropertyChanged(nameof(CurrentPageDisplay));

                        MessageBox.Show("All customers have been deleted successfully.",
                            "Success",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Failed to delete all customers.",
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

        private void ExecuteNextPage()
        {
            if (_currentPage < _totalPages)
            {
                _currentPage++;
                LoadCustomers();
            }
        }

        private void ExecutePreviousPage()
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                LoadCustomers();
            }
        }

        #endregion

        #region Helper Methods

        private async Task LoadCustomers()
        {
            try
            {
                var (customers, totalCount) = await _customerService.GetCustomersAsync(_currentPage, SearchText, SelectedSortOption);

                Customers.Clear();
                foreach (var customer in customers)
                {
                    Customers.Add(customer);
                }

                _totalPages = (int)Math.Ceiling(totalCount / (double)_customerService.PageSize);
                OnPropertyChanged(nameof(CurrentPageDisplay));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading customers: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private string GenerateNewCustomerId()
        {
            // Format: 0000-0000
            return $"{new Random().Next(0, 9999):D4}-{new Random().Next(0, 9999):D4}";
        }

        #endregion
    }
}