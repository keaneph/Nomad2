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
    // service class that handles all bike-related database operations
    public class BikeService : IBikeService
    {
        // database helper instance for managing connections
        private readonly DatabaseHelper _db;
        // number of bikes to display per page, defaults to 12
        private int _pageSize = 12;


        // property to get/set page size with minimum value of 1
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = Math.Max(1, value);
        }

        // constructor initializes database helper
        public BikeService()
        {
            _db = new DatabaseHelper();
        }

        // retrieves all bikes from database without pagination (i used this for combobox in rental)
        public async Task<List<Bike>> GetAllBikesAsync()
        {
            using (var connection = _db.GetConnection())
            {
                await connection.OpenAsync();
                var bikes = new List<Bike>();

                string query = "SELECT * FROM bike";

                using (var command = new MySqlCommand(query, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            bikes.Add(new Bike
                            {
                                BikeId = reader.GetString("bike_id"),
                                BikeModel = reader.GetString("bike_model"),
                                BikeType = reader.GetString("bike_type"),
                                DailyRate = reader.GetInt32("daily_rate"),
                                BikePicture = reader.GetString("bike_picture"),
                                BikeStatus = reader.GetString("bike_status")
                            });
                        }
                    }
                }
                return bikes;
            }
        }

        // gets paginated list of bikes with search and sort capabilities
        public async Task<(List<Bike> bikes, int totalCount)> GetBikesAsync(
            int page = 1,
            string searchTerm = "",
            SortOption<BikeSortOption> sortOption = null)
        {
            using (var connection = _db.GetConnection())
            {
                await connection.OpenAsync();

                var offset = (page - 1) * _pageSize;
                var bikes = new List<Bike>();

                string orderByColumn = sortOption?.Option switch
                {
                    BikeSortOption.BikeId => "bike_id",
                    BikeSortOption.BikeModel => "bike_model",
                    BikeSortOption.BikeType => "bike_type",
                    BikeSortOption.DailyRate => "daily_rate",
                    BikeSortOption.Status => "bike_status",
                    _ => "bike_model"
                };

                string direction = sortOption?.IsAscending == true ? "ASC" : "DESC";

                string query = @"
                    SELECT SQL_CALC_FOUND_ROWS *
                    FROM bike
                    WHERE 
                        LOWER(bike_id) LIKE LOWER(@SearchTerm) OR
                        LOWER(bike_model) LIKE LOWER(@SearchTerm) OR
                        LOWER(bike_type) LIKE LOWER(@SearchTerm) OR
                        CAST(daily_rate AS CHAR) LIKE @SearchTerm OR
                        LOWER(bike_status) LIKE LOWER(@SearchTerm)
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
                            bikes.Add(new Bike
                            {
                                BikeId = reader.GetString("bike_id"),
                                BikeModel = reader.GetString("bike_model"),
                                BikeType = reader.GetString("bike_type"),
                                DailyRate = reader.GetInt32("daily_rate"),
                                BikePicture = reader.GetString("bike_picture"),
                                BikeStatus = reader.GetString("bike_status")
                            });
                        }
                    }
                }

                using (var countCommand = new MySqlCommand("SELECT FOUND_ROWS()", connection))
                {
                    var totalCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync());
                    return (bikes, totalCount);
                }
            }
        }

        // retrieves a single bike by its id
        public async Task<Bike> GetBikeByIdAsync(string id)
        {
            using (var connection = _db.GetConnection())
            {
                await connection.OpenAsync();
                string query = "SELECT * FROM bike WHERE bike_id = @Id";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new Bike
                            {
                                BikeId = reader.GetString("bike_id"),
                                BikeModel = reader.GetString("bike_model"),
                                BikeType = reader.GetString("bike_type"),
                                DailyRate = reader.GetInt32("daily_rate"),
                                BikePicture = reader.GetString("bike_picture"),
                                BikeStatus = reader.GetString("bike_status")
                            };
                        }
                        return null;
                    }
                }
            }
        }

        // checks if a bike model already exists
        private async Task<bool> IsBikeModelExistsAsync(string bikeModel, string excludeBikeId = null)
        {
            using (var connection = _db.GetConnection())
            {
                await connection.OpenAsync();
                string query = @"
                    SELECT COUNT(*) 
                    FROM bike 
                    WHERE LOWER(bike_model) = LOWER(@BikeModel)";

                if (!string.IsNullOrEmpty(excludeBikeId))
                {
                    query += " AND bike_id != @ExcludeBikeId";
                }

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@BikeModel", bikeModel);
                    if (!string.IsNullOrEmpty(excludeBikeId))
                    {
                        command.Parameters.AddWithValue("@ExcludeBikeId", excludeBikeId);
                    }

                    int count = Convert.ToInt32(await command.ExecuteScalarAsync());
                    return count > 0;
                }
            }
        }

        // adds a new bike to the database
        public async Task<bool> AddBikeAsync(Bike bike)
        {
            var (isValid, errorMessage) = BikeValidator.ValidateBike(bike);
            if (!isValid)
            {
                throw new ArgumentException(errorMessage);
            }

            // check for duplicate bike model
            if (await IsBikeModelExistsAsync(bike.BikeModel))
            {
                throw new InvalidOperationException("A bike with this model already exists");
            }

            using (var connection = _db.GetConnection())
            {
                await connection.OpenAsync();
                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        string query = @"
                            INSERT INTO bike 
                            (bike_id, bike_model, bike_type, daily_rate, bike_picture, bike_status) 
                            VALUES 
                            (@BikeId, @BikeModel, @BikeType, @DailyRate, @BikePicture, @BikeStatus)";

                        using (var command = new MySqlCommand(query, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@BikeId", bike.BikeId);
                            command.Parameters.AddWithValue("@BikeModel", bike.BikeModel);
                            command.Parameters.AddWithValue("@BikeType", bike.BikeType);
                            command.Parameters.AddWithValue("@DailyRate", bike.DailyRate);
                            command.Parameters.AddWithValue("@BikePicture", bike.BikePicture);
                            command.Parameters.AddWithValue("@BikeStatus", bike.BikeStatus);

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
                                throw new InvalidOperationException("Bike ID already exists", ex);
                            case 3819: // check constraint violation
                                throw new InvalidOperationException("Invalid bike status or daily rate value", ex);
                            default:
                                throw new Exception("Database error occurred while adding bike", ex);
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

        // updates existing bike information
        public async Task<bool> UpdateBikeAsync(Bike bike)
        {
            var (isValid, errorMessage) = BikeValidator.ValidateBike(bike);
            if (!isValid)
            {
                throw new ArgumentException(errorMessage);
            }

            // check for duplicate bike model, excluding the current bike
            if (await IsBikeModelExistsAsync(bike.BikeModel, bike.BikeId))
            {
                throw new InvalidOperationException("A bike with this model already exists");
            }

            using (var connection = _db.GetConnection())
            {
                await connection.OpenAsync();
                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        // check if bike is currently rented
                        if (bike.BikeStatus == "Available")
                        {
                            string checkRentalQuery = @"
                                SELECT COUNT(*) 
                                FROM rentals 
                                WHERE bike_id = @BikeId 
                                AND rental_status = 'Active'";

                            using (var checkCommand = new MySqlCommand(checkRentalQuery, connection, transaction))
                            {
                                checkCommand.Parameters.AddWithValue("@BikeId", bike.BikeId);
                                int activeRentals = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());
                                if (activeRentals > 0)
                                {
                                    throw new InvalidOperationException("Cannot set bike status to Available while it has active rentals");
                                }
                            }
                        }

                        string query = @"
                            UPDATE bike 
                            SET bike_model = @BikeModel, 
                                bike_type = @BikeType, 
                                daily_rate = @DailyRate, 
                                bike_picture = @BikePicture, 
                                bike_status = @BikeStatus
                            WHERE bike_id = @BikeId";

                        using (var command = new MySqlCommand(query, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@BikeId", bike.BikeId);
                            command.Parameters.AddWithValue("@BikeModel", bike.BikeModel);
                            command.Parameters.AddWithValue("@BikeType", bike.BikeType);
                            command.Parameters.AddWithValue("@DailyRate", bike.DailyRate);
                            command.Parameters.AddWithValue("@BikePicture", bike.BikePicture);
                            command.Parameters.AddWithValue("@BikeStatus", bike.BikeStatus);

                            int rowsAffected = await command.ExecuteNonQueryAsync();
                            if (rowsAffected == 0)
                            {
                                throw new InvalidOperationException("Bike not found");
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
                            case 3819: // check constraint violation
                                throw new InvalidOperationException("Invalid bike status or daily rate value", ex);
                            default:
                                throw new Exception("Database error occurred while updating bike", ex);
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

        // deletes a bike from the database
        public async Task<bool> DeleteBikeAsync(string id)
        {
            using (var connection = _db.GetConnection())
            {
                await connection.OpenAsync();
                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        // check if bike has any active rentals
                        string checkRentalsQuery = @"
                            SELECT COUNT(*) 
                            FROM rentals 
                            WHERE bike_id = @BikeId 
                            AND rental_status = 'Active'";

                        using (var checkCommand = new MySqlCommand(checkRentalsQuery, connection, transaction))
                        {
                            checkCommand.Parameters.AddWithValue("@BikeId", id);
                            int activeRentals = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());
                            if (activeRentals > 0)
                            {
                                throw new InvalidOperationException("Cannot delete bike with active rentals");
                            }
                        }

                        string query = "DELETE FROM bike WHERE bike_id = @Id";
                        using (var command = new MySqlCommand(query, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@Id", id);
                            int rowsAffected = await command.ExecuteNonQueryAsync();
                            if (rowsAffected == 0)
                            {
                                throw new InvalidOperationException("Bike not found");
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
                                throw new InvalidOperationException("Cannot delete bike because it has associated records", ex);
                            default:
                                throw new Exception("Database error occurred while deleting bike", ex);
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


        // removes all bikes from the database
        public async Task<bool> ClearAllBikesAsync()
        {
            using (var connection = _db.GetConnection())
            {
                await connection.OpenAsync();
                string query = "DELETE FROM bike";

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


        // gets the id of the last bike in the database
        public async Task<string> GetLastBikeIdAsync()
        {
            using (var connection = _db.GetConnection())
            {
                await connection.OpenAsync();
                string query = "SELECT bike_id FROM bike ORDER BY bike_id DESC LIMIT 1";

                using (var command = new MySqlCommand(query, connection))
                {
                    var result = await command.ExecuteScalarAsync();
                    return result?.ToString();
                }
            }
        }

        // searches bikes by search term
        public async Task<List<Bike>> SearchBikesAsync(string searchTerm)
        {
            using (var connection = _db.GetConnection())
            {
                await connection.OpenAsync();
                var bikes = new List<Bike>();

                string query = @"
                    SELECT * FROM bike
                    WHERE 
                        LOWER(bike_id) LIKE LOWER(@SearchTerm) OR
                        LOWER(bike_model) LIKE LOWER(@SearchTerm) OR
                        LOWER(bike_type) LIKE LOWER(@SearchTerm) OR
                        CAST(daily_rate AS CHAR) LIKE @SearchTerm OR
                        LOWER(bike_status) LIKE LOWER(@SearchTerm)";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@SearchTerm", $"%{searchTerm}%");

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            bikes.Add(new Bike
                            {
                                BikeId = reader.GetString("bike_id"),
                                BikeModel = reader.GetString("bike_model"),
                                BikeType = reader.GetString("bike_type"),
                                DailyRate = reader.GetInt32("daily_rate"),
                                BikePicture = reader.GetString("bike_picture"),
                                BikeStatus = reader.GetString("bike_status")
                            });
                        }
                    }
                }
                return bikes;
            }
        }
    }
}