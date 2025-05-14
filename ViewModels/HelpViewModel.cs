using Nomad2.Scripts;
using Nomad2.Services;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Nomad2.ViewModels
{
    public class HelpViewModel : BaseViewModel
    {
        private readonly ICustomerService _customerService;
        private readonly IBikeService _bikeService;

        public HelpViewModel()
        {
            Title = "Help";
            Description = "Help and developer tools";

            _customerService = new CustomerService();
            _bikeService = new BikeService();

            SeedCustomerDataCommand = new RelayCommand(async () => await ExecuteSeedCustomerData());
            SeedBikeDataCommand = new RelayCommand(async () => await ExecuteSeedBikeData());
        }

        public ICommand SeedCustomerDataCommand { get; }
        public ICommand SeedBikeDataCommand { get; }

        private async Task ExecuteSeedCustomerData()
        {
            var seeder = new CustomerDataSeeder(_customerService);
            await seeder.SeedCustomersAsync(100);
        }

        private async Task ExecuteSeedBikeData()
        {
            var seeder = new BikeDataSeeder(_bikeService);
            await seeder.SeedBikesAsync(50);
        }
    }
}