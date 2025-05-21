using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nomad2.Models;
using Nomad2.Sorting;

namespace Nomad2.Services
{
    public interface IRentalService
    {
        int PageSize { get; set; }
        Task<(List<Rental> rentals, int totalCount)> GetRentalsAsync(
            int page,
            string searchTerm,
            SortOption<RentalSortOption> sortOption);
        Task<Rental> GetRentalByIdAsync(string id);
        Task<bool> AddRentalAsync(Rental rental);
        Task<bool> UpdateRentalAsync(Rental rental);
        Task<bool> DeleteRentalAsync(string id);
        Task<string> GetLastRentalIdAsync();

        // Rental-specific operations
        Task<List<Rental>> GetActiveRentalsByCustomerAsync(string customerId);
        Task<List<Rental>> GetActiveRentalsByBikeAsync(string bikeId);
        Task<bool> IsCustomerEligibleForRental(string customerId);
        Task<bool> IsBikeAvailableForRental(string bikeId);
    }
}
