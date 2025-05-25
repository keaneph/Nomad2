using MySql.Data.MySqlClient;
using Nomad2.Models;
using Nomad2.Sorting;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Windows;

namespace Nomad2.Services
{
    public class ReturnService : IReturnService
    {
        private readonly DatabaseHelper _db;
        private int _pageSize = 12;

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = Math.Max(1, value);
        }

        public ReturnService()
        {
            _db = new DatabaseHelper();
        }

        public async Task<List<Return>> GetAllReturnsAsync()
        {
            using (var connection = _db.GetConnection())
            {
                await connection.OpenAsync();
                var returns = new List<Return>();
                string query = @"SELECT r.*, c.name AS customer_name, b.bike_model AS bike_model
                                 FROM `returns` r
                                 LEFT JOIN customer c ON r.customer_id = c.customer_id
                                 LEFT JOIN bike b ON r.bike_id = b.bike_id";
                using (var command = new MySqlCommand(query, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            returns.Add(new Return
                            {
                                ReturnId = reader["return_id"].ToString(),
                                CustomerId = reader["customer_id"].ToString(),
                                BikeId = reader["bike_id"].ToString(),
                                ReturnDate = reader.GetDateTime("return_date"),
                                Customer = new Customer { Name = reader["customer_name"]?.ToString() },
                                Bike = new Bike { BikeModel = reader["bike_model"]?.ToString() }
                            });
                        }
                    }
                }
                return returns;
            }
        }

        public async Task<(List<Return> returns, int totalCount)> GetReturnsAsync(int page, string searchTerm, SortOption<ReturnSortOption> sortOption)
        {
            using (var connection = _db.GetConnection())
            {
                await connection.OpenAsync();
                var offset = (page - 1) * _pageSize;
                var returns = new List<Return>();

                string orderByColumn = sortOption?.Option switch
                {
                    ReturnSortOption.ReturnId => "r.return_id",
                    ReturnSortOption.RentalId => "r.rental_id",
                    ReturnSortOption.CustomerId => "r.customer_id",
                    ReturnSortOption.CustomerName => "c.name",
                    ReturnSortOption.BikeId => "r.bike_id",
                    ReturnSortOption.BikeModel => "b.bike_model",
                    ReturnSortOption.ReturnDate => "r.return_date",
                    _ => "r.return_date"
                };
                string direction = sortOption?.IsAscending == true ? "ASC" : "DESC";

                string query = @"SELECT SQL_CALC_FOUND_ROWS r.*, c.name AS customer_name, b.bike_model AS bike_model
                                 FROM `returns` r
                                 LEFT JOIN customer c ON r.customer_id = c.customer_id
                                 LEFT JOIN bike b ON r.bike_id = b.bike_id
                                 WHERE
                                    LOWER(r.return_id) LIKE LOWER(@SearchTerm) OR
                                    LOWER(r.rental_id) LIKE LOWER(@SearchTerm) OR
                                    LOWER(r.customer_id) LIKE LOWER(@SearchTerm) OR
                                    LOWER(c.name) LIKE LOWER(@SearchTerm) OR
                                    LOWER(r.bike_id) LIKE LOWER(@SearchTerm) OR
                                    LOWER(b.bike_model) LIKE LOWER(@SearchTerm) OR
                                    DATE_FORMAT(r.return_date, '%Y-%m-%d') LIKE @SearchTerm
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
                            returns.Add(new Return
                            {
                                ReturnId = reader["return_id"].ToString(),
                                RentalId = reader["rental_id"].ToString(),
                                CustomerId = reader["customer_id"].ToString(),
                                BikeId = reader["bike_id"].ToString(),
                                ReturnDate = reader.GetDateTime("return_date"),
                                Customer = new Customer { Name = reader["customer_name"]?.ToString() },
                                Bike = new Bike { BikeModel = reader["bike_model"]?.ToString() }
                            });
                        }
                    }
                }

                using (var countCommand = new MySqlCommand("SELECT FOUND_ROWS()", connection))
                {
                    var totalCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync());
                    return (returns, totalCount);
                }
            }
        }

        public async Task<Return> GetReturnByIdAsync(string id)
        {
            using (var connection = _db.GetConnection())
            {
                await connection.OpenAsync();
                string query = @"SELECT r.*, c.name AS customer_name, b.bike_model AS bike_model
                                 FROM `returns` r
                                 LEFT JOIN customer c ON r.customer_id = c.customer_id
                                 LEFT JOIN bike b ON r.bike_id = b.bike_id
                                 WHERE r.return_id = @Id";
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new Return
                            {
                                ReturnId = reader["return_id"].ToString(),
                                CustomerId = reader["customer_id"].ToString(),
                                BikeId = reader["bike_id"].ToString(),
                                ReturnDate = reader.GetDateTime("return_date"),
                                Customer = new Customer { Name = reader["customer_name"]?.ToString() },
                                Bike = new Bike { BikeModel = reader["bike_model"]?.ToString() }
                            };
                        }
                        return null;
                    }
                }
            }
        }

        public async Task<bool> AddReturnAsync(Return returnItem)
        {
            using (var connection = _db.GetConnection())
            {
                await connection.OpenAsync();
                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        // check if a return record already exists for this rental
                        string checkExistingQuery = @"
                            SELECT COUNT(*) 
                            FROM `returns` 
                            WHERE rental_id = @RentalId";

                        using (var command = new MySqlCommand(checkExistingQuery, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@RentalId", returnItem.RentalId);
                            int existingReturns = Convert.ToInt32(await command.ExecuteScalarAsync());
                            if (existingReturns > 0)
                            {
                                MessageBox.Show(
                                    "A return record already exists for this rental.",
                                    "Duplicate Return",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning
                                );
                                return false;
                            }
                        }

                        // generate new return ID if not provided
                        if (string.IsNullOrEmpty(returnItem.ReturnId))
                        {
                            var lastId = await GetLastReturnIdAsync();
                            if (lastId == "0000-0000")
                            {
                                returnItem.ReturnId = "0000-0001";
                            }
                            else
                            {
                                string[] parts = lastId.Split('-');
                                if (parts.Length == 2 && int.TryParse(parts[1], out int number))
                                {
                                    returnItem.ReturnId = $"{parts[0]}-{(number + 1):D4}";
                                }
                                else
                                {
                                    returnItem.ReturnId = "0000-0001";
                                }
                            }
                        }

                        // first update the rental status to Completed
                        string updateRentalQuery = @"
                            UPDATE rentals 
                            SET rental_status = 'Completed' 
                            WHERE rental_id = @RentalId";

                        using (var command = new MySqlCommand(updateRentalQuery, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@RentalId", returnItem.RentalId);
                            await command.ExecuteNonQueryAsync();
                        }

                        // then insert the return record
                        string query = @"INSERT INTO `returns` (return_id, rental_id, customer_id, bike_id, return_date)
                                     VALUES (@ReturnId, @RentalId, @CustomerId, @BikeId, @ReturnDate)";
                        using (var command = new MySqlCommand(query, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@ReturnId", returnItem.ReturnId);
                            command.Parameters.AddWithValue("@RentalId", returnItem.RentalId);
                            command.Parameters.AddWithValue("@CustomerId", returnItem.CustomerId);
                            command.Parameters.AddWithValue("@BikeId", returnItem.BikeId);
                            command.Parameters.AddWithValue("@ReturnDate", returnItem.ReturnDate);
                            await command.ExecuteNonQueryAsync();
                        }

                        // finally update bike status to Available
                        string updateBikeQuery = @"
                            UPDATE bike 
                            SET bike_status = 'Available' 
                            WHERE bike_id = @BikeId";

                        using (var command = new MySqlCommand(updateBikeQuery, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@BikeId", returnItem.BikeId);
                            await command.ExecuteNonQueryAsync();
                        }

                        await transaction.CommitAsync();
                        return true;
                    }
                    catch (Exception)
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
        }

        public async Task<bool> UpdateReturnAsync(Return returnItem)
        {
            using (var connection = _db.GetConnection())
            {
                await connection.OpenAsync();
                string query = @"UPDATE `returns` SET customer_id = @CustomerId, bike_id = @BikeId, return_date = @ReturnDate
                                 WHERE return_id = @ReturnId";
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@ReturnId", returnItem.ReturnId);
                    command.Parameters.AddWithValue("@CustomerId", returnItem.CustomerId);
                    command.Parameters.AddWithValue("@BikeId", returnItem.BikeId);
                    command.Parameters.AddWithValue("@ReturnDate", returnItem.ReturnDate);
                    return await command.ExecuteNonQueryAsync() > 0;
                }
            }
        }

        public async Task<bool> DeleteReturnAsync(string id)
        {
            using (var connection = _db.GetConnection())
            {
                await connection.OpenAsync();
                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        // get the return record before deleting it
                        string getReturnQuery = @"
                            SELECT r.rental_id, r.customer_id, r.bike_id, 
                                   (SELECT COUNT(*) FROM rentals WHERE bike_id = r.bike_id AND rental_status = 'Active') as active_rentals
                            FROM `returns` r
                            WHERE r.return_id = @Id";

                        string rentalId = null;
                        string customerId = null;
                        string bikeId = null;
                        int activeRentals = 0;

                        using (var command = new MySqlCommand(getReturnQuery, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@Id", id);
                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    rentalId = reader.GetString("rental_id");
                                    customerId = reader.GetString("customer_id");
                                    bikeId = reader.GetString("bike_id");
                                    activeRentals = reader.GetInt32("active_rentals");
                                }
                            }
                        }

                        // check if bike is already rented
                        if (activeRentals > 0)
                        {
                            MessageBox.Show(
                                "Cannot delete return record because the bike is currently rented.",
                                "Cannot Delete",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning
                            );
                            return false;
                        }

                        // delete the return record
                        string deleteQuery = "DELETE FROM `returns` WHERE return_id = @Id";
                        using (var command = new MySqlCommand(deleteQuery, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@Id", id);
                            await command.ExecuteNonQueryAsync();
                        }

                        // if we found the return record, update the rental status back to Active
                        if (rentalId != null)
                        {
                            // update rental status back to Active
                            string updateRentalQuery = @"
                                UPDATE rentals 
                                SET rental_status = 'Active' 
                                WHERE rental_id = @RentalId";

                            using (var command = new MySqlCommand(updateRentalQuery, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@RentalId", rentalId);
                                await command.ExecuteNonQueryAsync();
                            }

                            // update bike status back to Rented
                            string updateBikeQuery = @"
                                UPDATE bike 
                                SET bike_status = 'Rented' 
                                WHERE bike_id = @BikeId";

                            using (var command = new MySqlCommand(updateBikeQuery, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@BikeId", bikeId);
                                await command.ExecuteNonQueryAsync();
                            }

                            // update customer status back to Active
                            string updateCustomerQuery = @"
                                UPDATE customer 
                                SET customer_status = 'Active' 
                                WHERE customer_id = @CustomerId";

                            using (var command = new MySqlCommand(updateCustomerQuery, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@CustomerId", customerId);
                                await command.ExecuteNonQueryAsync();
                            }
                        }

                        await transaction.CommitAsync();
                        return true;
                    }
                    catch (Exception)
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
        }

        public async Task<bool> ClearAllReturnsAsync()
        {
            using (var connection = _db.GetConnection())
            {
                await connection.OpenAsync();
                string query = "DELETE FROM `returns`";
                using (var command = new MySqlCommand(query, connection))
                {
                    return await command.ExecuteNonQueryAsync() > 0;
                }
            }
        }

        public async Task<string> GetLastReturnIdAsync()
        {
            using (var connection = _db.GetConnection())
            {
                await connection.OpenAsync();
                string query = "SELECT return_id FROM `returns` ORDER BY return_id DESC LIMIT 1";
                using (var command = new MySqlCommand(query, connection))
                {
                    var result = await command.ExecuteScalarAsync();
                    return result?.ToString() ?? "0000-0000";
                }
            }
        }
    }
} 