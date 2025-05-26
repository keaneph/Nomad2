using System;
using System.Windows;
using System.Windows.Controls;
using Nomad2.ViewModels;

namespace Nomad2.Views
{
    /// <summary>
    /// Interaction logic for PaymentsView.xaml
    /// </summary>
    public partial class PaymentsView : UserControl
    {
        public PaymentsView()
        {
            InitializeComponent();
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is PaymentsViewModel viewModel)
            {
                viewModel.SelectedPayments = paymentsDataGrid.SelectedItems;
            }
        }
    }
}
