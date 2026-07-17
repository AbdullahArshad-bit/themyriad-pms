using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Security;
using Ninject.Activation;
using PMS.Common.Classes;
using PMS.DTO.ViewModels;
using PMS.DTO.ViewModels.CorrespondenceViewModels;
using PMS.EF;
using PMS.Repository.UnitOfWork;
using PMS.Services.Services.AuditLogs;
using PMS.Services.Services.Email;
using PMS.Services.Services.LocationContext;

namespace PMS.Services.Services.Correspondence
{
    public class CorrespondenceService : ICorrespondenceService
    {
        private readonly IAuditLogsService auditLogsService;
        private readonly UnitOfWork<PMSEntities> uow;
        private readonly IEmailService emailService;
        private readonly ILocationContextService locationContextService;

        public CorrespondenceService(UnitOfWork<PMSEntities> _uow, IAuditLogsService _auditLogsService, IEmailService _emailService
            , ILocationContextService _locationContextService)
        {
            auditLogsService = _auditLogsService;
            uow = _uow;
            emailService = _emailService;
            locationContextService = _locationContextService;
        }

        public List<AddEmailSettingsVM> GetEmailSettings()
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();
            int locationId = assignedLocationIds.FirstOrDefault();

            return uow.GenericRepository<EF.EmailSetting>().Table.Where(x => x.IsEnable == true && (x.LocationId == locationId))
                .Select(x => new AddEmailSettingsVM
                {
                    EmailSettingsID = x.EmailSettingsID,
                    EnableEmail = x.EmailEnabled,
                    EmailServer = x.EmailServer,
                    EmailServerPort = x.EmailServerPort,
                    UseSSL = x.UseSSL
                }).ToList();
        }

        public AddEmailSettingsVM GetEmailSettingsById(int id)
        {
            var setting = uow.GenericRepository<EF.EmailSetting>().GetById(id);
            if (setting != null)
            {
                AddEmailSettingsVM model = new AddEmailSettingsVM
                {
                    EmailSettingsID = setting.EmailSettingsID,
                    EnableEmail = setting.EmailEnabled,
                    EmailServer = setting.EmailServer,
                    EmailServerPort = setting.EmailServerPort,
                    UseSSL = setting.UseSSL
                };
                return model;
            }
            else
                return null;
        }

        public EmailSetting AddEmailSettings(AddEmailSettingsVM model)
        {
            EF.EmailSetting setting = new EF.EmailSetting
            {
                EmailEnabled = model.EnableEmail,
                EmailServer = model.EmailServer,
                EmailServerPort = model.EmailServerPort,
                UseSSL = model.UseSSL,
                CreatedDate = model.CreatedDate,
                CreatedBy = model.CreatedBy,
                LocationId = model.LocationId,

                IsEnable = true
            };

            uow.GenericRepository<EF.EmailSetting>().Insert(setting);
            uow.SaveChanges();

            return setting;
        }

        public EmailSetting UpdateEmailSettings(AddEmailSettingsVM model)
        {
            EF.EmailSetting Oldsetting = uow.GenericRepository<EF.EmailSetting>().GetByIdAsNoTracking(x => x.EmailSettingsID == model.EmailSettingsID);
            EF.EmailSetting setting = uow.GenericRepository<EF.EmailSetting>().GetById(model.EmailSettingsID);

            if (setting != null)
            {
                setting.EmailEnabled = model.EnableEmail;
                setting.EmailServer = model.EmailServer;
                setting.EmailServerPort = model.EmailServerPort;
                setting.UseSSL = model.UseSSL;
                setting.UpdatedDate = model.UpdatedDate;
                setting.UpdatedBy = model.UpdatedBy;

                uow.GenericRepository<EF.EmailSetting>().Update(setting);
                uow.SaveChanges();

                //Insert Audit Log
                {
                    var difference = Common.Classes.Common.DetailedCompare<EF.EmailSetting>(Oldsetting, setting);
                    List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();

                    EF.AuditLog auditLog = new EF.AuditLog()
                    {
                        AuditType = (int)Enumeration.AuditType.Update,
                        ActionId = (int)Enumeration.CorrespondenceAction.EmailSettings,
                        PK = setting.EmailSettingsID.ToString(),
                        UserId = Common.Globals.User.ID,
                        TableName = "EmailSetting",
                        UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                        AuditLogDetails = difference
                    };
                    auditLogsService.AddAuditLog(auditLog);
                }



                return setting;
            }
            else
            {
                throw new Exception("Email setting not found to update.");
            }
        }

        public bool DeleteEmailSettings(int id)
        {
            EF.EmailSetting setting = uow.GenericRepository<EF.EmailSetting>().GetById(id);

            if (setting != null)
            {
                setting.IsEnable = false;

                uow.GenericRepository<EF.EmailSetting>().Update(setting);
                uow.SaveChanges();

                return true;
            }
            else
            {
                throw new Exception("Email setting not found to delete.");
            }
        }

        public List<SelectListVM> SendersList()
        {
            return uow.GenericRepository<EF.EmailSender>().Table.Where(x => x.IsActive == true).Where(x => x.IsEnable == true)
                .Select(x => new SelectListVM
                {
                    Text = x.EmailSenderName,
                    Value = x.EmailSenderID.ToString()
                }).ToList();
        }

        public List<EmailSendersListVM> GetEmailSenders()
        {
            return uow.GenericRepository<EF.EmailSender>().Table.Where(x => x.IsEnable == true)
                .ToList()
                .Select(x => new EmailSendersListVM
                {
                    EmailSenderID = x.EmailSenderID,
                    EmailSenderName = x.EmailSenderName,
                    Description = x.EmailSenderDescription,
                    FromAddress = x.FromAddress,
                    EmailSenderPassword = Common.Security.StringCipher.Decrypt(x.EmailPassword),
                    FromName = x.FromName,
                    CC = x.CC,
                    BCC = x.BCC,
                    IsActive = x.IsActive,
                    ReplyToAddress = x.ReplyToAddress,
                    EmailSignature = x.EmailSignature
                }).ToList();
        }

        public EmailSendersListVM GetEmailSenderById(int id)
        {
            var sender = uow.GenericRepository<EF.EmailSender>().GetById(id);
            if (sender != null)
            {
                EmailSendersListVM senderVM = new EmailSendersListVM
                {
                    EmailSenderID = sender.EmailSenderID,
                    EmailSenderName = sender.EmailSenderName,
                    Description = sender.EmailSenderDescription,
                    FromAddress = sender.FromAddress,
                    EmailSenderPassword = Common.Security.StringCipher.Decrypt(sender.EmailPassword),
                    FromName = sender.FromName,
                    CC = sender.CC,
                    BCC = sender.BCC,
                    IsActive = sender.IsActive,
                    ReplyToAddress = sender.ReplyToAddress,
                    EmailSignature = sender.EmailSignature
                };
                return senderVM;
            }
            else
                return null;
        }

        public EmailSender AddEmailSender(AddEmailSendersVM model)
        {
            EF.EmailSender sender = new EF.EmailSender
            {
                EmailSenderName = model.EmailSenderName,
                EmailSenderDescription = model.EmailSenderDescription,
                FromAddress = model.FromAddress,
                EmailPassword = Common.Security.StringCipher.Encrypt(model.EmailPassword),
                FromName = model.FromName,
                CC = model.CC,
                BCC = model.BCC,
                ReplyToAddress = model.ReplyToAddress,
                EmailSignature = model.EmailSignature,
                IsActive = model.IsActive,
                IsEnable = true,
                CreatedDate = model.CreatedDate,
                CreatedBy = model.CreatedBy,
            };

            uow.GenericRepository<EF.EmailSender>().Insert(sender);
            uow.SaveChanges();

            return sender;
        }

        public EmailSender UpdateEmailSender(AddEmailSendersVM model)
        {
            EF.EmailSender Oldsender = uow.GenericRepository<EF.EmailSender>().GetByIdAsNoTracking(x => x.EmailSenderID == model.EmailSenderID);
            EF.EmailSender sender = uow.GenericRepository<EF.EmailSender>().GetById(model.EmailSenderID);

            if (sender != null)
            {
                sender.EmailSenderName = model.EmailSenderName;
                sender.EmailSenderDescription = model.EmailSenderDescription;
                sender.FromAddress = model.FromAddress;
                sender.EmailPassword = Common.Security.StringCipher.Encrypt(model.EmailPassword);
                sender.FromName = model.FromName;
                sender.CC = model.CC;
                sender.BCC = model.BCC;
                sender.ReplyToAddress = model.ReplyToAddress;
                sender.EmailSignature = model.EmailSignature;
                sender.IsActive = model.IsActive;
                sender.UpdatedDate = model.UpdatedDate;
                sender.UpdatedBy = model.UpdatedBy;

                uow.GenericRepository<EF.EmailSender>().Update(sender);
                uow.SaveChanges();


                //Insert Audit Log
                {
                    var difference = Common.Classes.Common.DetailedCompare<EF.EmailSender>(Oldsender, sender);
                    List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();

                    EF.AuditLog auditLog = new EF.AuditLog()
                    {
                        AuditType = (int)Enumeration.AuditType.Update,
                        ActionId = (int)Enumeration.CorrespondenceAction.UpdateEmailSender,
                        PK = sender.EmailSenderID.ToString(),
                        UserId = Common.Globals.User.ID,
                        TableName = "EmailSender",
                        UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                        AuditLogDetails = difference
                    };
                    auditLogsService.AddAuditLog(auditLog);
                }



                return sender;
            }
            else
            {
                throw new Exception("Email sender not found to update.");
            }
        }

        public bool DeleteEmailSender(int id)
        {
            EF.EmailSender Oldsender = uow.GenericRepository<EF.EmailSender>().GetByIdAsNoTracking(x => x.EmailSenderID == id);
            EF.EmailSender sender = uow.GenericRepository<EF.EmailSender>().GetById(id);

            if (sender != null)
            {
                sender.IsEnable = false;

                uow.GenericRepository<EF.EmailSender>().Update(sender);
                uow.SaveChanges();

                //Insert Audit Log
                {
                    var difference = Common.Classes.Common.DetailedCompare<EF.EmailSender>(Oldsender, sender);
                    List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();

                    EF.AuditLog auditLog = new EF.AuditLog()
                    {
                        AuditType = (int)Enumeration.AuditType.Delete,
                        ActionId = (int)Enumeration.CorrespondenceAction.DeleteEmailSender,
                        PK = sender.EmailSenderID.ToString(),
                        UserId = Common.Globals.User.ID,
                        TableName = "EmailSender",
                        UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                        AuditLogDetails = difference
                    };
                    auditLogsService.AddAuditLog(auditLog);
                }


                return true;
            }
            else
            {
                throw new Exception("Email sender not found to delete.");
            }
        }

        public List<EmailMessagesListVM> GetEmailMessages()
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();

            return uow.Context.EmailMessages.Include("EmailSender").Where(x => x.IsEnable == true && assignedLocationIds.Contains((int)x.LocationId))
                 .Select(x => new EmailMessagesListVM
                 {
                     EmailMessageID = x.EmailMessageID,
                     EmailMessageSenderID = x.EmailSenderID,
                     EmailMessageSender = x.EmailSender.EmailSenderName,
                     EmailMessageName = x.EmailMessageName,
                     EmailMessageDescription = x.EmailMessageDescription,
                     EmailMessageSubject = x.EmailMessageSubject,
                     EmailMessageBody = x.EmailMessageBody,
                     IsActive = x.IsActive,
                     LocationId = x.LocationId,
                     LocationName = x.Location.LocationName

                 }).ToList();
        }

        public EmailMessagesListVM GetEmailMessageById(int id)
        {
            var message = uow.Context.EmailMessages.Include("EmailSender").FirstOrDefault(x => x.IsEnable == true && x.EmailMessageID == id);
            if (message != null)
            {
                EmailMessagesListVM messageVM = new EmailMessagesListVM
                {
                    EmailMessageID = message.EmailMessageID,
                    EmailMessageSenderID = message.EmailSenderID,
                    EmailMessageSender = message.EmailSender.EmailSenderName,
                    EmailMessageName = message.EmailMessageName,
                    EmailMessageDescription = message.EmailMessageDescription,
                    EmailMessageSubject = message.EmailMessageSubject,
                    EmailMessageBody = message.EmailMessageBody,
                    IsActive = message.IsActive,
                    ActionId = (int)message.ActionId,
                    LocationId = message.LocationId
                };
                return messageVM;
            }
            else
                return null;
        }

        public EmailMessagesListVM GetEmailMessagesByActionId(int ActionId, int LocationId)
        {
            var message = uow.Context.EmailMessages.Include("EmailSender").FirstOrDefault(x => x.IsEnable == true && x.ActionId == ActionId && x.IsActive == true && x.LocationId == LocationId);
            if (message != null)
            {
                EmailMessagesListVM messageVM = new EmailMessagesListVM
                {
                    EmailMessageID = message.EmailMessageID,
                    EmailMessageSenderID = message.EmailSenderID,
                    EmailMessageSender = message.EmailSender.EmailSenderName,
                    EmailMessageName = message.EmailMessageName,
                    EmailMessageDescription = message.EmailMessageDescription,
                    EmailMessageSubject = message.EmailMessageSubject,
                    EmailMessageBody = message.EmailMessageBody,
                    IsActive = message.IsActive,
                    ActionId = (int)message.ActionId
                };
                return messageVM;
            }
            else
                return null;
        }

        public EmailMessage AddEmailMessage(AddEmailMessageVM model)
        {
            EF.EmailMessage message = new EF.EmailMessage
            {
                EmailSenderID = model.EmailSenderID,
                EmailMessageName = model.EmailMessageName,
                EmailMessageDescription = model.EmailMessageDescription,
                EmailMessageSubject = model.EmailMessageSubject,
                EmailMessageBody = model.EmailMessageBody,
                IsActive = model.IsActive,
                IsEnable = true,
                CreatedDate = model.CreatedDate,
                CreatedBy = model.CreatedBy,
                ActionId = model.ActionId,
                LocationId = model.LocationId
            };

            uow.GenericRepository<EF.EmailMessage>().Insert(message);
            uow.SaveChanges();

            return message;
        }

        public EmailMessage UpdateEmailMessage(AddEmailMessageVM model)
        {
            EF.EmailMessage Oldmessage = uow.GenericRepository<EF.EmailMessage>().GetByIdAsNoTracking(x => x.EmailMessageID == model.EmailMessageID);
            EF.EmailMessage message = uow.GenericRepository<EF.EmailMessage>().GetById(model.EmailMessageID);

            if (message != null)
            {
                message.EmailSenderID = model.EmailSenderID;
                message.EmailMessageName = model.EmailMessageName;
                message.EmailMessageDescription = model.EmailMessageDescription;
                message.EmailMessageSubject = model.EmailMessageSubject;
                message.EmailMessageBody = model.EmailMessageBody;
                message.IsActive = model.IsActive;
                message.UpdatedDate = model.UpdatedDate;
                message.UpdatedBy = model.UpdatedBy;
                message.ActionId = model.ActionId;
                message.LocationId = model.LocationId;

                uow.GenericRepository<EF.EmailMessage>().Update(message);
                uow.SaveChanges();


                //Insert Audit Log
                {
                    var difference = Common.Classes.Common.DetailedCompare<EF.EmailMessage>(Oldmessage, message);
                    List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();

                    EF.AuditLog auditLog = new EF.AuditLog()
                    {
                        AuditType = (int)Enumeration.AuditType.Update,
                        ActionId = (int)Enumeration.CorrespondenceAction.UpdateEmailMessage,
                        PK = message.EmailMessageID.ToString(),
                        UserId = Common.Globals.User.ID,
                        TableName = "EmailMessage",
                        UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                        AuditLogDetails = difference
                    };
                    auditLogsService.AddAuditLog(auditLog);
                }


                return message;
            }
            else
            {
                throw new Exception("Email message not found to update.");
            }
        }

        public bool DeleteEmailMessage(int id)
        {
            EF.EmailMessage Oldmessage = uow.GenericRepository<EF.EmailMessage>().GetByIdAsNoTracking(x => x.EmailMessageID == id);
            EF.EmailMessage message = uow.GenericRepository<EF.EmailMessage>().GetById(id);

            if (message != null)
            {
                message.IsEnable = false;

                uow.GenericRepository<EF.EmailMessage>().Update(message);
                uow.SaveChanges();

                //Insert Audit Log
                {
                    var difference = Common.Classes.Common.DetailedCompare<EF.EmailMessage>(Oldmessage, message);
                    List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();

                    EF.AuditLog auditLog = new EF.AuditLog()
                    {
                        AuditType = (int)Enumeration.AuditType.Delete,
                        ActionId = (int)Enumeration.CorrespondenceAction.DeleteEmailMessage,
                        PK = message.EmailMessageID.ToString(),
                        UserId = Common.Globals.User.ID,
                        TableName = "EmailMessage",
                        UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                        AuditLogDetails = difference
                    };
                    auditLogsService.AddAuditLog(auditLog);
                }

                return true;
            }




            else
            {
                throw new Exception("Email message not found to delete.");
            }
        }

        public List<SelectListVM> GetEmailMessagesDDList()
        {
            return uow.Context.EmailMessages.Where(x => x.IsActive == true).Where(x => x.IsEnable == true)
                 .Select(x => new SelectListVM
                 {
                     Text = x.EmailMessageName,
                     Value = x.EmailMessageID.ToString()

                 }).ToList();
        }

        public List<CorrespondenceAction> GetCorrespondenceActions()
        {
            return uow.Context.CorrespondenceActions.Where(x => x.Status == true).ToList();
        }

        public List<EmailSchulerListVM> GetEmailSchedulers()
        {
            return uow.GenericRepository<EF.EmailScheduler>().Table.Where(x => x.IsEnable == true)
                .ToList()
                .Select(x => new EmailSchulerListVM
                {
                    ID = x.ID,
                    ScheduleName = x.ScheduleName,
                    LocationName = x.Location.LocationName,
                    Type = x.Type,
                    SubType = x.SubType,
                    TaskName = x.TaskName,
                    Recurrence = x.Recurrence ?? false,
                    LastRun = x.LastRun,
                    NextRun = x.NextRun,
                    ExecutionTime = x.ExecutionTime,
                    IsActive = x.IsActive ?? false,
                    EmailSenderID = x.EmailSenderID,
                    EmailMessageBody = x.EmailMessageBody,
                    CreatedDate = x.CreatedDate,
                    CreatedBy = x.CreatedBy,
                    UpdatedDate = x.UpdatedDate,
                    UpdatedBy = x.UpdatedBy

                }).ToList();
        }

        public EmailSchulerListVM GetEmailSchedulerById(int id)
        {
            var emailScheduler = uow.GenericRepository<EF.EmailScheduler>().GetById(id);
            if (emailScheduler != null)
            {
                EmailSchulerListVM schedluerVM = new EmailSchulerListVM
                {
                    ScheduleName = emailScheduler.ScheduleName,
                    Type = emailScheduler.Type,
                    SubType = emailScheduler.SubType,
                    TaskName = emailScheduler.TaskName,
                    Recurrence = emailScheduler.Recurrence ?? false,
                    LastRun = emailScheduler.LastRun,
                    NextRun = emailScheduler.NextRun,
                    ExecutionTime = emailScheduler.ExecutionTime,
                    IsActive = emailScheduler.IsActive ?? false,
                    EmailSenderID = emailScheduler.EmailSenderID,
                    EmailMessageBody = emailScheduler.EmailMessageBody,
                    CreatedDate = emailScheduler.CreatedDate,
                    CreatedBy = emailScheduler.CreatedBy,
                    UpdatedDate = emailScheduler.UpdatedDate,
                    UpdatedBy = emailScheduler.UpdatedBy,
                    FromDate = emailScheduler.Fromdate,
                    ToDate = emailScheduler.Todate,
                    SendTo = !string.IsNullOrEmpty(emailScheduler.SendTo) ? emailScheduler.SendTo.Split(',').ToList() : new List<string>(),
                    LocationId = emailScheduler.LocationId ?? 0,




                };
                return schedluerVM;
            }
            else
                return null;
        }

        public EmailScheduler AddEmailSchedule(AddEmailSchedulerVM model)
        {
            EF.EmailScheduler scheduler = new EF.EmailScheduler
            {
                ScheduleName = model.ScheduleName,
                Type = model.Type,
                SubType = model.SubType,
                TaskName = model.TaskName,
                Recurrence = model.Recurrence,
                NextRun = model.NextRun,
                ExecutionTime = model.ExecutionTime,
                EmailSenderID = model.EmailSenderID,
                EmailMessageBody = model.EmailMessageBody,
                IsActive = model.IsActive,
                IsEnable = true,
                CreatedDate = model.CreatedDate,
                CreatedBy = model.CreatedBy,
                SendTo = model.SendToEmails,
                LocationId = model.LocationId,
                Fromdate = model.FromDate,
                Todate = model.ToDate

            };

            uow.GenericRepository<EF.EmailScheduler>().Insert(scheduler);
            uow.SaveChanges();

            return scheduler;
        }

        public EmailScheduler UpdateEmailSchedule(AddEmailSchedulerVM model)
        {
            EF.EmailScheduler Oldscheduler = uow.GenericRepository<EF.EmailScheduler>().GetByIdAsNoTracking(x => x.ID == model.ID);
            EF.EmailScheduler scheduler = uow.GenericRepository<EF.EmailScheduler>().GetById(model.ID);

            if (scheduler != null)
            {
                // Check if ExecutionTime has changed and IsSent is true
                if (scheduler.ExecutionTime != model.ExecutionTime && scheduler.IsSent)
                {
                    scheduler.IsSent = false; // Reset IsSent flag if ExecutionTime changes
                }
                scheduler.ScheduleName = model.ScheduleName;
                scheduler.Type = model.Type;
                scheduler.SubType = model.SubType;
                scheduler.TaskName = model.TaskName;
                scheduler.Recurrence = model.Recurrence;
                scheduler.IsActive = model.IsActive;
                scheduler.EmailSenderID = model.EmailSenderID;
                scheduler.EmailMessageBody = model.EmailMessageBody;
                scheduler.SendTo = model.SendToEmails;
                scheduler.UpdatedDate = model.UpdatedDate;
                scheduler.UpdatedBy = model.UpdatedBy;
                scheduler.SendTo = model.SendToEmails;
                scheduler.ExecutionTime = model.ExecutionTime;
                scheduler.NextRun = model.NextRun;
                scheduler.LocationId = model.LocationId;
                scheduler.Fromdate = model.FromDate;
                scheduler.Todate = model.ToDate;

                uow.GenericRepository<EF.EmailScheduler>().Update(scheduler);
                uow.SaveChanges();


                //Insert Audit Log
                {
                    var difference = Common.Classes.Common.DetailedCompare<EF.EmailScheduler>(Oldscheduler, scheduler);
                    List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();

                    EF.AuditLog auditLog = new EF.AuditLog()
                    {
                        AuditType = (int)Enumeration.AuditType.Update,
                        ActionId = (int)Enumeration.CorrespondenceAction.UpdateEmailSender,
                        PK = scheduler.ID.ToString(),
                        UserId = Common.Globals.User.ID,
                        TableName = "EmailScheduler",
                        UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                        AuditLogDetails = difference
                    };
                    auditLogsService.AddAuditLog(auditLog);
                }



                return scheduler;
            }
            else
            {
                throw new Exception("Email sender not found to update.");
            }
        }

        public bool DeleteEmailSchedule(int id)
        {
            EF.EmailScheduler Oldscheduler = uow.GenericRepository<EF.EmailScheduler>().GetByIdAsNoTracking(x => x.ID == id);
            EF.EmailScheduler scheduler = uow.GenericRepository<EF.EmailScheduler>().GetById(id);

            if (scheduler != null)
            {
                scheduler.IsEnable = false;

                uow.GenericRepository<EF.EmailScheduler>().Update(scheduler);
                uow.SaveChanges();

                //Insert Audit Log
                {
                    var difference = Common.Classes.Common.DetailedCompare<EF.EmailScheduler>(Oldscheduler, scheduler);
                    List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();

                    EF.AuditLog auditLog = new EF.AuditLog()
                    {
                        AuditType = (int)Enumeration.AuditType.Delete,
                        ActionId = (int)Enumeration.CorrespondenceAction.DeleteEmailMessage,
                        PK = scheduler.ID.ToString(),
                        UserId = Common.Globals.User.ID,
                        TableName = "EmailScheduler",
                        UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                        AuditLogDetails = difference
                    };
                    auditLogsService.AddAuditLog(auditLog);
                }

                return true;
            }




            else
            {
                throw new Exception("Email schedule not found to delete.");
            }
        }

        public bool ManuallySendEmail(int id)
        {
            try
            {
                var message = uow.Context.EmailSchedulers
                    .FirstOrDefault(x => x.IsEnable == true && x.ID == id && x.IsActive == true);

                var body = message.EmailMessageBody;

                var recipientList = message.SendTo.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var recipient in recipientList)
                {
                    emailService.SendEmail("Test Subject", body, true, recipient.Trim(), message.EmailSenderID ?? 0);
                }

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }




    }
}
