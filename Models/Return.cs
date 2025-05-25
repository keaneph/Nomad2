using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nomad2.Models
{
    public class Return
    {
        public string ReturnId { get; set; } // length = 9
        public string CustomerId { get; set; } // foreign key
        public string BikeId { get; set; } // foreign key
        public string RentalId { get; set; } // foreign key, is a reference to a rental record
        public DateTime ReturnDate { get; set; } // YYYY-MM-DD

        // navigation properties (not in DB, using in UI)
        public Customer Customer { get; set; }
        public Bike Bike { get; set; }
    }
}
