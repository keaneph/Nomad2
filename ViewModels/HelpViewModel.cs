using Nomad2.Scripts;
using Nomad2.Services;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Nomad2.ViewModels
{
    public class HelpViewModel : BaseViewModel
    {
        private readonly ICustomerService _customerService;

        public HelpViewModel()
        {
            Title = "Help";
            Description = "Help and developer tools";

            _customerService = new CustomerService();
            SeedDataCommand = new RelayCommand(async () => await ExecuteSeedData());
        }

        public ICommand SeedDataCommand { get; }

        private async Task ExecuteSeedData()
        {
            var seeder = new CustomerDataSeeder(_customerService);
            await seeder.SeedCustomersAsync(100);
        }
    }
}