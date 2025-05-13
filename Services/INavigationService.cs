// Services/INavigationService.cs
using System;

namespace Nomad2.Services
{
    public interface INavigationService
    {
        // just for changing the content of the main window
        //mostly user controls for nav service
        void NavigateTo(string viewName);
        event Action<string> CurrentViewChanged;
    }
}