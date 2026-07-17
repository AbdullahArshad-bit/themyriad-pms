using PMS.Common.Classes;
using PMS.DTO;
using PMS.DTO.ViewModels;
using PMS.DTO.ViewModels.VehicleViewModel;
using PMS.EF;
using PMS.Repository.UnitOfWork;
using PMS.Services.Services.LocationContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI.WebControls;

namespace PMS.Services.Services.Vehicle
{
    public class VehicleService : IVehicleService
    {
        private readonly UnitOfWork<PMSEntities> uow;
        private readonly ILocationContextService locationContextService;
        public VehicleService(UnitOfWork<PMSEntities> _uow, ILocationContextService _locationContextService)
        {
            uow = _uow;
            locationContextService = _locationContextService;
        }
        public VehicleViewModel GetById(int id)
        {
            var vehicle = uow.GenericRepository<Bus>().Table.Where(x => x.BusID == id).Select(x => new VehicleViewModel
            {
                BusId = x.BusID,
                LocationId = x.LocationId,
                BusName = x.BusName,
                RegistrationNumber = x.RegistrationNumber,
                Type = x.Type,
                ImageUrl = x.ImageUrl,
                Prefix = x.Prefix,
                TotalSeats = x.VehicleSeats.Where(y => y.IsEnable == true).Count(),
                IsActive = x.IsActive
            }).FirstOrDefault();
            return vehicle;
        }
        public List<VehicleViewModel> GetVehicles()
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();

            var res = uow.GenericRepository<EF.Bus>().GetAll(x => x.IsEnable == true && assignedLocationIds.Contains((int)x.LocationId)).Select(x => new VehicleViewModel
            {
                BusId = x.BusID,
                BusName = x.BusName,
                IsActive = x.IsActive,
                Type = x.Type,
                RegistrationNumber = x.RegistrationNumber,
                Prefix = x.Prefix,
                TotalSeats = x.VehicleSeats.Where(y => y.IsEnable == true).Count(),
                ImageUrl = x.ImageUrl,
                LocationName = x.Location.LocationName
            }).ToList();
            return res;

        }
        public List<VehicleViewModel> GetList()
        {
            var res = uow.GenericRepository<EF.Bus>().GetAll(x => x.IsActive == true && x.IsEnable == true).Select(x => new VehicleViewModel
            {
                BusId = x.BusID,
                BusName = x.BusName,
                LocationId = x.LocationId
            }).ToList();
            return res;
        }
        
        public List<VehicleSeatsViewModel> GetVehicleSeat()
        {
            var res = uow.GenericRepository<EF.VehicleSeat>().GetAll().Select(x => new VehicleSeatsViewModel
            {
                Id = x.Id,
                SeatNumber = x.SeatNumber,
                VehicleId = x.VechicleId,
                Status = x.Status,
                IsActive = x.IsActive,
                CreatedDate = x.CreatedDate,
                CreatedBy = x.CreatedBy,
                UpdatedDate = (DateTime)x.UpdateDate,
                UpdatedBy = x.UpdateBy,
                VechicleName = x.Bus.BusName,
                RegistrationNumber = x.Bus.RegistrationNumber,
                TotalSeats = x.Bus.TotalSeats ?? 0
            }).ToList();
            return res;

        }
        public List<VehicleSeatsViewModel> GetVehicleSeatsById(int busId)
        {
            var res = uow.GenericRepository<EF.VehicleSeat>().Table.Where(x => x.VechicleId == busId && x.IsEnable == true).OrderBy(x => x.CreatedDate).Select(x => new VehicleSeatsViewModel
            {
                Id = x.Id,
                SeatNumber = x.SeatNumber,
                VehicleId = x.VechicleId,
                Status = x.Status,
                IsActive = x.IsActive,
                CreatedDate = x.CreatedDate,
                CreatedBy = x.CreatedBy,
                UpdatedDate = (DateTime)x.UpdateDate,
                UpdatedBy = x.UpdateBy,
                VechicleName = x.Bus.BusName,
                RegistrationNumber = x.Bus.RegistrationNumber,
                TotalSeats = x.Bus.TotalSeats ?? 0

            }).ToList();
            return res;
        }
        public bool AddVehicle(VehicleViewModel vehicleVM, HttpPostedFileBase file)
        {
            try
            {
                if (file != null)
                {
                    var result = Common.ImageUpload.SaveFile(file, "Vehicle");
                    vehicleVM.ImageUrl = "/Upload/Files/Vehicle/" + result;
                }
                var vehicle = new Bus
                {
                    BusName = vehicleVM.BusName,
                    RegistrationNumber = vehicleVM.RegistrationNumber,
                    Type = 1,
                    Prefix = vehicleVM.Prefix,
                    TotalSeats = vehicleVM.TotalSeats,
                    IsActive = vehicleVM.IsActive,
                    IsEnable = true,
                    ImageUrl = vehicleVM.ImageUrl,
                    LocationId = vehicleVM.LocationId
                };
                for (var i = 1; i <= vehicleVM.TotalSeats; i++)
                {
                    var seats = new VehicleSeat();
                    string value = String.Format("{0:D2}", i);
                    var Code = vehicleVM.Prefix + "-" + value;
                    seats.SeatNumber = Code;
                    seats.Status = (int)SeatStatus.Open;
                    seats.IsActive = true;
                    seats.IsEnable = true;
                    seats.CreatedDate = DateTime.Now;
                    seats.CreatedBy = Common.Globals.User.Email;
                    seats.UpdateDate = DateTime.Now;
                    seats.UpdateBy = Common.Globals.User.Email;


                    vehicle.VehicleSeats.Add(seats);
                }
                uow.GenericRepository<Bus>().Insert(vehicle);
                uow.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public VehicleViewModel UpdateVehicle(VehicleViewModel vehicleVM, HttpPostedFileBase file)
        {
            var vehicle = uow.GenericRepository<Bus>().GetById(vehicleVM.BusId);

            if (file != null)
            {
                Common.ImageUpload upload = new Common.ImageUpload();
                var result = Common.ImageUpload.SaveFile(file, "Vehicle");
                vehicleVM.ImageUrl = "/Upload/Files/Vehicle/" + result;
            }
            if (vehicle != null)
            {
                vehicle.BusName = vehicleVM.BusName;
                vehicle.RegistrationNumber = vehicleVM.RegistrationNumber;
                vehicle.Type = 1;
                vehicle.IsActive = vehicleVM.IsActive;
                vehicle.ImageUrl = vehicleVM.ImageUrl;
                vehicle.LocationId = vehicleVM.LocationId;
                uow.GenericRepository<Bus>().Update(vehicle);
                uow.SaveChanges();

                return vehicleVM;
            }
            else
                throw new Exception("Vehicle not found to update.");
        }

        public bool DeleteVehicle(int vehicleId)
        {
            var Oldvehicle = uow.GenericRepository<Bus>().GetByIdAsNoTracking(x => x.BusID == vehicleId);
            var vehicle = uow.GenericRepository<Bus>().GetById(vehicleId);

            if (vehicle != null)
            {
                vehicle.IsEnable = false;

                uow.GenericRepository<Bus>().Update(vehicle);
                uow.SaveChanges();
                return true;
            }
            else
                throw new Exception("Vehicle not found to delete.");
        }
        public bool AddSeat(VehicleSeatsViewModel model)
        {
            var vehicle = uow.GenericRepository<EF.Bus>().Table.Where(x => x.BusID == model.VehicleId).FirstOrDefault();
            if (vehicle == null)
            {
                throw new Exception("Not found");
            }

            var seats = vehicle.VehicleSeats.Where(x => x.SeatNumber.ToLower() == model.SeatNumber.ToLower() && x.IsEnable == true && x.VechicleId == model.VehicleId).FirstOrDefault();
            if (seats != null)
            {
                throw new Exception("Seat Number " + seats.SeatNumber + "  Already Exist With Same Name.");
            }
            //vehicle.TotalSeats += 1;
            var seat = new VehicleSeat
            {
                SeatNumber = model.SeatNumber,
                IsActive = model.IsActive,
                IsEnable = true,
                VechicleId = model.VehicleId,
                Status = (int)SeatStatus.Open,
                CreatedDate = DateTime.Now,
                CreatedBy = Common.Globals.User.Email,
                UpdateDate = DateTime.Now,
                UpdateBy = Common.Globals.User.Email
            };
            vehicle.VehicleSeats.Add(seat);
            uow.GenericRepository<Bus>().Update(vehicle);
            uow.SaveChanges();
            return true;
        }
        public bool UpdateSeat(VehicleSeatsViewModel model)
        {
            var seat = uow.GenericRepository<VehicleSeat>().GetById(model.Id);
            if (seat == null)
            {
                throw new Exception("Not found");
            }

            var IsAlreadyExist = uow.GenericRepository<VehicleSeat>().Table.Where(x =>
            x.SeatNumber.ToLower() == model.SeatNumber.ToLower() && x.Id != model.Id && x.IsEnable == true && x.VechicleId == model.VehicleId).Any();
            if (IsAlreadyExist)
            {
                throw new Exception("Seat Number " + seat.SeatNumber + " Already Exist With Same Name.");
            }
            //vehicle.TotalSeats += 1;

            seat.SeatNumber = model.SeatNumber;
            seat.IsActive = model.IsActive;
            seat.UpdateDate = DateTime.Now;
            seat.UpdateBy = Common.Globals.User.Email;
            uow.GenericRepository<VehicleSeat>().Update(seat);
            uow.SaveChanges();
            return true;

        }
        public ApiResponse<VehicleSeatsViewModel> GetSeatDetailById(int Id)
        {
            var response = new ApiResponse<VehicleSeatsViewModel>();
            try
            {
                var seat = uow.GenericRepository<EF.VehicleSeat>().Table.Where(x => x.Id == Id).FirstOrDefault();
                if (seat == null)
                {
                    response.Success = false;
                    response.Message = "Not Found!";
                    response.Code = (int)HttpStatusCode.OK;
                    response.Data = null;
                    return response;
                }
                var model = new VehicleSeatsViewModel
                {
                    Id = seat.Id,
                    IsActive = seat.IsActive,
                    SeatNumber = seat.SeatNumber,

                };

                response.Code = (int)HttpStatusCode.OK;
                response.Message = "Success";
                response.Data = model;
                response.Success = true;
                return response;

            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.GetBaseException().ToString();
                response.Code = (int)HttpStatusCode.BadRequest;
                response.Data = null;
                return response;
            }
        }
        public bool DeleteSeat(int id)
        {
            var Oldseat = uow.GenericRepository<VehicleSeat>().GetByIdAsNoTracking(x => x.Id == id);
            var seat = uow.GenericRepository<VehicleSeat>().GetById(id);

            if (seat != null)
            {
                seat.IsEnable = false;

                uow.GenericRepository<VehicleSeat>().Update(seat);
                uow.SaveChanges();
                return true;
            }
            else
                throw new Exception("Seat not found to delete.");
        }

    }
}
