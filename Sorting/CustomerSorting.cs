using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nomad2.Sorting
{
    // enum defining all possible sorting options for customer records
    public enum CustomerSortOption
    {
        CustomerId,
        Name,
        PhoneNumber,
        Address,
        RegistrationDate,
        Status
    }

    public class SortOption
    {
        //displayname is used for the XAML UI
        public string DisplayName { get; set; }
        // actual sorting option to be used
        public CustomerSortOption Option { get; set; }
        // the direction of sorting (ascending or descending) used by the toggle button in the UI
        public bool IsAscending { get; set; } = true; 
    }
}
