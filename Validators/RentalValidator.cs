using Nomad2.Models;
using System;

namespace Nomad2.Validators
{
    public static class RentalValidator
    {
        public static (bool isValid, string errorMessage) ValidateRental(Rental rental)
        {
            // Check for null
            if (rental == null)
                return (false, "Rental cannot be null.");

            // Validate RentalId (following your 9-character pattern)
            if (string.IsNullOrWhiteSpace(rental.RentalId) || rental.RentalId.Length > 9)
                return (false, "Invalid Rental ID. Must not be empty and no more than 9 characters.");

            // Validate CustomerId
            if (string.IsNullOrWhiteSpace(rental.CustomerId))
                return (false, "Customer ID is required.");

            // Validate BikeId
            if (string.IsNullOrWhiteSpace(rental.BikeId))
                return (false, "Bike ID is required.");

            // Validate RentalDate
            if (rental.RentalDate == default)
                return (false, "Rental date is required.");

            if (rental.RentalDate > DateTime.Now)
                return (false, "Rental date cannot be in the future.");

            // Validate RentalStatus
            if (string.IsNullOrWhiteSpace(rental.RentalStatus))
                return (false, "Rental status is required.");

            // Validate RentalStatus values
            if (!IsValidRentalStatus(rental.RentalStatus))
                return (false, "Invalid rental status. Must be 'Active', 'Completed', or 'Overdue'.");

            // If all validations pass
            return (true, string.Empty);
        }

        private static bool IsValidRentalStatus(string status)
        {
            return status is "Active" or "Completed" or "Overdue";
        }
    }
}