using PMS.DTO.ViewModels.TransportationViewModels;
using PMS.EF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Services.Services.StudentPortal.Transportation
{
    public interface ITransportationService
    {
        List<Route> GetDepartureTimeDetail();
        List<Route> GetAllScheduleDetail(int ScheduleID);
        List<EF.Schedule> GetAllScheduleDetail();
        List<Route> GetAllRouteDetail(int ScheduleID);
        List<RouteStope> GetSingleRouteDetail(int RouteID);
        bool RemoveRoute(int RouteID);
        bool RemoveSchedule(int ScheduleID);
        bool AddRoute(AddRouteVM addRouteVM);

        EF.Schedule GetScheduleByName(string scheduleName);

        bool AddScheduleByName(AddScheduleVM Schedule);

        EF.Schedule GetScheduleByID(int scheduleID);

        bool UpdateSchedule(int ScheduleID, string ScheduleName);

        bool UpdateStopByID(UpdateRouteTimeVM updateRouteTimeVM);

        RouteStope GetStopByID(int RouteStopID);
    }
}
