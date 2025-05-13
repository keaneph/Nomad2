using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nomad2.Services
{
    public interface ISearchable
    {
        // property to get the current search term
        void UpdateSearch(string searchTerm);
    }
}
