using Microsoft.Extensions.Hosting;
using Ninject;
using PMS.DTO.ViewModels;
using PMS.QuartzJob.JobFactory;
using PMS.QuartzJob.Jobs;
using PMS.Services.QuatzJobs.Jobs;
using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PMS.Services.QuatzJobs.Schedular
{
    public class JobsSchedular
    {
        private readonly IKernel kernel;
        public JobsSchedular(IKernel _kernel)
        {
            kernel = _kernel;
        }
        public async Task StartAsync()
        {
            var cornExpression = ConfigurationManager.AppSettings["CornExpression"];
            var cronExpressionForEmails = ConfigurationManager.AppSettings["CronExpressionForEmails"];

            // setup Quartz scheduler that uses our NinjectJobFactory
            kernel.Bind<IScheduler>().ToMethod(x =>
            {
                var sched = new StdSchedulerFactory().GetScheduler().Result;
                sched.JobFactory = new JobsFactory(kernel);
                return sched;
            }).InSingletonScope();

            var scheduler = kernel.Get<IScheduler>();

            //create TicketingSLAJob
            var metadata = new JobMetadata(Guid.NewGuid(), typeof(TicketingSLAJob), "TicketingEscalation Job", cornExpression);
            IJobDetail job = CreateJob(metadata);
            ITrigger trigger = CreateTrigger(metadata);
            await scheduler.ScheduleJob(job, trigger);

            //create EmailScheduleJob
            var emailJobMetadata = new JobMetadata(Guid.NewGuid(), typeof(EmailScheduleJob), "EmailJob", cronExpressionForEmails);
            IJobDetail emailJob = CreateJob(emailJobMetadata);
            ITrigger emailTrigger = CreateTrigger(emailJobMetadata);
            await scheduler.ScheduleJob(emailJob, emailTrigger);

            //start scheduler
            await scheduler.Start();
        }

        private IJobDetail CreateJob(JobMetadata metadata)
        {
            return JobBuilder.Create(metadata.JobType)
                .WithIdentity(metadata.JobId.ToString())
                .WithDescription(metadata.JobName)
                .Build();
        }

        private ITrigger CreateTrigger(JobMetadata metadata)
        {
            return TriggerBuilder.Create()
                .WithIdentity($"{metadata.JobType.FullName}.trigger")
                .WithDescription(metadata.JobName)
                .WithCronSchedule(metadata.CornExpression)
                .Build();
        }
    }
}
