using MySql.Data.MySqlClient;
using Nomad2.Models;
using Nomad2.Sorting;
using Nomad2.Validators;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Nomad2.Services
{
    public class CustomerService : ICustomerService
    {
        //db instance and pagination fixed size
        private readonly DatabaseHelper _db;
        private int _pageSize = 12; // default size


        // dynamic page
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = Math.Max(1, value); // ensure at least 1
        }

        public CustomerService()
        {
            _db = new DatabaseHelper();
        }

        public async Task<List<Customer>> GetAllCustomersAsync()
        {
            using (var connection = _db.GetConnection())
            {
                await connection.OpenAsync();
                var customers = new List<Customer>();

                string query = "SELECT * FROM customer";

                using (var command = new MySqlCommand(query, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            customers.Add(new Customer
                            {
                                CustomerId = reader.GetString("customer_id"),
                                Name = reader.GetString("name"),
                                PhoneNumber = reader.GetString("phone_number"),
                                Address = reader.GetString("address"),
                                GovernmentIdPicture = reader.GetString("government_id_picture"),
                                CustomerStatus = reader.GetString("customer_status"),
                                RegistrationDate = reader.GetDateTime("registration_date")
                            });
                        }
                    }
                }
                return customers;
            }
        }

        public async Task<(List<Customer> customers, int totalCount)> GetCustomersAsync(
            int page = 1,
            string searchTerm = "",
            SortOption<CustomerSortOption> sortOption = null)
        {
            //db check
            using (var connection = _db.GetConnection())
            {
                await connection.OpenAsync();

                var offset = (page - 1) * _pageSize;
                var customers = new List<Customer>();

                // determines the column name for sorting based on the sort option
                string orderByColumn = sortOption?.Option switch
                {
                    CustomerSortOption.CustomerId => "customer_id",
                    CustomerSortOption.Name => "name",
                    CustomerSortOption.PhoneNumber => "phone_number",
                    CustomerSortOption.Address => "address",
                    CustomerSortOption.RegistrationDate => "registration_date",
                    CustomerSortOption.Status => "customer_status",
                    _ => "name"
                };

                //sets sort direction
                string direction = sortOption?.IsAscending == true ? "ASC" : "DESC";


                // SQL query to fetch customers with search and sort functionality
                // date_format is implemented like that so that it can be searched in different formats
                // yyyy-mm-dd, mm/dd/yyyy, dd/mm/yyyy
                string query = @"
                                SELECT SQL_CALC_FOUND_ROWS *
                                FROM customer
                                WHERE 
                                    LOWER(customer_id) LIKE LOWER(@SearchTerm) OR
                                    LOWER(name) LIKE LOWER(@SearchTerm) OR
                                    LOWER(phone_number) LIKE LOWER(@SearchTerm) OR
                                    LOWER(address) LIKE LOWER(@SearchTerm) OR
                                    LOWER(customer_status) LIKE LOWER(@SearchTerm) OR
                                    DATE_FORMAT(registration_date, '%Y-%m-%d') LIKE @SearchTerm OR
                                    DATE_FORMAT(registration_date, '%m/%d/%Y') LIKE @SearchTerm OR
                                    DATE_FORMAT(registration_date, '%d/%m/%Y') LIKE @SearchTerm
                                ORDER BY " + orderByColumn + " " + direction + @"
                                LIMIT @Offset, @PageSize";

                //just commands to prevent injection
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@SearchTerm", $"%{searchTerm}%");
                    command.Parameters.AddWithValue("@Offset", offset);
                    command.Parameters.AddWithValue("@PageSize", _pageSize);

                    // executes
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        // reads each record and creates Customer objects
                        while (await reader.ReadAsync())
                        {
                            customers.Add(new Customer
                            {
                                CustomerId = reader.GetString("customer_id"),
                                Name = reader.GetString("name"),
                                PhoneNumber = reader.GetString("phone_number"),
                                Address = reader.GetString("address"),
                                GovernmentIdPicture = reader.GetString("government_id_picture"),
                                CustomerStatus = reader.GetString("customer_status"),
                                RegistrationDate = reader.GetDateTime("registration_date")
                            });
                        }
                    }
                }

                // this gets the total count of records
                using (var countCommand = new MySqlCommand("SELECT FOUND_ROWS()", connection))
                {
                    var totalCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync());
                    return (customers, totalCount);
                }
            }
        }

        //gets a customer by id
        public async Task<Customer> GetCustomerByIdAsync(string id)
        {
            using (var connection = _db.GetConnection())
            {
                await connection.OpenAsync();
                string query = "SELECT * FROM customer WHERE customer_id = @Id";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new Customer
                            {
                                CustomerId = reader.GetString("customer_id"),
                                Name = reader.GetString("name"),
                                PhoneNumber = reader.GetString("phone_number"),
                                Address = reader.GetString("address"),
                                GovernmentIdPicture = reader.GetString("government_id_picture"),
                                CustomerStatus = reader.GetString("customer_status"),
                                RegistrationDate = reader.GetDateTime("registration_date")
                            };
                        }
                        return null;
                    }
                }
            }
        }

        //adding customer to db
        public async Task<bool> AddCustomerAsync(Customer customer)

        {
            //using CustomerValidator.cs, will add more later on
            var (isValid, errorMessage) = CustomerValidator.ValidateCustomer(customer);
            if (!isValid)
            {
                throw new ArgumentException(errorMessage);
            }

            using (var connection = _db.GetConnection())
            {
                await connection.OpenAsync();
                // query to insert customer
                string query = @"
                    INSERT INTO customer 
                    (customer_id, name, phone_number, address, government_id_picture, customer_status, registration_date) 
                    VALUES 
                    (@CustomerId, @Name, @PhoneNumber, @Address, @GovernmentIdPicture, @CustomerStatus, @RegistrationDate)";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@CustomerId", customer.CustomerId);
                    command.Parameters.AddWithValue("@Name", customer.Name);
                    command.Parameters.AddWithValue("@PhoneNumber", customer.PhoneNumber);
                    command.Parameters.AddWithValue("@Address", customer.Address);
                    command.Parameters.AddWithValue("@GovernmentIdPicture", customer.GovernmentIdPicture);
                    command.Parameters.AddWithValue("@CustomerStatus", customer.CustomerStatus);
                    command.Parameters.AddWithValue("@RegistrationDate", customer.RegistrationDate);

                    return await command.ExecuteNonQueryAsync() > 0;
                }
            }
        }

        //for the edit part (update)
        public async Task<bool> UpdateCustomerAsync(Customer customer)
        {
            //same validaiton
            var (isValid, errorMessage) = CustomerValidator.ValidateCustomer(customer);
            if (!isValid)
            {
                throw new ArgumentException(errorMessage);
            }

            using (var connection = _db.GetConnection())
            {
                await connection.OpenAsync();
                // just simple updates based on the customerid
                string query = @"
                    UPDATE customer 
                    SET name = @Name, 
                        phone_number = @PhoneNumber, 
                        address = @Address, 
                        government_id_picture = @GovernmentIdPicture, 
                        customer_status = @CustomerStatus, 
                        registration_date = @RegistrationDate
                    WHERE customer_id = @CustomerId";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@CustomerId", customer.CustomerId);
                    command.Parameters.AddWithValue("@Name", customer.Name);
                    command.Parameters.AddWithValue("@PhoneNumber", customer.PhoneNumber);
                    command.Parameters.AddWithValue("@Address", customer.Address);
                    command.Parameters.AddWithValue("@GovernmentIdPicture", customer.GovernmentIdPicture);
                    command.Parameters.AddWithValue("@CustomerStatus", customer.CustomerStatus);
                    command.Parameters.AddWithValue("@RegistrationDate", customer.RegistrationDate);

                    return await command.ExecuteNonQueryAsync() > 0;
                }
            }
        }

        //deletes based on id
        public async Task<bool> DeleteCustomerAsync(string id)
        {
            using (var connection = _db.GetConnection())
            {
                await connection.OpenAsync();
                string query = "DELETE FROM customer WHERE customer_id = @Id";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    return await command.ExecuteNonQueryAsync() > 0;
                }
            }
        }

        //cleas all to clear table
        public async Task<bool> ClearAllCustomersAsync()
        {
            using (var connection = _db.GetConnection())
            {
                await connection.OpenAsync();
                string query = "DELETE FROM customer";

                using (var command = new MySqlCommand(query, connection))
                {
                    try
                    {
                        await command.ExecuteNonQueryAsync();
                        return true;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                }
            }

        }

        //gets last id, then adds 1 to it, for the automatic customer id implementation
        public async Task<string> GetLastCustomerIdAsync()
        {
            using (var connection = _db.GetConnection())
            {
                await connection.OpenAsync();
                string query = "SELECT customer_id FROM customer ORDER BY customer_id DESC LIMIT 1";

                using (var command = new MySqlCommand(query, connection))
                {
                    var result = await command.ExecuteScalarAsync();
                    return result?.ToString();
                }
            }
        }
    }
}