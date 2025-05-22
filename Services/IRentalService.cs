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
        // controls how many rentals to show per page
        int PageSize { get; set; }
        // gets paginated list of rentals with search and sort capabilities
        Task<(List<Rental> rentals, int totalCount)> GetRentalsAsync(
            int page,
            string searchTerm,
            SortOption<RentalSortOption> sortOption);
        // gets a specific rental by its id
        Task<Rental> GetRentalByIdAsync(string id);
        // creates a new rental record
        Task<bool> AddRentalAsync(Rental rental);
        // updates existing rental information
        Task<bool> UpdateRentalAsync(Rental rental);
        // removes a rental record
        Task<bool> DeleteRentalAsync(string id);
        // gets the last rental id for auto-generation purposes
        Task<string> GetLastRentalIdAsync();

        // rental-specific operations
        // gets all active rentals for a specific customer
        Task<List<Rental>> GetActiveRentalsByCustomerAsync(string customerId);
        // gets all active rentals for a specific bike
        Task<List<Rental>> GetActiveRentalsByBikeAsync(string bikeId);
        // checks if a customer can rent more bikes
        Task<bool> IsCustomerEligibleForRental(string customerId);
        // checks if a bike is available to be rented
        Task<bool> IsBikeAvailableForRental(string bikeId);
        Task<bool> ClearAllRentalsAsync();
    }
}
