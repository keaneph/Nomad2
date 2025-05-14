using Nomad2.Models;
using Nomad2.Services;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace Nomad2.Scripts
{
    public class BikeDataSeeder
    {
        private readonly IBikeService _bikeService;
        private readonly Random _random = new Random();

        private static readonly string[] Models = new[]
        {
            "Deore", "Alivio", "Altus", "Acera", "Tourney", "XTR", "XT", "SLX",
            "Saint", "Zee", "105", "Ultegra", "Dura-Ace", "Claris", "Sora"
        };

        private static readonly string[] Brands = new[]
        {
            "Trek", "Giant", "Specialized", "Cannondale", "Scott", "Merida", "Cube", "Focus",
            "Bianchi", "BMC", "Felt", "Fuji", "GT", "Jamis", "Kona"
        };

        private static readonly string[] Types = new[]
        {
            "Mountain", "Road", "Hybrid", "BMX", "Electric", "Folding", "Kids", "Cruiser",
            "Touring", "Cyclocross", "Gravel", "City", "Comfort", "Dirt Jump", "Fat"
        };

        private static readonly string[] BikeStatuses = new[]
        {
            "Available", "Rented", "Under Maintenance"
        };

        public BikeDataSeeder(IBikeService bikeService)
        {
            _bikeService = bikeService;
        }

        private string GetRandomElement(string[] array)
        {
            return array[_random.Next(array.Length)];
        }

        private int GenerateDailyRate()
        {
            return _random.Next(500, 3001); // Random rate between 500 and 3000
        }

        private string GenerateBikeModel()
        {
            string brand = GetRandomElement(Brands);
            string model = GetRandomElement(Models);
            string year = _random.Next(2020, 2024).ToString();
            return $"{brand} {model} {year}";
        }

        private async Task<string> GenerateNewBikeId()
        {
            string lastId = await _bikeService.GetLastBikeIdAsync();

            if (string.IsNullOrEmpty(lastId))
            {
                return "BIKE-0001";
            }

            string[] parts = lastId.Split('-');
            if (parts.Length == 2 && int.TryParse(parts[1], out int lastNumber))
            {
                int newNumber = lastNumber + 1;
                return $"BIKE-{newNumber:D4}";
            }

            return "BIKE-0001";
        }

        public async Task SeedBikesAsync(int count = 50)
        {
            try
            {
                for (int i = 1; i <= count; i++)
                {
                    var bike = new Bike
                    {
                        BikeId = await GenerateNewBikeId(),
                        BikeModel = GenerateBikeModel(),
                        BikeType = GetRandomElement(Types),
                        DailyRate = GenerateDailyRate(),
                        BikePicture = "default_bike.png",
                        BikeStatus = GetRandomElement(BikeStatuses)
                    };

                    await _bikeService.AddBikeAsync(bike);
                }

                MessageBox.Show($"Successfully added {count} sample bikes!", "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding sample bikes: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}