using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using PMS.Common;
using PMS.Common.Classes;
using PMS.DTO;
using PMS.DTO.ViewModels.ApiViewModels;
using PMS.DTO.ViewModels.UserManageViewModels;
using PMS.EF;
using PMS.Repository.UnitOfWork;
using PMS.Services.Services.AuditLogs;
using PMS.Services.Services.LocationContext;

namespace PMS.Services.Services.UserManage
{
    public class UserManageService : IUserManageService
    {
        private readonly UnitOfWork<PMSEntities> uow;
        private readonly IAuditLogsService auditLogsService;
        private readonly ILocationContextService locationContextService;

        public UserManageService(UnitOfWork<PMSEntities> _uow, IAuditLogsService _auditLogsService, ILocationContextService _locationContextService)
        {
            auditLogsService = _auditLogsService;
            uow = _uow;
            locationContextService = _locationContextService;
        }

        public List<UserMaster> GetUsers()
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();


            // Ensure we only proceed if there are valid assigned locations
            if (assignedLocationIds == null || !assignedLocationIds.Any())
                return new List<UserMaster>();

            // Fetch users while applying SQL-compatible filters in the database
            var users = uow.GenericRepository<UserMaster>().Table
                .Where(x => x.IsEnable == true && x.ID != 1)
                .AsEnumerable() // Switch to in-memory filtering
                .Where(x =>
                    // Condition 1: User has an assigned location that matches session locations
                    (!string.IsNullOrEmpty(x.AssignedLocation) &&
                     x.AssignedLocation.Split(',').Select(int.Parse).Intersect(assignedLocationIds).Any())
                    ||
                    // Condition 2: User's LastLocationID matches session locations
                    assignedLocationIds.Contains(x.LastLocationId ?? 0)
                    ||
                    // New Condition 3: Include new users who have AssignedLocation = 0 and LastLocation = NULL
                    (x.IsStudent == false && (string.IsNullOrEmpty(x.AssignedLocation) || x.AssignedLocation == "0") && x.LastLocationId == null)
                )
                .ToList();

            // Condition 4: Fetch users based on Person table location (done in DB for performance)
            var personUsers = (from user in uow.GenericRepository<UserMaster>().Table
                               join person in uow.GenericRepository<EF.Person>().Table
                               on user.PersonID equals person.PersonID
                               where assignedLocationIds.Contains(person.LocationId ?? 0)
                               select user)
                               .ToList();

            // Merge both lists (removing duplicates)
            users.AddRange(personUsers);
            users = users.Distinct().ToList();

            return users;
        }



        public List<UserMaster> GetActiveUsers()
        {
            return uow.GenericRepository<UserMaster>().Table.Where(x => x.IsEnable == true && x.IsActive == true && x.ID != 1).ToList();
        }

        public UserMaster GetUserById(int id)
        {
            return uow.GenericRepository<UserMaster>().GetById(id);
        }

        public UserMaster AddUser(AddUserVM userVM)
        {
            try
            {
                uow.CreateTransaction();

                bool exist = uow.GenericRepository<UserMaster>().Table.Any(x => x.IsEnable == true && x.Email == userVM.Email);
                if (exist)
                {
                    throw new Exception("Email already registered.");
                }
                //var assignedLocationIds = (List<int>)HttpContext.Current.Session["locationid"] ?? PMS.Common.Globals.User.AssignedLocations;

                UserMaster user = new UserMaster
                {
                    FullName = userVM.FullName,
                    Username = userVM.Email,
                    Email = userVM.Email,
                    Password = PMS.Common.Security.StringCipher.Encrypt(userVM.Password),
                    Gender = userVM.Gender,
                    DOB = userVM.DOB,
                    Phone = userVM.Phone,
                    IsActive = userVM.IsActive,
                    IsEnable = true,
                    Address = userVM.Address,
                    CreatedAt = userVM.CreatedDate,
                    Department = userVM.Department,
                    Designation = userVM.Designation,
                    CreatedBy = userVM.CreatedBy,
                    IsStudent = userVM.IsStudent,
                    PersonID = userVM.PersonID,
                    AssignedLocation = userVM.LocationId != null ? string.Join(",", userVM.LocationId) : "0",
                    LastLocationId = userVM.LocationId

                    //AssignedLocation = assignedLocationIds != null ? string.Join(",", assignedLocationIds): "0",
                    //LastLocationId =assignedLocationIds?.FirstOrDefault()
                    //AssignedLocation = "0"
                };

                uow.GenericRepository<UserMaster>().Insert(user);
                uow.SaveChanges();

                uow.GenericRepository<UserRole>().Insert(new UserRole
                {
                    UserMasterId = user.ID,
                    RoleId = userVM.RoleId,
                    IsEnable = true,
                    CreatedBy = userVM.CreatedBy,
                    CreatedDate = userVM.CreatedDate
                });

                uow.SaveChanges();

                uow.Commit();

                return user;
            }
            catch (Exception ex)
            {
                uow.Rollback();
                throw ex;
            }
        }
        public UserMaster UpdateUser(AddUserVM userVM)
        {
            try
            {
                uow.CreateTransaction();

                bool exist = uow.GenericRepository<UserMaster>().Table.Any(x => x.IsEnable == true && x.Email == userVM.Email && x.ID != userVM.UserID);
                if (exist)
                {
                    throw new Exception("Email already registered.");
                }
                var olduser = uow.GenericRepository<UserMaster>().GetByIdAsNoTracking(x => x.ID == userVM.UserID);

                var user = GetUserById(userVM.UserID);
                if (user != null)
                {
                    user.FullName = userVM.FullName;
                    user.Username = userVM.Email;
                    user.Email = userVM.Email;
                    user.Password = PMS.Common.Security.StringCipher.Encrypt(userVM.Password);
                    user.Gender = userVM.Gender;
                    user.DOB = userVM.DOB;
                    user.Phone = userVM.Phone;
                    user.IsActive = userVM.IsActive;
                    user.Address = userVM.Address;
                    user.Department = userVM.Department;
                    user.Designation = userVM.Designation;
                    user.UpdatedAt = userVM.UpdatedDate;
                    user.UpdatedBy = userVM.UpdatedBy;

                    uow.GenericRepository<UserMaster>().Update(user);

                    var roles = uow.GenericRepository<UserRole>().Table.Where(x => x.UserMasterId == userVM.UserID);

                    foreach (var r in roles)
                    {
                        uow.GenericRepository<UserRole>().Delete(r);
                    }

                    uow.GenericRepository<UserRole>().Insert(new UserRole
                    {
                        UserMasterId = userVM.UserID,
                        RoleId = userVM.RoleId,
                        IsEnable = true,
                        CreatedBy = userVM.UpdatedBy,
                        CreatedDate = userVM.UpdatedDate
                    });

                    uow.SaveChanges();

                    //Insert Audit Log
                    {
                        var difference = Common.Classes.Common.DetailedCompare<EF.UserMaster>(olduser, user);
                        List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();

                        EF.AuditLog auditLog = new EF.AuditLog()
                        {
                            AuditType = (int)Enumeration.AuditType.Update,
                            ActionId = (int)Enumeration.CorrespondenceAction.UpdateUserMaster,
                            PK = user.ID.ToString(),
                            UserId = Common.Globals.User.ID,
                            TableName = "User Master",
                            Reference = user.FullName,
                            UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                            AuditLogDetails = difference
                        };
                        auditLogsService.AddAuditLog(auditLog);
                    }
                    uow.Commit();

                    return user;
                }
                else
                    throw new Exception("User not found to update.");
            }
            catch (Exception ex)
            {
                uow.Rollback();
                throw ex;
            }
        }

        public bool DeleteUser(int id)
        {
            var user = GetUserById(id);
            if (user != null)
            {
                user.IsEnable = false;

                uow.GenericRepository<UserMaster>().Update(user);
                uow.SaveChanges();

                return true;
            }
            else
                throw new Exception("User not found to delete.");
        }

        public List<Role> GetRoles()
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();


            return uow.GenericRepository<Role>().Table.Where(x => x.IsEnable == true && assignedLocationIds.Contains((int)x.LocationId)).ToList();

        }

        public Role GetRoleById(int id)
        {
            return uow.GenericRepository<Role>().GetById(id);
        }

        public Role AddRole(AddRoleVM roleVM)
        {
            Role role = new Role
            {
                RoleName = roleVM.RoleName,
                RoleDescription = roleVM.RoleDescription,
                IsEnable = true,
                CreatedAt = roleVM.CreatedDate,
                CreatedBy = roleVM.CreatedBy,
                LocationId = roleVM.LocationId
            };

            uow.GenericRepository<Role>().Insert(role);
            uow.SaveChanges();

            return role;

        }

        public Role UpdateRole(AddRoleVM roleVM)
        {
            var role = GetRoleById(roleVM.RoleID);
            if (role != null)
            {
                role.RoleName = roleVM.RoleName;
                role.RoleDescription = roleVM.RoleDescription;
                role.UpdatedAt = roleVM.UpdatedDate;
                role.UpdatedBy = roleVM.UpdatedBy;

                uow.GenericRepository<Role>().Update(role);
                uow.SaveChanges();

                return role;
            }
            else
                throw new Exception("Role not found to update.");
        }

        public bool DeleteRole(int id)
        {
            var role = GetRoleById(id);
            if (role != null)
            {
                role.IsEnable = false;

                uow.GenericRepository<Role>().Update(role);
                uow.SaveChanges();

                return true;
            }
            else
                throw new Exception("Role not found to delete.");
        }

        public List<MenuVM> GetMenus(int roleId)
        {
            var db = uow.Context;
            List<MenuVM> menus = new List<MenuVM>();

            var allMainMenu = uow.GenericRepository<MainMenu>().Table.Where(x => x.IsEnable == true).ToList();

            foreach (var m in allMainMenu)
            {
                MenuVM menuVM = new MenuVM();
                menuVM.MainMenuId = m.MainMenuId;
                menuVM.MainMenuName = m.MenuName;

                menuVM.SubMenuList = uow.GenericRepository<SubMenu>().Table.Where(x =>
                x.IsEnable == true && x.ShouldDisplay == true
                && x.MainMenuId == m.MainMenuId).Select(
                    x => new SubMenuVM
                    {
                        SubMenuId = x.SubMenuId,
                        DisplayName = x.DisplayName,
                        ControllerName = x.ControllerName,
                        ActionName = x.ActionName,
                        IsChecked = db.RoleRights.Any(y => y.SubMenuId == x.SubMenuId && y.RoleId == roleId)
                    }).ToList();

                menus.Add(menuVM);
            }

            return menus;
        }


        public bool SaveRoleRights(RoleRightsVM rightsVM)
        {
            bool ret = false;
            try
            {
                uow.CreateTransaction();
                var rights = uow.GenericRepository<RoleRight>().Table.Where(x => x.IsEnable == true && x.RoleId == rightsVM.RoleId);

                // Capture old rights before changes for audit trail
                var oldSubMenuIds = rights.Select(x => x.SubMenuId).OrderBy(x => x).ToList();

                foreach (var r in rights)
                {
                    uow.GenericRepository<RoleRight>().Delete(r);
                }

                foreach (var vm in rightsVM.MenuList)
                {
                    foreach (var sm in vm.SubMenuList.Where(x => x.IsChecked == true))
                    {
                        uow.GenericRepository<RoleRight>().Insert(
                            new RoleRight
                            {
                                RoleId = rightsVM.RoleId,
                                SubMenuId = sm.SubMenuId,
                                IsEnable = true,
                                CreatedDate = rightsVM.CreatedDate,
                                CreatedBy = rightsVM.CreatedBy
                            });
                    }
                }

                uow.SaveChanges();

                // Capture new rights after changes
                var newSubMenuIds = uow.GenericRepository<RoleRight>().Table
                    .Where(x => x.IsEnable == true && x.RoleId == rightsVM.RoleId)
                    .Select(x => x.SubMenuId)
                    .ToList()
                    .OrderBy(x => x)
                    .ToList();

                // Insert Audit Log for Role Rights change
                {
                    var role = uow.GenericRepository<Role>().GetById(rightsVM.RoleId);

                    var oldSet = new HashSet<int>(oldSubMenuIds);
                    var newSet = new HashSet<int>(newSubMenuIds);
                    var removedIds = oldSet.Except(newSet).ToList();
                    var addedIds = newSet.Except(oldSet).ToList();

                    var allChangedIds = removedIds.Concat(addedIds).Distinct().ToList();
                    var subMenusLookup = uow.GenericRepository<SubMenu>().Table
                        .Where(s => allChangedIds.Contains(s.SubMenuId))
                        .ToDictionary(s => s.SubMenuId, s => s.DisplayName);

                    var auditDetails = new List<EF.AuditLogDetail>();

                    foreach (var id in removedIds)
                    {
                        auditDetails.Add(new EF.AuditLogDetail
                        {
                            PropertyName = subMenusLookup.ContainsKey(id) ? subMenusLookup[id] : ("SubMenu:" + id),
                            OldValue = "Allowed",
                            NewValue = "Revoked"
                        });
                    }

                    foreach (var id in addedIds)
                    {
                        auditDetails.Add(new EF.AuditLogDetail
                        {
                            PropertyName = subMenusLookup.ContainsKey(id) ? subMenusLookup[id] : ("SubMenu:" + id),
                            OldValue = "Revoked",
                            NewValue = "Allowed"
                        });
                    }

                    var auditLog = new EF.AuditLog()
                    {
                        AuditType = (int)Enumeration.AuditType.Update,
                        ActionId = (int)Enumeration.CorrespondenceAction.UpdateRoleRights,
                        PK = rightsVM.RoleId.ToString(),
                        UserId = Common.Globals.User.ID,
                        TableName = "RoleRights",
                        Reference = role != null ? role.RoleName : ("RoleId:" + rightsVM.RoleId),
                        UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                        AuditLogDetails = auditDetails
                    };
                    auditLogsService.AddAuditLog(auditLog);
                }

                uow.Commit();

                ret = true;

            }
            catch (Exception ex)
            {
                uow.Rollback();
            }

            return ret;
        }

        public UserRole GetUserRoleByUserId(int userId)
        {
            return uow.GenericRepository<UserRole>().Table.FirstOrDefault(x => x.UserMasterId == userId);
        }
        //api services && Student Portal
        public bool UnActivePerson(int PersonId)
        {

            var user = uow.GenericRepository<UserMaster>().Table.Where(x => x.PersonID == PersonId).FirstOrDefault();
            if (user != null)
            {
                user.IsActive = false;
                uow.SaveChanges();
                return true;
            }
            return false;
        }
        public bool updateImage(int Id, HttpPostedFileBase file)
        {
            var user = uow.GenericRepository<UserMaster>().GetById(Id);
            if (user == null)
            {
                throw new Exception("Not Found!");
            }

            var fileUpload = new FileUpload();
            var path = fileUpload.Upload(file, "/Assets/Images/Student/Uploads");

            user.ImageUrl = path.ServerPath;
            uow.SaveChanges();
            return true;
        }
        public LocatonRightsVM GetAssignedLocation(int id)
        {
            var db = uow.Context;


            var allAssignedLocations = uow.GenericRepository<UserMaster>().Table.Where(x => x.IsEnable == true && x.ID == id).Select(x => new LocatonRightsVM
            {
                AssignedLocations = x.AssignedLocation
            }).FirstOrDefault();


            return allAssignedLocations;
        }
        public bool SaveLocationRights(int ID, string AssignedLocation)
        {
            bool ret = false;
            try
            {
                uow.CreateTransaction();
                var rights = uow.GenericRepository<UserMaster>().Table.Where(x => x.ID == ID).FirstOrDefault();
                if (AssignedLocation == null || AssignedLocation == "")
                {
                    rights.AssignedLocation = "0";
                    rights.LastLocationId = 0;
                }
                else
                {
                    rights.AssignedLocation = AssignedLocation;
                    var locationIds = AssignedLocation.Split(',').Select(int.Parse).ToList();
                    rights.LastLocationId = locationIds.First();
                }
                uow.GenericRepository<UserMaster>().Update(rights);

                uow.SaveChanges();
                uow.Commit();
                ret = true;

            }
            catch (Exception ex)
            {
                uow.Rollback();
            }

            return ret;
        }


        //api's
        public ApiResponse<UserMasterViewModel> GetById(int Id)
        {

            var response = new ApiResponse<UserMasterViewModel>();
            try
            {
                var data = uow.GenericRepository<UserMaster>().Table.Where(x => x.ID == Id).Select(x =>
                      new UserMasterViewModel
                      {
                          Id = x.ID,
                          FullName = x.FullName,
                          Email = x.Email,
                          Nationality = x.Person.Nationality,
                          Phone = x.Phone,
                          DOB = x.DOB,
                          University = x.Person.Universiry,
                          ImageUrl = x.ImageUrl ?? "/Assets/dist/img/profile.png"


                      }).FirstOrDefault();


                response.Code = (int)HttpStatusCode.OK;
                response.Success = true;
                response.Message = "success";
                response.Data = data;
                return response;
            }
            catch (Exception ex)
            {
                response.Code = (int)HttpStatusCode.BadRequest;
                response.Success = false;
                response.Message = ex.Message;
                response.Data = null;
                return response;
            }

        }
        public ApiResponse<UserMasterViewModel> updateImage(int Id, HttpFileCollection files)
        {

            var response = new ApiResponse<UserMasterViewModel>();
            try
            {
                var user = uow.GenericRepository<UserMaster>().GetById(Id);

                if (files.Count == 0)
                {
                    response.Code = (int)HttpStatusCode.BadRequest;
                    response.Message = "File Not Found!";
                    response.Success = true;
                    response.Data = null;
                    return response;
                }
                if (user == null)
                {
                    throw new Exception("Not Found!");
                }
                var uploadDirectory = "/Assets/Images/Student/Uploads";
                var file = files[0];
                string filePath = string.Empty;

                string path = System.Configuration.ConfigurationSettings.AppSettings["UploadedImages"] + uploadDirectory;


                string fileName = DateTime.Now.ToString("[dd_MMM_yyyy]_[HH_mm_ss]_") + Path.GetFileName(file.FileName);

                filePath = Path.Combine(path, fileName);
                string extension = Path.GetExtension(file.FileName);
                file.SaveAs(filePath);

                user.ImageUrl = Helper.GetBaseUrlWeb(HttpContext.Current.Request) + uploadDirectory + "/" + fileName;

                uow.SaveChanges();
                response.Code = (int)HttpStatusCode.OK;
                response.Success = true;
                response.Message = "success";
                response.Data = null;
                return response;
            }
            catch (Exception ex)
            {
                response.Code = (int)HttpStatusCode.BadRequest;
                response.Success = false;
                response.Message = ex.Message;
                response.Data = null;
                return response;
            }
        }
    }
}
