using Nomad2.Models;
using Nomad2.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using Nomad2.Views;
using System.Linq;
using Nomad2.Sorting;
using Microsoft.Win32;

namespace Nomad2.ViewModels
{
    // view model for managing bike inventory and operations
    public class BikesViewModel : BaseViewModel, ISearchable
    {
        // service for database operations
        private readonly IBikeService _bikeService;

        // collection of bikes for display
        private ObservableCollection<Bike> _bikes;

        // fields for search, pagination, and sorting
        private string _searchText;
        private int _currentPage = 1;
        private int _totalPages;
        private string _currentPageDisplay;
        private bool _isDialogOpen;
        private Bike _selectedBike;
        private System.Collections.IList _selectedBikes;
        private SortOption<BikeSortOption> _currentSortOption;
        private ObservableCollection<SortOption<BikeSortOption>> _availableSortOptions;
        private bool _isAscending = true;

        // property for sorting direction with automatic refresh
        public bool IsAscending
        {
            get => _isAscending;
            set
            {
                _isAscending = value;
                OnPropertyChanged();
                LoadBikes(); // refresh when sort direction changes
            }
        }

        #region Filtering

        // current status filter
        private string _selectedStatusFilter = "All";
        public string SelectedStatusFilter
        {
            get => _selectedStatusFilter;
            set
            {
                _selectedStatusFilter = value;
                OnPropertyChanged();
                LoadBikes();
            }
        }

        // available status filter options
        public ObservableCollection<string> StatusFilters { get; } = new ObservableCollection<string>
        {
            "All",
            "Available",
            "Rented",
            "Maintenance"
        };

        #endregion

        // commands for UI interactions
        public ICommand ToggleSortDirectionCommand { get; }
        public ICommand ViewImageCommand { get; }

        // constructor initializes services, collections, and commands
        public BikesViewModel()
        {
            Title = "Bikes";
            Description = "Manage bike inventory";

            _bikeService = new BikeService();
            Bikes = new ObservableCollection<Bike>();

            // initializes bike management and commands
            AvailableSortOptions = new ObservableCollection<SortOption<BikeSortOption>>
{
            new SortOption<BikeSortOption> { DisplayName = "ID", Option = BikeSortOption.BikeId },
            new SortOption<BikeSortOption> { DisplayName = "Model", Option = BikeSortOption.BikeModel },
            new SortOption<BikeSortOption> { DisplayName = "Type", Option = BikeSortOption.BikeType },
            new SortOption<BikeSortOption> { DisplayName = "Rate", Option = BikeSortOption.DailyRate },
            new SortOption<BikeSortOption> { DisplayName = "Status", Option = BikeSortOption.Status }
        };

            CurrentSortOption = AvailableSortOptions.First();

            AddBikeCommand = new RelayCommand(ExecuteAddBike);
            EditBikeCommand = new RelayCommand<Bike>(ExecuteEditBike);
            DeleteBikeCommand = new RelayCommand<Bike>(ExecuteDeleteBike);
            ClearCommand = new RelayCommand(() => ExecuteClear());
            NextPageCommand = new RelayCommand(ExecuteNextPage, CanExecuteNextPage);
            PreviousPageCommand = new RelayCommand(ExecutePreviousPage, CanExecutePreviousPage);
            ViewImageCommand = new RelayCommand<string>(ExecuteViewImage);
            ToggleSortDirectionCommand = new RelayCommand(() => IsAscending = !IsAscending);

            _ = LoadBikes();
        }

        #region Properties

        // displays bike image in popup window
        private void ExecuteViewImage(string imagePath)
        {
            if (!string.IsNullOrEmpty(imagePath))
            {
                var imageViewer = new ImageViewerWindow(imagePath);
                imageViewer.ShowDialog();
            }
        }

        // updates the page size and reloads bikes if it changes
        public void UpdatePageSize(int newSize)
        {
            if (_bikeService.PageSize != newSize)
            {
                _bikeService.PageSize = newSize;
                _ = LoadBikes();
            }
        }

        // properties for data binding
        public ObservableCollection<Bike> Bikes
        {
            get => _bikes;
            set
            {
                _bikes = value;
                OnPropertyChanged();
            }
        }

        // property for selected bikes (for multi-select)
        public System.Collections.IList SelectedBikes
        {
            get => _selectedBikes;
            set
            {
                _selectedBikes = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<SortOption<BikeSortOption>> AvailableSortOptions
        {
            get => _availableSortOptions;
            set
            {
                _availableSortOptions = value;
                OnPropertyChanged();
            }
        }

        public SortOption<BikeSortOption> CurrentSortOption
        {
            get => _currentSortOption;
            set
            {
                _currentSortOption = value;
                OnPropertyChanged();
                LoadBikes();
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                _currentPage = 1; // reset to first page when searching
                LoadBikes();
            }
        }

        public string CurrentPageDisplay
        {
            get => $"Page {_currentPage} of {_totalPages}";
            set
            {
                _currentPageDisplay = value;
                OnPropertyChanged();
            }
        }

        public bool IsDialogOpen
        {
            get => _isDialogOpen;
            set
            {
                _isDialogOpen = value;
                OnPropertyChanged();
            }
        }

        public Bike SelectedBike
        {
            get => _selectedBike;
            set
            {
                _selectedBike = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Commands

        public ICommand AddBikeCommand { get; }
        public ICommand EditBikeCommand { get; }
        public ICommand DeleteBikeCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand PreviousPageCommand { get; }

        #endregion

        #region Command Methods

        // creates and adds new bike to database
        private async void ExecuteAddBike()
        {
            var newBike = new Bike
            {
                BikeId = await GenerateNewBikeId(),
                BikeStatus = "Available" // Default status
            };

            var dialog = new BikeDialog(newBike, false);
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    await _bikeService.AddBikeAsync(newBike);
                    _currentPage = 1; // Reset to first page
                    await LoadBikes(); // Refresh the bike list
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error adding bike: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // updates existing bike information
        private async void ExecuteEditBike(Bike bike)
        {
            if (bike != null)
            {
                var dialog = new BikeDialog(bike, true);
                if (dialog.ShowDialog() == true)
                {
                    try
                    {
                        await _bikeService.UpdateBikeAsync(bike);
                        await LoadBikes(); // Refresh the bike list
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error updating bike: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        // single delete for bikes
        private async void ExecuteDeleteBike(Bike bike)
        {
            if (SelectedBikes != null && SelectedBikes.Count > 0)
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to delete {SelectedBikes.Count} bikes?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        foreach (Bike selectedBike in SelectedBikes.Cast<Bike>().ToList())
                        {
                            await _bikeService.DeleteBikeAsync(selectedBike.BikeId);
                        }
                        await LoadBikes();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting bikes: {ex.Message}", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else if (bike != null)
            {
                var result = MessageBox.Show(
                    "Are you sure you want to delete this bike?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        await _bikeService.DeleteBikeAsync(bike.BikeId);
                        await LoadBikes();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting bike: {ex.Message}", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        // deletes all bikes
        private async void ExecuteClear()
        {
            var result = MessageBox.Show(
                "Are you sure you want to delete ALL bikes? This action cannot be undone.",
                "Confirm Delete All",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    bool success = await _bikeService.ClearAllBikesAsync();

                    if (success)
                    {
                        Bikes.Clear();
                        _currentPage = 1;
                        _totalPages = 0;
                        OnPropertyChanged(nameof(CurrentPageDisplay));

                        MessageBox.Show("All bikes have been deleted successfully.",
                            "Success",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Failed to delete all bikes.",
                            "Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred: {ex.Message}",
                        "Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        // pagination
        private void ExecuteNextPage()
        {
            if (_currentPage < _totalPages)
            {
                _currentPage++;
                LoadBikes();
            }
        }

        private void ExecutePreviousPage()
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                LoadBikes();
            }
        }

        #endregion

        #region Helper Methods

        // loads bikes with current filters and sorting
        private async Task LoadBikes()
        {
            try
            {
                var sortOption = new SortOption<BikeSortOption>
                {
                    Option = CurrentSortOption.Option,
                    IsAscending = IsAscending
                };

                var (bikes, totalCount) = await _bikeService.GetBikesAsync(
                    _currentPage, SearchText, sortOption);

                // Apply status filter if not "All"
                if (SelectedStatusFilter != "All")
                {
                    bikes = bikes.Where(b => b.BikeStatus == SelectedStatusFilter).ToList();
                }

                Bikes.Clear();
                foreach (var bike in bikes)
                {
                    Bikes.Add(bike);
                }

                _totalPages = (int)Math.Ceiling(totalCount / (double)_bikeService.PageSize);
                OnPropertyChanged(nameof(CurrentPageDisplay));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading bikes: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanExecuteNextPage()
        {
            return _currentPage < _totalPages;
        }

        private bool CanExecutePreviousPage()
        {
            return _currentPage > 1;
        }

        // generates next available bike id
        private async Task<string> GenerateNewBikeId()
        {
            string lastId = await _bikeService.GetLastBikeIdAsync();

            if (string.IsNullOrEmpty(lastId))
            {
                return "BIKE-0001";
            }

            string[] parts = lastId.Split('-');
            if (parts.Length == 2 && int.TryParse(parts[1], out int lastNumber))
            {
                int newNumber = lastNumber + 1;
                return $"BIKE-{newNumber:D4}";
            }

            return "BIKE-0001";
        }

        #endregion

        public void UpdateSearch(string searchTerm)
        {
            SearchText = searchTerm;
        }

        public void SelectBike(Bike bike)
        {
            if (bike != null)
            {
                // find the bike in the current collection
                var existingBike = Bikes.FirstOrDefault(b => b.BikeId == bike.BikeId);
                if (existingBike != null)
                {
                    SelectedBike = existingBike;
                }
                else
                {
                    // if bike is not in current view, search for it
                    SearchText = bike.BikeId;
                    // the LoadBikes() will be called automatically due to SearchText property setter
                    // after loading, we'll select the bike
                    SelectedBike = bike;
                }
            }
        }
    }
}