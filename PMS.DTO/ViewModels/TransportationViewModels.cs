using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.DTO.ViewModels.TransportationViewModels
{
    public class AddRouteVM
    {
        public DateTime DepartureTime { get; set; }
        public Nullable<int> ScheduleID { get; set; }
        public bool IsEnable { get; set; }

        public DateTime CreatedDate { get; set; }

        public string Routename { get; set; }

        public int RouteID { get; set; }
        public int? DepartureStopId { get; set; }
        public string stopName { get; set; }
        public int TravelTime { get; set; }
        public List<RouteStopVm> RouteStop { get; set; }
        public int StopCount { get; set; }
        public string ArrivalStop { get; set; }
        public int? LocationId { get; set; }
        public string LocationName { get; set; }
        
    }

    public class AddScheduleVM
    {
        public int Id { get; set; }
        public string ScheduleName { get; set; }
        [Required(ErrorMessage = "Please select Vehicle.")]
        public int VehicleId { get; set; }
        [Required(ErrorMessage = "Please select Route.")]
        public int RouteId { get; set; }
        [Required(ErrorMessage = "Please select Departure Time.")]
        public List<int> DepartureTimeId { get; set; }
        [Required(ErrorMessage = "Please select From Date.")]
        public DateTime FromDate { get; set; }
        [Required(ErrorMessage = "Please select To Date.")]
        public DateTime ToDate { get; set; }
        public bool IsActive { get; set; }
        public bool IsEnable { get; set; }
        public DateTime CretedDate { get; set; }
        public string CreatedBy { get; set; }
        public string DepartureTypeName { get; set; }
        public string VehicleName { get; set; }
        public string RouteName { get; set; }
        public TimeSpan Time { get; set; }
        public int? LocationId { get; set; }
        public string LocationName { get; set; }

    }
    public class UpdateScheduleVM
    {
        public int Id { get; set; }
        public string ScheduleName { get; set; }
        [Required(ErrorMessage = "Please select Vehicle.")]
        public int VehicleId { get; set; }
        [Required(ErrorMessage = "Please select Route.")]
        public int RouteId { get; set; }
        [Required(ErrorMessage = "Please select Departure Time.")]

        public int DepartureTimeId { get; set; }
        [Required(ErrorMessage = "Please select From Date.")]
        public DateTime FromDate { get; set; }
        public bool IsActive { get; set; }
        public bool IsEnable { get; set; }
        public string DepartureTypeName { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public string VehicleName { get; set; }
        public string RouteName { get; set; }
        public int? LocationId { get; set; }

    }


    public class UpdateRouteTimeVM
    {
        public Nullable<DateTime> StopTime { get; set; }

        public int StopNumber { get; set; }


        public int StopID { get; set; }

        public int RouteID { get; set; }

        public int RouteStopID { get; set; }
        public int TotalStopTime { get; set; }
        




    }
    public class AddRouteStopVM
    {
        public int Id { get; set; }
        public int RouteId { get; set; }
        public int StopId { get; set; }
        public int StopNumber { get; set; }
    }
    public class TravelSettingVM
    {
        public int Id { get; set; }
        public TimeSpan Time { get; set; }
        public bool IsActive { get; set; }
        public int ScheduleId { get; set; }
        public int RouteId { get; set; }
        public int BusId { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public string Schedule { get; set; }
        public string Route { get; set; }
        public string Bus { get; set; }
        
    }
    public class TimeSlotLookUpVm
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
    public class RouteStopVm
    {
        public int DepartureStopId { get; set; }
        public int OrderNumber { get; set; }
        public int StopTravelTime { get; set; }
        public bool IsDepartureStop { get; set; }
        public bool IsActive { get; set; }
        public string StopName { get; set; }
    }
    public class VehiclePriceVM
    {
        public int VehiclePriceId { get; set; }
        public string PriceName { get; set; }
        public int RouteId { get; set; }
        public decimal Price { get; set; }
        public int FrequencyId { get; set; }
        public DateTime CretedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public bool IsActive { get; set; }
        public bool IsEnable { get; set; }
        public string VehicleName { get; set; }
        public string RouteName { get; set; }
        public string FrequencyName { get; set; }
        public int? LocationId { get; set; }
        public string LocationName { get; set; }

    }
    public class VehiclePriceLookUpVm
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool Status { get; set; }
    }
    public class VehicleSubscriptionVM
    {
        public int SubscriptionId { get; set; }
        public int StudentID { get; set; }
        public int VehiclePriceID { get; set; }
        public DateTime FromDate { get; set; } 
        public DateTime ToDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public bool IsEnable { get; set; }
        public string StudentName { get; set; }
        public string PriceName { get; set; }
        public decimal SubscriptionPrice { get; set; }
        public string FrequencyName { get; set; }
        public int FrequencyId { get; set; }
        public int SubscriptionStatus { get; set; }
        public int? LocationId { get; set; }
        public string LocationName { get; set; }

    }
    public class SeatBookingVm
    {
        public int Id { get; set; }
        public int RouteId { get; set; }
        public int Scheduleid { get; set; }
        public int StudentId { get; set; }
        public int SeatId { get; set; }
        public string RouteName { get; set; }
        public string ResidentName { get; set; }
        public string SeatName { get; set; }
    }
    public class BookingTransportationVM
    {
        public string BusName { get; set; }
        public string RegistrationNumber { get; set; }
        public string SeatNumber { get; set; }
        public string RouteName { get; set; }
        public string DepartureTime { get; set; }
        public string DepartureDate { get; set; }
        public int SeatId { get; set; }
        public int? LocationId { get; set; }
    }

}
