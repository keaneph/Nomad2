using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nomad2.Sorting
{
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
        public string DisplayName { get; set; }
        public CustomerSortOption Option { get; set; }
        public bool IsAscending { get; set; } = true; // Keep this for the service
    }
}
