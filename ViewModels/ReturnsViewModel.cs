using Nomad2.Models;
using Nomad2.Services;
using Nomad2.Sorting;
using Nomad2.Views;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;

namespace Nomad2.ViewModels
{
    public class ReturnsViewModel : BaseViewModel, ISearchable
    {
        private readonly IReturnService _returnService;
        private ObservableCollection<Return> _returns;
        private string _searchText;
        private int _currentPage = 1;
        private int _totalPages;
        private string _currentPageDisplay;
        private bool _isDialogOpen;
        private Return _selectedReturn;
        private System.Collections.IList _selectedReturns;
        private SortOption<ReturnSortOption> _currentSortOption;
        private ObservableCollection<SortOption<ReturnSortOption>> _availableSortOptions;
        private bool _isAscending = true;
        private int _pageSize = 10;

        public ReturnsViewModel()
        {
            Title = "Returns";
            Description = "Manage returns";

            _returnService = new ReturnService();
            Returns = new ObservableCollection<Return>();

            AvailableSortOptions = new ObservableCollection<SortOption<ReturnSortOption>>
            {
                new SortOption<ReturnSortOption> { DisplayName = "Return ID", Option = ReturnSortOption.ReturnId },
                new SortOption<ReturnSortOption> { DisplayName = "Rental ID", Option = ReturnSortOption.RentalId },
                new SortOption<ReturnSortOption> { DisplayName = "Customer ID", Option = ReturnSortOption.CustomerId },
                new SortOption<ReturnSortOption> { DisplayName = "Customer Name", Option = ReturnSortOption.CustomerName },
                new SortOption<ReturnSortOption> { DisplayName = "Bike ID", Option = ReturnSortOption.BikeId },
                new SortOption<ReturnSortOption> { DisplayName = "Bike Model", Option = ReturnSortOption.BikeModel },
                new SortOption<ReturnSortOption> { DisplayName = "Return Date", Option = ReturnSortOption.ReturnDate }
            };
            CurrentSortOption = AvailableSortOptions.First();

            AddReturnCommand = new RelayCommand(ExecuteAddReturn);
            EditReturnCommand = new RelayCommand<Return>(ExecuteEditReturn);
            DeleteReturnCommand = new RelayCommand<Return>(ExecuteDeleteReturn);
            ClearCommand = new RelayCommand(() => ExecuteClear());
            NextPageCommand = new RelayCommand(ExecuteNextPage, CanExecuteNextPage);
            PreviousPageCommand = new RelayCommand(ExecutePreviousPage, CanExecutePreviousPage);
            ToggleSortDirectionCommand = new RelayCommand(() => IsAscending = !IsAscending);

            _ = LoadReturns();
        }

        public ObservableCollection<Return> Returns
        {
            get => _returns;
            set { _returns = value; OnPropertyChanged(); }
        }

        public System.Collections.IList SelectedReturns
        {
            get => _selectedReturns;
            set { _selectedReturns = value; OnPropertyChanged(); }
        }

        public ObservableCollection<SortOption<ReturnSortOption>> AvailableSortOptions
        {
            get => _availableSortOptions;
            set { _availableSortOptions = value; OnPropertyChanged(); }
        }

        public SortOption<ReturnSortOption> CurrentSortOption
        {
            get => _currentSortOption;
            set { _currentSortOption = value; OnPropertyChanged(); LoadReturns(); }
        }

        public bool IsAscending
        {
            get => _isAscending;
            set 
            { 
                _isAscending = value;
                if (_currentSortOption != null)
                {
                    _currentSortOption.IsAscending = value;
                }
                OnPropertyChanged(); 
                LoadReturns(); 
            }
        }

        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); _currentPage = 1; LoadReturns(); }
        }

        public string CurrentPageDisplay
        {
            get => _currentPageDisplay;
            set { _currentPageDisplay = value; OnPropertyChanged(); }
        }

        public bool IsDialogOpen
        {
            get => _isDialogOpen;
            set { _isDialogOpen = value; OnPropertyChanged(); }
        }

        public Return SelectedReturn
        {
            get => _selectedReturn;
            set { _selectedReturn = value; OnPropertyChanged(); }
        }

        public ICommand AddReturnCommand { get; }
        public ICommand EditReturnCommand { get; }
        public ICommand DeleteReturnCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand PreviousPageCommand { get; }
        public ICommand ToggleSortDirectionCommand { get; }

        public int PageSize
        {
            get => _pageSize;
            set
            {
                if (_pageSize != value)
                {
                    _pageSize = value;
                    OnPropertyChanged();
                    _returnService.PageSize = value;
                    _ = LoadReturns();
                }
            }
        }

        public void UpdatePageSize(int newSize)
        {
            if (_returnService.PageSize != newSize)
            {
                _returnService.PageSize = newSize;
                _ = LoadReturns();
            }
        }

        private async void ExecuteAddReturn()
        {
            var dialog = new AddReturnDialog();
            if (dialog.ShowDialog() == true)
            {
                await LoadReturns();
            }
        }

        private async void ExecuteEditReturn(Return returnItem)
        {
            if (returnItem == null) return;

            var dialog = new EditReturnDialog(returnItem);
            if (dialog.ShowDialog() == true)
            {
                await LoadReturns();
            }
        }

        private async void ExecuteDeleteReturn(Return returnItem)
        {
            if (returnItem == null) return;

            var result = MessageBox.Show(
                "Are you sure you want to delete this return?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            if (result == MessageBoxResult.Yes)
            {
                await _returnService.DeleteReturnAsync(returnItem.ReturnId);
                await LoadReturns();
            }
        }

        private async void ExecuteClear()
        {
            var result = MessageBox.Show(
                "Are you sure you want to clear all returns? This action cannot be undone.",
                "Confirm Clear All",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            if (result == MessageBoxResult.Yes)
            {
                await _returnService.ClearAllReturnsAsync();
                await LoadReturns();
            }
        }

        private void ExecuteNextPage()
        {
            if (_currentPage < _totalPages)
            {
                _currentPage++;
                _ = LoadReturns();
            }
        }

        private void ExecutePreviousPage()
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                _ = LoadReturns();
            }
        }

        private bool CanExecuteNextPage() => _currentPage < _totalPages;
        private bool CanExecutePreviousPage() => _currentPage > 1;

        private async Task LoadReturns()
        {
            var (returns, totalCount) = await _returnService.GetReturnsAsync(_currentPage, _searchText ?? string.Empty, _currentSortOption);
            Returns = new ObservableCollection<Return>(returns);
            _totalPages = (int)Math.Ceiling((double)totalCount / _returnService.PageSize);
            CurrentPageDisplay = $"Page {_currentPage} of {_totalPages}";
        }

        public void UpdateSearch(string searchTerm)
        {
            SearchText = searchTerm;
        }

        public void SelectRental(Rental rental)
        {
            if (rental != null)
            {
                // find the return record for this rental
                var returnRecord = Returns.FirstOrDefault(r => r.RentalId == rental.RentalId);
                if (returnRecord != null)
                {
                    SelectedReturn = returnRecord;
                }
                else
                {
                    // if return record is not in current view, search for it
                    SearchText = rental.RentalId;
                    // the LoadReturns() will be called automatically due to SearchText property setter
                    // after loading, we'll select the return record
                    SelectedReturn = returnRecord;
                }
            }
        }
    }
}
