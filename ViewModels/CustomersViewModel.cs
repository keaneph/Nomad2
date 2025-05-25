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

    // view model for managing customer ops
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
        private SortOption<CustomerSortOption> _currentSortOption;
        private ObservableCollection<SortOption<CustomerSortOption>> _availableSortOptions;
        private bool _isAscending = true;

        // property for sorting direction with automatic refresh
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
                LoadCustomers();
            }
        }

        // available status filter options
        public ObservableCollection<string> StatusFilters { get; } = new ObservableCollection<string>
        {
            "All",
            "Inactive",
            "Active",
            "Blacklisted"
        };

        #endregion

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


            // initialize sort options
            AvailableSortOptions = new ObservableCollection<SortOption<CustomerSortOption>>
{
            new SortOption<CustomerSortOption> { DisplayName = "ID", Option = CustomerSortOption.CustomerId },
            new SortOption<CustomerSortOption> { DisplayName = "Name", Option = CustomerSortOption.Name },
            new SortOption<CustomerSortOption> { DisplayName = "Phone", Option = CustomerSortOption.PhoneNumber },
            new SortOption<CustomerSortOption> { DisplayName = "Address", Option = CustomerSortOption.Address },
            new SortOption<CustomerSortOption> { DisplayName = "Date", Option = CustomerSortOption.RegistrationDate },
            new SortOption<CustomerSortOption> { DisplayName = "Status", Option = CustomerSortOption.Status }
        };

            CurrentSortOption = AvailableSortOptions.First();

            // unitialize Commands
            AddCustomerCommand = new RelayCommand(ExecuteAddCustomer);
            EditCustomerCommand = new RelayCommand<Customer>(ExecuteEditCustomer);
            DeleteCustomerCommand = new RelayCommand<Customer>(ExecuteDeleteCustomer);
            ClearCommand = new RelayCommand(() => ExecuteClear());
            NextPageCommand = new RelayCommand(ExecuteNextPage, CanExecuteNextPage);
            PreviousPageCommand = new RelayCommand(ExecutePreviousPage, CanExecutePreviousPage);
            ViewImageCommand = new RelayCommand<string>(ExecuteViewImage);
            ToggleSortDirectionCommand = new RelayCommand(() => IsAscending = !IsAscending);

            // load initial data
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


        // updates the page size and reloads customers if it changes
        public void UpdatePageSize(int newSize)
        {
            if (_customerService.PageSize != newSize)
            {
                _customerService.PageSize = newSize;
                _ = LoadCustomers(); // using _ = to handle the async call
            }
        }

        // properties for data binding
        public ObservableCollection<Customer> Customers
        {
            get => _customers;
            set
            {
                _customers = value;
                OnPropertyChanged();
            }
        }

        // property for selected customers (for multi-select)
        public System.Collections.IList SelectedCustomers
        {
            get => _selectedCustomers;
            set
            {
                _selectedCustomers = value;
                OnPropertyChanged();
            }
        }

        // property for available sort options
        public ObservableCollection<SortOption<CustomerSortOption>> AvailableSortOptions
        {
            get => _availableSortOptions;
            set
            {
                _availableSortOptions = value;
                OnPropertyChanged();
            }
        }

        // property for current sort option
        public SortOption<CustomerSortOption> CurrentSortOption
        {
            get => _currentSortOption;
            set
            {
                _currentSortOption = value;
                OnPropertyChanged();
                LoadCustomers();
            }
        }

        // property for search text
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

        // property for current page display
        public string CurrentPageDisplay
        {
            get => $"Page {_currentPage} of {_totalPages}";
            set
            {
                _currentPageDisplay = value;
                OnPropertyChanged();
            }
        }

        // property for total pages
        public bool IsDialogOpen
        {
            get => _isDialogOpen;
            set
            {
                _isDialogOpen = value;
                OnPropertyChanged();
            }
        }

        // property for selected customer
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

        // commands for UI interactions
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

            // open dialog for new customer
            var dialog = new CustomerDialog(newCustomer, false);
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // add new customer to database
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
                // create a copy of the customer object
                var customerCopy = new Customer
                {
                    CustomerId = customer.CustomerId,
                    Name = customer.Name,
                    PhoneNumber = customer.PhoneNumber,
                    Address = customer.Address,
                    GovernmentIdPicture = customer.GovernmentIdPicture,
                    CustomerStatus = customer.CustomerStatus,
                    RegistrationDate = customer.RegistrationDate
                };

                var dialog = new CustomerDialog(customerCopy, true);
                if (dialog.ShowDialog() == true)
                {
                    try
                    {
                        // update customer in database
                        await _customerService.UpdateCustomerAsync(customerCopy);
                        // only update the original customer object if the database update was successful
                        customer.Name = customerCopy.Name;
                        customer.PhoneNumber = customerCopy.PhoneNumber;
                        customer.Address = customerCopy.Address;
                        customer.GovernmentIdPicture = customerCopy.GovernmentIdPicture;
                        customer.CustomerStatus = customerCopy.CustomerStatus;
                        customer.RegistrationDate = customerCopy.RegistrationDate;
                        await LoadCustomers(); // refresh the list to show updated data
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
                        // delete each selected customer
                        foreach (Customer selectedCustomer in SelectedCustomers.Cast<Customer>().ToList())
                        {
                            // delete customer from database
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
                        // delete customer from database
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
                var sortOption = new SortOption<CustomerSortOption>
                {
                    Option = CurrentSortOption.Option,
                    IsAscending = IsAscending
                };

                var (customers, totalCount) = await _customerService.GetCustomersAsync(
                    _currentPage, SearchText, sortOption);

                // apply status filter if not "All"
                if (SelectedStatusFilter != "All")
                {
                    customers = customers.Where(c => c.CustomerStatus == SelectedStatusFilter).ToList();
                }

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

        public void SelectCustomer(Customer customer)
        {
            if (customer != null)
            {
                // find the customer in the current collection
                var existingCustomer = Customers.FirstOrDefault(c => c.CustomerId == customer.CustomerId);
                if (existingCustomer != null)
                {
                    SelectedCustomer = existingCustomer;
                }
                else
                {
                    // if customer is not in current view, search for them
                    SearchText = customer.CustomerId;
                    // the LoadCustomers() will be called automatically due to SearchText property setter
                    // after loading, we'll select the customer
                    SelectedCustomer = customer;
                }
            }
        }
    }
}