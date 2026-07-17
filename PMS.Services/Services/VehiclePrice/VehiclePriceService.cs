using PMS.DTO.ViewModels.TransportationViewModels;
using PMS.EF;
using PMS.Repository.UnitOfWork;
using PMS.Services.Services.LocationContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace PMS.Services.Services.VehiclePrice
{
    public class VehiclePriceService : IVehiclePriceService
    {
        private readonly UnitOfWork<PMSEntities> uow;
        private readonly ILocationContextService locationContextService;
        public VehiclePriceService(UnitOfWork<PMSEntities> _uow, ILocationContextService _locationContextService)
        {
            uow = _uow;
            locationContextService = _locationContextService;
        }
        public List<VehiclePriceVM> GetAll()
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();

            var res = uow.GenericRepository<EF.VehiclePrice>().GetAll(x => x.IsEnable == true && assignedLocationIds.Contains((int)x.LocationId)).Select(x => new VehiclePriceVM
            {
                VehiclePriceId = x.VehiclePriceId,
                PriceName = x.PriceName,
                RouteId = x.RouteId,
                Price = x.Price,
                RouteName = x.Route.RouteName,
                FrequencyName = x.VehiclePriceLookUp.PriceRate,
                CretedDate = x.CreateDate,
                IsActive = x.IsActive,
                LocationName = x.Location.LocationName

            }).ToList();
            return res;
        }
        public VehiclePriceVM GetById(int id)
        {
            var vehiclePrice = uow.GenericRepository<EF.VehiclePrice>().Table.Where(x => x.VehiclePriceId == id).Select(x => new VehiclePriceVM
            {
                VehiclePriceId = x.VehiclePriceId,
                PriceName = x.PriceName,
                RouteId = x.RouteId,
                FrequencyId = x.FrequencyId,
                Price = x.Price,
                RouteName = x.Route.RouteName,
                FrequencyName = x.VehiclePriceLookUp.PriceRate,
                CretedDate = x.CreateDate,
                CreatedBy = x.CreatedBy,
                IsActive = x.IsActive,
                LocationId = x.LocationId
            }).FirstOrDefault();
            return vehiclePrice;
        }
        public List<VehiclePriceLookUpVm> GetActivePrices()
        {
            return uow.GenericRepository < EF.VehiclePriceLookUp>().Table.Where(x => x.Status == true).Select(x => new VehiclePriceLookUpVm
            {
                Id = x.Id,
                Name = x.PriceRate,
            }).ToList();
        }
        public bool AddVehiclePrice(VehiclePriceVM vehiclePriceVM)
        {
            try
            {
                var Price = new EF.VehiclePrice
                {
                    PriceName = vehiclePriceVM.PriceName,
                    RouteId = vehiclePriceVM.RouteId,
                    FrequencyId = vehiclePriceVM.FrequencyId,
                    Price = vehiclePriceVM.Price,
                    CreateDate = DateTime.Now,
                    CreatedBy = Common.Globals.User.Email,
                    IsActive = vehiclePriceVM.IsActive,
                    IsEnable = true,
                    LocationId = vehiclePriceVM.LocationId
                };
                uow.GenericRepository<EF.VehiclePrice>().Insert(Price);
                uow.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public VehiclePriceVM UpdateVehiclePrice(VehiclePriceVM vehiclePriceVM)
        {
            var vehiclePrice = uow.GenericRepository<EF.VehiclePrice>().GetById(vehiclePriceVM.VehiclePriceId);
           
            if (vehiclePrice != null)
            {
                vehiclePrice.PriceName = vehiclePriceVM.PriceName;
                vehiclePrice.RouteId = vehiclePriceVM.RouteId;
                vehiclePrice.Price = vehiclePriceVM.Price;
                vehiclePrice.FrequencyId = vehiclePriceVM.FrequencyId;
                vehiclePrice.UpdatedDate = DateTime.Now;
                vehiclePrice.UpdatedBy = Common.Globals.User.Email;
                vehiclePrice.IsActive = vehiclePriceVM.IsActive;
                vehiclePrice.LocationId = vehiclePriceVM.LocationId;
                uow.GenericRepository<EF.VehiclePrice>().Update(vehiclePrice);
                uow.SaveChanges();

                return vehiclePriceVM;
            }
            else
                throw new Exception("Vehicle Price not found to update.");
        }

        public bool DeleteVehiclePrice(int vehiclePriceId)
        {
            var Oldvehicle = uow.GenericRepository<EF.VehiclePrice>().GetByIdAsNoTracking(x => x.VehiclePriceId == vehiclePriceId);
            var vehiclePrice = uow.GenericRepository<EF.VehiclePrice>().GetById(vehiclePriceId);

            if (vehiclePrice != null)
            {
                vehiclePrice.IsEnable = false;

                uow.GenericRepository<EF.VehiclePrice>().Update(vehiclePrice);
                uow.SaveChanges();
                return true;
            }
            else
                throw new Exception("Vehicle Price not found to delete.");
        }
        public List<VehiclePriceVM> GetPrice(int frequencyid)
        {
            var price = uow.GenericRepository<EF.VehiclePrice>().Table.Where(x => x.IsEnable == true && x.FrequencyId==frequencyid).Select(x => new VehiclePriceVM
            {
                VehiclePriceId = x.VehiclePriceId,
                PriceName=x.PriceName
            }).ToList();
            return price;
        }
    }
}
