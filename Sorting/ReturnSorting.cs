using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nomad2.Sorting
{
    // enum defining all possible sorting options for return records
    public enum ReturnSortOption
    {
        ReturnId,
        RentalId,
        CustomerId,
        CustomerName,
        BikeId,
        BikeModel,
        ReturnDate
    }

    // SortOption<T> is already defined in CustomerSorting.cs, so we do not redefine it here.
} 