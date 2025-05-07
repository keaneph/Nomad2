using Nomad2.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nomad2.Services
{
    public interface ICustomerService
    {
        int PageSize { get; }
        Task<(List<Customer> customers, int totalCount)> GetCustomersAsync(int page, string searchTerm, string sortBy);
        Task<Customer> GetCustomerByIdAsync(string id);
        Task<bool> AddCustomerAsync(Customer customer);
        Task<bool> UpdateCustomerAsync(Customer customer);
        Task<bool> DeleteCustomerAsync(string id);
        Task<bool> ClearAllCustomersAsync();
    }
}