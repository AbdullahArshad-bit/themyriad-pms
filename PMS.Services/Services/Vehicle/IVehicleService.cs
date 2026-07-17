using PMS.DTO;
using PMS.DTO.ViewModels;
using PMS.DTO.ViewModels.VehicleViewModel;
using PMS.EF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace PMS.Services.Services.Vehicle
{
    public interface IVehicleService
    {
        VehicleViewModel GetById(int id);
        List<VehicleViewModel> GetVehicles();
        List<VehicleViewModel> GetList();
        List<VehicleSeatsViewModel> GetVehicleSeat();
        List<VehicleSeatsViewModel> GetVehicleSeatsById(int busId);
        bool AddVehicle(VehicleViewModel vehicleVM, HttpPostedFileBase file);
        VehicleViewModel UpdateVehicle(VehicleViewModel vehicleVM, HttpPostedFileBase file);
        bool DeleteVehicle(int vehicleId);
        bool AddSeat(VehicleSeatsViewModel model);
        bool UpdateSeat(VehicleSeatsViewModel model);
        ApiResponse<VehicleSeatsViewModel> GetSeatDetailById(int Id);
        bool DeleteSeat(int id);
    }
}
