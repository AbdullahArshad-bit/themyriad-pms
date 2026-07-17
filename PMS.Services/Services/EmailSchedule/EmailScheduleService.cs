using PMS.DTO.ViewModels.CorrespondenceViewModels;
using PMS.EF;
using PMS.Repository.UnitOfWork;
using PMS.Services.Services.Email;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Services.Services.EmailSchedule
{
    public class EmailScheduleService : IEmailScheduleServices
    {
        private readonly UnitOfWork<PMSEntities> _uow;
        private readonly IEmailService _emailService;

        public EmailScheduleService(UnitOfWork<PMSEntities> uow, IEmailService emailService)
        {
            _uow = uow;
            _emailService = emailService;
        }

        public async Task SendScheduledEmails()
        {
            // Fetch the list of emails to be sent
            var emails = await GetEmailsToSend();

            foreach (var email in emails)
            {
                var isSent = SendScheduleEmail(email);
                if (isSent)
                {
                    await MarkEmailAsSent(email.ID);
                }
            }
        }

        private async Task<List<AddEmailSchedulerVM>> GetEmailsToSend()
        {
            var currentTime = DateTime.Now;

            var emailSchedulers = await _uow.Context.EmailSchedulers
                .Where(e => e.ExecutionTime <= currentTime && !e.IsSent)
                .ToListAsync();

            var emailSchedulerVMs = emailSchedulers.Select(e => new AddEmailSchedulerVM
            {
                ID = e.ID,
                EmailSubject = e.EmailSubject,
                EmailMessageBody = e.EmailMessageBody,
                SendTo = e.SendTo.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList(),
                NextRun = e.NextRun,
                EmailSenderID = e.EmailSenderID,
                IsSent = e.IsSent
            }).ToList();

            return emailSchedulerVMs;
        }

        private  bool SendScheduleEmail(AddEmailSchedulerVM email)
        {
            var subject = email.EmailSubject;
            var body = email.EmailMessageBody;
            var recipientList = email.SendTo;

            bool result = true;

            foreach (var recipient in recipientList)
            {
                var isEmailSent = _emailService.SendEmail(subject, body, true, recipient.Trim(), email.EmailSenderID ?? 0);
                if (!isEmailSent)
                {
                    result = false;
                }
            }

            return result;
        }

        private async Task MarkEmailAsSent(int emailId)
        {
            var email = await _uow.Context.EmailSchedulers.FindAsync(emailId);
            if (email != null)
            {
                if (email.Recurrence.GetValueOrDefault())
                {
                    // For recurring emails, update fields without adding intervals
                    email.IsSent = false; // Keep IsSent as false for recurring emails
                    email.LastRun = email.ExecutionTime; // Update LastRun to the current ExecutionTime
                    email.ExecutionTime = email.NextRun; // Set NextRun to the current ExecutionTime
                    email.NextRun = null;
                    email.Recurrence=false;

                    // Mark the entity as modified
                    _uow.Context.Entry(email).State = EntityState.Modified;
                }
                else
                {
                    // For non-recurring emails, just mark as sent
                    email.IsSent = true;

                    // Mark the entity as modified
                    _uow.Context.Entry(email).State = EntityState.Modified;
                }

                // Save changes to the database
                await _uow.Context.SaveChangesAsync();
            }
        }

    }
}
