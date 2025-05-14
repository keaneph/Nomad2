using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nomad2.Models
{
    public class Bike
    {
        // basing this on data dict
        public string BikeId { get; set; } // length should be set to 9
        public string BikeModel { get; set; }  // length should be set to 100
        public string BikeType { get; set; } // length should be set to 30
        public int DailyRate { get; set; } //  length should be set to 9
        public string BikePicture { get; set; } // this is the image_path (is set to string tho)
        public string BikeStatus { get; set; } // length should be set to 30
    }
}
