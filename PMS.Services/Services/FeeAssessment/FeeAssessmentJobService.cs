using PMS.DTO.ViewModels;
using PMS.EF;
using PMS.Repository.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Services.Services.FeeAssessment
{
    public class FeeAssessmentJobService : IFeeAssessmentJobService
    {
        private readonly UnitOfWork<PMSEntities> _uow;

        public FeeAssessmentJobService(UnitOfWork<PMSEntities> uow)
        {
            _uow = uow;
        }

        //public async Task<Guid> StartFeeAssessmentJob(int[] personIds, DateTime startDate, DateTime endDate, int userId)
        //{
        //    var jobId = Guid.NewGuid();

        //    // Save job to database
        //    var jobRecord = new FeeAssessmentJob_DB
        //    {
        //        JobId = jobId,
        //        PersonIds = string.Join(",", personIds),
        //        StartDate = startDate,
        //        EndDate = endDate,
        //        UserId = userId,
        //        Status = "Queued",
        //        Message = "Job queued for processing",
        //        CreatedDate = DateTime.Now
        //    };

        //    _uow.GenericRepository<FeeAssessmentJob_DB>().Insert(jobRecord);
        //    _uow.SaveChanges();

        //    // Schedule the job
        //    var job = JobBuilder.Create<FeeAssessmentJob>()
        //        .WithIdentity($"FeeAssessment_{jobId}", "FeeAssessment")
        //        .UsingJobData("JobId", jobId)
        //        .UsingJobData("PersonIds", string.Join(",", personIds))
        //        .UsingJobData("StartDate", startDate)
        //        .UsingJobData("EndDate", endDate)
        //        .UsingJobData("UserId", userId)
        //        .Build();

        //    var trigger = TriggerBuilder.Create()
        //        .WithIdentity($"FeeAssessmentTrigger_{jobId}", "FeeAssessment")
        //        .StartNow()
        //        .Build();

        //    await scheduler.ScheduleJob(job, trigger);

        //    return jobId;
        //}

        //public async Task<FeeAssessmentJobStatusVM> GetJobStatus(Guid jobId)
        //{
        //    var jobRecord = _uow.GenericRepository<FeeAssessmentJob_DB>().Table
        //        .FirstOrDefault(x => x.JobId == jobId);

        //    if (jobRecord == null)
        //        return null;

        //    return new FeeAssessmentJobStatusVM
        //    {
        //        JobId = jobRecord.JobId,
        //        Status = jobRecord.Status,
        //        Message = jobRecord.Message,
        //        SuccessMessage = jobRecord.SuccessMessage,
        //        ErrorMessage = jobRecord.ErrorMessage,
        //        CreatedDate = jobRecord.CreatedDate,
        //        CompletedDate = jobRecord.CompletedDate
        //    };
        //}

        //public async Task UpdateJobStatus(Guid jobId, string status, string message, string successMessage = null, string errorMessage = null)
        //{
        //    var jobRecord = _uow.GenericRepository<FeeAssessmentJob_DB>().Table
        //        .FirstOrDefault(x => x.JobId == jobId);

        //    if (jobRecord != null)
        //    {
        //        jobRecord.Status = status;
        //        jobRecord.Message = message;
        //        jobRecord.SuccessMessage = successMessage;
        //        jobRecord.ErrorMessage = errorMessage;

        //        if (status == "Completed" || status == "Failed")
        //        {
        //            jobRecord.CompletedDate = DateTime.Now;
        //        }

        //        _uow.GenericRepository<FeeAssessmentJob_DB>().Update(jobRecord);
        //        _uow.SaveChanges();
        //    }
        //}

        //public async Task<List<FeeAssessmentJobStatusVM>> GetJobHistory(int userId, int pageSize = 10)
        //{
        //    var jobs = _uow.GenericRepository<FeeAssessmentJob_DB>().Table
        //        .Where(x => x.UserId == userId)
        //        .OrderByDescending(x => x.CreatedDate)
        //        .Take(pageSize)
        //        .Select(x => new FeeAssessmentJobStatusVM
        //        {
        //            JobId = x.JobId,
        //            Status = x.Status,
        //            Message = x.Message,
        //            SuccessMessage = x.SuccessMessage,
        //            ErrorMessage = x.ErrorMessage,
        //            CreatedDate = x.CreatedDate,
        //            CompletedDate = x.CompletedDate
        //        })
        //        .ToList();

        //    return jobs;
        //}
    }
}
