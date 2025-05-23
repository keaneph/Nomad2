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
    // service class that handles all customer-related database operations
    public class CustomerService : ICustomerService
    {
        private readonly DatabaseHelper _db;
        // default page size of 12 customers per page
        private int _pageSize = 12;

        // property to ensure page size is always at least 1
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = Math.Max(1, value); 
        }

        public CustomerService()
        {
            _db = new DatabaseHelper();
        }

        // gets all customers without pagination
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

        // gets paginated list of customers with search and sort capabilities
        public async Task<(List<Customer> customers, int totalCount)> GetCustomersAsync(
            int page = 1,
            string searchTerm = "",
            SortOption<CustomerSortOption> sortOption = null)
        {
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
            var (isValid, errorMessage) = CustomerValidator.ValidateCustomer(customer);
            if (!isValid)
            {
                throw new ArgumentException(errorMessage);
            }

            using (var connection = _db.GetConnection())
            {
                await connection.OpenAsync();
                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        // check if phone number already exists
                        string checkPhoneQuery = "SELECT COUNT(*) FROM customer WHERE phone_number = @PhoneNumber";
                        using (var checkCommand = new MySqlCommand(checkPhoneQuery, connection, transaction))
                        {
                            checkCommand.Parameters.AddWithValue("@PhoneNumber", customer.PhoneNumber);
                            int count = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());
                            if (count > 0)
                            {
                                throw new InvalidOperationException("Phone number already exists");
                            }
                        }

                        string query = @"
                            INSERT INTO customer 
                            (customer_id, name, phone_number, address, government_id_picture, customer_status, registration_date) 
                            VALUES 
                            (@CustomerId, @Name, @PhoneNumber, @Address, @GovernmentIdPicture, @CustomerStatus, @RegistrationDate)";

                        using (var command = new MySqlCommand(query, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@CustomerId", customer.CustomerId);
                            command.Parameters.AddWithValue("@Name", customer.Name);
                            command.Parameters.AddWithValue("@PhoneNumber", customer.PhoneNumber);
                            command.Parameters.AddWithValue("@Address", customer.Address);
                            command.Parameters.AddWithValue("@GovernmentIdPicture", customer.GovernmentIdPicture);
                            command.Parameters.AddWithValue("@CustomerStatus", customer.CustomerStatus);
                            command.Parameters.AddWithValue("@RegistrationDate", customer.RegistrationDate);

                            await command.ExecuteNonQueryAsync();
                        }

                        await transaction.CommitAsync();
                        return true;
                    }
                    catch (MySqlException ex)
                    {
                        await transaction.RollbackAsync();
                        switch (ex.Number)
                        {
                            case 1062: // duplicate entry
                                throw new InvalidOperationException("Customer ID or phone number already exists", ex);
                            case 3819: // check constraint violation
                                throw new InvalidOperationException("Invalid customer status value", ex);
                            default:
                                throw new Exception("Database error occurred while adding customer", ex);
                        }
                    }
                    catch (Exception)
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
        }

        //for the edit part (update)
        public async Task<bool> UpdateCustomerAsync(Customer customer)
        {
            var (isValid, errorMessage) = CustomerValidator.ValidateCustomer(customer);
            if (!isValid)
            {
                throw new ArgumentException(errorMessage);
            }

            using (var connection = _db.GetConnection())
            {
                await connection.OpenAsync();
                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        // check if phone number already exists for other customers
                        string checkPhoneQuery = @"
                            SELECT COUNT(*) 
                            FROM customer 
                            WHERE phone_number = @PhoneNumber 
                            AND customer_id != @CustomerId";

                        using (var checkCommand = new MySqlCommand(checkPhoneQuery, connection, transaction))
                        {
                            checkCommand.Parameters.AddWithValue("@PhoneNumber", customer.PhoneNumber);
                            checkCommand.Parameters.AddWithValue("@CustomerId", customer.CustomerId);
                            int count = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());
                            if (count > 0)
                            {
                                throw new InvalidOperationException("Phone number already exists for another customer");
                            }
                        }

                        // check if trying to set status to Inactive while having active rentals
                        if (customer.CustomerStatus == "Inactive")
                        {
                            string checkRentalsQuery = @"
                                SELECT COUNT(*) 
                                FROM rentals 
                                WHERE customer_id = @CustomerId 
                                AND rental_status = 'Active'";

                            using (var checkCommand = new MySqlCommand(checkRentalsQuery, connection, transaction))
                            {
                                checkCommand.Parameters.AddWithValue("@CustomerId", customer.CustomerId);
                                int activeRentals = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());
                                if (activeRentals > 0)
                                {
                                    throw new InvalidOperationException("Cannot set customer status to Inactive while they have active rentals");
                                }
                            }
                        }

                        string query = @"
                            UPDATE customer 
                            SET name = @Name, 
                                phone_number = @PhoneNumber, 
                                address = @Address, 
                                government_id_picture = @GovernmentIdPicture, 
                                customer_status = @CustomerStatus, 
                                registration_date = @RegistrationDate
                            WHERE customer_id = @CustomerId";

                        using (var command = new MySqlCommand(query, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@CustomerId", customer.CustomerId);
                            command.Parameters.AddWithValue("@Name", customer.Name);
                            command.Parameters.AddWithValue("@PhoneNumber", customer.PhoneNumber);
                            command.Parameters.AddWithValue("@Address", customer.Address);
                            command.Parameters.AddWithValue("@GovernmentIdPicture", customer.GovernmentIdPicture);
                            command.Parameters.AddWithValue("@CustomerStatus", customer.CustomerStatus);
                            command.Parameters.AddWithValue("@RegistrationDate", customer.RegistrationDate);

                            int rowsAffected = await command.ExecuteNonQueryAsync();
                            if (rowsAffected == 0)
                            {
                                throw new InvalidOperationException("Customer not found");
                            }
                        }

                        await transaction.CommitAsync();
                        return true;
                    }
                    catch (MySqlException ex)
                    {
                        await transaction.RollbackAsync();
                        switch (ex.Number)
                        {
                            case 1062: // duplicate entry
                                throw new InvalidOperationException("Phone number already exists", ex);
                            case 3819: // check constraint violation
                                throw new InvalidOperationException("Invalid customer status value", ex);
                            default:
                                throw new Exception("Database error occurred while updating customer", ex);
                        }
                    }
                    catch (Exception)
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
        }

        //deletes based on id
        public async Task<bool> DeleteCustomerAsync(string id)
        {
            using (var connection = _db.GetConnection())
            {
                await connection.OpenAsync();
                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        // check if customer has any active rentals
                        string checkRentalsQuery = @"
                            SELECT COUNT(*) 
                            FROM rentals 
                            WHERE customer_id = @CustomerId 
                            AND rental_status = 'Active'";

                        using (var checkCommand = new MySqlCommand(checkRentalsQuery, connection, transaction))
                        {
                            checkCommand.Parameters.AddWithValue("@CustomerId", id);
                            int activeRentals = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());
                            if (activeRentals > 0)
                            {
                                throw new InvalidOperationException("Cannot delete customer with active rentals");
                            }
                        }

                        string query = "DELETE FROM customer WHERE customer_id = @Id";
                        using (var command = new MySqlCommand(query, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@Id", id);
                            int rowsAffected = await command.ExecuteNonQueryAsync();
                            if (rowsAffected == 0)
                            {
                                throw new InvalidOperationException("Customer not found");
                            }
                        }

                        await transaction.CommitAsync();
                        return true;
                    }
                    catch (MySqlException ex)
                    {
                        await transaction.RollbackAsync();
                        switch (ex.Number)
                        {
                            case 1451: // cannot delete or update a parent row
                                throw new InvalidOperationException("Cannot delete customer because they have associated records", ex);
                            default:
                                throw new Exception("Database error occurred while deleting customer", ex);
                        }
                    }
                    catch (Exception)
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
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
                    // retrieves the highest customer_id for auto-generation purposes
                    var result = await command.ExecuteScalarAsync();
                    return result?.ToString();
                }
            }
        }
    }
}