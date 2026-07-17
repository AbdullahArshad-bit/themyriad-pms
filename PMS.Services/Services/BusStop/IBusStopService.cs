using PMS.DTO.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Services.Services.BusStop
{
    public interface IBusStopService
    {
        //BusStop AddBusStop(AddBusStopViewModel busStopVM);
        List<AddBusStopViewModel> GetAllStops();
        bool Add(AddBusStopViewModel vm);
        AddBusStopViewModel GetStopById(int StopID);
        bool Delete(int StopID);
        bool Update(AddBusStopViewModel vm);

    }
}
