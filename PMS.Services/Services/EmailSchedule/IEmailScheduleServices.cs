using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Services.Services.EmailSchedule
{
    public interface IEmailScheduleServices
    {
        Task SendScheduledEmails();
    }
}
