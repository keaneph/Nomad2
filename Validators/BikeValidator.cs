using System;
using System.Text.RegularExpressions;
using Nomad2.Models;

namespace Nomad2.Validators
{
    public class BikeValidator
    {
        public static (bool isValid, string errorMessage) ValidateBike(Bike bike)
        {
            // check for null
            if (bike == null)
                return (false, "Bike cannot be null");

            // validate BikeId (VARCHAR(9))
            if (string.IsNullOrWhiteSpace(bike.BikeId) || bike.BikeId.Length > 9)
                return (false, "Bike ID must not be empty and cannot exceed 9 characters");

            // validate BikeModel (VARCHAR(100))
            if (string.IsNullOrWhiteSpace(bike.BikeModel))
                return (false, "Bike model is required");
            if (bike.BikeModel.Length > 100)
                return (false, "Bike model cannot exceed 100 characters");
            if (!Regex.IsMatch(bike.BikeModel, @"^[a-zA-Z0-9\s\-']+$"))
                return (false, "Bike model can only contain letters, numbers, spaces, hyphens, and apostrophes");

            // validate BikeType (VARCHAR(30))
            if (string.IsNullOrWhiteSpace(bike.BikeType))
                return (false, "Bike type is required");
            if (bike.BikeType.Length > 30)
                return (false, "Bike type cannot exceed 30 characters");
            if (!Regex.IsMatch(bike.BikeType, @"^[a-zA-Z\s\-']+$"))
                return (false, "Bike type can only contain letters, spaces, hyphens, and apostrophes");

            // validate DailyRate
            if (bike.DailyRate <= 0)
                return (false, "Daily rate must be greater than 0");
            if (bike.DailyRate.ToString().Length > 9)
                return (false, "Daily rate cannot exceed 9 digits");

            // validate BikePicture (VARCHAR(255))
            if (string.IsNullOrWhiteSpace(bike.BikePicture))
                return (false, "Bike picture is required");
            if (bike.BikePicture.Length > 255)
                return (false, "Bike picture path cannot exceed 255 characters");

            // validate file extension
            string[] allowedExtensions = { ".png", ".jpeg", ".jpg" };
            string extension = System.IO.Path.GetExtension(bike.BikePicture).ToLower();
            if (!allowedExtensions.Contains(extension))
                return (false, "Bike picture must be a PNG, JPEG, or JPG file");

            // validate if file exists
            if (!System.IO.File.Exists(bike.BikePicture))
                return (false, "Government ID picture file does not exist");

            // validate BikeStatus (VARCHAR(30))
            if (string.IsNullOrWhiteSpace(bike.BikeStatus))
                return (false, "Bike status is required");
            if (bike.BikeStatus.Length > 30)
                return (false, "Bike status cannot exceed 30 characters");
            // Since we're using a dropdown for status, we should validate against allowed values
            if (!new[] { "Available", "Rented", "Under Maintenance" }.Contains(bike.BikeStatus))
                return (false, "Invalid bike status");

            return (true, string.Empty);
        }
    }
}