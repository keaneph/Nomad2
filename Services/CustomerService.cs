using MySql.Data.MySqlClient;
using Nomad2.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Nomad2.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly DatabaseHelper _db;
        private readonly int _pageSize = 12;

        public int PageSize => _pageSize;

        public CustomerService()
        {
            _db = new DatabaseHelper();
        }

        public async Task<(List<Customer> customers, int totalCount)> GetCustomersAsync(int page = 1, string searchTerm = "", string sortBy = "Name")
        {
            using (var connection = _db.GetConnection())
            {
                await connection.OpenAsync();

                var offset = (page - 1) * _pageSize;
                var customers = new List<Customer>();

                string orderBy = sortBy switch
                {
                    "Name" => "name",
                    "Date" => "registration_date",
                    "Status" => "customer_status",
                    _ => "name"
                };

                string query = @"
                    SELECT SQL_CALC_FOUND_ROWS *
                    FROM customer
                    WHERE name LIKE @SearchTerm 
                       OR phone_number LIKE @SearchTerm 
                       OR address LIKE @SearchTerm
                    ORDER BY " + orderBy + @"
                    LIMIT @Offset, @PageSize";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@SearchTerm", $"%{searchTerm}%");
                    command.Parameters.AddWithValue("@Offset", offset);
                    command.Parameters.AddWithValue("@PageSize", _pageSize);

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

                using (var countCommand = new MySqlCommand("SELECT FOUND_ROWS()", connection))
                {
                    var totalCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync());
                    return (customers, totalCount);
                }
            }
        }

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

        public async Task<bool> AddCustomerAsync(Customer customer)
        {
            using (var connection = _db.GetConnection())
            {
                await connection.OpenAsync();
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

        public async Task<bool> UpdateCustomerAsync(Customer customer)
        {
            using (var connection = _db.GetConnection())
            {
                await connection.OpenAsync();
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
    }
}