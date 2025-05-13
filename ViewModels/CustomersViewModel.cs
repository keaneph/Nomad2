using Nomad2.Models;
using Nomad2.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using Nomad2.Views;
using Nomad2.Scripts;
using Nomad2.Sorting;
using System.Linq;

namespace Nomad2.ViewModels
{
    
    public class CustomersViewModel : BaseViewModel, ISearchable
    {
        // service for handling customer-related database operations
        private readonly ICustomerService _customerService;

        // observable collection to store and display customers
        private ObservableCollection<Customer> _customers;

        // fields for search, pagination, and sorting
        private string _searchText;
        private int _currentPage = 1;
        private int _totalPages;
        private string _currentPageDisplay;
        private bool _isDialogOpen;
        private Customer _selectedCustomer;
        private System.Collections.IList _selectedCustomers;
        private SortOption _currentSortOption;
        private ObservableCollection<SortOption> _availableSortOptions;
        private bool _isAscending = true;

        // property for sorting direction with da automatic refresh
        public bool IsAscending
        {
            get => _isAscending;
            set
            {
                _isAscending = value;
                OnPropertyChanged();
                LoadCustomers(); // refresh when sort direction changes
            }
        }

        // commands for UI interactions
        public ICommand ToggleSortDirectionCommand { get; }
        public ICommand ViewImageCommand { get; }


        // constructor initializes services, collections, and commands
        public CustomersViewModel()
        {
            Title = "Customers";
            Description = "Manage customer records";

            _customerService = new CustomerService();
            Customers = new ObservableCollection<Customer>();

           
            // Initialize sort options
            AvailableSortOptions = new ObservableCollection<SortOption>
        {
            new SortOption { DisplayName = "ID", Option = CustomerSortOption.CustomerId },
            new SortOption { DisplayName = "Name", Option = CustomerSortOption.Name },
            new SortOption { DisplayName = "Phone", Option = CustomerSortOption.PhoneNumber },
            new SortOption { DisplayName = "Address", Option = CustomerSortOption.Address },
            new SortOption { DisplayName = "Date", Option = CustomerSortOption.RegistrationDate },
            new SortOption { DisplayName = "Status", Option = CustomerSortOption.Status }
        };

            CurrentSortOption = AvailableSortOptions.First();

            // Initialize Commands
            AddCustomerCommand = new RelayCommand(ExecuteAddCustomer);
            EditCustomerCommand = new RelayCommand<Customer>(ExecuteEditCustomer);
            DeleteCustomerCommand = new RelayCommand<Customer>(ExecuteDeleteCustomer);
            ClearCommand = new RelayCommand(() => ExecuteClear());
            NextPageCommand = new RelayCommand(ExecuteNextPage, CanExecuteNextPage);
            PreviousPageCommand = new RelayCommand(ExecutePreviousPage, CanExecutePreviousPage);
            ViewImageCommand = new RelayCommand<string>(ExecuteViewImage);
            ToggleSortDirectionCommand = new RelayCommand(() => IsAscending = !IsAscending);

            // Load initial data
            _ = LoadCustomers();
        }

        #region Properties
        // properties with change notification for UI binding

        private void ExecuteViewImage(string imagePath)
        {
            if (!string.IsNullOrEmpty(imagePath))
            {
                var imageViewer = new ImageViewerWindow(imagePath);
                imageViewer.ShowDialog();
            }
        }

        public ObservableCollection<Customer> Customers
        {
            get => _customers;
            set
            {
                _customers = value;
                OnPropertyChanged();
            }
        }

        public System.Collections.IList SelectedCustomers
        {
            get => _selectedCustomers;
            set
            {
                _selectedCustomers = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<SortOption> AvailableSortOptions
        {
            get => _availableSortOptions;
            set
            {
                _availableSortOptions = value;
                OnPropertyChanged();
            }
        }

        public SortOption CurrentSortOption
        {
            get => _currentSortOption;
            set
            {
                _currentSortOption = value;
                OnPropertyChanged();
                LoadCustomers();
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                _currentPage = 1; // reset to first page when searching
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
        // handles adding new customer with dialog
        private async void ExecuteAddCustomer()
        {
            var newCustomer = new Customer
            {
                CustomerId = await GenerateNewCustomerId(),
                RegistrationDate = DateTime.Now,
                CustomerStatus = "Active"
            };

            var dialog = new CustomerDialog(newCustomer, false);
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    await _customerService.AddCustomerAsync(newCustomer);
                    await LoadCustomers();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error adding customer: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        // handles editing existing customer with dialog
        private async void ExecuteEditCustomer(Customer customer)
        {
            if (customer != null)
            {
                var dialog = new CustomerDialog(customer, true);
                if (dialog.ShowDialog() == true)
                {
                    try
                    {
                        await _customerService.UpdateCustomerAsync(customer);
                        await LoadCustomers(); // Refresh the list to show updated data
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error updating customer: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        // handles deleting single or multiple customers with confirmation
        private async void ExecuteDeleteCustomer(Customer customer)
        {
            // check if we have multiple selections
            if (SelectedCustomers != null && SelectedCustomers.Count > 0)
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to delete {SelectedCustomers.Count} customers?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        foreach (Customer selectedCustomer in SelectedCustomers.Cast<Customer>().ToList())
                        {
                            await _customerService.DeleteCustomerAsync(selectedCustomer.CustomerId);
                        }
                        await LoadCustomers();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting customers: {ex.Message}", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            // if no multiple selection, delete single customer
            else if (customer != null)
            {
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
                        await LoadCustomers();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting customer: {ex.Message}", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
        // handles clearing all customers with confirmation
        private async void ExecuteClear()
        {
            // show confirmation dialog
            var result = MessageBox.Show(
                "Are you sure you want to delete ALL customers? This action cannot be undone.",
                "Confirm Delete All",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // delete all records from database
                    bool success = await _customerService.ClearAllCustomersAsync();

                    if (success)
                    {
                        // clear the local collection
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

        // navigation methods for pagination

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
        // loads customers from database with pagination and sorting
        private async Task LoadCustomers()
        {
            try
            {
                var sortOption = new SortOption
                {
                    Option = CurrentSortOption.Option,
                    IsAscending = IsAscending
                };

                var (customers, totalCount) = await _customerService.GetCustomersAsync(
                    _currentPage, SearchText, sortOption);

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


        // checks if page nav is possible
        private bool CanExecuteNextPage()
        {
            return _currentPage < _totalPages;
        }

        private bool CanExecutePreviousPage()
        {
            return _currentPage > 1;
        }

        // generates new customer ID based on last used ID
        private async Task<string> GenerateNewCustomerId()
        {
            string lastId = await _customerService.GetLastCustomerIdAsync();

            if (string.IsNullOrEmpty(lastId))
            {
                return "0000-0001";
            }

            // Extract the numeric part after the hyphen
            string[] parts = lastId.Split('-');
            if (parts.Length == 2 && int.TryParse(parts[1], out int lastNumber))
            {
                int newNumber = lastNumber + 1;
                return $"0000-{newNumber:D4}";
            }

            // Fallback in case of unexpected format
            return "0000-0001";
        }
        #endregion
        public void UpdateSearch(string searchTerm)
        {
            SearchText = searchTerm;
            // LoadCustomers() will be called automatically due to the SearchText property setter
        }

    }
}