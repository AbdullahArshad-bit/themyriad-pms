using PMS.Services.Services.Correspondence;
using PMS.Services.Services.Jobs;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.QuartzJob.Jobs
{
    public class EmailSchedulerJob : IJob
    {
        private readonly ICorrespondenceService _correspondenceService;

        public EmailSchedulerJob(ICorrespondenceService correspondenceService)
        {
            _correspondenceService = correspondenceService;
        }

        public Task Execute(IJobExecutionContext context)
        {
            
            return Task.CompletedTask;
        }
    }
}
