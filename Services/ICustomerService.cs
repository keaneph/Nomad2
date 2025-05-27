using Nomad2.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nomad2.Sorting;

namespace Nomad2.Services
{
    // interface defining the contract for customer-related database ops
    public interface ICustomerService
    {
        //gets the page size for pagination
        int PageSize { get; set; }
        //retrieves paginated list
        Task<(List<Customer> customers, int totalCount)> GetCustomersAsync(int page, string searchTerm, SortOption<CustomerSortOption> sortOption);
        // fetches a single customer record by their unique identifier
        Task<Customer> GetCustomerByIdAsync(string id);
        // creates a new customer record in the database, returns true if successful
        Task<bool> AddCustomerAsync(Customer customer);
        // updates an existing customers information, returns true if successful
        Task<bool> UpdateCustomerAsync(Customer customer);
        // removes a specific customer from the database by their ID, returns true if successful
        Task<bool> DeleteCustomerAsync(string id);
        // removes all customer records from the database, returns true if successful
        Task<bool> ClearAllCustomersAsync();
        // retrieves the ID of the most recently added customer, used for generating new IDs
        Task<string> GetLastCustomerIdAsync();
        // retrieves all customer without pagination
        Task<List<Customer>> GetAllCustomersAsync();
        // searches customers by search term
        Task<List<Customer>> SearchCustomersAsync(string searchTerm);
    }
}