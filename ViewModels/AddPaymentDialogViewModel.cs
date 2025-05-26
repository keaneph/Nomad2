using System;
using System.Windows.Input;
using Nomad2.Models;
using Nomad2.Services;
using System.Threading.Tasks;
using System.Windows;

namespace Nomad2.ViewModels
{
    public class AddPaymentDialogViewModel : BaseViewModel
    {
        private readonly IPaymentService _paymentService;
        private readonly Window _window;
        private string _errorMessage;
        private int _amountPaid;
        private DateTime _paymentDate;

        public string DialogTitle => "Add Payment";
        public string PaymentId { get; private set; }
        public string CustomerName { get; private set; }
        public string BikeModel { get; private set; }
        public string RentalDates { get; private set; }
        public int DailyRate { get; private set; }
        public int TotalAmount { get; private set; }

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

        public AddPaymentDialogViewModel(Window window)
        {
            _paymentService = new PaymentService();
            _window = window;
            _paymentDate = DateTime.Now;

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
            else if (AmountPaid > TotalAmount)
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
                var payment = new Payment
                {
                    PaymentId = await _paymentService.GetLastPaymentIdAsync(),
                    AmountPaid = AmountPaid,
                    PaymentDate = PaymentDate
                };

                await _paymentService.AddPaymentAsync(payment);
                CloseDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving payment: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseDialog()
        {
            _window.Close();
        }
    }
} 