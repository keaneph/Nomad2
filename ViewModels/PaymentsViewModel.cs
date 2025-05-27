using Nomad2.Models;
using Nomad2.Services;
using Nomad2.Sorting;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using Nomad2.Views;
using Nomad2.Scripts;

namespace Nomad2.ViewModels
{
    public class PaymentsViewModel : BaseViewModel, ISearchable
    {
        private readonly IPaymentService _paymentService;
        private ObservableCollection<Payment> _payments;
        private string _searchText;
        private int _currentPage = 1;
        private int _totalPages;
        private string _currentPageDisplay;
        private bool _isDialogOpen;
        private Payment _selectedPayment;
        private System.Collections.IList _selectedPayments;
        private SortOption<PaymentSortOption> _currentSortOption;
        private ObservableCollection<SortOption<PaymentSortOption>> _availableSortOptions;
        private bool _isAscending = true;
        private int _pageSize = 10;

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
                LoadPayments();
            }
        }

        // available status filter options
        public ObservableCollection<string> StatusFilters { get; } = new ObservableCollection<string>
        {
            "All",
            "Paid",
            "Pending",
            "Unpaid"
        };

        #endregion

        public PaymentsViewModel()
        {
            Title = "Payments";
            Description = "View payment history";

            _paymentService = new PaymentService();
            Payments = new ObservableCollection<Payment>();

            AvailableSortOptions = new ObservableCollection<SortOption<PaymentSortOption>>
            {
                new SortOption<PaymentSortOption> { DisplayName = "Payment ID", Option = PaymentSortOption.PaymentId },
                new SortOption<PaymentSortOption> { DisplayName = "Rental ID", Option = PaymentSortOption.RentalId },
                new SortOption<PaymentSortOption> { DisplayName = "Customer ID", Option = PaymentSortOption.CustomerId },
                new SortOption<PaymentSortOption> { DisplayName = "Customer Name", Option = PaymentSortOption.CustomerName },
                new SortOption<PaymentSortOption> { DisplayName = "Bike ID", Option = PaymentSortOption.BikeId },
                new SortOption<PaymentSortOption> { DisplayName = "Bike Model", Option = PaymentSortOption.BikeModel },
                new SortOption<PaymentSortOption> { DisplayName = "Amount To Pay", Option = PaymentSortOption.AmountToPay },
                new SortOption<PaymentSortOption> { DisplayName = "Amount Paid", Option = PaymentSortOption.AmountPaid },
                new SortOption<PaymentSortOption> { DisplayName = "Payment Date", Option = PaymentSortOption.PaymentDate },
                new SortOption<PaymentSortOption> { DisplayName = "Payment Status", Option = PaymentSortOption.PaymentStatus }
            };
            CurrentSortOption = AvailableSortOptions.First();

            DeletePaymentCommand = new RelayCommand<Payment>(ExecuteDeletePayment);
            ClearCommand = new RelayCommand(() => ExecuteClear());
            NextPageCommand = new RelayCommand(ExecuteNextPage, CanExecuteNextPage);
            PreviousPageCommand = new RelayCommand(ExecutePreviousPage, CanExecutePreviousPage);
            ToggleSortDirectionCommand = new RelayCommand(() => IsAscending = !IsAscending);
            AddPaymentCommand = new RelayCommand(ExecuteAddPayment);
            EditPaymentCommand = new RelayCommand<Payment>(ExecuteEditPayment);

            _ = LoadPayments();
        }

        public ObservableCollection<Payment> Payments
        {
            get => _payments;
            set { _payments = value; OnPropertyChanged(); }
        }

        public System.Collections.IList SelectedPayments
        {
            get => _selectedPayments;
            set { _selectedPayments = value; OnPropertyChanged(); }
        }

        public ObservableCollection<SortOption<PaymentSortOption>> AvailableSortOptions
        {
            get => _availableSortOptions;
            set { _availableSortOptions = value; OnPropertyChanged(); }
        }

        public SortOption<PaymentSortOption> CurrentSortOption
        {
            get => _currentSortOption;
            set { _currentSortOption = value; OnPropertyChanged(); LoadPayments(); }
        }

        public bool IsAscending
        {
            get => _isAscending;
            set 
            { 
                _isAscending = value;
                if (_currentSortOption != null)
                {
                    _currentSortOption.IsAscending = value;
                }
                OnPropertyChanged(); 
                LoadPayments(); 
            }
        }

        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); _currentPage = 1; LoadPayments(); }
        }

        public string CurrentPageDisplay
        {
            get => _currentPageDisplay;
            set { _currentPageDisplay = value; OnPropertyChanged(); }
        }

        public bool IsDialogOpen
        {
            get => _isDialogOpen;
            set { _isDialogOpen = value; OnPropertyChanged(); }
        }

        public Payment SelectedPayment
        {
            get => _selectedPayment;
            set { _selectedPayment = value; OnPropertyChanged(); }
        }

        public ICommand DeletePaymentCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand PreviousPageCommand { get; }
        public ICommand ToggleSortDirectionCommand { get; }
        public ICommand AddPaymentCommand { get; }
        public ICommand EditPaymentCommand { get; }

        public int PageSize
        {
            get => _pageSize;
            set
            {
                if (_pageSize != value)
                {
                    _pageSize = value;
                    OnPropertyChanged();
                    _paymentService.PageSize = value;
                    _ = LoadPayments();
                }
            }
        }

        public void UpdatePageSize(int newSize)
        {
            if (_paymentService.PageSize != newSize)
            {
                _paymentService.PageSize = newSize;
                _ = LoadPayments();
            }
        }

        private async Task LoadPayments()
        {
            try
            {
                var sortOption = new SortOption<PaymentSortOption>
                {
                    Option = CurrentSortOption.Option,
                    IsAscending = IsAscending
                };

                var (payments, totalCount) = await _paymentService.GetPaymentsAsync(_currentPage, SearchText ?? "", CurrentSortOption);

                // Apply status filter if not "All"
                if (SelectedStatusFilter != "All")
                {
                    payments = payments.Where(p => p.PaymentStatus == SelectedStatusFilter).ToList();
                }

                Payments = new ObservableCollection<Payment>(payments);
                _totalPages = (int)Math.Ceiling(totalCount / (double)_paymentService.PageSize);
                CurrentPageDisplay = $"Page {_currentPage} of {_totalPages}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading payments: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ExecuteDeletePayment(Payment payment)
        {
            if (payment == null) return;

            // If multiple items are selected, use the selected payments
            var itemsToDelete = SelectedPayments?.Count > 1 ? SelectedPayments.Cast<Payment>() : new[] { payment };

            var message = itemsToDelete.Count() > 1
                ? $"Are you sure you want to delete {itemsToDelete.Count()} selected payments?"
                : "Are you sure you want to delete this payment?";

            var result = MessageBox.Show(
                message,
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            if (result == MessageBoxResult.Yes)
            {
                foreach (var item in itemsToDelete)
                {
                    await _paymentService.DeletePaymentAsync(item.PaymentId);
                }
                await LoadPayments();
            }
        }

        private async void ExecuteClear()
        {
            var result = MessageBox.Show(
                "Are you sure you want to clear all payments? This action cannot be undone.",
                "Confirm Clear All",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            if (result == MessageBoxResult.Yes)
            {
                await _paymentService.ClearAllPaymentsAsync();
                await LoadPayments();
            }
        }

        private void ExecuteNextPage()
        {
            if (_currentPage < _totalPages)
            {
                _currentPage++;
                _ = LoadPayments();
            }
        }

        private void ExecutePreviousPage()
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                _ = LoadPayments();
            }
        }

        private bool CanExecuteNextPage() => _currentPage < _totalPages;
        private bool CanExecutePreviousPage() => _currentPage > 1;

        public void UpdateSearch(string searchTerm)
        {
            SearchText = searchTerm;
            _currentPage = 1;
            _ = LoadPayments();
        }

        public void SelectPaymentByRentalId(string rentalId)
        {
            if (!string.IsNullOrEmpty(rentalId))
            {
                // Clear any existing search text first
                SearchText = string.Empty;
                
                // First try to find the payment in the current collection
                var existingPayment = Payments.FirstOrDefault(p => p.RentalId == rentalId);
                if (existingPayment != null)
                {
                    SelectedPayment = existingPayment;
                }
                else
                {
                    // If payment is not in current view, search for it
                    SearchText = rentalId;
                    // The LoadPayments() will be called automatically due to SearchText property setter
                    // After loading, we'll select the first payment for this rental
                    var payment = Payments.FirstOrDefault(p => p.RentalId == rentalId);
                    if (payment != null)
                    {
                        SelectedPayment = payment;
                    }
                }
            }
        }

        private async void ExecuteAddPayment()
        {
            var dialog = new AddPaymentDialog();
            if (dialog.ShowDialog() == true)
            {
                await LoadPayments();
            }
        }

        private async void ExecuteEditPayment(Payment payment)
        {
            if (payment == null) return;

            var dialog = new EditPaymentDialog(payment);
            if (dialog.ShowDialog() == true)
            {
                await LoadPayments();
            }
        }
    }
}

