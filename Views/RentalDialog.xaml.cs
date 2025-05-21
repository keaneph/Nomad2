using Nomad2.Models;
using Nomad2.Services;
using Nomad2.ViewModels;
using System.Windows;

namespace Nomad2.Views
{
    // dialog window for creating and editing rentals
    public partial class RentalDialog : Window
    {
        public RentalDialog(Rental rental, ICustomerService customerService, IBikeService bikeService, bool isEdit = false)
        {
            InitializeComponent();
            DataContext = new RentalDialogViewModel(rental, customerService, bikeService, this, isEdit);
        }
    }
}