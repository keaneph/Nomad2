using System.Data;
using MySql.Data.MySqlClient;
using Nomad2.Models;
using Nomad2.Services;
using Nomad2.Sorting;

// rental service class that implements the rental interface
public class RentalService : IRentalService
{
    // database helper and default page size
    private readonly DatabaseHelper _db;
    private int _pageSize = 4;

    // ensures page size is at least 1
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = Math.Max(1, value);
    }

    public RentalService()
    {
        _db = new DatabaseHelper();
    }


    // get rentals with pagination, search, and sort
    public async Task<(List<Rental> rentals, int totalCount)> GetRentalsAsync(
        int page = 1,
        string searchTerm = "",
        SortOption<RentalSortOption> sortOption = null)
    {
        using (var connection = _db.GetConnection())
        {
            await connection.OpenAsync();

            var offset = (page - 1) * _pageSize;
            var rentals = new List<Rental>();

            // determine sort column based on user selection
            string orderByColumn = sortOption?.Option switch
            {
                RentalSortOption.RentalId => "r.rental_id",
                RentalSortOption.CustomerId => "r.customer_id",
                RentalSortOption.CustomerName => "c.name",
                RentalSortOption.BikeId => "r.bike_id",
                RentalSortOption.BikeModel => "b.bike_model",
                RentalSortOption.RentalDate => "r.rental_date",
                RentalSortOption.RentalStatus => "r.rental_status",
                _ => "r.rental_date"
            };

            string direction = sortOption?.IsAscending == true ? "ASC" : "DESC";

            //join query to get rental data with customer and bike info
            string query = @"
                SELECT SQL_CALC_FOUND_ROWS 
                    r.*, 
                    c.name as customer_name, 
                    c.phone_number,
                    b.bike_model,
                    b.daily_rate
                FROM rentals r
                LEFT JOIN customer c ON r.customer_id = c.customer_id
                LEFT JOIN bike b ON r.bike_id = b.bike_id
                WHERE 
                    LOWER(r.rental_id) LIKE LOWER(@SearchTerm) OR
                    LOWER(r.customer_id) LIKE LOWER(@SearchTerm) OR
                    LOWER(c.name) LIKE LOWER(@SearchTerm) OR
                    LOWER(r.bike_id) LIKE LOWER(@SearchTerm) OR
                    LOWER(b.bike_model) LIKE LOWER(@SearchTerm) OR
                    LOWER(r.rental_status) LIKE LOWER(@SearchTerm) OR
                    DATE_FORMAT(r.rental_date, '%Y-%m-%d') LIKE @SearchTerm
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
                        var rental = new Rental
                        {
                            // read results and create rental objects with related data
                            RentalId = reader.GetString("rental_id"),
                            CustomerId = reader.GetString("customer_id"),
                            BikeId = reader.GetString("bike_id"),
                            RentalDate = reader.GetDateTime("rental_date"),
                            RentalStatus = reader.GetString("rental_status"),
                            // initialize navigation properties
                            Customer = new Customer
                            {
                                CustomerId = reader.GetString("customer_id"),
                                Name = reader.GetString("customer_name"),
                                PhoneNumber = reader.GetString("phone_number")
                            },
                            Bike = new Bike
                            {
                                BikeId = reader.GetString("bike_id"),
                                BikeModel = reader.GetString("bike_model"),
                                DailyRate = reader.GetInt32("daily_rate")
                            }
                        };
                        rentals.Add(rental);
                    }
                }

                using (var countCommand = new MySqlCommand("SELECT FOUND_ROWS()", connection))
                {
                    var totalCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync());
                    return (rentals, totalCount);
                }
            }
        }
    }

    // get specific rental by id including customer and bike details
    public async Task<Rental> GetRentalByIdAsync(string id)
    {
        using (var connection = _db.GetConnection())
        {
            await connection.OpenAsync();
            string query = @"
                SELECT r.*, c.name as customer_name, c.phone_number, 
                       b.bike_model, b.daily_rate
                FROM rentals r
                LEFT JOIN customer c ON r.customer_id = c.customer_id
                LEFT JOIN bike b ON r.bike_id = b.bike_id
                WHERE r.rental_id = @Id";

            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Id", id);
                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return new Rental
                        {
                            RentalId = reader.GetString("rental_id"),
                            CustomerId = reader.GetString("customer_id"),
                            BikeId = reader.GetString("bike_id"),
                            RentalDate = reader.GetDateTime("rental_date"),
                            RentalStatus = reader.GetString("rental_status"),
                            Customer = new Customer
                            {
                                CustomerId = reader.GetString("customer_id"),
                                Name = reader.GetString("customer_name"),
                                PhoneNumber = reader.GetString("phone_number")
                            },
                            Bike = new Bike
                            {
                                BikeId = reader.GetString("bike_id"),
                                BikeModel = reader.GetString("bike_model"),
                                DailyRate = reader.GetInt32("daily_rate")
                            }
                        };
                    }
                    return null;
                }
            }
        }
    }

    // add new rental record to database
    public async Task<bool> AddRentalAsync(Rental rental)
    {
        using (var connection = _db.GetConnection())
        {
            await connection.OpenAsync();
            string query = @"
                INSERT INTO rentals 
                (rental_id, customer_id, bike_id, rental_date, rental_status) 
                VALUES 
                (@RentalId, @CustomerId, @BikeId, @RentalDate, @RentalStatus)";

            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@RentalId", rental.RentalId);
                command.Parameters.AddWithValue("@CustomerId", rental.CustomerId);
                command.Parameters.AddWithValue("@BikeId", rental.BikeId);
                command.Parameters.AddWithValue("@RentalDate", rental.RentalDate);
                command.Parameters.AddWithValue("@RentalStatus", rental.RentalStatus);

                return await command.ExecuteNonQueryAsync() > 0;
            }
        }
    }

    // update existing rental information
    public async Task<bool> UpdateRentalAsync(Rental rental)
    {
        using (var connection = _db.GetConnection())
        {
            await connection.OpenAsync();
            string query = @"
                UPDATE rentals 
                SET customer_id = @CustomerId,
                    bike_id = @BikeId,
                    rental_date = @RentalDate,
                    rental_status = @RentalStatus
                WHERE rental_id = @RentalId";

            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@RentalId", rental.RentalId);
                command.Parameters.AddWithValue("@CustomerId", rental.CustomerId);
                command.Parameters.AddWithValue("@BikeId", rental.BikeId);
                command.Parameters.AddWithValue("@RentalDate", rental.RentalDate);
                command.Parameters.AddWithValue("@RentalStatus", rental.RentalStatus);

                return await command.ExecuteNonQueryAsync() > 0;
            }
        }
    }

    // delete rental record from database
    public async Task<bool> DeleteRentalAsync(string id)
    {
        using (var connection = _db.GetConnection())
        {
            await connection.OpenAsync();
            string query = "DELETE FROM rentals WHERE rental_id = @Id";

            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Id", id);
                return await command.ExecuteNonQueryAsync() > 0;
            }
        }
    }

    // clear all rental records from database
    public async Task<bool> ClearAllRentalsAsync()
    {
        using (var connection = _db.GetConnection())
        {
            await connection.OpenAsync();
            string query = "DELETE FROM rentals";

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

    // get last rental id for auto-generation
    public async Task<string> GetLastRentalIdAsync()
    {
        using (var connection = _db.GetConnection())
        {
            await connection.OpenAsync();
            string query = "SELECT rental_id FROM rentals ORDER BY rental_id DESC LIMIT 1";

            using (var command = new MySqlCommand(query, connection))
            {
                var result = await command.ExecuteScalarAsync();
                return result?.ToString();
            }
        }
    }

    // get all active rentals for a specific customer
    public async Task<List<Rental>> GetActiveRentalsByCustomerAsync(string customerId)
    {
        using (var connection = _db.GetConnection())
        {
            await connection.OpenAsync();
            var rentals = new List<Rental>();

            string query = @"
                SELECT r.*, c.name as customer_name, c.phone_number,
                       b.bike_model, b.daily_rate
                FROM rentals r
                LEFT JOIN customer c ON r.customer_id = c.customer_id
                LEFT JOIN bike b ON r.bike_id = b.bike_id
                WHERE r.customer_id = @CustomerId 
                AND r.rental_status = 'Active'";

            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@CustomerId", customerId);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        rentals.Add(new Rental
                        {
                            RentalId = reader.GetString("rental_id"),
                            CustomerId = reader.GetString("customer_id"),
                            BikeId = reader.GetString("bike_id"),
                            RentalDate = reader.GetDateTime("rental_date"),
                            RentalStatus = reader.GetString("rental_status"),
                            Customer = new Customer
                            {
                                CustomerId = reader.GetString("customer_id"),
                                Name = reader.GetString("customer_name"),
                                PhoneNumber = reader.GetString("phone_number")
                            },
                            Bike = new Bike
                            {
                                BikeId = reader.GetString("bike_id"),
                                BikeModel = reader.GetString("bike_model"),
                                DailyRate = reader.GetInt32("daily_rate")
                            }
                        });
                    }
                }
            }
            return rentals;
        }
    }

    // get all active rentals for a specific bike
    public async Task<List<Rental>> GetActiveRentalsByBikeAsync(string bikeId)
    {
        using (var connection = _db.GetConnection())
        {
            await connection.OpenAsync();
            var rentals = new List<Rental>();

            string query = @"
                SELECT r.*, c.name as customer_name, c.phone_number,
                       b.bike_model, b.daily_rate
                FROM rentals r
                LEFT JOIN customer c ON r.customer_id = c.customer_id
                LEFT JOIN bike b ON r.bike_id = b.bike_id
                WHERE r.bike_id = @BikeId 
                AND r.rental_status = 'Active'";

            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@BikeId", bikeId);
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        rentals.Add(new Rental
                        {
                            RentalId = reader.GetString("rental_id"),
                            CustomerId = reader.GetString("customer_id"),
                            BikeId = reader.GetString("bike_id"),
                            RentalDate = reader.GetDateTime("rental_date"),
                            RentalStatus = reader.GetString("rental_status"),
                            Customer = new Customer
                            {
                                CustomerId = reader.GetString("customer_id"),
                                Name = reader.GetString("customer_name"),
                                PhoneNumber = reader.GetString("phone_number")
                            },
                            Bike = new Bike
                            {
                                BikeId = reader.GetString("bike_id"),
                                BikeModel = reader.GetString("bike_model"),
                                DailyRate = reader.GetInt32("daily_rate")
                            }
                        });
                    }
                }
            }
            return rentals;
        }
    }

    public async Task<bool> IsCustomerEligibleForRental(string customerId)
    {
        using (var connection = _db.GetConnection())
        {
            await connection.OpenAsync();

            // check if customer has any active rentals
            string query = @"
                SELECT COUNT(*) 
                FROM rentals 
                WHERE customer_id = @CustomerId 
                AND rental_status = 'Active'";

            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@CustomerId", customerId);
                int activeRentals = Convert.ToInt32(await command.ExecuteScalarAsync());

                // FIXME set business rules here
                // example, maximum 3 active rentals per customer
                return activeRentals < 3;
            }
        }
    }

    public async Task<bool> IsBikeAvailableForRental(string bikeId)
    {
        using (var connection = _db.GetConnection())
        {
            await connection.OpenAsync();

            // Check if bike is already rented
            string query = @"
                SELECT COUNT(*) 
                FROM rentals 
                WHERE bike_id = @BikeId 
                AND rental_status = 'Active'";

            using (var command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@BikeId", bikeId);
                int activeRentals = Convert.ToInt32(await command.ExecuteScalarAsync());

                // a bike can only be rented once at a time
                return activeRentals == 0;
            }
        }
    }
}