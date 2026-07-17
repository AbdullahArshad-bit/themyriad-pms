using PMS.DTO;
using PMS.DTO.ViewModels;
using PMS.EF;
using PMS.Repository.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Services.Services.TicketGroup
{
    public class TicketGroupService : ITicketGroupService
    {
        private readonly UnitOfWork<PMSEntities> uow;
        public TicketGroupService(UnitOfWork<PMSEntities> _uow)
        {
            uow = _uow;
        }
        public List<TicketGroupVm> GetAll(int? GroupId)
        {
            var db = uow.Context;
            var response = (from user in db.UserMasters

                            join userRole in db.UserRoles on user.ID equals userRole.UserMasterId
                            into usr
                            from userrole in usr.DefaultIfEmpty()

                            join role in db.Roles on userrole.RoleId equals role.RoleId
                            into rl
                            from roles in rl.DefaultIfEmpty()



                            where user.IsStudent != true && user.IsActive == true && user.ID != 1 && user.IsEnable==true


                            select new TicketGroupVm
                            {
                                Name = user.FullName,
                                RoleName = roles.RoleName,
                                UserId = user.ID,
                                GroupUserID = (from userGrp in db.TicketingGroupUsers
                                               where GroupId == userGrp.GroupId && userGrp.UserId == user.ID
                                               select userGrp.UserId
                                               ).FirstOrDefault(),
                                GroupRoleId = (from userGrp in db.TicketingGroupUsers
                                               where GroupId == userGrp.GroupId && userGrp.UserId == user.ID
                                               select userGrp.GroupRoleId
                                               ).FirstOrDefault(),
                                TicketGroupRole = (from grpRole in db.GroupRoleLookups
                                                   select new TicketGroupRole
                                                   {
                                                       Id = grpRole.Id,
                                                       Name = grpRole.Name
                                                   }).ToList()
                            }).OrderBy(x=>x.Name);
            return response.ToList();
        }
        public bool SaveTicketGroup(List<TicketGroupVm> groupsVM)
        {
            
            try
            {
                uow.CreateTransaction();
                if (groupsVM != null)
                {
                    var groupId = groupsVM.FirstOrDefault();
                    if (groupId != null) {
                        var previouse = uow.GenericRepository<TicketingGroupUser>().Table.Where(x => x.GroupId ==groupId.GroupId ).ToList();

                        foreach (var previouseItem in previouse)
                        {
                            uow.GenericRepository<TicketingGroupUser>().Delete(previouseItem);
                        }
                    }
                    foreach (var item in groupsVM)
                    {
                        var db_model = new TicketingGroupUser
                        {
                            UserId = item.UserId,
                            GroupId = item.GroupId,
                            GroupRoleId = (int)item.GroupRoleId
                        };
                        uow.GenericRepository<TicketingGroupUser>().Insert(db_model);
                    }
                    uow.SaveChanges();
                   
                }
                uow.Commit();
                return true;
                

           
            }
            catch (Exception ex)
            {
                uow.Rollback();
                return false;
            }

            
        }

        public List<TicketGroupVm> GetGroupUserByRoleId(int RoleId,int GroupId)
        {
            var db = uow.Context;
            var response = (from user in db.UserMasters

                            join userRole in db.TicketingGroupUsers on user.ID equals userRole.UserId
                            where user.IsStudent != true && user.IsActive == true && user.ID != 1
                            && userRole.GroupRoleId==RoleId && userRole.GroupId==GroupId


                            select new TicketGroupVm
                            {
                                Name = user.FullName,
                                GroupRoleId=userRole.GroupRoleId,
                                UserId = user.ID,
                                UserEmail=user.Email
                                 
                            }).OrderBy(x => x.Name);
            return response.ToList();
        }
    }

}
