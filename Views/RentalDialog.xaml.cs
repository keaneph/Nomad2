using Nomad2.Models;
using Nomad2.Services;
using Nomad2.ViewModels;
using System.Windows;

namespace Nomad2.Views
{
    public partial class RentalDialog : Window
    {
        public RentalDialog(Rental rental, ICustomerService customerService, IBikeService bikeService)
        {
            InitializeComponent();
            DataContext = new RentalDialogViewModel(rental, customerService, bikeService, this);
        }
    }
}