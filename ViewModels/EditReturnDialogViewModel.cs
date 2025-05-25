using Nomad2.Models;
using Nomad2.Services;
using System;
using System.Windows;
using System.Windows.Input;
using System.Threading.Tasks;

namespace Nomad2.ViewModels
{
    public class EditReturnDialogViewModel : BaseViewModel
    {
        private readonly Return _return;
        private readonly Window _dialog;
        private readonly IReturnService _returnService;
        private readonly ICustomerService _customerService;
        private readonly IBikeService _bikeService;
        private readonly IRentalService _rentalService;
        private string _errorMessage;
        private DateTime? _returnDate;
        private string _customerName;
        private string _bikeModel;
        private DateTime _rentalDate;

        public EditReturnDialogViewModel(Return returnItem, Window dialog)
        {
            _return = returnItem ?? throw new ArgumentNullException(nameof(returnItem));
            _dialog = dialog ?? throw new ArgumentNullException(nameof(dialog));
            _returnService = new ReturnService();
            _customerService = new CustomerService();
            _bikeService = new BikeService();
            _rentalService = new RentalService();

            // initialize commands
            SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
            CancelCommand = new RelayCommand(ExecuteCancel);

            // load initial data
            _ = LoadInitialData();
        }

        public string DialogTitle => "Edit Return";

        public string ReturnId => _return.ReturnId;
        public string CustomerId => _return.CustomerId;
        public string BikeId => _return.BikeId;

        public string CustomerName
        {
            get => _customerName;
            set { _customerName = value; OnPropertyChanged(); }
        }

        public string BikeModel
        {
            get => _bikeModel;
            set { _bikeModel = value; OnPropertyChanged(); }
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

        private async Task LoadInitialData()
        {
            try
            {
                // load customer name
                var customer = await _customerService.GetCustomerByIdAsync(_return.CustomerId);
                if (customer != null)
                {
                    CustomerName = customer.Name;
                }

                // load bike model
                var bike = await _bikeService.GetBikeByIdAsync(_return.BikeId);
                if (bike != null)
                {
                    BikeModel = bike.BikeModel;
                }

                // load rental date
                var rental = await _rentalService.GetRentalByIdAsync(_return.RentalId);
                if (rental != null)
                {
                    _rentalDate = rental.RentalDate;
                }

                // set return date
                ReturnDate = _return.ReturnDate;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error loading data: {ex.Message}";
            }
        }

        private bool CanExecuteSave()
        {
            if (!ReturnDate.HasValue)
            {
                ErrorMessage = "Please select a return date";
                return false;
            }

            if (ReturnDate.Value < _rentalDate)
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
                    // update return record
                    _return.ReturnDate = ReturnDate.Value;
                    await _returnService.UpdateReturnAsync(_return);

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