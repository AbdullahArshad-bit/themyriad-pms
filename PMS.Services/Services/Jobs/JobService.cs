using PMS.Common;
using PMS.Common.Classes;
using PMS.DTO.ViewModels;
using PMS.DTO.ViewModels.CorrespondenceViewModels;
using PMS.EF;
using PMS.Repository.UnitOfWork;
using PMS.Services.Services.Correspondence;
using PMS.Services.Services.Email;
using PMS.Services.Services.JobConfuguration;
using PMS.Services.Services.Notifications;
using PMS.Services.Services.Sync;
using PMS.Services.Services.Ticket;
using PMS.Services.Services.TicketGroup;
using PMS.Services.Services.UserManage;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using static PMS.Common.Classes.Enumeration;

namespace PMS.Services.Services.Jobs
{
   public class JobService :IJobService
    {
       
        private readonly INotificationService notificationService;
        private ICorrespondenceService correspondenceService;
        private readonly IEmailService emailService;
        private readonly ISyncService syncService;
        private readonly IJobConfigurationService jobConfigService;
        private readonly UnitOfWork<PMSEntities> uow;
        private readonly ITicketGroupService groupservice;
        private readonly ITicketService ticketService;
        private readonly IUserManageService userService;
        public JobService(INotificationService _notificationService, ICorrespondenceService _correspondenceService, IEmailService _emailService,
            ISyncService _syncService, IJobConfigurationService _jobConfigService,
            UnitOfWork<PMSEntities> _uow, ITicketGroupService _groupservice, ITicketService _ticketService,
            IUserManageService _userService)
        {
            notificationService = _notificationService;
            correspondenceService = _correspondenceService;
            emailService = _emailService;
            syncService = _syncService;
            jobConfigService = _jobConfigService;
            uow = _uow;
            groupservice = _groupservice;
            ticketService = _ticketService;
            userService = _userService;
        }

      
        public async Task EscalationJobs()
        {
            var syncservices = await syncService.GetAllSync();
            if (syncservices.Count > 0)
            {
               await EscaltionTickets(syncservices.Where(x=>x.SyncCategoryId==(int)SyncCategory.Ticket).ToList());
            }
        }

        
        public async Task EscaltionTickets(List<SyncViewModel> Ticketsyncs)
        {
            foreach (var item in Ticketsyncs)
            {
                var jobsetting = await jobConfigService.GetAll(item.SyncCategoryId);
                if (item.SyncTypeId == (int)SyncType.Status)
                {
                    var itemsetting= jobsetting.Where(x=>x.CategoryId==item.SyncCategoryId && x.PropertyValue==item.PropertyValue).FirstOrDefault();
                    if (itemsetting!=null)
                    {
                        
                        var ticket = ticketService.GetById(item.EnitityId);
                        var ticketData = ticket.Data;
                        string AssignName = "";
                        if (item.LastUsedOn != null)
                            {


                                //validation For TimeDate To Excecute Today
                                bool Valid = CheckValidForJob(item.NextExcecutionOn);

                                if (Valid)
                                {
                                var users = groupservice.GetGroupUserByRoleId((int)item.NextExecutionActionId, ticketData.GroupId);

                                if (ticketData.AssignTo != 0)
                                {
                                   var Assignuser= userService.GetById(ticketData.AssignTo);
                                    var data = Assignuser.Data;
                                    AssignName = data.FullName;
                                    users.Add
                                        (new TicketGroupVm{
                                            UserId=data.Id,
                                            UserEmail=data.Email,
                                         });
                                }
                                    foreach (var user in users)
                                    {
                                         SendTicketMessage(itemsetting.IsEmail, itemsetting.IsNotify, user.UserId, ticketData.CreatedDate.ToString(), ticketData.Code, user.UserEmail, ticketData.StatusId, ticketData.FullName, AssignName);
                                    };
                                    await syncService.UpdateSyncByEntity(item.SyncId);
                                }

                            }
                            else
                            {
                                bool Valid = CheckValidForJob(item.FirstExecution);
                               

                                if (Valid)
                                {
                                    var users = groupservice.GetGroupUserByRoleId((int)item.FirstExecutionActionId,ticketData.GroupId);
                                if (ticketData.AssignTo != 0)
                                {
                                    var Assignuser = userService.GetById(ticketData.AssignTo);
                                    var data = Assignuser.Data;
                                    AssignName = data.FullName;
                                    users.Add
                                        (new TicketGroupVm
                                        {
                                            UserId = data.Id,
                                            UserEmail = data.Email,
                                        });
                                }
                                foreach (var user in users)
                                    {
                                         SendTicketMessage(itemsetting.IsEmail, itemsetting.IsNotify, user.UserId, ticketData.CreatedDate.ToString(), ticketData.Code, user.UserEmail,ticketData.StatusId,ticketData.FullName,AssignName);
                                    };
                                    await syncService.UpdateSyncByEntity(item.SyncId);
                                }

                            }
                    } 
                }
            }
           
        }
        public async Task SendTicketMessage(bool IsEmail,bool IsNotify,int SenderId,string Date,string EntityCode,string SenderMail,int StatusId=1,string FullName="",string AssignName="")
        {
            string description = "";
            string AssingText = "";
            if (!string.IsNullOrEmpty(AssignName))
                AssingText = " and assign to " + AssignName;

            if (StatusId == (int)TicketStatus.Open)
            {
                description = "A complaint of code " + EntityCode + " is  open since " + Date +". issue by "+FullName+ AssingText+". Please check & verfiy!";
            }
            else if (StatusId == (int)TicketStatus.Pending)
            {
                description = "A complaint of code " + EntityCode + " is  pending since " + Date + ". issue by " + FullName + AssingText + ". Please check & verfiy!";
            }

            if (IsNotify)
            {
              
               
                var Notification = new NotificationViewModel
                {
                    Subject = "Complaint Reminder",
                    Description = description,
                    UserId = SenderId,
                    TypeId = (int)NotifiactionType.Group,
                    CreatedBy = "admin@gmail.com",
                    RedirectURL = "/Ticket/Index/",

                };
                notificationService.SendNotificationasync(Notification);
            }
            if (IsEmail)
            {
                var mail = correspondenceService.GetEmailMessagesByActionId(Convert.ToInt32(((int)Common.Classes.Enumeration.CorrespondenceAction.Ticket)),16);
                var body = mail.EmailMessageBody;
                body =body.Replace("[[Description]]", description);
                body = body.Replace("[[Subject]]", "Complaint Reminder");
                body = body.Replace("{{ConfirmationLink}}", ConfigurationManager.AppSettings["BaseUrl"]);
                emailService.SendEmailTaskAsync(mail.EmailMessageSubject, body, true, SenderMail, mail.EmailMessageSenderID);
            }
        }
        public bool CheckValidForJob(DateTime? ExecutionTime)
        {
            var TodayDatetTime = DateTime.Now;
            if(ExecutionTime <= TodayDatetTime)
            {
                return true;
            }
            return false;
        }
       
       

    }
}
