using System.Windows.Controls;
using Nomad2.ViewModels;

namespace Nomad2.Views
{
    public partial class HelpView : UserControl
    {
        public HelpView()
        {
            InitializeComponent();
            DataContext = new HelpViewModel();
        }
    }
}