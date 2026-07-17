using PMS.Common.Classes;
using PMS.DTO.ViewModels.TransportationViewModels;
using PMS.EF;
using PMS.Repository.UnitOfWork;
using PMS.Services.Services.Correspondence;
using PMS.Services.Services.Email;
using PMS.Services.Services.LocationContext;
using PMS.Services.Services.Vehicle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace PMS.Services.Services.Schedule
{
    public class ScheduleService : IScheduleService
    {
        private readonly UnitOfWork<PMSEntities> uow;
        private readonly ICorrespondenceService correspondenceService;
        private readonly IEmailService emailService;
        private readonly IVehicleService vehicleService;
        private readonly ILocationContextService locationContextService;

        public ScheduleService(UnitOfWork<PMSEntities> _uow, ICorrespondenceService _correspondenceService, IEmailService _emailService, IVehicleService _vehicleService
            , ILocationContextService _locationContextService)
        {
            uow = _uow;
            correspondenceService = _correspondenceService;
            emailService = _emailService;
            vehicleService = _vehicleService;
            locationContextService = _locationContextService;
        }
        public AddScheduleVM GetScheduleByID(int scheduleID)
        {
            try
            {
                return uow.GenericRepository<EF.Schedule>().GetAll().Where(x => x.ScheduleID == scheduleID).Select(x => new AddScheduleVM
                {
                    Id = x.ScheduleID
                }).FirstOrDefault();


            }
            catch (Exception ex)
            {
                return null;
            }

        }
        public List<AddScheduleVM> GetAll()
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();

            var res = uow.GenericRepository<EF.Schedule>().GetAll(x => x.IsEnable == true && assignedLocationIds.Contains((int)x.LocationId)).Select(x => new AddScheduleVM
            {
                Id = x.ScheduleID,
                ScheduleName = x.ScheduleName,
                FromDate = x.FromDate,
                RouteId = x.RouteId,
                VehicleName = x.Bus.BusName,
                RouteName = x.Route.RouteName,
                DepartureTypeName = x.TimeSlotLookUp.Slot,
                IsActive = x.IsActive,
                LocationId = x.LocationId,
                LocationName = x.Location.LocationName

            }).ToList();
            return res;

        }
        public UpdateScheduleVM GetById(int id)
        {
            var schedule = uow.GenericRepository<EF.Schedule>().Table.Where(x => x.ScheduleID == id).Select(x => new UpdateScheduleVM
            {
                Id = x.ScheduleID,
                UpdatedDate = DateTime.Now,
                UpdatedBy = Common.Globals.User.Email,
                VehicleId = x.VehicleId,
                RouteId = x.RouteId,
                FromDate = x.FromDate,
                DepartureTimeId = x.DepartureTimeId,
                IsActive = x.IsActive,
                IsEnable = true,
                LocationId = x.LocationId


            }).FirstOrDefault();
            return schedule;
        }
        public bool Add(AddScheduleVM Schedule)
        {
            try
            {

                foreach (var Id in Schedule.DepartureTimeId)
                {

                    for (var i = Schedule.FromDate; i <= Schedule.ToDate; i = i.AddDays(1))
                    {
                        var schedule = new EF.Schedule();
                        schedule.RouteId = Schedule.RouteId;
                        schedule.VehicleId = Schedule.VehicleId;
                        schedule.IsEnable = true;
                        schedule.IsActive = true;
                        schedule.CreatedDate = DateTime.Now;
                        schedule.CreatedBy = PMS.Common.Globals.User.Email;
                        schedule.LocationId = Schedule.LocationId;
                        schedule.DepartureTimeId = Id;
                        schedule.FromDate = i;
                        uow.GenericRepository<EF.Schedule>().Insert(schedule);
                    }
                }


                uow.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public bool RemoveSchedule(int ScheduleID)
        {
            try
            {
                var schedule = uow.GenericRepository<EF.Schedule>().GetAll(x => x.ScheduleID == ScheduleID).FirstOrDefault();
                if (schedule != null)
                {
                    schedule.IsEnable = false;
                    uow.GenericRepository<EF.Schedule>().Update(schedule);
                    uow.SaveChanges();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public bool Update(UpdateScheduleVM Schedule)
        {
            try
            {
                var schedule = uow.GenericRepository<EF.Schedule>().GetById(Schedule.Id);
                if (schedule == null)
                {
                    throw new Exception("Not Found");
                }
                schedule.ScheduleID = schedule.ScheduleID;
                schedule.UpdatedDate = DateTime.Now;
                schedule.UpdatedBy = Common.Globals.User.Email;
                schedule.VehicleId = Schedule.VehicleId;
                schedule.RouteId = Schedule.RouteId;
                schedule.IsActive = true;
                schedule.IsEnable = true;
                schedule.FromDate = Schedule.FromDate;
                schedule.DepartureTimeId = Schedule.DepartureTimeId;
                schedule.LocationId = Schedule.LocationId;

                uow.GenericRepository<EF.Schedule>().Update(schedule);
                uow.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }

        }
        public List<AddScheduleVM> GetAllSchedule()
        {
            var schedules = uow.GenericRepository<EF.Schedule>().GetAll(x => x.IsEnable == true).Select(x => new AddScheduleVM
            {
                Id = x.ScheduleID
            }).ToList();
            return schedules;
        }

        public List<TimeSlotLookUpVm> GetActiveTimes()
        {
            return uow.GenericRepository<EF.TimeSlotLookUp>().Table.Where(x => x.IsActive == true).Select(x => new TimeSlotLookUpVm
            {
                Name = x.Slot,
                Id = x.Id
            }).ToList();
        }
        //For Student Portal
        public List<AddScheduleVM> GetRoutes(DateTime? date)
        {
            var res = uow.GenericRepository<EF.Schedule>().Table.Where(x => x.FromDate == date && x.IsEnable == true).Select(x => new AddScheduleVM
            {
                Id = x.ScheduleID,
                ScheduleName = x.ScheduleName,
                FromDate = x.FromDate,
                RouteId = x.RouteId,
                VehicleId = x.VehicleId,
                VehicleName = x.Bus.BusName,
                RouteName = x.Route.RouteName,
                DepartureTypeName = x.TimeSlotLookUp.Slot,
                Time = x.TimeSlotLookUp.Time

            }).ToList();
            return res;
        }
        public bool BookSeats(int busid, int Scheduleid)
        {

            var seat = uow.GenericRepository<EF.VehicleSeat>().Table.Where(x => x.VechicleId == busid && x.IsActive == true && x.IsEnable == true).Select(x => x.Id).ToList();
            var check = uow.GenericRepository<EF.VehicleBooking>().Table.Where(x => seat.Contains(x.SeatId) && x.ScheduleID == Scheduleid && x.Status == "Booked").Select(x => x.SeatId).ToList();
            var seatid = uow.GenericRepository<EF.VehicleSeat>().Table.Where(x => x.VechicleId == busid && x.IsActive == true && x.IsEnable == true && !check.Contains(x.Id)).Select(x => x.Id).FirstOrDefault();
            if (seatid == 0)
            {
                throw new Exception("All Seats have already been booked");
            }
            var student = uow.GenericRepository<EF.VehicleBooking>().Table.Where(x => x.ScheduleID == Scheduleid && x.StudentId == PMS.Common.Globals.User.PersonId && x.Status == "Booked").FirstOrDefault();
            if (student != null)
            {
                throw new Exception("You have already booked a seat for today");
            }
            try
            {
                var book = new EF.VehicleBooking()
                {
                    ScheduleID = Scheduleid,
                    StudentId = PMS.Common.Globals.User.PersonId,
                    SeatId = seatid,
                    CreatedDate = DateTime.Now,
                    Status = "Booked"
                };
                uow.GenericRepository<EF.VehicleBooking>().Insert(book);
                uow.SaveChanges();

                //Seat Booking Email
                var reservationDetail = uow.GenericRepository<EF.VehicleBooking>().Table.Where(x => x.ScheduleID == Scheduleid && x.StudentId == book.StudentId && x.Status == "Booked").Select(x => new BookingTransportationVM
                {
                    BusName = x.VehicleSeat.Bus.BusName,
                    RegistrationNumber = x.VehicleSeat.Bus.RegistrationNumber,
                    SeatId = book.SeatId,
                    SeatNumber = x.VehicleSeat.SeatNumber,
                    RouteName = x.Schedule.Route.RouteName,
                    LocationId = x.VehicleSeat.Bus.LocationId,
                    DepartureTime = x.Schedule.TimeSlotLookUp.Slot,
                    DepartureDate = x.Schedule.FromDate.ToString()
                }).FirstOrDefault();
                var NotifyEmail = correspondenceService.GetEmailMessagesByActionId(((int)Enumeration.CorrespondenceAction.TransportBookingEmail), reservationDetail.LocationId??0);
                if (NotifyEmail != null)
                {
                    var body = NotifyEmail.EmailMessageBody;
                    body = body.Replace("[[Bus_Name]]", reservationDetail.BusName);
                    body = body.Replace("[[Registration_Number]]", reservationDetail.RegistrationNumber);
                    body = body.Replace("[[Seat_Number]]", reservationDetail.SeatNumber);
                    body = body.Replace("[[Route_Name]]", reservationDetail.RouteName);
                    body = body.Replace("[[Departure_Time]]", reservationDetail.DepartureTime);
                    body = body.Replace("[[Departure_Date]]", reservationDetail.DepartureDate);
                    emailService.SendEmailAsync(Convert.ToString(NotifyEmail.EmailMessageSubject), body, true, PMS.Common.Globals.User.Email, NotifyEmail.EmailMessageSenderID);
                }

                return true;
            }
            catch (Exception ex)
            {
                return false;

            }
        }
    }
}