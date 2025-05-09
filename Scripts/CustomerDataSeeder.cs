using Nomad2.Models;
using Nomad2.Services;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace Nomad2.Scripts
{
    public class CustomerDataSeeder
    {
        private readonly ICustomerService _customerService;
        private readonly Random _random = new Random();

        private static readonly string[] FirstNames = new[]
        {
            "James", "John", "Robert", "Michael", "William", "David", "Richard", "Joseph", "Thomas", "Charles",
            "Mary", "Patricia", "Jennifer", "Linda", "Elizabeth", "Barbara", "Susan", "Jessica", "Sarah", "Karen"
        };

        private static readonly string[] LastNames = new[]
        {
            "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis", "Rodriguez", "Martinez",
            "Hernandez", "Lopez", "Gonzalez", "Wilson", "Anderson", "Thomas", "Taylor", "Moore", "Jackson", "Martin"
        };

        private static readonly string[] Streets = new[]
        {
            "Main Street", "Oak Avenue", "Maple Road", "Cedar Lane", "Pine Street", "Elm Street", "Washington Avenue",
            "Park Road", "Lake Street", "River Road", "Highland Avenue", "Madison Street", "Adams Road", "Jefferson Avenue"
        };

        private static readonly string[] Cities = new[]
        {
            "New York", "Los Angeles", "Chicago", "Houston", "Phoenix", "Philadelphia", "San Antonio", "San Diego",
            "Dallas", "San Jose", "Austin", "Jacksonville", "Fort Worth", "Columbus", "San Francisco"
        };

        private static readonly string[] States = new[]
        {
            "NY", "CA", "IL", "TX", "AZ", "PA", "FL", "OH", "NC", "MI"
        };

        private static readonly string[] CustomerStatuses = new[]
        {
            "Active", "Inactive", "Blacklisted"
        };

        public CustomerDataSeeder(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        private string GetRandomElement(string[] array)
        {
            return array[_random.Next(array.Length)];
        }

        private string GeneratePhoneNumber()
        {
            return $"0{_random.Next(100000000, 999999999)}";
        }

        private string GenerateAddress()
        {
            int houseNumber = _random.Next(1, 999);
            string street = GetRandomElement(Streets);
            string city = GetRandomElement(Cities);
            string state = GetRandomElement(States);
            return $"{houseNumber} {street}, {city}, {state}";
        }

        private DateTime GenerateRandomDate()
        {
            return DateTime.Now.AddDays(-_random.Next(0, 365));
        }

        private string GenerateRandomName()
        {
            return $"{GetRandomElement(FirstNames)} {GetRandomElement(LastNames)}";
        }

        private async Task<string> GenerateNewCustomerId()
        {
            string lastId = await _customerService.GetLastCustomerIdAsync();

            if (string.IsNullOrEmpty(lastId))
            {
                return "0000-0001";
            }

            string[] parts = lastId.Split('-');
            if (parts.Length == 2 && int.TryParse(parts[1], out int lastNumber))
            {
                int newNumber = lastNumber + 1;
                return $"0000-{newNumber:D4}";
            }

            return "0000-0001";
        }

        public async Task SeedCustomersAsync(int count = 100)
        {
            try
            {
                for (int i = 1; i <= count; i++)
                {
                    var customer = new Customer
                    {
                        CustomerId = await GenerateNewCustomerId(),
                        Name = GenerateRandomName(),
                        PhoneNumber = GeneratePhoneNumber(),
                        Address = GenerateAddress(),
                        GovernmentIdPicture = "default_id.png",
                        CustomerStatus = GetRandomElement(CustomerStatuses),
                        RegistrationDate = GenerateRandomDate()
                    };

                    await _customerService.AddCustomerAsync(customer);
                }

                MessageBox.Show($"Successfully added {count} sample customers!", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding sample customers: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}