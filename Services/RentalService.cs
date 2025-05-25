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
            string query = $@"
                SELECT SQL_CALC_FOUND_ROWS r.*, c.name as customer_name, c.phone_number,
                       c.address, c.government_id_picture, c.customer_status, c.registration_date,
                       b.bike_model, b.daily_rate,
                       CASE WHEN ret.return_id IS NOT NULL THEN 'Completed' ELSE 'Active' END as rental_status,
                       ret.return_date
                FROM rentals r
                LEFT JOIN customer c ON r.customer_id = c.customer_id
                LEFT JOIN bike b ON r.bike_id = b.bike_id
                LEFT JOIN `returns` ret ON r.rental_id = ret.rental_id
                WHERE (@SearchTerm = '' OR 
                      r.rental_id LIKE @SearchTerm OR 
                      c.name LIKE @SearchTerm OR 
                      b.bike_model LIKE @SearchTerm)
                ORDER BY {orderByColumn} {direction}
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
                            ReturnDate = reader.IsDBNull(reader.GetOrdinal("return_date")) ? null : (DateTime?)reader.GetDateTime("return_date"),
                            // initialize navigation properties
                            Customer = new Customer
                            {
                                CustomerId = reader.GetString("customer_id"),
                                Name = reader.GetString("customer_name"),
                                PhoneNumber = reader.GetString("phone_number"),
                                Address = reader.GetString("address"),
                                GovernmentIdPicture = reader.GetString("government_id_picture"),
                                CustomerStatus = reader.GetString("customer_status"),
                                RegistrationDate = reader.GetDateTime("registration_date")
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
                SELECT r.*, c.name as customer_name, c.phone_number, c.address, 
                       c.government_id_picture, c.customer_status, c.registration_date,
                       b.bike_model, b.daily_rate,
                       CASE WHEN ret.return_id IS NOT NULL THEN 'Completed' ELSE 'Active' END as rental_status
                FROM rentals r
                LEFT JOIN customer c ON r.customer_id = c.customer_id
                LEFT JOIN bike b ON r.bike_id = b.bike_id
                LEFT JOIN `returns` ret ON r.rental_id = ret.rental_id
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
                                PhoneNumber = reader.GetString("phone_number"),
                                Address = reader.GetString("address"),
                                GovernmentIdPicture = reader.GetString("government_id_picture"),
                                CustomerStatus = reader.GetString("customer_status"),
                                RegistrationDate = reader.GetDateTime("registration_date")
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
            using (var transaction = await connection.BeginTransactionAsync())
            {
                try
                {
                    // check if customer exists and is eligible
                    if (!await IsCustomerEligibleForRental(rental.CustomerId))
                    {
                        throw new InvalidOperationException("Customer is not eligible for rental (may have too many active rentals)");
                    }

                    // check if bike is available
                    if (!await IsBikeAvailableForRental(rental.BikeId))
                    {
                        throw new InvalidOperationException("Bike is not available for rental");
                    }

                    string query = @"
                        INSERT INTO rentals 
                        (rental_id, customer_id, bike_id, rental_date, rental_status) 
                        VALUES 
                        (@RentalId, @CustomerId, @BikeId, @RentalDate, 'Active')";

                    using (var command = new MySqlCommand(query, connection, transaction))
                    {
                        command.Parameters.AddWithValue("@RentalId", rental.RentalId);
                        command.Parameters.AddWithValue("@CustomerId", rental.CustomerId);
                        command.Parameters.AddWithValue("@BikeId", rental.BikeId);
                        command.Parameters.AddWithValue("@RentalDate", rental.RentalDate);

                        await command.ExecuteNonQueryAsync();
                    }

                    // update bike status to 'Rented'
                    string updateBikeQuery = @"
                        UPDATE bike 
                        SET bike_status = 'Rented' 
                        WHERE bike_id = @BikeId";

                    using (var command = new MySqlCommand(updateBikeQuery, connection, transaction))
                    {
                        command.Parameters.AddWithValue("@BikeId", rental.BikeId);
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
                        case 1452: // foreign key constraint violation
                            throw new InvalidOperationException("Invalid customer or bike reference", ex);
                        case 1062: // duplicate entry
                            throw new InvalidOperationException("Rental ID already exists", ex);
                        default:
                            throw new Exception("Database error occurred while adding rental", ex);
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

    // update existing rental information
    public async Task<bool> UpdateRentalAsync(Rental rental)
    {
        using (var connection = _db.GetConnection())
        {
            await connection.OpenAsync();
            using (var transaction = await connection.BeginTransactionAsync())
            {
                try
                {
                    // first check if rental has a return record
                    string checkReturnQuery = @"
                        SELECT COUNT(*) 
                        FROM `returns` 
                        WHERE rental_id = @RentalId";

                    using (var checkCommand = new MySqlCommand(checkReturnQuery, connection, transaction))
                    {
                        checkCommand.Parameters.AddWithValue("@RentalId", rental.RentalId);
                        int hasReturn = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());
                        if (hasReturn > 0)
                        {
                            throw new InvalidOperationException("Cannot update rental because it has a return record");
                        }
                    }

                    // update the rental
                    string query = @"
                        UPDATE rentals 
                        SET customer_id = @CustomerId,
                            bike_id = @BikeId,
                            rental_date = @RentalDate,
                            rental_status = 'Active'
                        WHERE rental_id = @RentalId";

                    using (var command = new MySqlCommand(query, connection, transaction))
                    {
                        command.Parameters.AddWithValue("@RentalId", rental.RentalId);
                        command.Parameters.AddWithValue("@CustomerId", rental.CustomerId);
                        command.Parameters.AddWithValue("@BikeId", rental.BikeId);
                        command.Parameters.AddWithValue("@RentalDate", rental.RentalDate);

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        if (rowsAffected == 0)
                        {
                            throw new InvalidOperationException("Rental not found");
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

    // delete rental record from database
    public async Task<bool> DeleteRentalAsync(string id)
    {
        using (var connection = _db.GetConnection())
        {
            await connection.OpenAsync();
            using (var transaction = await connection.BeginTransactionAsync())
            {
                try
                {
                    // first get the rental to check its status and get the bike ID
                    string getRentalQuery = @"
                        SELECT r.rental_status, r.bike_id, r.customer_id,
                               (SELECT COUNT(*) FROM `returns` WHERE rental_id = r.rental_id) as has_return
                        FROM rentals r 
                        WHERE r.rental_id = @Id";
                    string rentalStatus = null;
                    string bikeId = null;
                    string customerId = null;
                    int hasReturn = 0;

                    using (var command = new MySqlCommand(getRentalQuery, connection, transaction))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                rentalStatus = reader.GetString("rental_status");
                                bikeId = reader.GetString("bike_id");
                                customerId = reader.GetString("customer_id");
                                hasReturn = reader.GetInt32("has_return");
                            }
                        }
                    }

                    if (rentalStatus == null)
                    {
                        throw new InvalidOperationException("Rental not found");
                    }

                    // check if rental has a return record
                    if (hasReturn > 0)
                    {
                        throw new InvalidOperationException("Cannot delete rental because it has a return record");
                    }

                    // delete the rental
                    string deleteQuery = "DELETE FROM rentals WHERE rental_id = @Id";
                    using (var command = new MySqlCommand(deleteQuery, connection, transaction))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        await command.ExecuteNonQueryAsync();
                    }

                    // if the rental was active, update the bike status back to available
                    if (rentalStatus == "Active" && bikeId != null)
                    {
                        string updateBikeQuery = @"
                            UPDATE bike 
                            SET bike_status = 'Available' 
                            WHERE bike_id = @BikeId";

                        using (var command = new MySqlCommand(updateBikeQuery, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@BikeId", bikeId);
                            await command.ExecuteNonQueryAsync();
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
                            throw new InvalidOperationException("Cannot delete rental because it has associated payments or returns", ex);
                        default:
                            throw new Exception("Database error occurred while deleting rental", ex);
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

    // clear all rental records from database
    public async Task<bool> ClearAllRentalsAsync()
    {
        using (var connection = _db.GetConnection())
        {
            await connection.OpenAsync();
            using (var transaction = await connection.BeginTransactionAsync())
            {
                try
                {
                    // check if any rentals have return records
                    string checkReturnsQuery = @"
                        SELECT COUNT(*) 
                        FROM rentals r
                        WHERE EXISTS (
                            SELECT 1 
                            FROM `returns` ret 
                            WHERE ret.customer_id = r.customer_id 
                            AND ret.bike_id = r.bike_id
                        )";

                    using (var command = new MySqlCommand(checkReturnsQuery, connection, transaction))
                    {
                        int rentalsWithReturns = Convert.ToInt32(await command.ExecuteScalarAsync());
                        if (rentalsWithReturns > 0)
                        {
                            throw new InvalidOperationException("Cannot clear rentals because some have return records");
                        }
                    }

                    // first get all active rentals to update bike and customer statuses
                    string getActiveRentalsQuery = @"
                        SELECT DISTINCT r.customer_id, r.bike_id
                        FROM rentals r
                        WHERE r.rental_status = 'Active'";

                    var activeRentals = new List<(string CustomerId, string BikeId)>();
                    using (var command = new MySqlCommand(getActiveRentalsQuery, connection, transaction))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                activeRentals.Add((
                                    reader.GetString("customer_id"),
                                    reader.GetString("bike_id")
                                ));
                            }
                        }
                    }

                    // update all bikes to Available
                    foreach (var rental in activeRentals)
                    {
                        string updateBikeQuery = @"
                            UPDATE bike 
                            SET bike_status = 'Available' 
                            WHERE bike_id = @BikeId";

                        using (var command = new MySqlCommand(updateBikeQuery, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@BikeId", rental.BikeId);
                            await command.ExecuteNonQueryAsync();
                        }
                    }

                    // update all customers to Inactive
                    foreach (var rental in activeRentals)
                    {
                        string updateCustomerQuery = @"
                            UPDATE customer 
                            SET customer_status = 'Inactive' 
                            WHERE customer_id = @CustomerId";

                        using (var command = new MySqlCommand(updateCustomerQuery, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@CustomerId", rental.CustomerId);
                            await command.ExecuteNonQueryAsync();
                        }
                    }

                    // finally delete all rentals
                    string deleteQuery = "DELETE FROM rentals";
                    using (var command = new MySqlCommand(deleteQuery, connection, transaction))
                    {
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
                        case 1451: // cannot delete or update a parent row
                            throw new InvalidOperationException("Cannot clear rentals because some have associated payments or returns", ex);
                        default:
                            throw new Exception("Database error occurred while clearing rentals", ex);
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
                       c.address, c.government_id_picture, c.customer_status, c.registration_date,
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
                                PhoneNumber = reader.GetString("phone_number"),
                                Address = reader.GetString("address"),
                                GovernmentIdPicture = reader.GetString("government_id_picture"),
                                CustomerStatus = reader.GetString("customer_status"),
                                RegistrationDate = reader.GetDateTime("registration_date")
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
                       c.address, c.government_id_picture, c.customer_status, c.registration_date,
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
                                PhoneNumber = reader.GetString("phone_number"),
                                Address = reader.GetString("address"),
                                GovernmentIdPicture = reader.GetString("government_id_picture"),
                                CustomerStatus = reader.GetString("customer_status"),
                                RegistrationDate = reader.GetDateTime("registration_date")
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

            // check if bike is already rented
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