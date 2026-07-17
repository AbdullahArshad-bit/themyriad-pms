using Microsoft.Extensions.Hosting;
using Ninject;
using PMS.QuartzJob.JobFactory;
using PMS.Services.QuatzJobs.Jobs;
using PMS.Services.QuatzJobs.Schedular;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.QuartzJob
{
    public class JobRegistration
    {
        public static IKernel GlobalKernel { get; private set; }
        public static void BindAll(IKernel kernel)
        {
            var scheduler = new JobsSchedular(kernel);
            scheduler.StartAsync();
            GlobalKernel = kernel;
        }
    }
}
