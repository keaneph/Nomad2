using Nomad2.Models;
using Nomad2.Services;
using System;
using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;

namespace Nomad2.ViewModels
{
    public class PaymentDialogViewModel : BaseViewModel
    {
        private readonly Rental _rental;
        private readonly Window _dialog;
        private readonly IPaymentService _paymentService;
        private readonly IRentalService _rentalService;
        private readonly ICustomerService _customerService;
        private readonly IBikeService _bikeService;
        private readonly bool _isCompletionPayment;
        private string _errorMessage;
        private DateTime? _paymentDate;
        private int _amountPaid;
        private int? _amountToPay;
        private int _totalPaidSoFar;
        private int _remainingBalance;
        private int _daysRented;
        private string _amountPaidLabel;

        public PaymentDialogViewModel(Rental rental, Window dialog, bool isCompletionPayment = false)
        {
            _rental = rental ?? throw new ArgumentNullException(nameof(rental));
            _dialog = dialog ?? throw new ArgumentNullException(nameof(dialog));
            _paymentService = new PaymentService();
            _rentalService = new RentalService();
            _customerService = new CustomerService();
            _bikeService = new BikeService();
            _isCompletionPayment = isCompletionPayment;

            // initialize commands
            SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
            CancelCommand = new RelayCommand(ExecuteCancel);

            // set default payment date to current date
            PaymentDate = DateTime.Now;

            // Generate and set the next payment ID
            _ = GenerateNextPaymentId();

            // Fetch total paid so far and calculate amounts
            _ = InitializePaymentState();
        }

        public string DialogTitle => _isCompletionPayment ? "Complete Payment" : "Process Payment";
        public bool IsCompletionPayment => _isCompletionPayment;
        public string AmountPaidLabel
        {
            get => _isCompletionPayment ? "Remaining Balance" : "Total Paid So Far";
            set
            {
                _amountPaidLabel = value;
                OnPropertyChanged();
            }
        }

        public string PaymentId { get; private set; }
        public string RentalId => _rental.RentalId;
        public DateTime RentalDate => _rental.RentalDate;
        public string CustomerName => _rental.Customer?.Name;
        public string BikeModel => _rental.Bike?.BikeModel;
        public int DailyRate => _rental.Bike?.DailyRate ?? 0;
        public string RentalDates => $"{RentalDate:MM/dd/yyyy}";
        public int DaysRented => _daysRented;

        public int? AmountToPay
        {
            get => _amountToPay;
            set
            {
                _amountToPay = value;
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

        public int TotalPaidSoFar => _totalPaidSoFar;
        public int RemainingBalance => _remainingBalance;

        private async Task GenerateNextPaymentId()
        {
            try
            {
                var lastId = await _paymentService.GetLastPaymentIdAsync();
                if (string.IsNullOrWhiteSpace(lastId) || lastId == "0000-0000")
                {
                    PaymentId = "0000-0001";
                }
                else
                {
                    string[] parts = lastId.Split('-');
                    if (parts.Length == 2 && int.TryParse(parts[1], out int number))
                    {
                        PaymentId = $"{parts[0]}-{(number + 1):D4}";
                    }
                    else
                    {
                        PaymentId = "0000-0001";
                    }
                }
                OnPropertyChanged(nameof(PaymentId));
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error generating payment ID: {ex.Message}";
            }
        }

        private async Task InitializePaymentState()
        {
            _totalPaidSoFar = await _paymentService.GetTotalPaidForRentalAsync(_rental.RentalId);
            
            if (_isCompletionPayment)
            {
                DateTime endDate = _rental.ReturnDate ?? DateTime.Now;
                _daysRented = (endDate - _rental.RentalDate).Days + 1;
                _amountToPay = _daysRented * DailyRate;
                _remainingBalance = (_amountToPay ?? 0) - _totalPaidSoFar;
                AmountPaid = _remainingBalance;
            }
            else
            {
                // For advance payment, amount to pay is unknown
                _amountToPay = null;
                _remainingBalance = 0;
                AmountPaid = 0;
            }
            OnPropertyChanged(nameof(AmountToPay));
            OnPropertyChanged(nameof(AmountPaid));
            OnPropertyChanged(nameof(TotalPaidSoFar));
            OnPropertyChanged(nameof(RemainingBalance));
            OnPropertyChanged(nameof(DaysRented));
        }

        private bool CanExecuteSave()
        {
            if (!PaymentDate.HasValue)
            {
                ErrorMessage = "Please select a payment date";
                return false;
            }

            if (PaymentDate.Value > DateTime.Now)
            {
                ErrorMessage = "Payment date cannot be in the future";
                return false;
            }

            if (_isCompletionPayment)
            {
                if (AmountPaid != _remainingBalance)
                {
                    ErrorMessage = $"Amount paid must match the remaining balance: {_remainingBalance}";
                    return false;
                }
            }
            else
            {
                if (AmountPaid <= 0)
                {
                    ErrorMessage = "Amount paid must be greater than 0";
                    return false;
                }
                // If _amountToPay is set, check for overpayment
                if (_amountToPay.HasValue && AmountPaid > _amountToPay.Value)
                {
                    ErrorMessage = $"Amount paid cannot exceed amount to pay: {_amountToPay.Value}";
                    return false;
                }
            }

            ErrorMessage = string.Empty;
            return true;
        }

        private async void ExecuteSave()
        {
            if (CanExecuteSave())
            {
                try
                {
                    if (string.IsNullOrEmpty(PaymentId))
                    {
                        PaymentId = await _paymentService.GetLastPaymentIdAsync();
                        if (string.IsNullOrEmpty(PaymentId) || PaymentId == "0000-0000")
                        {
                            PaymentId = "0000-0001";
                        }
                        else
                        {
                            string[] parts = PaymentId.Split('-');
                            if (parts.Length == 2 && int.TryParse(parts[1], out int number))
                            {
                                PaymentId = $"{parts[0]}-{(number + 1):D4}";
                            }
                            else
                            {
                                PaymentId = "0000-0001";
                            }
                        }
                    }

                    string paymentStatus;
                    int? amountToPayToSave = _amountToPay;
                    if (_isCompletionPayment)
                    {
                        paymentStatus = "Paid";
                        // For completion payment, amount to pay should be the total rental cost
                        amountToPayToSave = _daysRented * DailyRate;
                    }
                    else
                    {
                        // For advance payment, set amount_to_pay = null
                        amountToPayToSave = null;
                        if (AmountPaid == 0)
                        {
                            paymentStatus = "Unpaid";
                        }
                        else
                        {
                            paymentStatus = "Pending";
                        }
                    }

                    var payment = new Payment
                    {
                        PaymentId = PaymentId,
                        RentalId = _rental.RentalId,
                        CustomerId = _rental.CustomerId,
                        BikeId = _rental.BikeId,
                        AmountToPay = amountToPayToSave,
                        AmountPaid = AmountPaid,
                        PaymentDate = PaymentDate.Value,
                        PaymentStatus = paymentStatus
                    };

                    var success = await _paymentService.AddPaymentAsync(payment);
                    if (!success)
                    {
                        ErrorMessage = "Failed to save payment";
                        return;
                    }

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