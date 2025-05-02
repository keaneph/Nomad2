// Services/NavigationService.cs
using System;

namespace Nomad2.Services
{
    public class NavigationService : INavigationService
    {
        private string _currentView;
        public event Action<string> CurrentViewChanged;

        public void NavigateTo(string viewName)
        {
            _currentView = viewName;
            CurrentViewChanged?.Invoke(viewName);
        }
    }
}