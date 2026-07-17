using PMS.DTO.ViewModels.TransportationViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Services.Services.Schedule
{
    public interface IScheduleService
    {
        AddScheduleVM GetScheduleByID(int scheduleID);
        UpdateScheduleVM GetById(int id);

        bool Add(AddScheduleVM Schedule);
        List<AddScheduleVM> GetAll();
        bool RemoveSchedule(int ScheduleID);
        bool Update(UpdateScheduleVM Schedule);
        List<TimeSlotLookUpVm> GetActiveTimes();
        List<AddScheduleVM> GetAllSchedule();
        //for student portal
        List<AddScheduleVM> GetRoutes(DateTime? date);
        bool BookSeats(int busid, int scheduleid);
    }
}
