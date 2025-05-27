using System;
using System.Windows.Input;
using Nomad2.Models;
using Nomad2.Services;
using System.Threading.Tasks;
using System.Windows;

namespace Nomad2.ViewModels
{
    public class EditPaymentDialogViewModel : BaseViewModel
    {
        private readonly Payment _payment;
        private readonly Window _dialog;
        private readonly IPaymentService _paymentService;
        private readonly ICustomerService _customerService;
        private readonly IBikeService _bikeService;
        private readonly IRentalService _rentalService;
        private string _errorMessage;
        private DateTime? _paymentDate;
        private string _customerName;
        private string _customerPhone;
        private string _bikeModel;
        private int _amountPaid;

        public EditPaymentDialogViewModel(Payment payment, Window dialog)
        {
            _payment = payment ?? throw new ArgumentNullException(nameof(payment));
            _dialog = dialog ?? throw new ArgumentNullException(nameof(dialog));
            _paymentService = new PaymentService();
            _customerService = new CustomerService();
            _bikeService = new BikeService();
            _rentalService = new RentalService();

            // initialize commands
            SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
            CancelCommand = new RelayCommand(ExecuteCancel);

            // load initial data
            _ = LoadInitialData();
        }

        public string DialogTitle => "Edit Payment";

        public string PaymentId => _payment.PaymentId;
        public string RentalId => _payment.RentalId;
        public string CustomerId => _payment.CustomerId;
        public string BikeId => _payment.BikeId;

        public string CustomerName
        {
            get => _customerName;
            set { _customerName = value; OnPropertyChanged(); }
        }

        public string CustomerPhone
        {
            get => _customerPhone;
            set { _customerPhone = value; OnPropertyChanged(); }
        }

        public string BikeModel
        {
            get => _bikeModel;
            set { _bikeModel = value; OnPropertyChanged(); }
        }

        public DateTime? PaymentDate
        {
            get => _paymentDate;
            set
            {
                _paymentDate = value;
                OnPropertyChanged();
                (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        public int AmountPaid
        {
            get => _amountPaid;
            set
            {
                _amountPaid = value;
                OnPropertyChanged();
                (SaveCommand as RelayCommand)?.RaiseCanExecuteChanged();
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

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        private async Task LoadInitialData()
        {
            try
            {
                // load customer info
                var customer = await _customerService.GetCustomerByIdAsync(_payment.CustomerId);
                if (customer != null)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        CustomerName = customer.Name;
                        CustomerPhone = customer.PhoneNumber;
                    });
                }

                // load bike info
                var bike = await _bikeService.GetBikeByIdAsync(_payment.BikeId);
                if (bike != null)
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        BikeModel = bike.BikeModel;
                    });
                }

                // set payment date and amount
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    PaymentDate = _payment.PaymentDate;
                    AmountPaid = _payment.AmountPaid;
                });
            }
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    ErrorMessage = $"Error loading data: {ex.Message}";
                });
            }
        }

        private bool CanExecuteSave()
        {
            if (!PaymentDate.HasValue)
            {
                ErrorMessage = "Please select a payment date";
                return false;
            }

            if (AmountPaid <= 0)
            {
                ErrorMessage = "Payment amount must be greater than 0";
                return false;
            }

            ErrorMessage = string.Empty;
            return true;
        }

        private async Task<bool> ValidateSaveAsync()
        {
            if (!CanExecuteSave())
            {
                return false;
            }

            // Get the rental date
            var rental = await _rentalService.GetRentalByIdAsync(_payment.RentalId);
            if (rental != null && PaymentDate.Value < rental.RentalDate)
            {
                ErrorMessage = "Payment date cannot be before rental date";
                return false;
            }

            return true;
        }

        private async void ExecuteSave()
        {
            if (await ValidateSaveAsync())
            {
                try
                {
                    // update payment record
                    _payment.PaymentDate = PaymentDate.Value;
                    _payment.AmountPaid = AmountPaid;
                    await _paymentService.UpdatePaymentAsync(_payment);

                    _dialog.DialogResult = true;
                    _dialog.Close();
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"Error saving payment: {ex.Message}";
                }
            }
        }

        private void ExecuteCancel()
        {
            _dialog.DialogResult = false;
            _dialog.Close();
        }
    }
} 