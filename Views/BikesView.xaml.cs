using System.Windows.Controls;
using Nomad2.ViewModels;

namespace Nomad2.Views
{
    public partial class BikesView : UserControl
    {
        public BikesView()
        {
            InitializeComponent();
        }

        private void Grid_SizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
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

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is BikesViewModel viewModel)
            {
                viewModel.SelectedBikes = bikesDataGrid.SelectedItems;
            }
        }
    }
}