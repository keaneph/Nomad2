using Nomad2.Models;
using Nomad2.Services;
using System;
using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;

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
                    // create return record
                    var returnRecord = new Return
                    {
                        ReturnId = ReturnId, // use the pre-generated ID
                        RentalId = _rental.RentalId,
                        CustomerId = _rental.CustomerId,
                        BikeId = _rental.BikeId,
                        ReturnDate = ReturnDate.Value
                    };

                    // add return record
                    await _returnService.AddReturnAsync(returnRecord);

                    // check if customer has any more active rentals
                    var activeRentals = await _rentalService.GetActiveRentalsByCustomerAsync(_rental.CustomerId);
                    if (activeRentals.Count == 0)
                    {
                        var customer = await _customerService.GetCustomerByIdAsync(_rental.CustomerId);
                        if (customer != null)
                        {
                            customer.CustomerStatus = "Inactive";
                            await _customerService.UpdateCustomerAsync(customer);
                        }
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