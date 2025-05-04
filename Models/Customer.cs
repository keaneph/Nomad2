using System;

namespace Nomad2.Models
{
    public class Customer
    {
        public string CustomerId { get; set; }
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
        public string GovernmentIdPicture { get; set; }
        public string CustomerStatus { get; set; }
        public DateTime RegistrationDate { get; set; }
    }
}