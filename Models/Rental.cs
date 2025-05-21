using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nomad2.Models
{
    public class Rental
    {
        public string RentalId { get; set; }        // length 9, following your pattern
        public string CustomerId { get; set; }
        public string BikeId { get; set; }
        public DateTime RentalDate { get; set; }
        public string RentalStatus { get; set; }    // length 30, following your pattern

        // Navigation properties (not in DB, but useful for UI)
        public Customer Customer { get; set; }
        public Bike Bike { get; set; }
    }
}
