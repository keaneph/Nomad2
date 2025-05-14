using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nomad2.Models;
using Nomad2.Sorting;

namespace Nomad2.Services
{
    public interface IBikeService
    {
        int PageSize { get; set; }
        Task<(List<Bike> bikes, int totalCount)> GetBikesAsync(int page, string searchTerm, SortOption<BikeSortOption> sortOption);
        Task<Bike> GetBikeByIdAsync(string id);
        Task<bool> AddBikeAsync(Bike bike);
        Task<bool> UpdateBikeAsync(Bike bike);
        Task<bool> DeleteBikeAsync(string id);
        Task<bool> ClearAllBikesAsync();
        Task<string> GetLastBikeIdAsync();
    }
}
