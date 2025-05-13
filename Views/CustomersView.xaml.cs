using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Nomad2.ViewModels;

namespace Nomad2.Views
{
    public partial class CustomersView : UserControl
    {
        public CustomersView()
        {
            InitializeComponent();
        }

        // event handler for when the size of the grid changes
        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (DataContext is CustomersViewModel viewModel)
            {
                // calculate rows that can fit in the visible area
                // subtract heights of other elements (top controls, pagination)
                double availableHeight = e.NewSize.Height - 120; // adjust based on other controls
                double rowHeight = 53; // adjust based on row height
                int visibleRows = Math.Max(1, (int)(availableHeight / rowHeight));

                viewModel.UpdatePageSize(visibleRows);
            }
        }

        // event handler for when the selection changes in the data grid
        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is CustomersViewModel viewModel)
            {
                viewModel.SelectedCustomers = customersDataGrid.SelectedItems;
            }
        }
    }
}
