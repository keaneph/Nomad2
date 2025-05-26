using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nomad2.Models
{
    public class Payment
    {
        public string PaymentId { get; set; } // length = 9
        public string CustomerId { get; set; } // foreign key
        public string BikeId { get; set; } // foreign key
        public string RentalId { get; set; } // foreign key, is a reference to a rental record
        public int? AmountToPay { get; set; } // length should be set to 9
        public int AmountPaid { get; set; } //  length should be set to 9
        public DateTime PaymentDate { get; set; } // YYYY-MM-DD
        public string PaymentStatus { get; set; }    // length 30
        public string RefundStatus { get; set; }

        // navigation properties (not in DB, using in UI)
        public Customer Customer { get; set; }
        public Bike Bike { get; set; }
    }
}
