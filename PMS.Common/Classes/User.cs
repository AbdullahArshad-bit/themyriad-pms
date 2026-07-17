using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Common
{
    public class User
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public Nullable<DateTime> DOB { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool? IsStudent { get; set; }
        public int PersonId { get; set; }
        public string ImageUrl { get; set; }
        public List<int> AssignedLocations { get; set; }
    }
}
