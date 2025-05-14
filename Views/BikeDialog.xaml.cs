using Nomad2.Models;
using Nomad2.ViewModels;
using System.Windows;

namespace Nomad2.Views
{
    public partial class BikeDialog : Window
    {
        public BikeDialog(Bike bike, bool isEdit)
        {
            InitializeComponent();
            DataContext = new BikeDialogViewModel(bike, isEdit, CloseDialog);
        }

        private void CloseDialog(bool result)
        {
            DialogResult = result;
            Close();
        }
    }
}