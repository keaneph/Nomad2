// Services/INavigationService.cs
using System;

namespace Nomad2.Services
{
    public interface INavigationService
    {
        void NavigateTo(string viewName);
        event Action<string> CurrentViewChanged;
    }
}