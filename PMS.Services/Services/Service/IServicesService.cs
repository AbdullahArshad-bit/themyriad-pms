using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.DTO.ViewModels.ServiceViewModels;


namespace PMS.Services.Services.Service
{
     public interface IServicesService
    {
        //Service
        List<ServicesListVM> GetServices();

        AddServiceVM GetServicesById(int id);
        
        Dictionary<int, AddServiceVM> GetServicesByIds(IEnumerable<int> ids);

        bool AddService(AddServiceVM model);

        bool UpdateService(AddServiceVM model);

        bool DeleteService(int id);
        List<ServiceTypes> GetServiceType();
    }
}
