// Services/NavigationService.cs
using System;

namespace Nomad2.Services
{
    public class NavigationService : INavigationService
    {
        // current view that is being displayed in the main window
        private string _currentView;
        public event Action<string> CurrentViewChanged;

        // basic constructor
        public void NavigateTo(string viewName)
        {
            _currentView = viewName;
            CurrentViewChanged?.Invoke(viewName);
        }
    }
}