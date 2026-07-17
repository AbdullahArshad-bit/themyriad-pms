using PMS.Services.Services.Jobs;
using PMS.Services.Services.Ticket;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Services.QuatzJobs.Jobs
{
   public  class TicketingSLAJob : IJob
    {
        private readonly IJobService jobService;
        public TicketingSLAJob(IJobService _jobService)
        {
            jobService = _jobService;
        }
        public Task Execute(IJobExecutionContext context)
        {
            jobService.EscalationJobs();
            return Task.CompletedTask;
        }
    }
}
