using System.Windows;
using Nomad2.ViewModels;
using Nomad2.Models;

namespace Nomad2.Views
{
    public partial class EditPaymentDialog : Window
    {
        public EditPaymentDialog(Payment payment)
        {
            InitializeComponent();
            DataContext = new EditPaymentDialogViewModel(payment);
        }
    }
} 