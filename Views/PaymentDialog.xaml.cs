using System.Windows;
using Nomad2.Models;
using Nomad2.ViewModels;

namespace Nomad2.Views
{
    public partial class PaymentDialog : Window
    {
        public PaymentDialog(Rental rental, bool isCompletionPayment = false)
        {
            InitializeComponent();
            DataContext = new PaymentDialogViewModel(rental, this, isCompletionPayment);
        }
    }
} 