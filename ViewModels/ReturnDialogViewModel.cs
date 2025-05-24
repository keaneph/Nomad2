using Nomad2.Models;
using System;
using System.Windows;
using System.Windows.Input;

namespace Nomad2.ViewModels
{
    public class ReturnDialogViewModel : BaseViewModel
    {
        private readonly Rental _rental;
        private readonly Window _dialog;
        private string _errorMessage;
        private DateTime? _returnDate;

        public ReturnDialogViewModel(Rental rental, Window dialog)
        {
            _rental = rental ?? throw new ArgumentNullException(nameof(rental));
            _dialog = dialog ?? throw new ArgumentNullException(nameof(dialog));

            // Initialize commands
            SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
            CancelCommand = new RelayCommand(ExecuteCancel);

            // Set default return date to current date
            ReturnDate = DateTime.Now;
        }

        public string DialogTitle => "Return Bike";

        public string RentalId => _rental.RentalId;
        public DateTime RentalDate => _rental.RentalDate;
        public string CustomerName => _rental.Customer?.Name;
        public string CustomerPhone => _rental.Customer?.PhoneNumber;
        public string BikeModel => _rental.Bike?.BikeModel;
        public int DailyRate => _rental.Bike?.DailyRate ?? 0;

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

        private void ExecuteSave()
        {
            if (CanExecuteSave())
            {
                _rental.ReturnDate = ReturnDate.Value;
                _dialog.DialogResult = true;
                _dialog.Close();
            }
        }

        private void ExecuteCancel()
        {
            _dialog.DialogResult = false;
            _dialog.Close();
        }
    }
} 