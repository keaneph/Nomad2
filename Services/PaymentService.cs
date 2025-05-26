using MySql.Data.MySqlClient;
using Nomad2.Models;
using Nomad2.Sorting;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Windows;
using System.Linq;

namespace Nomad2.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly DatabaseHelper _db;
        private int _pageSize = 12;
        private static readonly object _paymentLock = new object();

        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = Math.Max(1, value);
        }

        public PaymentService()
        {
            _db = new DatabaseHelper();
        }

        public async Task<List<Payment>> GetAllPaymentsAsync()
        {
            using (var connection = _db.GetConnection())
            {
                await connection.OpenAsync();
                var payments = new List<Payment>();
                string query = @"SELECT p.*, c.name AS customer_name, b.bike_model AS bike_model
                                 FROM `payments` p
                                 LEFT JOIN customer c ON p.customer_id = c.customer_id
                                 LEFT JOIN bike b ON p.bike_id = b.bike_id";
                using (var command = new MySqlCommand(query, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            payments.Add(new Payment
                            {
                                PaymentId = reader["payment_id"].ToString(),
                                CustomerId = reader["customer_id"].ToString(),
                                BikeId = reader["bike_id"].ToString(),
                                RentalId = reader["rental_id"].ToString(),
                                AmountToPay = reader.IsDBNull(reader.GetOrdinal("amount_to_pay")) ? (int?)null : Convert.ToInt32(reader["amount_to_pay"]),
                                AmountPaid = Convert.ToInt32(reader["amount_paid"]),
                                PaymentDate = reader.GetDateTime("payment_date"),
                                PaymentStatus = reader["payment_status"].ToString(),
                                Customer = new Customer { Name = reader["customer_name"]?.ToString() },
                                Bike = new Bike { BikeModel = reader["bike_model"]?.ToString() }
                            });
                        }
                    }
                }
                return payments;
            }
        }

        public async Task<(List<Payment> payments, int totalCount)> GetPaymentsAsync(int page, string searchTerm, SortOption<PaymentSortOption> sortOption)
        {
            using (var connection = _db.GetConnection())
            {
                await connection.OpenAsync();
                var offset = (page - 1) * _pageSize;
                var payments = new List<Payment>();

                string orderByColumn = sortOption?.Option switch
                {
                    PaymentSortOption.PaymentId => "p.payment_id",
                    PaymentSortOption.RentalId => "p.rental_id",
                    PaymentSortOption.CustomerId => "p.customer_id",
                    PaymentSortOption.CustomerName => "c.name",
                    PaymentSortOption.BikeId => "p.bike_id",
                    PaymentSortOption.BikeModel => "b.bike_model",
                    PaymentSortOption.AmountToPay => "p.amount_to_pay",
                    PaymentSortOption.AmountPaid => "p.amount_paid",
                    PaymentSortOption.PaymentDate => "p.payment_date",
                    PaymentSortOption.PaymentStatus => "p.payment_status",
                    _ => "p.payment_date"
                };
                string direction = sortOption?.IsAscending == true ? "ASC" : "DESC";

                string query = @"SELECT SQL_CALC_FOUND_ROWS p.*, c.name AS customer_name, b.bike_model AS bike_model
                                 FROM `payments` p
                                 LEFT JOIN customer c ON p.customer_id = c.customer_id
                                 LEFT JOIN bike b ON p.bike_id = b.bike_id
                                 WHERE
                                    LOWER(p.payment_id) LIKE LOWER(@SearchTerm) OR
                                    LOWER(p.rental_id) LIKE LOWER(@SearchTerm) OR
                                    LOWER(p.customer_id) LIKE LOWER(@SearchTerm) OR
                                    LOWER(c.name) LIKE LOWER(@SearchTerm) OR
                                    LOWER(p.bike_id) LIKE LOWER(@SearchTerm) OR
                                    LOWER(b.bike_model) LIKE LOWER(@SearchTerm) OR
                                    LOWER(p.payment_status) LIKE LOWER(@SearchTerm) OR
                                    DATE_FORMAT(p.payment_date, '%Y-%m-%d') LIKE @SearchTerm OR
                                    CAST(p.amount_to_pay AS CHAR) LIKE @SearchTerm OR
                                    CAST(p.amount_paid AS CHAR) LIKE @SearchTerm
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
                            payments.Add(new Payment
                            {
                                PaymentId = reader["payment_id"].ToString(),
                                RentalId = reader["rental_id"].ToString(),
                                CustomerId = reader["customer_id"].ToString(),
                                BikeId = reader["bike_id"].ToString(),
                                AmountToPay = reader.IsDBNull(reader.GetOrdinal("amount_to_pay")) ? (int?)null : Convert.ToInt32(reader["amount_to_pay"]),
                                AmountPaid = Convert.ToInt32(reader["amount_paid"]),
                                PaymentDate = reader.GetDateTime("payment_date"),
                                PaymentStatus = reader["payment_status"].ToString(),
                                Customer = new Customer { Name = reader["customer_name"]?.ToString() },
                                Bike = new Bike { BikeModel = reader["bike_model"]?.ToString() }
                            });
                        }
                    }
                }

                using (var countCommand = new MySqlCommand("SELECT FOUND_ROWS()", connection))
                {
                    var totalCount = Convert.ToInt32(await countCommand.ExecuteScalarAsync());
                    return (payments, totalCount);
                }
            }
        }

        public async Task<Payment> GetPaymentByIdAsync(string id)
        {
            using (var connection = _db.GetConnection())
            {
                await connection.OpenAsync();
                string query = @"SELECT p.*, c.name AS customer_name, b.bike_model AS bike_model
                                 FROM `payments` p
                                 LEFT JOIN customer c ON p.customer_id = c.customer_id
                                 LEFT JOIN bike b ON p.bike_id = b.bike_id
                                 WHERE p.payment_id = @Id";
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new Payment
                            {
                                PaymentId = reader["payment_id"].ToString(),
                                CustomerId = reader["customer_id"].ToString(),
                                BikeId = reader["bike_id"].ToString(),
                                RentalId = reader["rental_id"].ToString(),
                                AmountToPay = reader.IsDBNull(reader.GetOrdinal("amount_to_pay")) ? (int?)null : Convert.ToInt32(reader["amount_to_pay"]),
                                AmountPaid = Convert.ToInt32(reader["amount_paid"]),
                                PaymentDate = reader.GetDateTime("payment_date"),
                                PaymentStatus = reader["payment_status"].ToString(),
                                Customer = new Customer { Name = reader["customer_name"]?.ToString() },
                                Bike = new Bike { BikeModel = reader["bike_model"]?.ToString() }
                            };
                        }
                        return null;
                    }
                }
            }
        }

        public async Task<bool> AddPaymentAsync(Payment payment)
        {
            using (var connection = _db.GetConnection())
            {
                await connection.OpenAsync();
                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        // Get current total paid
                        string getTotalQuery = "SELECT COALESCE(SUM(amount_paid), 0) FROM `payments` WHERE `rental_id` = @RentalId";
                        int currentTotal;
                        using (var command = new MySqlCommand(getTotalQuery, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@RentalId", payment.RentalId);
                            currentTotal = Convert.ToInt32(await command.ExecuteScalarAsync());
                        }

                        // Check if rental is completed (has a return date)
                        string getReturnDateQuery = "SELECT return_date FROM returns WHERE rental_id = @RentalId LIMIT 1";
                        DateTime? returnDate = null;
                        using (var command = new MySqlCommand(getReturnDateQuery, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@RentalId", payment.RentalId);
                            var result = await command.ExecuteScalarAsync();
                            if (result != null && result != DBNull.Value)
                                returnDate = Convert.ToDateTime(result);
                        }

                        // Only enforce the check if rental is completed
                        if (returnDate.HasValue)
                        {
                            if (payment.AmountToPay.HasValue && (currentTotal + payment.AmountPaid > payment.AmountToPay.Value))
                            {
                                throw new Exception("Total payment amount would exceed the total amount due");
                            }
                        }
                        // For ongoing rentals, allow any positive payment

                        // Insert payment
                        string insertQuery = @"
                            INSERT INTO `payments` (
                                `payment_id`, `rental_id`, `customer_id`, `bike_id`,
                                `amount_to_pay`, `amount_paid`, `payment_date`, `payment_status`
                            ) VALUES (
                                @PaymentId, @RentalId, @CustomerId, @BikeId,
                                @AmountToPay, @AmountPaid, @PaymentDate, @PaymentStatus
                            )";
                        using (var command = new MySqlCommand(insertQuery, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@PaymentId", payment.PaymentId);
                            command.Parameters.AddWithValue("@RentalId", payment.RentalId);
                            command.Parameters.AddWithValue("@CustomerId", payment.CustomerId);
                            command.Parameters.AddWithValue("@BikeId", payment.BikeId);
                            if (payment.AmountToPay.HasValue)
                                command.Parameters.AddWithValue("@AmountToPay", payment.AmountToPay.Value);
                            else
                                command.Parameters.AddWithValue("@AmountToPay", DBNull.Value);
                            command.Parameters.AddWithValue("@AmountPaid", payment.AmountPaid);
                            command.Parameters.AddWithValue("@PaymentDate", payment.PaymentDate);
                            command.Parameters.AddWithValue("@PaymentStatus", payment.PaymentStatus);
                            await command.ExecuteNonQueryAsync();
                        }

                        await transaction.CommitAsync();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        MessageBox.Show($"Error adding payment: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                }
            }
        }

        public async Task<bool> UpdatePaymentAsync(Payment payment)
        {
            using (var connection = _db.GetConnection())
            {
                await connection.OpenAsync();
                string query = @"UPDATE payments 
                                SET amount_to_pay = @AmountToPay,
                                    amount_paid = @AmountPaid,
                                    payment_date = @PaymentDate,
                                    payment_status = @PaymentStatus
                                WHERE payment_id = @PaymentId";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@PaymentId", payment.PaymentId);
                    command.Parameters.AddWithValue("@AmountToPay", payment.AmountToPay);
                    command.Parameters.AddWithValue("@AmountPaid", payment.AmountPaid);
                    command.Parameters.AddWithValue("@PaymentDate", payment.PaymentDate);
                    command.Parameters.AddWithValue("@PaymentStatus", payment.PaymentStatus);

                    try
                    {
                        await command.ExecuteNonQueryAsync();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error updating payment: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                }
            }
        }

        public async Task<bool> DeletePaymentAsync(string id)
        {
            using (var connection = _db.GetConnection())
            {
                await connection.OpenAsync();
                string query = "DELETE FROM payments WHERE payment_id = @Id";

                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);

                    try
                    {
                        await command.ExecuteNonQueryAsync();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting payment: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                }
            }
        }

        public async Task<bool> ClearAllPaymentsAsync()
        {
            using (var connection = _db.GetConnection())
            {
                await connection.OpenAsync();
                string query = "DELETE FROM payments";

                using (var command = new MySqlCommand(query, connection))
                {
                    try
                    {
                        await command.ExecuteNonQueryAsync();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error clearing payments: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                }
            }
        }

        public async Task<string> GetLastPaymentIdAsync()
        {
            using (var connection = _db.GetConnection())
            {
                await connection.OpenAsync();
                string query = "SELECT payment_id FROM payments ORDER BY payment_id DESC LIMIT 1";

                using (var command = new MySqlCommand(query, connection))
                {
                    try
                    {
                        var result = await command.ExecuteScalarAsync();
                        return result?.ToString() ?? "0000-0000";
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error getting last payment ID: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return "0000-0000";
                    }
                }
            }
        }

        public async Task<int> GetTotalPaidForRentalAsync(string rentalId)
        {
            using (var connection = _db.GetConnection())
            {
                await connection.OpenAsync();
                string query = "SELECT COALESCE(SUM(amount_paid), 0) FROM payments WHERE rental_id = @RentalId";
                using (var command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@RentalId", rentalId);
                    var result = await command.ExecuteScalarAsync();
                    return Convert.ToInt32(result);
                }
            }
        }

        public async Task ProcessRefundAsync(string paymentId, int refundAmount)
        {
            using (var connection = _db.GetConnection())
            {
                await connection.OpenAsync();
                using (var transaction = await connection.BeginTransactionAsync())
                {
                    try
                    {
                        // Get the payment details
                        string getPaymentQuery = "SELECT * FROM payments WHERE payment_id = @PaymentId";
                        Payment payment;
                        using (var command = new MySqlCommand(getPaymentQuery, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@PaymentId", paymentId);
                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                if (!await reader.ReadAsync())
                                {
                                    throw new Exception("Payment not found");
                                }
                                payment = new Payment
                                {
                                    PaymentId = reader.GetString("payment_id"),
                                    RentalId = reader.GetString("rental_id"),
                                    CustomerId = reader.GetString("customer_id"),
                                    BikeId = reader.GetString("bike_id"),
                                    AmountToPay = reader.IsDBNull(reader.GetOrdinal("amount_to_pay")) ? (int?)null : Convert.ToInt32(reader["amount_to_pay"]),
                                    AmountPaid = reader.GetInt32("amount_paid"),
                                    PaymentDate = reader.GetDateTime("payment_date"),
                                    PaymentStatus = reader.GetString("payment_status")
                                };
                            }
                        }

                        // Validate refund amount
                        if (refundAmount <= 0)
                        {
                            throw new Exception("Refund amount must be greater than 0");
                        }

                        // Get total paid for this rental
                        string getTotalPaidQuery = "SELECT COALESCE(SUM(amount_paid), 0) FROM payments WHERE rental_id = @RentalId";
                        int totalPaid;
                        using (var command = new MySqlCommand(getTotalPaidQuery, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@RentalId", payment.RentalId);
                            totalPaid = Convert.ToInt32(await command.ExecuteScalarAsync());
                        }

                        // Get the actual rental cost (from the completion payment)
                        string getRentalCostQuery = "SELECT amount_to_pay FROM payments WHERE rental_id = @RentalId AND payment_status = 'Paid' AND amount_to_pay IS NOT NULL LIMIT 1";
                        int? rentalCost = null;
                        using (var command = new MySqlCommand(getRentalCostQuery, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@RentalId", payment.RentalId);
                            var result = await command.ExecuteScalarAsync();
                            if (result != null && result != DBNull.Value)
                            {
                                rentalCost = Convert.ToInt32(result);
                            }
                        }

                        if (!rentalCost.HasValue)
                        {
                            throw new Exception("Cannot find the rental cost for this rental");
                        }

                        // Calculate the maximum refund amount (total paid - rental cost)
                        int maxRefundAmount = totalPaid - rentalCost.Value;
                        if (refundAmount > maxRefundAmount)
                        {
                            throw new Exception($"Refund amount cannot exceed the excess payment amount of {maxRefundAmount}");
                        }

                        // Generate new payment ID for refund
                        string lastId = await GetLastPaymentIdAsync();
                        string newPaymentId;
                        if (string.IsNullOrWhiteSpace(lastId) || lastId == "0000-0000")
                        {
                            newPaymentId = "0000-0001";
                        }
                        else
                        {
                            string[] parts = lastId.Split('-');
                            if (parts.Length == 2 && int.TryParse(parts[1], out int number))
                            {
                                newPaymentId = $"{parts[0]}-{(number + 1):D4}";
                            }
                            else
                            {
                                newPaymentId = "0000-0001";
                            }
                        }

                        // Create refund payment record
                        string insertRefundQuery = @"
                            INSERT INTO `payments` (
                                `payment_id`, `rental_id`, `customer_id`, `bike_id`,
                                `amount_to_pay`, `amount_paid`, `payment_date`, `payment_status`
                            ) VALUES (
                                @PaymentId, @RentalId, @CustomerId, @BikeId,
                                NULL, @AmountPaid, @PaymentDate, 'Refunded'
                            )";
                        using (var command = new MySqlCommand(insertRefundQuery, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@PaymentId", newPaymentId);
                            command.Parameters.AddWithValue("@RentalId", payment.RentalId);
                            command.Parameters.AddWithValue("@CustomerId", payment.CustomerId);
                            command.Parameters.AddWithValue("@BikeId", payment.BikeId);
                            command.Parameters.AddWithValue("@AmountPaid", -refundAmount); // Negative amount to indicate refund
                            command.Parameters.AddWithValue("@PaymentDate", DateTime.Now);
                            await command.ExecuteNonQueryAsync();
                        }

                        await transaction.CommitAsync();
                    }
                    catch (Exception)
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
        }
    }
} 