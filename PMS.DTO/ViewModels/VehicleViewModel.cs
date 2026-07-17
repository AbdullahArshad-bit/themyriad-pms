using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.DTO.ViewModels.VehicleViewModel
{
    public class VehicleViewModel
    {
        public int BusId { get; set; }
        public bool IsActive { get; set; }
        [Required(ErrorMessage = "Vehicle name is required.")]
        public string BusName { get; set; }
        public int Type { get; set; }
        [Required(ErrorMessage = "Registration number is required.")]
        public string RegistrationNumber { get; set; }
        public string Prefix { get; set; }
        //[Required(ErrorMessage = "Total seat is required.")]
        public int TotalSeats { get; set; }
        public string ImageUrl { get; set; }
        public bool IsEnable { get; set; }
        [Required(ErrorMessage = "Location is required.")]
        public int? LocationId { get; set; }
        public string LocationName { get; set; }
        public List<VehicleSeatsViewModel> Seats { get; set; }

    }
    public class VehicleSeatsViewModel
    {
        public int Id { get; set; }
        public string SeatNumber { get; set; }
        public int VehicleId { get; set; }
        public string VechicleName { get; set; }
        public int Status { get; set; }
        public string RegistrationNumber { get; set; }
        public int TotalSeats { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public bool IsEnable { get; set; }


    }
    public class AddVehicleSeatsViewModel
    {
        public int Id { get; set; }
        public string SeatNumber { get; set; }
        public int VehicleId { get; set; }
        public string VechicleName { get; set; }
        public int Status { get; set; }
        public bool IsActive { get; set; }  
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public bool IsEnable { get; set; }

    }


}
