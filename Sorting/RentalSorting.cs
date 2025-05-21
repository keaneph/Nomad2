using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nomad2.Sorting
{
    public enum RentalSortOption
    {
        RentalId,
        CustomerId,
        CustomerName,  // for joined queries
        BikeId,
        BikeModel,     // for joined queries
        RentalDate,
        RentalStatus
    }
}
