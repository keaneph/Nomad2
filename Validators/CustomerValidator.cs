using System;
using System.Text.RegularExpressions;
using Nomad2.Models;

namespace Nomad2.Validators
{
    // is being called by addcustomer and updatecustomer to check if the customer input is valid
    public class CustomerValidator
    {
        public static (bool isValid, string errorMessage) ValidateCustomer(Customer customer)
        {
            // check for null
            if (customer == null)
                return (false, "Customer cannot be null");

            // validate CustomerId (VARCHAR(9))
            // not actually needed since the customer id is auto generated
            if (string.IsNullOrWhiteSpace(customer.CustomerId) || customer.CustomerId.Length > 9)
                return (false, "Customer ID must not be empty and cannot exceed 9 characters");

            // validate Name (VARCHAR(100))
            if (string.IsNullOrWhiteSpace(customer.Name))
                return (false, "Name is required");
            if (customer.Name.Length > 100)
                return (false, "Name cannot exceed 100 characters");
            if (!Regex.IsMatch(customer.Name, @"^[a-zA-Z\s\-']+$"))
                return (false, "Name can only contain letters, spaces, hyphens, and apostrophes");

            // validate PhoneNumber (VARCHAR(30))
            if (string.IsNullOrWhiteSpace(customer.PhoneNumber))
                return (false, "Phone number is required");
            if (customer.PhoneNumber.Length > 30)
                return (false, "Phone number cannot exceed 30 characters");
            if (!Regex.IsMatch(customer.PhoneNumber, @"^[0-9+]+$"))
                return (false, "Phone number can only contain numbers and +");

            // validate Address (VARCHAR(200))
            if (string.IsNullOrWhiteSpace(customer.Address))
                return (false, "Address is required");
            if (customer.Address.Length > 200)
                return (false, "Address cannot exceed 200 characters");

            // validate GovernmentIdPicture (VARCHAR(255))
            if (string.IsNullOrWhiteSpace(customer.GovernmentIdPicture))
                return (false, "Government ID picture is required");
            // not sure how to validate the image path but i dont think image path will exceed 255 characters
            // this is just a placeholder
            // might include actual image validation in the future
            if (customer.GovernmentIdPicture.Length > 255)
                return (false, "Government ID picture path cannot exceed 255 characters");

            // validate CustomerStatus (VARCHAR(30))
            if (string.IsNullOrWhiteSpace(customer.CustomerStatus))
                return (false, "Customer status is required");
            // not sure how to validate the customer status but i think it should be one of the following
            // i changed this to combobox so the customer status doesnt need the length
            // if (customer.CustomerStatus.Length > 30)
            // but might include an additional feature in the future
            if (customer.CustomerStatus.Length > 30)
                return (false, "Customer status cannot exceed 30 characters");
            if (!new[] { "Active", "Inactive", "Blacklisted" }.Contains(customer.CustomerStatus))
                return (false, "Invalid customer status");

            // validate RegistrationDate (DATE)
            if (customer.RegistrationDate == default)
                return (false, "Registration date is required");
            if (customer.RegistrationDate > DateTime.Now)
                return (false, "Registration date cannot be in the future");

            return (true, string.Empty);
        }
    }
}