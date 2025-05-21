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
    public class BikeService : IBikeService
    {
        private readonly DatabaseHelper _db;
        private int _pageSize = 12; // default size

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = Math.Max(1, value);
        }

        public BikeService()
        {
            _db = new DatabaseHelper();
        }

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

        public async Task<bool> AddBikeAsync(Bike bike)
        {
            var (isValid, errorMessage) = BikeValidator.ValidateBike(bike);
            if (!isValid)
            {
                throw new ArgumentException(errorMessage);
            }

            using (var connection = _db.GetConnection())
            {
                await connection.OpenAsync();
                string query = @"
                    INSERT INTO bike 
                    (bike_id, bike_model, bike_type, daily_rate, bike_picture, bike_status) 
                    VALUES 
                    (@BikeId, @BikeModel, @BikeType, @DailyRate, @BikePicture, @BikeStatus)";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@BikeId", bike.BikeId);
                    command.Parameters.AddWithValue("@BikeModel", bike.BikeModel);
                    command.Parameters.AddWithValue("@BikeType", bike.BikeType);
                    command.Parameters.AddWithValue("@DailyRate", bike.DailyRate);
                    command.Parameters.AddWithValue("@BikePicture", bike.BikePicture);
                    command.Parameters.AddWithValue("@BikeStatus", bike.BikeStatus);

                    return await command.ExecuteNonQueryAsync() > 0;
                }
            }
        }

        public async Task<bool> UpdateBikeAsync(Bike bike)
        {
            var (isValid, errorMessage) = BikeValidator.ValidateBike(bike);
            if (!isValid)
            {
                throw new ArgumentException(errorMessage);
            }

            using (var connection = _db.GetConnection())
            {
                await connection.OpenAsync();
                string query = @"
                    UPDATE bike 
                    SET bike_model = @BikeModel, 
                        bike_type = @BikeType, 
                        daily_rate = @DailyRate, 
                        bike_picture = @BikePicture, 
                        bike_status = @BikeStatus
                    WHERE bike_id = @BikeId";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@BikeId", bike.BikeId);
                    command.Parameters.AddWithValue("@BikeModel", bike.BikeModel);
                    command.Parameters.AddWithValue("@BikeType", bike.BikeType);
                    command.Parameters.AddWithValue("@DailyRate", bike.DailyRate);
                    command.Parameters.AddWithValue("@BikePicture", bike.BikePicture);
                    command.Parameters.AddWithValue("@BikeStatus", bike.BikeStatus);

                    return await command.ExecuteNonQueryAsync() > 0;
                }
            }
        }

        public async Task<bool> DeleteBikeAsync(string id)
        {
            using (var connection = _db.GetConnection())
            {
                await connection.OpenAsync();
                string query = "DELETE FROM bike WHERE bike_id = @Id";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    return await command.ExecuteNonQueryAsync() > 0;
                }
            }
        }

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
    }
}