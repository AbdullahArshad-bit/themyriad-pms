using PMS.DTO.ViewModels;
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

namespace PMS.Services.Services.VehicleRoutes
{
    public class VehicleRoutesService : IVehicleRoutesService
    {
        private readonly UnitOfWork<PMSEntities> uow;
        private readonly ILocationContextService locationContextService;
        public VehicleRoutesService(UnitOfWork<PMSEntities> _uow, ILocationContextService _locationContextService)
        {
            uow = _uow;
            locationContextService = _locationContextService;
        }

        Func<EF.Route, List<RouteStopVm>> GetStopList = delegate (EF.Route route)
        {
            var list = new List<RouteStopVm>();
            foreach(var item in route.RouteStopes)
            {
                var stop = new RouteStopVm
                {
                    OrderNumber=item.StopNumber,
                    DepartureStopId=item.StopID,
                    StopTravelTime=item.TotalStopTime,
                    IsActive=item.IsActive
                    //IsDepartureStop=item.IsDeparturStop
                };
                list.Add(stop);
            }
            return list;

        };
        public bool Add(AddRouteVM addRouteVM)
        {
            addRouteVM.IsEnable = true;
            addRouteVM.CreatedDate = DateTime.Now;
            try
            {
                var routes = new EF.Route()
                {
                    RouteName = addRouteVM.Routename,
                    DateCreated = addRouteVM.CreatedDate,
                    TotalTravelTime=addRouteVM.TravelTime,
                    //ScheduleID = addRouteVM.ScheduleID,
                    IsEnable = addRouteVM.IsEnable,
                    LocationId = addRouteVM.LocationId

                };
                foreach(var item in addRouteVM.RouteStop)
                {
                    

                   
                    var stop = new EF.RouteStope()
                    {
                        RouteID=addRouteVM.RouteID,
                        StopID=item.DepartureStopId,
                        StopNumber=item.OrderNumber,
                        TotalStopTime=item.StopTravelTime,
                        IsActive=item.IsActive,
                        //IsDeparturStop=item.IsDepartureStop

                
                   };
                    uow.GenericRepository<EF.RouteStope>().Insert(stop);
                }
                
                uow.GenericRepository<EF.Route>().Insert(routes);
                uow.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public List<AddRouteVM> GetRouteList()
        {
            var res = uow.GenericRepository<EF.Route>().GetAll(x=>x.IsEnable == true).Select(x => new AddRouteVM
            {
                RouteID = x.RouteID,
                Routename = x.RouteName,
                LocationId = x.LocationId
            }).ToList();
            return res;
        }
        public List<AddRouteVM> GetAll()
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();

            var routes = uow.GenericRepository<EF.Route>().GetAll(x => x.IsEnable == true && assignedLocationIds.Contains((int)x.LocationId)).Select(x => new AddRouteVM
            {       
                Routename = x.RouteName,
                LocationName = x.Location.LocationName,
                RouteID = x.RouteID,
                StopCount =x.RouteStopes.Where(y=>y.IsActive==true).Count(),
                stopName = x.RouteStopes.OrderBy(y=>y.StopNumber).Select(y=>y.Stop.StopName).FirstOrDefault(),
                ArrivalStop=x.RouteStopes.OrderByDescending(y=>y.StopNumber).Select(y=>y.Stop.StopName).FirstOrDefault()

                //stopName=x.Stop.StopName
            }).ToList();
            return routes;
        }
        public bool Delete(int RouteID)
        {
            try
            {
                var routes = uow.GenericRepository<EF.Route>().GetAll(x => x.RouteID == RouteID).FirstOrDefault();
                if (routes != null)
                {
                    routes.IsEnable = false;
                    uow.GenericRepository<EF.Route>().Update(routes);
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
        public AddRouteVM GetRouteById(int RouteID)
        {
            try
            {
              var res=  uow.GenericRepository<EF.Route>().GetAll().Where(x => x.RouteID == RouteID).Select(x=> new AddRouteVM
                {
                    Routename=x.RouteName,
                    RouteID=x.RouteID,
                    TravelTime=x.TotalTravelTime,
                    LocationId = x.LocationId,
                    LocationName = x.Location.LocationName,
                    RouteStop = GetStopList(x)
              }).FirstOrDefault();
                    
                return res;
                
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        public bool Update(AddRouteVM addRouteVM)
        {
            try
            {


                var res = uow.GenericRepository<EF.Route>().GetAll().Where(x => x.RouteID == addRouteVM.RouteID).FirstOrDefault();
                
                res.RouteName = addRouteVM.Routename;
                res.DateUpdated=DateTime.Now;
                res.TotalTravelTime = addRouteVM.TravelTime;
                res.LocationId = addRouteVM.LocationId;
                var previtemList = res.RouteStopes.ToList();
                foreach (var item in previtemList)
                {
                    uow.GenericRepository<EF.RouteStope>().Delete(item);
                }
                foreach (var item in addRouteVM.RouteStop)
                {



                    var stop = new EF.RouteStope()
                    {
                        RouteID = addRouteVM.RouteID,
                        StopID = item.DepartureStopId,
                        StopNumber = item.OrderNumber,
                        TotalStopTime = item.StopTravelTime,
                        IsActive = item.IsActive,
                        //IsDeparturStop=item.IsDepartureStop


                    };
                    uow.GenericRepository<EF.RouteStope>().Insert(stop);
                }

                uow.GenericRepository<EF.Route>().Update(res);
                uow.SaveChanges();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public List<AddBusStopViewModel> Getstops()
        {
            var stops = uow.GenericRepository<EF.Stop>().GetAll(x => x.IsActive == true).Select(x => new AddBusStopViewModel
            {
                Id=x.StopID,
                Name=x.StopName
            }).ToList();
            return stops;
        }
        //For Student Portal
        public List<RouteStopVm> Stops(int RouteId)
        {
            var res = uow.GenericRepository<EF.RouteStope>().Table.Where(x => x.RouteID == RouteId).Select(x => new RouteStopVm
            {
                StopName=x.Stop.StopName
                
            }).ToList();
            return res;
        }
    }
}
