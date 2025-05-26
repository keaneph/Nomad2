using System.Windows;

namespace Nomad2.Views
{
    public partial class AddPaymentDialog : Window
    {
        public AddPaymentDialog()
        {
            InitializeComponent();
            DataContext = new ViewModels.AddPaymentDialogViewModel(this);
        }
    }
} 