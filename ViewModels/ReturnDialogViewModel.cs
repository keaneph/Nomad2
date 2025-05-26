using Nomad2.Models;
using Nomad2.Services;
using Nomad2.Views;
using System;
using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;
using System.Linq;

namespace Nomad2.ViewModels
{
    public class ReturnDialogViewModel : BaseViewModel
    {
        private readonly Rental _rental;
        private readonly Window _dialog;
        private readonly IReturnService _returnService;
        private readonly IRentalService _rentalService;
        private readonly ICustomerService _customerService;
        private readonly IBikeService _bikeService;
        private readonly IPaymentService _paymentService;
        private string _errorMessage;
        private DateTime? _returnDate;

        public ReturnDialogViewModel(Rental rental, Window dialog)
        {
            _rental = rental ?? throw new ArgumentNullException(nameof(rental));
            _dialog = dialog ?? throw new ArgumentNullException(nameof(dialog));
            _returnService = new ReturnService();
            _rentalService = new RentalService();
            _customerService = new CustomerService();
            _bikeService = new BikeService();
            _paymentService = new PaymentService();

            // initialize commands
            SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
            CancelCommand = new RelayCommand(ExecuteCancel);

            // set default return date to current date
            ReturnDate = DateTime.Now;

            // Generate and set the next return ID
            _ = GenerateNextReturnId();
        }

        public string DialogTitle => "Return Bike";

        public string ReturnId { get; private set; }
        public string RentalId => _rental.RentalId;
        public DateTime RentalDate => _rental.RentalDate;
        public string CustomerName => _rental.Customer?.Name;
        public string CustomerPhone => _rental.Customer?.PhoneNumber;
        public string BikeModel => _rental.Bike?.BikeModel;
        public int DailyRate => _rental.Bike?.DailyRate ?? 0;

        private async Task GenerateNextReturnId()
        {
            try
            {
                var lastId = await _returnService.GetLastReturnIdAsync();
                if (string.IsNullOrWhiteSpace(lastId) || lastId == "0000-0000")
                {
                    ReturnId = "0000-0001";
                }
                else
                {
                    string[] parts = lastId.Split('-');
                    if (parts.Length == 2 && int.TryParse(parts[1], out int number))
                    {
                        ReturnId = $"{parts[0]}-{(number + 1):D4}";
                    }
                    else
                    {
                        ReturnId = "0000-0001";
                    }
                }
                OnPropertyChanged(nameof(ReturnId));
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error generating return ID: {ex.Message}";
            }
        }

        public DateTime? ReturnDate
        {
            get => _returnDate;
            set
            {
                _returnDate = value;
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

        private bool CanExecuteSave()
        {
            if (!ReturnDate.HasValue)
            {
                ErrorMessage = "Please select a return date";
                return false;
            }

            if (ReturnDate.Value < RentalDate)
            {
                ErrorMessage = "Return date cannot be before rental date";
                return false;
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
                    // Check if return record already exists for this rental
                    var existingReturns = await _returnService.GetAllReturnsAsync();
                    var existingReturn = existingReturns.FirstOrDefault(r => r.RentalId == _rental.RentalId);
                    if (existingReturn != null)
                    {
                        // If return record exists, just close the dialog
                        _dialog.DialogResult = true;
                        _dialog.Close();
                        return;
                    }

                    // Calculate total amount due using the return date
                    int daysRented = (ReturnDate.Value - _rental.RentalDate).Days + 1; // Include both start and end day
                    int totalAmount = (_rental.Bike?.DailyRate ?? 0) * daysRented;
                    int totalPaid = await _paymentService.GetTotalPaidForRentalAsync(_rental.RentalId);
                    int remainingBalance = totalAmount - totalPaid;

                    if (remainingBalance > 0)
                    {
                        // Show PaymentDialog for manual payment
                        _rental.ReturnDate = ReturnDate.Value;
                        var paymentDialog = new PaymentDialog(_rental, isCompletionPayment: true);
                        paymentDialog.Owner = _dialog;
                        if (paymentDialog.ShowDialog() != true)
                        {
                            return;
                        }
                    }
                    else
                    {
                        // Auto-create completion payment record
                        string lastPaymentId = await _paymentService.GetLastPaymentIdAsync();
                        string completionPaymentId;
                        if (string.IsNullOrWhiteSpace(lastPaymentId) || lastPaymentId == "0000-0000")
                        {
                            completionPaymentId = "0000-0001";
                        }
                        else
                        {
                            string[] parts = lastPaymentId.Split('-');
                            if (parts.Length == 2 && int.TryParse(parts[1], out int number))
                            {
                                completionPaymentId = $"{parts[0]}-{(number + 1):D4}";
                            }
                            else
                            {
                                completionPaymentId = "0000-0001";
                            }
                        }

                        var completionPayment = new Payment
                        {
                            PaymentId = completionPaymentId,
                            RentalId = _rental.RentalId,
                            CustomerId = _rental.CustomerId,
                            BikeId = _rental.BikeId,
                            AmountToPay = totalAmount,
                            AmountPaid = totalAmount,
                            PaymentDate = DateTime.Now,
                            PaymentStatus = "Paid"
                        };

                        await _paymentService.AddPaymentAsync(completionPayment);

                        // If there's an excess payment, create refund record
                        if (totalPaid > totalAmount)
                        {
                            int refundAmount = totalPaid - totalAmount;
                            string refundPaymentId;
                            lastPaymentId = await _paymentService.GetLastPaymentIdAsync();
                            if (string.IsNullOrWhiteSpace(lastPaymentId) || lastPaymentId == "0000-0000")
                            {
                                refundPaymentId = "0000-0001";
                            }
                            else
                            {
                                string[] parts = lastPaymentId.Split('-');
                                if (parts.Length == 2 && int.TryParse(parts[1], out int number))
                                {
                                    refundPaymentId = $"{parts[0]}-{(number + 1):D4}";
                                }
                                else
                                {
                                    refundPaymentId = "0000-0001";
                                }
                            }

                            var refundPayment = new Payment
                            {
                                PaymentId = refundPaymentId,
                                RentalId = _rental.RentalId,
                                CustomerId = _rental.CustomerId,
                                BikeId = _rental.BikeId,
                                AmountToPay = null,
                                AmountPaid = -refundAmount,
                                PaymentDate = DateTime.Now,
                                PaymentStatus = "Refunded"
                            };

                            await _paymentService.AddPaymentAsync(refundPayment);
                        }
                    }

                    // Create and save return record first
                    var returnRecord = new Return
                    {
                        ReturnId = ReturnId,
                        RentalId = _rental.RentalId,
                        CustomerId = _rental.CustomerId,
                        BikeId = _rental.BikeId,
                        ReturnDate = ReturnDate.Value
                    };

                    // Update rental status to completed
                    _rental.RentalStatus = "Completed";
                    _rental.ReturnDate = ReturnDate.Value;

                    // Update bike status to available
                    var bike = await _bikeService.GetBikeByIdAsync(_rental.BikeId);
                    if (bike != null)
                    {
                        bike.BikeStatus = "Available";
                    }

                    // Check if customer has any more active rentals
                    var activeRentals = await _rentalService.GetActiveRentalsByCustomerAsync(_rental.CustomerId);
                    var customer = await _customerService.GetCustomerByIdAsync(_rental.CustomerId);
                    if (activeRentals.Count == 0 && customer != null)
                    {
                        customer.CustomerStatus = "Inactive";
                    }

                    // Save all changes in a single transaction
                    var success = await _returnService.AddReturnWithStatusUpdatesAsync(returnRecord, _rental, bike, customer);
                    if (!success)
                    {
                        ErrorMessage = "Failed to save return record";
                        return;
                    }

                    _dialog.DialogResult = true;
                    _dialog.Close();
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"Error saving return: {ex.Message}";
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