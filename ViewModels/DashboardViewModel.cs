using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Nomad2.Services;
using Nomad2.Sorting;
using LiveCharts;
using LiveCharts.Wpf;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Windows;
using System.Collections.Generic;
using Nomad2.Models;

namespace Nomad2.ViewModels
{
    public class DashboardViewModel : BaseViewModel
    {
        private readonly ICustomerService _customerService;
        private readonly IBikeService _bikeService;
        private readonly IRentalService _rentalService;
        private readonly IPaymentService _paymentService;
        
        private int _totalCustomers;
        private int _totalRevenue;
        private int _totalBikes;
        private int _activeRentals;
        
        // LiveCharts properties for dashboard charts
        public SeriesCollection MonthlyRevenueSeries { get; set; }
        public ObservableCollection<string> MonthlyRevenueLabels { get; set; }
        public SeriesCollection PopularBikeTypesSeries { get; set; }
        public ObservableCollection<string> PopularBikeTypesLabels { get; set; }
        public SeriesCollection RentalsSeries { get; set; }
        public ObservableCollection<string> RentalsLabels { get; set; }
        
        public int TotalCustomers
        {
            get => _totalCustomers;
            set
            {
                _totalCustomers = value;
                OnPropertyChanged();
            }
        }
        
        public int TotalRevenue
        {
            get => _totalRevenue;
            set
            {
                _totalRevenue = value;
                OnPropertyChanged();
            }
        }
        
        public int TotalBikes
        {
            get => _totalBikes;
            set
            {
                _totalBikes = value;
                OnPropertyChanged();
            }
        }
        
        public int ActiveRentals
        {
            get => _activeRentals;
            set
            {
                _activeRentals = value;
                OnPropertyChanged();
            }
        }
        

        public DashboardViewModel(
            ICustomerService customerService,
            IBikeService bikeService,
            IRentalService rentalService,
            IPaymentService paymentService)
        {
            Title = "Dashboard";
            Description = "Overview of bike rental operations";
            
            _customerService = customerService;
            _bikeService = bikeService;
            _rentalService = rentalService;
            _paymentService = paymentService;
            
            
            // Initialize with default values
            TotalCustomers = 0;
            TotalBikes = 0;
            TotalRevenue = 0;
            ActiveRentals = 0;
            
            // Initialize chart collections
            MonthlyRevenueSeries = new SeriesCollection();
            MonthlyRevenueLabels = new ObservableCollection<string>();
            PopularBikeTypesSeries = new SeriesCollection();
            PopularBikeTypesLabels = new ObservableCollection<string>();
            RentalsSeries = new SeriesCollection();
            RentalsLabels = new ObservableCollection<string>();
            
            // Load data when the ViewModel is created
            LoadData();
            LoadChartData();
        }
        
        private void LoadData()
        {
            try
            {
                Task.Run(async () => await LoadDataAsync());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading dashboard data: {ex.Message}");
            }
        }
        
        private async Task LoadDataAsync()
        {
            try
            {
                // Get total customers
                var (customers, totalCustomers) = await _customerService.GetCustomersAsync(1, "", null);
                TotalCustomers = totalCustomers;
                
                // Get total bikes
                var (bikes, totalBikes) = await _bikeService.GetBikesAsync(1, "", null);
                TotalBikes = totalBikes;
                
                // Get active rentals
                var (rentals, _) = await _rentalService.GetRentalsAsync(1, "", null);
                ActiveRentals = rentals.Count(r => r.RentalStatus == "Active");
                
                // Get total revenue (only from paid payments)
                var (payments, _) = await _paymentService.GetPaymentsAsync(1, "", null);
                TotalRevenue = payments
                    .Where(p => p.PaymentStatus == "Paid")
                    .Sum(p => p.AmountPaid);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading dashboard data: {ex.Message}");
            }
        }

        private async void LoadChartData()
        {
            MonthlyRevenueSeries.Clear();
            MonthlyRevenueLabels.Clear();

            // Fetch all payments
            var payments = await _paymentService.GetAllPaymentsAsync();

            // Group by month and sum revenue
            var monthlyRevenue = payments
                .Where(p => p.PaymentStatus == "Paid" && p.PaymentDate != null)
                .GroupBy(p => p.PaymentDate.Month)
                .OrderBy(g => g.Key)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(p => p.AmountPaid)
                );

            // Prepare data for all 12 months
            var revenueValues = new ChartValues<double>();
            var months = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };
            for (int i = 1; i <= 12; i++)
            {
                revenueValues.Add(monthlyRevenue.ContainsKey(i) ? monthlyRevenue[i] : 0);
                MonthlyRevenueLabels.Add(months[i - 1]);
            }

            MonthlyRevenueSeries.Add(new LineSeries
            {
                Title = "Revenue",
                Values = revenueValues,
                PointGeometry = DefaultGeometries.Circle,
                PointGeometrySize = 10,
                Stroke = (Brush)Application.Current.Resources["PurpleBrush"],
                Fill = (Brush)Application.Current.Resources["PurpleGradientBackgroundTransparent"],
                StrokeThickness = 2
            });

            // --- Real data for popular bike types ---
            PopularBikeTypesSeries.Clear();
            PopularBikeTypesLabels.Clear();

            // Fetch all rentals
            var allRentals = new List<Rental>();
            int page = 1;
            int pageSize = 100;
            _rentalService.PageSize = pageSize;
            while (true)
            {
                var (rentals, totalCount) = await _rentalService.GetRentalsAsync(page, "", null);
                allRentals.AddRange(rentals);
                if (allRentals.Count >= totalCount) break;
                page++;
            }
            // Fetch all bikes
            var allBikes = await _bikeService.GetAllBikesAsync();

            // Join rentals to bikes to get bike types
            var rentalsWithBikeTypes = from rental in allRentals
                                       join bike in allBikes on rental.BikeId equals bike.BikeId
                                       select bike.BikeType;

            // Count rentals per bike type, take top 3
            var bikeTypeCounts = rentalsWithBikeTypes
                .GroupBy(type => type)
                .Select(g => new { BikeType = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .Take(3)
                .ToList();

            // Gradient brush keys for top 3
            var brushKeys = new[] { "PurpleGradientBackground", "RedGradientBackground", "GreenGradientBackground" };
            for (int i = 0; i < bikeTypeCounts.Count; i++)
            {
                var item = bikeTypeCounts[i];
                var brush = (System.Windows.Media.Brush)System.Windows.Application.Current.Resources[brushKeys[i]];
                PopularBikeTypesSeries.Add(new PieSeries
                {
                    Title = item.BikeType,
                    Values = new ChartValues<double> { item.Count },
                    DataLabels = true,
                    StrokeThickness=2,
                    Stroke = (Brush)Application.Current.Resources["ButtonBackgroundBrush"],
                    Fill = brush
                });
                PopularBikeTypesLabels.Add(item.BikeType);
            }

            // Rentals Bar Chart Data (real data)
            RentalsSeries.Clear();
            RentalsLabels.Clear();
            var RentalsPerMonth = new int[12];
            foreach (var rental in allRentals)
            {
                int month = rental.RentalDate.Month;
                RentalsPerMonth[month - 1]++;
            }
            var orderValues = new ChartValues<int>(RentalsPerMonth);
            foreach (var m in months) RentalsLabels.Add(m);
            RentalsSeries.Add(new ColumnSeries
            {
                Title = "Rentals",
                Values = orderValues,
                Fill = (Brush)Application.Current.Resources["PurpleGradientBackgroundTransparent"],
                StrokeThickness = 0
            });
        }
    }
}
