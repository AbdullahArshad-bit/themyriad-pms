using Ninject;
using Quartz;
using Quartz.Spi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.QuartzJob.JobFactory
{
  public  class JobsFactory : IJobFactory
    {
        private readonly IKernel kernel;
        public JobsFactory(IKernel _kernel)
        {
            kernel = _kernel;
        }
        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            var jobDetail = bundle.JobDetail;
            return (IJob)kernel.Get(jobDetail.JobType);
        }

        public void ReturnJob(IJob job)
        {
            
        }
    }
}
