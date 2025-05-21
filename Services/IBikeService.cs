using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using Nomad2.Models;
using Nomad2.Services;
using Nomad2.Sorting;
using Org.BouncyCastle.Utilities;

namespace Nomad2.Services
{
    // interface defining the contract for bike-related operations
    public interface IBikeService
    {
        // defines how many bikes should be displayed per page
        int PageSize { get; set; }
        // gets a paginated list of bikes with search and sort functionality
        // returns both the list of bikes and total count of all matching records
        Task<(List<Bike> bikes, int totalCount)> GetBikesAsync(int page, string searchTerm, SortOption<BikeSortOption> sortOption);
        // retrieves a specific bike by its unique identifier
        Task<Bike> GetBikeByIdAsync(string id);
        // adds a new bike to the system
        Task<bool> AddBikeAsync(Bike bike);
        // updates an existing bike's information
        Task<bool> UpdateBikeAsync(Bike bike);
        // removes a specific bike from the system
        Task<bool> DeleteBikeAsync(string id);
        // removes all bikes from the system
        Task<bool> ClearAllBikesAsync();
        // gets the id of the last bike in the system
        // using this in generating new bike ids
        Task<string> GetLastBikeIdAsync();
        // retrieves all bikes without pagination
        Task<List<Bike>> GetAllBikesAsync();
    }
}
