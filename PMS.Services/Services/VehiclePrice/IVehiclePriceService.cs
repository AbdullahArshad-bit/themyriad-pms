using PMS.DTO.ViewModels.TransportationViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Services.Services.VehiclePrice
{
    public interface IVehiclePriceService
    {
        List<VehiclePriceVM> GetAll();
        VehiclePriceVM GetById(int id);

        List<VehiclePriceLookUpVm> GetActivePrices();
        bool AddVehiclePrice(VehiclePriceVM vehiclePriceVM);
        VehiclePriceVM UpdateVehiclePrice(VehiclePriceVM vehiclePriceVM);
        bool DeleteVehiclePrice(int vehiclePriceId);
        List<VehiclePriceVM> GetPrice(int frequencyid);




    }
}
