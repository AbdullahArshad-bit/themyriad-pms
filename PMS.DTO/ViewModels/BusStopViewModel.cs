using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.DTO.ViewModels
{
    public class AddBusStopViewModel
    {
        
        public int Id { get; set; }
        public bool IsActive { get; set; }
        [Required]
        public string Name { get; set; }
        [Required]
        public string Latitude { get; set; }
        [Required]
        public string Longitude { get; set; }
        public DateTime TimeTravel { get; set; }
        public string FullName { get; set; }
        public string DistanceFromDeparture { get; set; }
        public int? LocationId { get; set; }
        public string LocationName { get; set; }

    }
}
