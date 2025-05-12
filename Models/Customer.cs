using System;

namespace Nomad2.Models
{
    public class Customer
    {
        // basing this on data dict
        public string CustomerId { get; set; } // length should be set to 9
        public string Name { get; set; }  // length should be set to 100
        public string PhoneNumber { get; set; } // length should be set to 30
        public string Address { get; set; } //  length should be set to 200
        public string GovernmentIdPicture { get; set; } // this is the image_path (is set to string tho)
        public string CustomerStatus { get; set; } // length should be set to 30
        public DateTime RegistrationDate { get; set; } //YYYY-MM-DD. Could just use DateTime.Now to get current date
    }
}