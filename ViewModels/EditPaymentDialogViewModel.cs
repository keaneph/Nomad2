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
        private readonly IPaymentService _paymentService;
        private readonly Payment _payment;
        private string _errorMessage;
        private int _amountPaid;
        private DateTime _paymentDate;

        public string DialogTitle => "Edit Payment";
        public string PaymentId => _payment.PaymentId;
        public string CustomerName => _payment.Customer?.Name;
        public string BikeModel => _payment.Bike?.BikeModel;
        public string RentalDates => $"{_payment.PaymentDate:d}";
        public int DailyRate => _payment.Bike?.DailyRate ?? 0;
        public int? TotalAmount => _payment.AmountToPay;

        public int AmountPaid
        {
            get => _amountPaid;
            set
            {
                _amountPaid = value;
                OnPropertyChanged(nameof(AmountPaid));
                ValidateAmountPaid();
            }
        }

        public DateTime PaymentDate
        {
            get => _paymentDate;
            set
            {
                _paymentDate = value;
                OnPropertyChanged(nameof(PaymentDate));
                ValidatePaymentDate();
            }
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                OnPropertyChanged(nameof(ErrorMessage));
            }
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public EditPaymentDialogViewModel(Payment payment)
        {
            _paymentService = new PaymentService();
            _payment = payment;
            _amountPaid = payment.AmountPaid;
            _paymentDate = payment.PaymentDate;

            SaveCommand = new RelayCommand(async () => await SavePayment(), () => IsValid());
            CancelCommand = new RelayCommand(() => CloseDialog());
        }

        private bool IsValid()
        {
            return string.IsNullOrEmpty(ErrorMessage) &&
                   AmountPaid >= 0 &&
                   PaymentDate != default;
        }

        private void ValidateAmountPaid()
        {
            if (AmountPaid < 0)
            {
                ErrorMessage = "Amount paid cannot be negative";
            }
            else if (TotalAmount.HasValue && AmountPaid > TotalAmount.Value)
            {
                ErrorMessage = "Amount paid cannot exceed total amount";
            }
            else
            {
                ErrorMessage = string.Empty;
            }
        }

        private void ValidatePaymentDate()
        {
            if (PaymentDate == default)
            {
                ErrorMessage = "Please select a payment date";
            }
            else
            {
                ErrorMessage = string.Empty;
            }
        }

        private async Task SavePayment()
        {
            try
            {
                _payment.AmountPaid = AmountPaid;
                _payment.PaymentDate = PaymentDate;

                await _paymentService.UpdatePaymentAsync(_payment);
                CloseDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving payment: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseDialog()
        {
            if (Application.Current.Windows.Count > 0)
            {
                Application.Current.Windows[Application.Current.Windows.Count - 1].Close();
            }
        }
    }
} 