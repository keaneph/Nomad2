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
using System.Windows.Shapes;

namespace Nomad2.Views
{
    /// <summary>
    /// Interaction logic for ImageViewerWindow.xaml
    /// </summary>
    public partial class ImageViewerWindow : Window
    {
        public ImageViewerWindow(string imagePath)
        {
            InitializeComponent();
            FullSizeImage.Source = new BitmapImage(new Uri(imagePath));
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
