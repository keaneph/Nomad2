// view model for the bike add/edit dialog window
using Microsoft.Win32;
using Nomad2.Models;
using Nomad2.Validators;
using Nomad2.ViewModels;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows;

public class BikeDialogViewModel : BaseViewModel
{
    // private fields to store bike data and dialog state
    private readonly Bike _bike;
    private readonly Action<bool> _closeCallback;
    private string _errorMessage;
    private readonly bool _isEdit;
    private bool _isInMaintenance;
    private readonly bool _hasActiveRentals;

    // constructor initializes the dialog with bike data and determines if its edit or add mode
    public BikeDialogViewModel(Bike bike, bool isEdit, Action<bool> closeCallback)
    {
        // null checks for required parameters
        _bike = bike ?? throw new ArgumentNullException(nameof(bike));
        _closeCallback = closeCallback ?? throw new ArgumentNullException(nameof(closeCallback));
        _isEdit = isEdit;

        // initialize commands for user interactions
        SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
        CancelCommand = new RelayCommand(ExecuteCancel);
        BrowseCommand = new RelayCommand(ExecuteBrowse);

        // initialize dropdown options for bike status
        StatusOptions = new ObservableCollection<string>
        {
            "Available",
            "Rented",
            "Maintenance"
        };

        // initialize maintenance status
        _isInMaintenance = _bike.BikeStatus == "Maintenance";

        // check if bike has active rentals
        _hasActiveRentals = _bike.BikeStatus == "Rented";
    }

    // properties for binding to the view
    public string DialogTitle => _isEdit ? "Edit Bike" : "Add Bike";
    public Bike Bike => _bike;
    public ObservableCollection<string> StatusOptions { get; }

    public bool IsInMaintenance
    {
        get => _isInMaintenance;
        set
        {
            if (_hasActiveRentals && value)
            {
                MessageBox.Show(
                    "Cannot put a bike in maintenance while it has active rentals",
                    "Invalid Operation",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            _isInMaintenance = value;
            OnPropertyChanged();
            UpdateBikeStatus();
        }
    }

    public bool HasActiveRentals => _hasActiveRentals;

    // error message property with notification
    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            _errorMessage = value;
            OnPropertyChanged();
        }
    }

    // command properties for button bindings
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand BrowseCommand { get; }

    // saves the bike data and closes dialog
    private void ExecuteSave()
    {
        if (CanExecuteSave())
        {
            _closeCallback(true);
        }
    }

    // cancels the operation and closes dialog
    private void ExecuteCancel()
    {
        _closeCallback(false);
    }

    // opens file dialog for selecting bike picture
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

    // validates bike data before saving
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

    private void UpdateBikeStatus()
    {
        if (_isInMaintenance)
        {
            _bike.BikeStatus = "Maintenance";
        }
        else
        {
            // if not in maintenance, status depends on rentals
            // this will be handled by the service layer when saving
            _bike.BikeStatus = "Available";
        }
    }
}