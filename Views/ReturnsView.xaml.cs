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
    /// <summary>
    /// Interaction logic for ReturnsView.xaml
    /// </summary>
    public partial class ReturnsView : UserControl
    {
        public ReturnsView()
        {
            InitializeComponent();
        }

        // Optional: implement Grid_SizeChanged if you want dynamic page size
        // private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        // {
        //     if (DataContext is ReturnsViewModel viewModel)
        //     {
        //         double availableHeight = e.NewSize.Height - 120;
        //         double rowHeight = 53;
        //         int visibleRows = Math.Max(1, (int)(availableHeight / rowHeight));
        //         viewModel.UpdatePageSize(visibleRows);
        //     }
        // }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is ReturnsViewModel viewModel)
            {
                viewModel.SelectedReturns = returnsDataGrid.SelectedItems;
            }
        }
    }
}
