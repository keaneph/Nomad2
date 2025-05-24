using Nomad2.Models;
using Nomad2.ViewModels;
using System.Windows;

namespace Nomad2.Views
{
    public partial class ReturnDialog : Window
    {
        public ReturnDialog(Rental rental)
        {
            InitializeComponent();
            DataContext = new ReturnDialogViewModel(rental, this);
        }
    }
} 