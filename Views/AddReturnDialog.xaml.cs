using Nomad2.Models;
using Nomad2.ViewModels;
using System.Windows;

namespace Nomad2.Views
{
    public partial class AddReturnDialog : Window
    {
        public AddReturnDialog()
        {
            InitializeComponent();
            DataContext = new AddReturnDialogViewModel(this);
        }
    }
} 