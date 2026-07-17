using PMS.DTO.ViewModels;
using PMS.EF;
using PMS.Repository.UnitOfWork;
using PMS.Services.Services.Firebase;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PMS.Common.Classes.Enumeration;

namespace PMS.Services.Services.Notifications
{
    public class NotificationService : INotificationService
    {
        private readonly UnitOfWork<PMSEntities> uow;
        private readonly IFirebaseNotificationService firebaseNotificationService;

        public NotificationService(
            UnitOfWork<PMSEntities> _uow,
            IFirebaseNotificationService _firebaseNotificationService)
        {
            uow = _uow;
            firebaseNotificationService = _firebaseNotificationService;
        }

        public async Task<bool> SendNotification(int? UserId, int? PersonId, string Type, string Subject, string Description, string RedirectURL,string CreatedBy)
        {
            try
            {
              
                var Notification = new Notification()
                {
                    Type = Type,
                    Subject = Subject,
                    Description = Description,
                    RedirectURL = RedirectURL,
                    IsRead = false,
                    UserId = UserId,
                    PersonId = PersonId,
                    CreatedDate = DateTime.Now,
                    CreatedBy = CreatedBy,
                };
                 uow.GenericRepository<Notification>().Insert(Notification);
                 uow.SaveChanges();

                if (PersonId.HasValue
                    && PersonId.Value > 0
                    && string.Equals(Type, "Student", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        await firebaseNotificationService.SendToStudentDevicesAsync(
                            PersonId.Value,
                            Subject,
                            Description,
                            "student_notification",
                            "notifications",
                            RedirectURL);
                    }
                    catch
                    {
                        // In-app notification is already saved; push delivery failure should not block the flow.
                    }
                }

                return true;
            }catch(Exception ex)
            {
                return false;
            }

        }
        public List<NotificationViewModel> GetAllNotification(int Id,int Type)
        {
            {
                var query = uow.GenericRepository<Notification>().Table;
                if (Type == (int)NotifiactionType.Admin)
                    query= query.Where(x => x.UserId == Id);
                else
                    query = query.Where(x => x.PersonId == Id);


                var history=query.Select(x => new NotificationViewModel
                    {
                        Id = x.Id,
                        PersonId = x.PersonId,
                        Subject = x.Subject,
                        Description = x.Description,
                        RedirectURL = x.RedirectURL,
                        CreatedDate = x.CreatedDate
                    }).OrderByDescending(x => x.CreatedDate)
                    .ToList();

                return history;
            }
        }
        public bool UpdateNotification(int Id,int Type)
        {
            var notificationList = new List<EF.Notification>();
            if (Type == (int)NotifiactionType.Admin)
                notificationList = uow.GenericRepository<Notification>().Table.Where(x => x.UserId == Id).ToList();
            else
                notificationList= uow.GenericRepository<Notification>().Table.Where(x => x.PersonId == Id).ToList();

            foreach (var item in notificationList)
            {
                item.IsRead = true;
            }
            uow.SaveChanges();
            return true;
        }
        public List<NotificationViewModel> SPGetNotification(string Type, int Id)
        {
            SqlParameter param1 = new SqlParameter("@Type", Type.ToString());
            SqlParameter param2 = new SqlParameter("@Id", Id.ToString());
            var result = uow.Context.Database.SqlQuery<NotificationViewModel>("SPNotificationReport @Type,@Id", param1, param2).ToList();

            return result;
        }
        public void SendNotificationasync(NotificationViewModel model)
        {
            var db = uow.Context;
            var userIds = new List<int>();

            if (model.TypeId == (int)NotifiactionType.Admin)
            {
                
                 var list = (from submenu in db.SubMenus
                               join rr in db.RoleRights on submenu.SubMenuId equals rr.SubMenuId
                               join ur in db.UserRoles on rr.RoleId equals ur.RoleId
                               where submenu.RoleName == model.RoleName && submenu.IsEnable == true
                               select ur.UserMasterId
                               ).ToList();
                userIds.Add(1);
                userIds.AddRange(list);
              
            }
            if (model.TypeId == (int)NotifiactionType.Group)
            {
                if (model.UserId == null)
                {
                    var list = (from groupuser in db.TicketingGroupUsers
                               join gr in db.GroupLookups on groupuser.GroupId equals gr.Id
                               join user in db.UserMasters on groupuser.UserId equals user.ID
                               where gr.Id== model.GroupId && gr.Status==true
                               select user.ID
                                  ).ToList();
                    userIds.AddRange(list);
                    userIds.Add(1);
                }
                else
                {
                    userIds.Add(model.UserId??0);
                }
            }
              
                foreach (var id in userIds)
                {
                     SendNotification(id, null, "Admin", model.Subject, model.Description, model.RedirectURL, model.CreatedBy).Wait();
                };


        }




    }
}
