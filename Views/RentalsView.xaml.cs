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
using System.Windows.Controls.Primitives;

namespace Nomad2.Views
{
    /// <summary>
    /// Interaction logic for RentalsView.xaml
    /// </summary>
    public partial class RentalsView : UserControl
    {
        public RentalsView()
        {
            InitializeComponent();
        }

        private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var grid = sender as Grid;
            if (grid != null)
            {
                // calculate available height for cards
                double availableHeight = grid.ActualHeight - 140; // adjust this value based on top and bottom controls

                // calculate columns for UniformGrid
                int columns;
                if (grid.ActualWidth < 1200) // minimized or small window
                {
                    columns = 3; // 2 rows x 3 columns = 6 cards
                }
                else // maximized or large window
                {
                    columns = Math.Max(1, (int)(grid.ActualWidth / 350)); // 330 card + margin
                }

                // find UniformGrid in ItemsControl
                UniformGrid? uniformGrid = FindUniformGrid(rentalsItemsControl);
                if (uniformGrid != null)
                {
                    uniformGrid.Columns = columns;
                }

                double cardHeight = 240; // adjust based on card height + margin
                int visibleRows = Math.Max(1, (int)(availableHeight / cardHeight));
                int itemsPerPage = columns * visibleRows;

                // update the page size in ViewModel
                if (DataContext is RentalsViewModel viewModel)
                {
                    viewModel.UpdatePageSize(itemsPerPage);
                    UpdatePaginationButtons(viewModel);
                }
            }
        }

        private void UpdatePaginationButtons(RentalsViewModel viewModel)
        {
            // force the commands to re-evaluate their CanExecute state
            CommandManager.InvalidateRequerySuggested();
        }

        private UniformGrid? FindUniformGrid(ItemsControl itemsControl)
        {
            var itemsPresenter = FindVisualChild<ItemsPresenter>(itemsControl);
            if (itemsPresenter != null)
            {
                var panel = VisualTreeHelper.GetChild(itemsPresenter, 0) as UniformGrid;
                return panel;
            }
            return null;
        }

        private T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T tChild)
                    return tChild;
                var result = FindVisualChild<T>(child);
                if (result != null)
                    return result;
            }
            return null;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
