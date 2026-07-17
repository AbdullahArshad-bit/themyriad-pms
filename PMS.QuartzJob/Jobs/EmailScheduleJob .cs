using PMS.Services.Services.Email;
using PMS.Services.Services.EmailSchedule;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.QuartzJob.Jobs
{
    public class EmailScheduleJob : IJob
    {
        private readonly IEmailScheduleServices emailScheduleServices;

        public EmailScheduleJob(IEmailScheduleServices _emailService)
        {
            emailScheduleServices = _emailService;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            await emailScheduleServices.SendScheduledEmails();
        }
    }

}
