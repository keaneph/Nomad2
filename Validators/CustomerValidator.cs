// Create new file: Validators/CustomerValidator.cs
using System;
using System.Text.RegularExpressions;
using Nomad2.Models;

namespace Nomad2.Validators
{
    public class CustomerValidator
    {
        public static (bool isValid, string errorMessage) ValidateCustomer(Customer customer)
        {
            // Check for null
            if (customer == null)
                return (false, "Customer cannot be null");

            // Validate CustomerId (VARCHAR(9))
            if (string.IsNullOrWhiteSpace(customer.CustomerId) || customer.CustomerId.Length > 9)
                return (false, "Customer ID must not be empty and cannot exceed 9 characters");

            // Validate Name (VARCHAR(100))
            if (string.IsNullOrWhiteSpace(customer.Name))
                return (false, "Name is required");
            if (customer.Name.Length > 100)
                return (false, "Name cannot exceed 100 characters");
            if (!Regex.IsMatch(customer.Name, @"^[a-zA-Z\s\-']+$"))
                return (false, "Name can only contain letters, spaces, hyphens, and apostrophes");

            // Validate PhoneNumber (VARCHAR(30))
            if (string.IsNullOrWhiteSpace(customer.PhoneNumber))
                return (false, "Phone number is required");
            if (customer.PhoneNumber.Length > 30)
                return (false, "Phone number cannot exceed 30 characters");
            if (!Regex.IsMatch(customer.PhoneNumber, @"^[0-9\+\-$$$$\s]+$"))
                return (false, "Phone number can only contain numbers, +, -, (), and spaces");

            // Validate Address (VARCHAR(200))
            if (string.IsNullOrWhiteSpace(customer.Address))
                return (false, "Address is required");
            if (customer.Address.Length > 200)
                return (false, "Address cannot exceed 200 characters");

            // Validate GovernmentIdPicture (VARCHAR(255))
            if (string.IsNullOrWhiteSpace(customer.GovernmentIdPicture))
                return (false, "Government ID picture is required");
            if (customer.GovernmentIdPicture.Length > 255)
                return (false, "Government ID picture path cannot exceed 255 characters");

            // Validate CustomerStatus (VARCHAR(30))
            if (string.IsNullOrWhiteSpace(customer.CustomerStatus))
                return (false, "Customer status is required");
            if (customer.CustomerStatus.Length > 30)
                return (false, "Customer status cannot exceed 30 characters");
            if (!new[] { "Active", "Inactive", "Blacklisted" }.Contains(customer.CustomerStatus))
                return (false, "Invalid customer status");

            // Validate RegistrationDate (DATE)
            if (customer.RegistrationDate == default)
                return (false, "Registration date is required");
            if (customer.RegistrationDate > DateTime.Now)
                return (false, "Registration date cannot be in the future");

            return (true, string.Empty);
        }
    }
}