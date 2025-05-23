using Microsoft.Win32;
using Nomad2.Models;
using Nomad2.Validators;
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Nomad2.ViewModels
{
    // viewModel for the customer add/edit dialog
    public class CustomerDialogViewModel : BaseViewModel
    {
        // private fields to store customer data and dialog state
        private readonly Customer _customer;                // current customer being edited or created
        private readonly Action<bool> _closeCallback;       // callback to close dialog with result
        private string _errorMessage;                       // stores validation error messages
        private readonly bool _isEdit;                      // indicates if this is an edit operation

        public CustomerDialogViewModel(Customer customer, bool isEdit, Action<bool> closeCallback)
        {
            // validate and store constructor parameters
            _customer = customer ?? throw new ArgumentNullException(nameof(customer));
            _closeCallback = closeCallback ?? throw new ArgumentNullException(nameof(closeCallback));
            _isEdit = isEdit;

            // initialize command objects for UI interactions
            SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
            CancelCommand = new RelayCommand(ExecuteCancel);
            BrowseCommand = new RelayCommand(ExecuteBrowse);

            // initialize status options
            StatusOptions = new ObservableCollection<string>
            {
                "Inactive",
                "Active",
                "Blacklisted"
            };

            // Set default status to Inactive when adding a new customer
            if (!_isEdit)
            {
                _customer.CustomerStatus = "Inactive";
            }
        }

        // props for ui binding
        public string DialogTitle => _isEdit ? "Edit Customer" : "Add Customer";
        public Customer Customer => _customer;
        public ObservableCollection<string> StatusOptions { get; }

        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                OnPropertyChanged();
            }
        }

        // commands for UI actions
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand BrowseCommand { get; }

        // command methods
        // saves customer data if validation passes
        private void ExecuteSave()
        {
            if (CanExecuteSave())
            {
                _closeCallback(true);   // close dialog with success result
            }
        }

        private void ExecuteCancel()
        {
            _closeCallback(false); // close dialog with cancel result
        }

        private void ExecuteBrowse()
        {

            // opens the filedialog
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg|All files (*.*)|*.*",
                Title = "Select Government ID Picture"
            };

            // updates customer data if a file is selected
            if (openFileDialog.ShowDialog() == true)
            {
                Customer.GovernmentIdPicture = openFileDialog.FileName;
                OnPropertyChanged(nameof(Customer));
            }
        }

        // validates customer data again before save
        private bool CanExecuteSave()
        {
            var (isValid, errorMessage) = CustomerValidator.ValidateCustomer(_customer);
            if (!isValid)
            {
                ErrorMessage = errorMessage;
                return false;
            }

            ErrorMessage = string.Empty;
            return true;
        }
    }
}