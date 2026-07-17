using PMS.DTO.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Services.Services.JobConfuguration
{
    public interface IJobConfigurationService
    {
         Task<List<JobConfigurationVM>> GetAll(int CategoryId);
        List<DropDownViewModel> GetActiveJobAction();
        bool Add(List<JobConfigurationVM> model);
        List<JobConfigurationVM> GetById(int Id);
    }
}
