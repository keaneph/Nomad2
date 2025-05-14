using Microsoft.Win32;
using Nomad2.Models;
using Nomad2.Validators;
using System;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Nomad2.ViewModels
{
    public class BikeDialogViewModel : BaseViewModel
    {
        private readonly Bike _bike;
        private readonly Action<bool> _closeCallback;
        private string _errorMessage;
        private readonly bool _isEdit;

        public BikeDialogViewModel(Bike bike, bool isEdit, Action<bool> closeCallback)
        {
            _bike = bike ?? throw new ArgumentNullException(nameof(bike));
            _closeCallback = closeCallback ?? throw new ArgumentNullException(nameof(closeCallback));
            _isEdit = isEdit;

            SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
            CancelCommand = new RelayCommand(ExecuteCancel);
            BrowseCommand = new RelayCommand(ExecuteBrowse);

            StatusOptions = new ObservableCollection<string>
            {
                "Available",
                "Rented",
                "Under Maintenance"
            };
        }

        public string DialogTitle => _isEdit ? "Edit Bike" : "Add Bike";
        public Bike Bike => _bike;
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
                Title = "Select Bike Picture"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                Bike.BikePicture = openFileDialog.FileName;
                OnPropertyChanged(nameof(Bike));
            }
        }

        private bool CanExecuteSave()
        {
            var (isValid, errorMessage) = BikeValidator.ValidateBike(_bike);
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