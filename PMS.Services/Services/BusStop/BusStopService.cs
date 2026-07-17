using PMS.DTO.ViewModels;
using PMS.EF;
using PMS.Repository.UnitOfWork;
using PMS.Services.Services.LocationContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace PMS.Services.Services.BusStop
{
    public class BusStopService : IBusStopService
    {
        private readonly UnitOfWork<PMSEntities> uow;
        private readonly ILocationContextService locationContextService;
        public BusStopService(UnitOfWork<PMSEntities> _uow, ILocationContextService _locationContextService)
        {
            uow = _uow;
            locationContextService = _locationContextService;
        }
        public Stop AddVehicle(AddBusStopViewModel vehicleVM)
        {
            Stop vehicle = new Stop
            {
               IsActive = vehicleVM.IsActive,
            };

            uow.GenericRepository<Stop>().Insert(vehicle);
            uow.SaveChanges();

            return vehicle;
        }
        public List<AddBusStopViewModel> GetAllStops()
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();

            var stops = uow.GenericRepository<EF.Stop>().GetAll(x => x.IsActive == true && assignedLocationIds.Contains((int)x.LocationId)).Select(x => new AddBusStopViewModel
            {
                Name = x.StopName,
                Id = x.StopID,
                FullName=x.StopFullName,
                Latitude=x.Lat,
                Longitude=x.Long,
                LocationName = x.Location.LocationName
                
            }).ToList();
            return stops;
        }
        public bool Add(AddBusStopViewModel vm)
        {
            vm.IsActive = true;
            try
            {
                var stop = new EF.Stop();
                {
                    stop.StopName = vm.Name;
                    stop.Lat = vm.Latitude;
                    stop.Long = vm.Longitude;
                    stop.StopFullName = vm.FullName;
                    stop.IsActive = vm.IsActive;
                    stop.LocationId = vm.LocationId;
                };
                uow.GenericRepository<EF.Stop>().Insert(stop);
                uow.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public bool Delete(int StopID)
        {
            try
            {
                var stops = uow.GenericRepository<EF.Stop>().GetAll().Where(x=>x.StopID==StopID).FirstOrDefault();
                if(stops!=null)
                {
                    stops.IsActive = false;
                    uow.GenericRepository<EF.Stop>().Update(stops);
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
        public AddBusStopViewModel GetStopById(int StopID)
        {
            try
            {
                var res = uow.GenericRepository<EF.Stop>().GetAll().Where(x => x.StopID == StopID).Select(x => new AddBusStopViewModel
                {
                    Id = x.StopID,
                    Name = x.StopName,
                    Longitude = x.Long,
                    Latitude = x.Lat,
                    FullName = x.StopFullName,
                    LocationName = x.Location.LocationName,
                    LocationId = x.LocationId
                }).FirstOrDefault();
                return res;
            }
            catch(Exception ex)
            {
                return null;
            }
        }
        public bool Update(AddBusStopViewModel vm)
        {
            try
            {
                var res = uow.GenericRepository<EF.Stop>().GetAll().Where(x => x.StopID == vm.Id).FirstOrDefault();
                res.StopName = vm.Name;
                res.StopFullName = vm.FullName;
                res.Long = vm.Longitude;
                res.Lat = vm.Latitude;
                res.LocationId = vm.LocationId;
                uow.GenericRepository<EF.Stop>().Update(res);
                uow.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }

        }
    }
}
