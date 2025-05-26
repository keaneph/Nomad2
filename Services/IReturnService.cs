using Nomad2.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nomad2.Sorting;

namespace Nomad2.Services
{
    // interface defining the contract for return-related database ops
    public interface IReturnService
    {
        int PageSize { get; set; }
        Task<List<Return>> GetAllReturnsAsync();
        Task<(List<Return> returns, int totalCount)> GetReturnsAsync(int page, string searchTerm, SortOption<ReturnSortOption> sortOption);
        Task<Return> GetReturnByIdAsync(string id);
        Task<bool> AddReturnAsync(Return returnItem);
        Task<bool> AddReturnWithStatusUpdatesAsync(Return returnItem, Rental rental, Bike bike, Customer customer);
        Task<bool> UpdateReturnAsync(Return returnItem);
        Task<bool> DeleteReturnAsync(string id);
        Task<bool> ClearAllReturnsAsync();
        Task<string> GetLastReturnIdAsync();
    }
} 