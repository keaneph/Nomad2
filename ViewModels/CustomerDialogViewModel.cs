using Microsoft.Win32;
using Nomad2.Models;
using Nomad2.Validators;
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Nomad2.ViewModels
{
    public class CustomerDialogViewModel : BaseViewModel
    {
        private readonly Customer _customer;
        private readonly Action<bool> _closeCallback;
        private string _errorMessage;
        private readonly bool _isEdit;

        public CustomerDialogViewModel(Customer customer, bool isEdit, Action<bool> closeCallback)
        {
            _customer = customer ?? throw new ArgumentNullException(nameof(customer));
            _closeCallback = closeCallback ?? throw new ArgumentNullException(nameof(closeCallback));
            _isEdit = isEdit;

            // Initialize commands
            SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
            CancelCommand = new RelayCommand(ExecuteCancel);
            BrowseCommand = new RelayCommand(ExecuteBrowse);

            // Initialize status options
            StatusOptions = new ObservableCollection<string>
            {
                "Active",
                "Inactive",
                "Blacklisted"
            };
        }

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

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand BrowseCommand { get; }

        private void ExecuteSave()
        {
            if (CanExecuteSave())
            {
                _closeCallback(true);
            }
        }

        private void ExecuteCancel()
        {
            _closeCallback(false);
        }

        private void ExecuteBrowse()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg|All files (*.*)|*.*",
                Title = "Select Government ID Picture"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                Customer.GovernmentIdPicture = openFileDialog.FileName;
                OnPropertyChanged(nameof(Customer));
            }
        }

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