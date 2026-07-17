using PMS.DTO.ViewModels;
using PMS.DTO.ViewModels.CorrespondenceViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Services.Services.Jobs
{
    public interface IJobService
    {
        Task EscalationJobs();
        Task EscaltionTickets(List<SyncViewModel> Ticketsyncs);
    }
}
