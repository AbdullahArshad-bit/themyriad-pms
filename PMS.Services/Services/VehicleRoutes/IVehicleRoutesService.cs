using PMS.DTO.ViewModels;
using PMS.DTO.ViewModels.TransportationViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Services.Services.VehicleRoutes
{
    public interface IVehicleRoutesService
    {
        List<AddRouteVM> GetRouteList();
        bool Add(AddRouteVM addRouteVM);
        List<AddRouteVM> GetAll();
        bool Delete(int RouteID);
        AddRouteVM GetRouteById(int RouteID);
        bool Update(AddRouteVM addRouteVM);
        List<AddBusStopViewModel> Getstops();
        //for student portal
        List<RouteStopVm> Stops(int RouteId);
    }
}
