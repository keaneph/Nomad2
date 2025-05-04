using Nomad2.Models;
using Nomad2.ViewModels;
using System.Windows;

namespace Nomad2.Views
{
    public partial class CustomerDialog : Window
    {
        public CustomerDialog(Customer customer, bool isEdit = false)
        {
            InitializeComponent();
            DataContext = new CustomerDialogViewModel(customer, isEdit, CloseDialog);
        }

        private void CloseDialog(bool result)
        {
            DialogResult = result;
            Close();
        }
    }
}