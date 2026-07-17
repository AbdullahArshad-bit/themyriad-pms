using PMS.DTO.ViewModels;
using PMS.DTO.ViewModels.TransportationViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Services.Services.VehicleSubscription
{
    public interface IVehicleSubscriptionService
    {
        List<VehicleSubscriptionVM> GetAll();
        VehicleSubscriptionVM GetById(int id);
        List<VehiclePriceVM> GetPriceNameByFrequency(int id);
        VehiclePriceVM GetPricesByPriceId(int id);
        bool Add(VehicleSubscriptionVM vehicleSubscriptionVM);
        bool Update(VehicleSubscriptionVM vehicleSubscriptionVM);
        bool Delete(int Id);
        OutputInvoicingVM GetStudentPackage(int studentId);
        bool Approve(int subscriptionid);
        bool Suspend(int subscriptionid);
        bool End(int subscriptionid);
        //for student portal
        List<VehicleSubscriptionVM> GetSubscription(int personid);

    }
}
