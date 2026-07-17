using Ninject.Activation;
using PMS.Common.Classes;
using PMS.DTO;
using PMS.DTO.ViewModels;
using PMS.EF;
using PMS.Repository.UnitOfWork;
using PMS.Services.Services.AuditLogs;
using PMS.Services.Services.Correspondence;
using PMS.Services.Services.Email;
using PMS.Services.Services.LocationContext;
using PMS.Services.Services.Notifications;
using PMS.Services.Services.Sync;
using PMS.Services.Services.UserManage;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using static PMS.Common.Classes.Enumeration;

namespace PMS.Services.Services.Ticket
{
    public class TicketService : ITicketService
    {
        private readonly UnitOfWork<PMSEntities> uow;
        private readonly INotificationService notificationService;
        private readonly ICorrespondenceService correspondenceService;
        private readonly IEmailService emailService;
        private ISyncService syncService;
        private readonly IAuditLogsService auditLogsService;
        private readonly IUserManageService userService;
        private readonly ILocationContextService locationContextService;

        public TicketService(UnitOfWork<PMSEntities> _uow, INotificationService _notificationService,
            ICorrespondenceService _correspondenceService, IEmailService _emailService, ISyncService _syncService, IAuditLogsService _auditLogsService,
            IUserManageService _userService, ILocationContextService _locationContextService)
        {
            uow = _uow;
            notificationService = _notificationService;
            syncService = _syncService;
            correspondenceService = _correspondenceService;
            emailService = _emailService;
            auditLogsService = _auditLogsService;
            userService = _userService;
            locationContextService = _locationContextService;
        }
        public List<TickeStatusLookupVm> GetActiveStatus()
        {
            return uow.GenericRepository<TicketStatusLookup>().GetAll(x => x.Status == true).Select(x => new TickeStatusLookupVm
            {
                Id = x.Id,
                Name = x.Name
            }).ToList();
        }
        public List<PeriorityLookupVm> GetActivePeriority()
        {
            return uow.GenericRepository<TicketPeriorityLookup>().GetAll(x => x.Status == true).Select(x => new PeriorityLookupVm
            {
                Id = x.Id,
                Name = x.Name
            }).ToList();
        }
        public List<GroupLookupVm> GetActiveGroup()
        {
            return uow.GenericRepository<GroupLookup>().GetAll(x => x.Status == true).Select(x => new GroupLookupVm
            {
                Id = x.Id,
                Name = x.Name
            }).ToList();
        }
        public ApiResponse<object> addTicket(TicketViewModel addTicketVM, HttpFileCollectionBase files)
        {
            var response = new ApiResponse<object>();
            try
            {
                var model = new EF.Ticket();
                var fileUpload = new FileUpload();
                var IsStudent = false;
                if (addTicketVM.LocationId == 0)
                {
                    var user = uow.GenericRepository<EF.UserMaster>().Table.Where(x => x.ID == PMS.Common.Globals.User.ID).FirstOrDefault();
                    model.LocationId = user.Person.LocationId ?? 0;
                    model.StatusId = (int)TicketStatus.Open;
                    model.PriorityId = (int)TicketPeriority.Low;
                    IsStudent = true;
                    model.IssueBy = addTicketVM.IssueBy;
                    model.Type = (int)TicketType.IssueByResident;
                }
                else
                {
                    model.StatusId = addTicketVM.StatusId;
                    model.LocationId = addTicketVM.LocationId;
                    model.AssignTo = addTicketVM.AssignTo;
                    model.PriorityId = addTicketVM.PeriorityId;
                    model.DueDate = addTicketVM.DueDate;
                    model.Type = addTicketVM.TypeId;
                    model.FullName = addTicketVM.FullName;
                    model.Phone = addTicketVM.Phone;
                    if (model.Type == (int)TicketType.IssueByResident)
                    {
                        var person = uow.GenericRepository<EF.Person>().GetById(addTicketVM.IssueBy);
                        model.IssueBy = addTicketVM.IssueBy;
                        model.IssueByStaff = null;
                        model.IssueByEmail = null;
                        model.FullName = person.FullName;
                        model.Phone = person.Phone;
                    }
                    else if (model.Type == (int)TicketType.IssueByStaff)
                    {
                        var user = uow.GenericRepository<EF.UserMaster>().GetById(addTicketVM.IssueByStaff);
                        model.IssueByStaff = addTicketVM.IssueByStaff;
                        model.IssueBy = null;
                        model.IssueByEmail = null;
                        IsStudent = true;
                        model.FullName = user.FullName;
                        model.Phone = user.Phone;
                    }
                    else if (model.Type == (int)TicketType.NonResident)
                    {
                        model.IssueByEmail = addTicketVM.IssueByEmail;
                        model.IssueBy = null;
                        model.IssueByStaff = null;
                    }
                }
                model.GroupId = addTicketVM.GroupId;
                model.Code = GetMaxCode(model.LocationId);
                model.Name = addTicketVM.Name;

                model.CreatedBy = addTicketVM.CreatedBy;
                model.CreatedDate = addTicketVM.CreatedDate;
                model.Source = "Website";
                model.IsEnable = true;


                var modelDetail = new EF.TicketDetail();
                modelDetail.Description = addTicketVM.Description;
                modelDetail.CreatedDate = DateTime.Now;
                modelDetail.CreatedBy = PMS.Common.Globals.User.ID;
                for (var i = 0; i < files.Count; i++)
                {
                    var fileName = fileUpload.Upload(files[i], "/Assets/Images/Ticket");

                    var attachment = new EF.TicketDetailAttachement
                    {
                        FileName = files[i].FileName,
                        FileUrl = fileName.ServerPath,
                        CreatedDate = DateTime.Now
                    };
                    modelDetail.TicketDetailAttachements.Add(attachment);
                }

                model.TicketDetails.Add(modelDetail);
                uow.GenericRepository<EF.Ticket>().Insert(model);
                uow.SaveChanges();


                var oldticket = new EF.Ticket();
                //ticketHistory
                AddTicketHistory(oldticket, model);


                //Notification Service
                if (!IsStudent)
                    notificationService.SendNotification(null, model.IssueBy, "Student", "New Complaint", "Your new complaint  " + model.Code + "  has been raised", "/Student/Ticket/Index", PMS.Common.Globals.User.Email);


                var adminNotification = new NotificationViewModel
                {
                    UserId = model.AssignTo,
                    PersonId = model.IssueBy,
                    CreatedDate = DateTime.Now,
                    TypeId = (int)NotifiactionType.Group,
                    Subject = "New Complaint",
                    Description = "New Complaint " + model.Code + " has been raised",
                    RedirectURL = "/Ticket/Index",
                    GroupId = (int)model.GroupId,
                    RoleName = AppUserRoles.view_ticketList,
                    CreatedBy = PMS.Common.Globals.User.Email
                };
                notificationService.SendNotificationasync(adminNotification);

                //Email
                if (model.Type == (int)TicketType.NonResident && !string.IsNullOrEmpty(model.IssueByEmail))
                {
                    var NotifyEmail = correspondenceService.GetEmailMessagesByActionId((int)Enumeration.CorrespondenceAction.Ticket, model.LocationId);
                    if (NotifyEmail != null)
                    {
                        var body = NotifyEmail.EmailMessageBody;
                        body = body.Replace("[[Subject]]", "New Complaint");
                        body = body.Replace("[[Description]]", "Your complaint has been raised of code  " + model.Code + " and issue by from " + model.FullName );

                        //var subject = "New Complaint";
                        //body = "New Complaint " + model.Code + " Has Been Raised";
                        emailService.SendEmailAsync(Convert.ToString(NotifyEmail.EmailMessageSubject), body, true, model.IssueByEmail, NotifyEmail.EmailMessageSenderID);
                    }
                }
                //end

                //Add Sync for Job
                syncService.AddTicketSync(model);
                ///end 
                ///

                //Insert Audit Log
                {
                    var difference = Common.Classes.Common.DetailedCompare<EF.Ticket>(oldticket, model);
                    List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();

                    EF.AuditLog auditLog = new EF.AuditLog()
                    {
                        AuditType = (int)Enumeration.AuditType.Create,
                        ActionId = (int)Enumeration.CorrespondenceAction.TicketCreate,
                        PK = model.Id.ToString(),
                        UserId = Common.Globals.User.ID,
                        TableName = "Ticket",
                        Reference = model.Code.ToString(),
                        UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                        AuditLogDetails = difference
                    };
                    auditLogsService.AddAuditLog(auditLog);
                }



                response.Success = true;
                response.Message = "Complaint Added Successfully!";
                response.Code = (int)HttpStatusCode.OK;
                response.Data = model.Id;
                return response;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.GetBaseException().ToString();
                response.Code = (int)HttpStatusCode.BadRequest;
                response.Data = null;
                return response;
            }
        }
        public ApiResponse<object> AddTicketDetail(HttpFileCollectionBase files, TicketDetailViewModel model)
        {

            var response = new ApiResponse<object>();
            try
            {
                var fileUpload = new FileUpload();
                var db_model = new EF.TicketDetail();
                db_model.Description = model.Description;
                db_model.TicketId = model.TicketId;
                db_model.CreatedDate = DateTime.Now;
                db_model.CreatedBy = PMS.Common.Globals.User.ID;
                for (var i = 0; i < files.Count; i++)
                {
                    var fileName = fileUpload.Upload(files[i], "/Ticket");

                    var attachment = new EF.TicketDetailAttachement
                    {
                        FileName = files[i].FileName,
                        FileUrl = fileName.ServerPath,
                        CreatedDate = DateTime.Now
                    };
                    db_model.TicketDetailAttachements.Add(attachment);
                }
                uow.GenericRepository<EF.TicketDetail>().Insert(db_model);
                uow.SaveChanges();

                response.Success = true;
                response.Message = "Complaint Updated Successfully!";
                response.Code = (int)HttpStatusCode.OK;
                response.Data = "Success!";
                return response;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.GetBaseException().ToString();
                response.Code = (int)HttpStatusCode.BadRequest;
                response.Data = null;
                return response;
            }

        }

        public ApiResponse<TicketViewModel> GetById(int Id)
        {
            var response = new ApiResponse<TicketViewModel>();
            try
            {
                var ticket = uow.GenericRepository<EF.Ticket>().GetById(Id);

                if (ticket == null)
                {
                    response.Success = false;
                    response.Message = "Not Found!";
                    response.Code = (int)HttpStatusCode.OK;
                    response.Data = null;
                    return response;
                }
                var model = new TicketViewModel
                {
                    Id = ticket.Id,
                    Code = ticket.Code,
                    LocationId = ticket.LocationId,
                    Name = ticket.Name,
                    IssueBy = ticket.IssueBy ?? 0,
                    IssueByEmail = ticket.IssueByEmail,
                    PeriorityId = ticket.PriorityId ?? 0,
                    StatusId = ticket.StatusId,
                    AssignTo = ticket.AssignTo ?? 0,
                    DueDate = ticket.DueDate ?? null,
                    GroupId = ticket.GroupId ?? 0,
                    TypeId = ticket.Type ?? 0,
                    CreatedDate = ticket.CreatedDate,
                    CreatedBy = ticket.CreatedBy,
                    FullName = ticket.FullName,
                    Phone = ticket.Phone

                };
                if (model.TypeId == (int)TicketType.IssueByResident)
                    model.IssueBy = ticket.IssueBy ?? 0;
                else if (model.TypeId == (int)TicketType.IssueByStaff)
                    model.IssueByStaff = ticket.IssueByStaff ?? 0;
                else
                    model.IssueByEmail = ticket.IssueByEmail;


                model.TicketDetailViewModel = new List<TicketDetailViewModel>();
                foreach (var item in ticket.TicketDetails)
                {
                    var detail = new TicketDetailViewModel
                    {
                        TicketId = item.TicketId,
                        Id = item.Id,
                        Description = item.Description
                    };
                    model.TicketDetailViewModel.Add(detail);
                }
                response.Code = (int)HttpStatusCode.OK;
                response.Message = "Success";
                response.Data = model;
                response.Success = true;
                return response;

            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.GetBaseException().ToString();
                response.Code = (int)HttpStatusCode.BadRequest;
                response.Data = null;
                return response;
            }
        }
        public ApiResponse<object> Update(TicketViewModel model, HttpFileCollectionBase files, List<MockFiles> ExistingFile)
        {
            var fileUpload = new FileUpload();
            var response = new ApiResponse<object>();
            try
            {
                var db_value = uow.GenericRepository<EF.Ticket>().GetById(model.Id);
                var oldticket = uow.GenericRepository<EF.Ticket>().GetByIdAsNoTracking(x => x.Id == model.Id);
                if (db_value == null)
                {
                    response.Success = false;
                    response.Message = "Not Found!";
                    response.Code = (int)HttpStatusCode.OK;
                    response.Data = null;
                    return response;
                }
                if (model.LocationId != 0)
                {
                    db_value.LocationId = model.LocationId;
                    db_value.StatusId = model.StatusId;
                    db_value.AssignTo = model.AssignTo;
                    db_value.PriorityId = model.PeriorityId;

                    db_value.DueDate = model.DueDate;
                    db_value.IsEnable = true;
                    db_value.Type = model.TypeId;
                    if (db_value.Type == (int)TicketType.IssueByResident)
                    {
                        db_value.IssueBy = model.IssueBy;
                        db_value.IssueByStaff = null;
                        db_value.IssueByEmail = null;
                    }
                    else if (db_value.Type == (int)TicketType.IssueByStaff)
                    {
                        db_value.IssueByStaff = model.IssueByStaff;
                        db_value.IssueBy = null;
                        db_value.IssueByEmail = null;
                    }
                    else
                    {
                        db_value.IssueByEmail = model.IssueByEmail;
                        db_value.IssueBy = null;
                        db_value.IssueByStaff = null;
                    }

                }
                db_value.Name = model.Name;
                db_value.GroupId = model.GroupId;
                db_value.UpdatedBy = model.CreatedBy;
                db_value.UpdatedDate = model.CreatedDate;
                var isUpdate = true;
                var modelDetail = db_value.TicketDetails.FirstOrDefault();
                if (modelDetail == null)
                {
                    isUpdate = false;
                    modelDetail = new EF.TicketDetail();
                    modelDetail.TicketId = db_value.Id;
                    modelDetail.CreatedBy = PMS.Common.Globals.User.ID;
                    modelDetail.CreatedDate = DateTime.Now;
                }
                modelDetail.Description = model.Description;
                var previouseList = modelDetail.TicketDetailAttachements.ToList();
                if (ExistingFile.Count > 0)
                {
                    foreach (var item in ExistingFile)
                    {
                        previouseList.Remove(previouseList.Where(x => x.Id == item.serverID).FirstOrDefault());
                    }
                }

                foreach (var detailfile in previouseList)
                {
                    var filename = detailfile.FileUrl.Split('/').LastOrDefault();
                    fileUpload.RemoveFile(filename, "/Assets/Images/Ticket");

                    uow.GenericRepository<EF.TicketDetailAttachement>().Delete(detailfile);
                    uow.SaveChanges();
                }


                for (var i = 0; i < files.Count; i++)
                {
                    var fileName = fileUpload.Upload(files[i], "/Assets/Images/Ticket");

                    var attachment = new EF.TicketDetailAttachement
                    {
                        FileName = files[i].FileName,
                        FileUrl = fileName.ServerPath,
                        CreatedDate = DateTime.Now
                    };
                    modelDetail.TicketDetailAttachements.Add(attachment);
                }



                if (isUpdate)
                {
                    uow.GenericRepository<EF.TicketDetail>().Update(modelDetail);
                }
                else
                {
                    uow.GenericRepository<EF.TicketDetail>().Insert(modelDetail);
                }
                uow.GenericRepository<EF.Ticket>().Update(db_value);
                uow.SaveChanges();


                //Tickethistory 
                AddTicketHistory(oldticket, db_value);


                //Job and Sync
                if (db_value.StatusId == (int)TicketStatus.Open || db_value.StatusId == (int)TicketStatus.Pending)
                    syncService.AddTicketSync(db_value);

                //end




                //Insert Audit Log
                {
                    var difference = Common.Classes.Common.DetailedCompare<EF.Ticket>(oldticket, db_value);
                    List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();

                    EF.AuditLog auditLog = new EF.AuditLog()
                    {
                        AuditType = (int)Enumeration.AuditType.Update,
                        ActionId = (int)Enumeration.CorrespondenceAction.TicketUpdate,
                        PK = db_value.Id.ToString(),
                        UserId = Common.Globals.User.ID,
                        TableName = "Ticket",
                        Reference = db_value.Code.ToString(),
                        UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                        AuditLogDetails = difference
                    };
                    auditLogsService.AddAuditLog(auditLog);
                }



                response.Success = true;
                response.Message = "Complaint Updated Successfully!";
                response.Code = (int)HttpStatusCode.OK;
                response.Data = db_value.Id;
                return response;

            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.GetBaseException().ToString();
                response.Code = (int)HttpStatusCode.BadRequest;
                response.Data = null;
                return response;
            }
        }


        public ApiResponse<object> GetAttachement(int Id)
        {
            var response = new ApiResponse<object>();
            var fileUpload = new FileUpload();
            try
            {
                var list = new List<TicketDetailAttachementVm>();
                var model = uow.GenericRepository<EF.TicketDetailAttachement>().Table.Where(x => x.TicketDetailId == Id);

                foreach (var item in model)
                {
                    long ImageSize = 0;
                    var imagePath = item.FileUrl.Split('/').LastOrDefault();
                    if (imagePath != null)
                    {
                        string imgUrl = HttpContext.Current.Request.MapPath("/Assets/Images/Ticket/" + imagePath);
                        if (new FileInfo(imgUrl).Exists)
                        {
                            ImageSize = new FileInfo(imgUrl).Length;
                        }


                    }

                    var detail = new TicketDetailAttachementVm
                    {
                        FileName = item.FileName,
                        FileUrl = item.FileUrl,
                        FileSize = ImageSize,
                        FileType = "image/" + Path.GetExtension(item.FileName),
                        Id = item.Id
                    };
                    list.Add(detail);
                }
                response.Success = true;
                response.Message = "Success!";
                response.Code = (int)HttpStatusCode.OK;
                response.Data = list;
                return response;

            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.GetBaseException().ToString();
                response.Code = (int)HttpStatusCode.BadRequest;
                response.Data = null;
                return response;
            }
        }

        Func<EF.Ticket, string> GetIssueName = delegate (EF.Ticket ticket)
        {
            if (ticket.Type == (int)TicketType.IssueByResident)
                return ticket.Person.Code + "-" + ticket.Person.FullName;
            else if (ticket.Type == (int)TicketType.IssueByStaff)
                return ticket.UserMaster3.FullName;
            else
                return ticket.FullName+" - "+ ticket.Phone;

        };

        public List<TicketViewModel> GetTickets(int? statusId, int? ticketId)
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();

            var userid = PMS.Common.Globals.User.ID;

            var Identity = HttpContext.Current.User;


            var tickets = uow.GenericRepository<EF.Ticket>().Table.Where(x => x.IsEnable && assignedLocationIds.Contains((int)x.LocationId)).ToList();
            var ticketGroup = uow.GenericRepository<EF.TicketingGroupUser>().Table.Where(x => x.UserId == userid);

            if (Identity != null && Identity.IsInRole(PMS.Common.Classes.AppUserRoles.see_All_Tickets))
            {
                tickets = tickets;
            }
            else
            {
                var tikcetlist = new List<EF.Ticket>();
                foreach (var item in ticketGroup)
                {

                    if (item.GroupRoleId == (int)TicketGroupRoles.Member || item.GroupRoleId == (int)TicketGroupRoles.Manager)
                    {
                        tikcetlist.AddRange(tickets.Where(x => x.GroupId == item.GroupId && (x.CreatedBy == userid || x.AssignTo == userid || x.IssueByStaff == userid)).ToList());
                    }
                    else if (item.GroupRoleId == (int)TicketGroupRoles.Head)
                    {
                        tikcetlist.AddRange(tickets.Where(x => x.GroupId == item.GroupId).ToList());
                    }
                    else
                    {
                        tikcetlist.AddRange(tickets.Where(x => x.IssueByStaff == userid).ToList());
                    }

                }
                tickets = tikcetlist;
            }

            //Filter
            if (ticketId == 1)
            {
                tickets = tickets.Where(x => x.IssueByStaff == PMS.Common.Globals.User.ID).ToList();
            }
            if (ticketId == 2)
            {
                tickets = tickets.Where(x => x.AssignTo == PMS.Common.Globals.User.ID).ToList();
            }

            if (statusId != null && statusId != 0)
            {
                tickets = tickets.Where(x => x.StatusId == statusId).ToList();
            }

            var model = tickets.OrderBy(x => x.StatusId).AsEnumerable().Select(x => new TicketViewModel
            {
                Id = x.Id,
                Code = x.Code,
                Description = x.TicketDetails.FirstOrDefault() == null ? "" : x.TicketDetails.FirstOrDefault().Description,
                Name = x.Name,
                LocationName = x.Location.LocationName,
                //TypeId =x.Type,
                PriorityName = x.TicketPeriorityLookup == null ? " " : x.TicketPeriorityLookup.Name,
                PeriorityId = x.PriorityId ?? 0,
                Status = x.TicketStatusLookup.Name,
                StatusId = x.StatusId,
                Source = x.Source,
                IsEnable = x.IsEnable,
                DueDate = x.DueDate,
                CreatedBy = x.CreatedBy,
                GroupName = x.GroupId == null ? "Other" : x.GroupLookup.Name,
                AssignToName = x.UserMaster1 == null ? " " : x.UserMaster1.FullName,
                IssueByName = GetIssueName(x),
                CreatedByName = x.UserMaster.Username,
                CreatedDate = x.CreatedDate,
                ResolvedDate = x.ResolvedDate.HasValue ? x.ResolvedDate.Value.ToString() : "------"
            }).ToList();
            return model;

        }

        public List<TicketViewModel> GetStudentTickets(int Id)
        {
            var personId = 0;
            var person = uow.GenericRepository<EF.UserMaster>().Table.Where(x => x.ID == Id).FirstOrDefault();
            if (person != null)
            {
                personId = person.PersonID ?? 0;
            }
            var model = uow.GenericRepository<EF.Ticket>().Table.Where(x => x.IsEnable == true && (x.CreatedBy == Id || x.IssueBy == personId)).Select(x => new TicketViewModel
            {
                Id = x.Id,
                Code = x.Code,
                Description = x.TicketDetails.FirstOrDefault() == null ? "" : x.TicketDetails.FirstOrDefault().Description,
                Name = x.Name,
                LocationName = x.Location.LocationName,
                PriorityName = x.TicketPeriorityLookup == null ? " " : x.TicketPeriorityLookup.Name,
                Status = x.TicketStatusLookup.Name,
                Source = x.Source,
                IsEnable = x.IsEnable,
                DueDate = x.DueDate,
                StatusId = x.StatusId,
                GroupName = x.GroupId == null ? "Other" : x.GroupLookup.Name,
                AssignToName = x.UserMaster == null ? " " : x.UserMaster.FullName,
                IssueByName = x.Person.Code + "-" + x.Person.FullName,
                CreatedByName = x.UserMaster.Username,
                CreatedBy = x.CreatedBy,
                CreatedDate = x.CreatedDate,
            }).ToList();
            return model;
        }


        public ApiResponse<List<CommentViewModel>> GetAllComments(int Id)
        {
            var response = new ApiResponse<List<CommentViewModel>>();
            try
            {
                var model = uow.GenericRepository<EF.TicketDetail>().Table.Where(x => x.TicketId == Id).OrderBy(x => x.CreatedDate).Select(x => new CommentViewModel
                {
                    Id = x.Id,
                    Description = x.Description,
                    TicketId = x.TicketId,
                    CreatedBy = x.UserMaster.FullName,
                    CreatedDate = x.CreatedDate.ToString(),
                    UserImageUrl = x.UserMaster.ImageUrl == null ? "/Assets/dist/img/profile.png" : x.UserMaster.ImageUrl
                }).ToList();

                response.Success = true;
                response.Message = "Successfull!";
                response.Data = model;
                response.Code = (int)HttpStatusCode.OK;
                return response;

            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.GetBaseException().ToString();
                response.Code = (int)HttpStatusCode.BadRequest;
                response.Data = null;
                return response;
            }
        }

        public ApiResponse<string> AddComment(CommentViewModel model)
        {
            var response = new ApiResponse<string>();
            try
            {
                var db = uow.GenericRepository<EF.TicketDetail>().Table.Where(x => x.TicketId == model.TicketId && x.ParentId == null).FirstOrDefault();

                if (db == null)
                {
                    response.Success = false;
                    response.Message = "Not Found!";
                    response.Data = null;
                    response.Code = (int)HttpStatusCode.BadRequest;
                    return response;
                }

                var detail = new EF.TicketDetail
                {
                    ParentId = db.Id,
                    TicketId = model.TicketId,
                    Description = model.Description,
                    CreatedBy = PMS.Common.Globals.User.ID,
                    CreatedDate = DateTime.Now
                };
                uow.GenericRepository<EF.TicketDetail>().Insert(detail);
                uow.SaveChanges();


                var user = uow.GenericRepository<EF.UserMaster>().GetById(detail.CreatedBy);
                var ticket = uow.GenericRepository<EF.Ticket>().GetById(detail.TicketId);
                if (user != null && ticket != null)
                {
                    if (user.IsStudent == false || user.IsStudent == null)
                        notificationService.SendNotification(null, ticket.IssueBy, "Student", "New Comment", "New comment added against complaint " + ticket.Code, "/Student/Ticket/Comments?id=" + model.TicketId, PMS.Common.Globals.User.Email);
                    else
                        notificationService.SendNotification(null, null, "Admin", "New Comment", "New comment added against complaint " + detail.Ticket.Code, "/Ticket/Comments?id=" + detail.Ticket.Id, PMS.Common.Globals.User.Email);
                }
                response.Success = true;
                response.Message = "Successful!";
                response.Data = "Success!";
                response.Code = (int)HttpStatusCode.OK;
                return response;

            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.GetBaseException().ToString();
                response.Code = (int)HttpStatusCode.BadRequest;
                response.Data = null;
                return response;
            }
        }
        public ApiResponse<TicketViewModel> GetDetailById(int Id)
        {
            var response = new ApiResponse<TicketViewModel>();
            try
            {
                var ticket = uow.GenericRepository<EF.Ticket>().Table.Where(x => x.Id == Id).FirstOrDefault();
                var detail = ticket.TicketDetails.Where(x => x.ParentId == null).FirstOrDefault();
                if (ticket == null)
                {
                    response.Success = false;
                    response.Message = "Not Found!";
                    response.Code = (int)HttpStatusCode.OK;
                    response.Data = null;
                    return response;
                }
                var model = new TicketViewModel
                {
                    Id = ticket.Id,
                    StatusId = ticket.StatusId,
                    DueDateString = ticket.DueDate.HasValue ? ticket.DueDate.Value.ToString("yyyy-MM-dd") : "",
                    PeriorityId = ticket.PriorityId ?? 0,
                    AssignTo = ticket.AssignTo ?? 0

                };
                model.TicketDetailViewModel = new List<TicketDetailViewModel>();

                var modelDetail = new TicketDetailViewModel
                {
                    Id = detail.Id,
                    Description = detail.Description

                };
                modelDetail.TicketDetailAttachementVm = new List<TicketDetailAttachementVm>();
                foreach (var item in detail.TicketDetailAttachements)
                {
                    var attachement = new TicketDetailAttachementVm
                    {
                        Id = item.Id,
                        FileUrl = item.FileUrl
                    };
                    modelDetail.TicketDetailAttachementVm.Add(attachement);
                }

                model.TicketDetailViewModel.Add(modelDetail);

                response.Code = (int)HttpStatusCode.OK;
                response.Message = "Success";
                response.Data = model;
                response.Success = true;
                return response;

            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.GetBaseException().ToString();
                response.Code = (int)HttpStatusCode.BadRequest;
                response.Data = null;
                return response;
            }
        }


        public ApiResponse<string> UpdateStatus(TicketViewModel model)
        {
            var response = new ApiResponse<string>();
            try
            {
                var ticket = uow.GenericRepository<EF.Ticket>().Table.Where(x => x.Id == model.Id).FirstOrDefault();
                var oldvalue = uow.GenericRepository<EF.Ticket>().GetByIdAsNoTracking(x => x.Id == model.Id);
                if (ticket == null)
                {
                    response.Success = false;
                    response.Message = "Not Found!";
                    response.Code = (int)HttpStatusCode.BadRequest;
                    response.Data = null;
                    return response;
                }
                ticket.StatusId = model.StatusId;
                if (ticket.StatusId == (int)TicketStatus.Resolved)
                {
                    ticket.ResolvedDate = DateTime.Now;

                    notificationService.SendNotification(null, ticket.IssueBy, "Student", "Complaint Update", GetNotificationDescription(ticket.StatusId, ticket.Code), "/Student/Ticket/Index", PMS.Common.Globals.User.Email);
                }
                else if (ticket.StatusId == (int)TicketStatus.Pending)
                {
                    ticket.ResolvedDate = null;
                    notificationService.SendNotification(null, ticket.IssueBy, "Student", "Complaint Update", GetNotificationDescription(ticket.StatusId, ticket.Code), "/Student/Ticket/Index", PMS.Common.Globals.User.Email);


                }

                if (ticket.AssignTo == null && model.AssignTo != 0)
                {

                    notificationService.SendNotification(ticket.AssignTo, null, "Admin", "Complaint Update", "An complaint of code " + ticket.Code + " is assign to you", "/Student/Ticket/Index", PMS.Common.Globals.User.Email);
                }
                var Notification = new NotificationViewModel
                {
                    Subject = "Complaint Update",
                    Description = "The complaint of code " + ticket.Code + " has been updated",
                    UserId = null,
                    TypeId = (int)NotifiactionType.Group,
                    CreatedBy = PMS.Common.Globals.User.Email,
                    RedirectURL = "/Ticket/Index/",

                };
                notificationService.SendNotificationasync(Notification);



                ticket.PriorityId = model.PeriorityId;
                ticket.AssignTo = model.AssignTo;
                ticket.DueDate = model.DueDate;
                ticket.UpdatedDate = DateTime.Now;
                ticket.UpdatedBy = PMS.Common.Globals.User.ID;
                uow.SaveChanges();

                //Ticket History
                AddTicketHistory(oldvalue, ticket);

                syncService.AddTicketSync(ticket);




                //Insert Audit Log
                {
                    var difference = Common.Classes.Common.DetailedCompare<EF.Ticket>(oldvalue, ticket);
                    List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();

                    EF.AuditLog auditLog = new EF.AuditLog()
                    {
                        AuditType = (int)Enumeration.AuditType.Update,
                        ActionId = (int)Enumeration.CorrespondenceAction.TicketUpdate,
                        PK = ticket.Id.ToString(),
                        UserId = Common.Globals.User.ID,
                        TableName = "Ticket",
                        Reference = ticket.Code.ToString(),
                        UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                        AuditLogDetails = difference
                    };
                    auditLogsService.AddAuditLog(auditLog);
                }


                response.Success = true;
                response.Message = "Complaint Status Updated";
                response.Code = (int)HttpStatusCode.OK;
                response.Data = "Update";
                return response;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.GetBaseException().ToString();
                response.Code = (int)HttpStatusCode.BadRequest;
                response.Data = null;
                return response;
            }
        }

        public string GetMaxCode(int LocationId)
        {
            var location = uow.GenericRepository<Location>().GetById(LocationId);
            var student = uow.GenericRepository<EF.Ticket>().Table.Where(x => x.Code != null && x.LocationId == LocationId);


            int code = 0;
            if (student.Count() != 0)
            {
                var nowithGRn = Convert.ToDecimal(student.AsEnumerable().Select(x => new { Number = Convert.ToDecimal(x.Code.Split('-')[2]) }).Max(x => x.Number)) + 1;
                code = (int)nowithGRn;


            }
            else

            {
                code = 1;
            }
            string value = String.Format("{0:D4}", code);
            var Code = "TCT-" + location.Prefix + "-" + value;

            return Code;
        }
        public ApiResponse<object> Delete(int Id)
        {
            var response = new ApiResponse<object>();
            try
            {
                var ticket = uow.GenericRepository<EF.Ticket>().Table.Where(x => x.Id == Id).FirstOrDefault();
                if (ticket == null)
                {
                    response.Code = (int)HttpStatusCode.OK;
                    response.Message = "Not Found!";
                    response.Success = false;
                    response.Data = null;
                }
                ticket.IsEnable = false;
                uow.SaveChanges();

                response.Code = (int)HttpStatusCode.OK;
                response.Message = "Complaint Deleted Successfuly!";
                response.Success = true;
                response.Data = "Complaint Deleted Successfuly";
            }
            catch (Exception ex)
            {
                response.Code = (int)HttpStatusCode.BadRequest;
                response.Message = ex.Message;
                response.Success = false;
                response.Data = null;
            }
            return response;
        }

        public List<DropDownViewModel> GetUserByGroupId(int GroupId)
        {
            var List = uow.GenericRepository<TicketingGroupUser>().Table.Where(x => x.GroupId == GroupId && x.UserMaster.IsActive == true && x.UserMaster.IsEnable == true && x.UserMaster.ID != 1).Select(x => new DropDownViewModel
            {
                Name = x.UserMaster.FullName,
                Id = x.UserMaster.ID
            }).ToList();
            return List;
        }
        private string GetNotificationDescription(int StatusId, string Code)
        {
            var description = "";
            if (StatusId == (int)TicketStatus.Resolved)
            {
                description = "Your complaint " + Code + " has been resolved";
            }
            else if (StatusId == (int)TicketStatus.Pending)
            {
                description = "Your complaint " + Code + " is in process";
            }
            return description;
        }

        public async Task<bool> AddTicketHistory(EF.Ticket oldTicket, EF.Ticket newticket)
        {
            var difference = Common.Classes.Common.DetailedCompare<EF.Ticket>(oldTicket, newticket);
            foreach (var item in difference)
            {
                if (item.PropertyName == "StatusId" && item.NewValue != item.OldValue)
                {
                    var ticketHistory = new TicketHistory
                    {
                        TicketId = newticket.Id,
                        StatusId = Convert.ToInt32(item.NewValue),
                    };
                    if (oldTicket.Id == 0)
                    {
                        ticketHistory.CreatedDate = DateTime.Now;
                        ticketHistory.CreatedBy = PMS.Common.Globals.User.ID;
                    }
                    else
                    {
                        ticketHistory.UpdatedDate = DateTime.Now;
                        ticketHistory.UpdatedBy = PMS.Common.Globals.User.ID;
                    }
                    uow.GenericRepository<EF.TicketHistory>().Insert(ticketHistory);
                }
            }
            uow.SaveChanges();
            return true;
        }

    }
}
