using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Nomad2.ViewModels
{
    // base class for all view models, implementing property change notifications
    public class BaseViewModel : INotifyPropertyChanged
    {
        // event notifier UI when a property value changes
        public event PropertyChangedEventHandler PropertyChanged;

        // properties for storing the title of the view/page and the description. to be used in the mainwindow.xaml window
        public string Title { get; set; }
        public string Description { get; set; }


        // helper method to raise PropertyChanged event when property values change
        // [CallerMemberName] automatically gets the calling property's name
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            // invokes the PropertyChanged event if there are any subscribers
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}