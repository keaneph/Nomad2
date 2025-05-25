using Nomad2.Models;
using Nomad2.ViewModels;
using System.Windows;

namespace Nomad2.Views
{
    public partial class EditReturnDialog : Window
    {
        public EditReturnDialog(Return returnItem)
        {
            InitializeComponent();
            DataContext = new EditReturnDialogViewModel(returnItem, this);
        }
    }
} 