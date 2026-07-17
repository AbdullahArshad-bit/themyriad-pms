using PMS.Common.Filters;
using PMS.Common.Classes;
using PMS.DTO.ViewModels.UserManageViewModels;
using PMS.Services.Services.UserManage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PMS.Services.Services.Setup;

namespace PMS.Controllers
{
    public class UserManageController : BaseController
    {
        private readonly IUserManageService userManageService;
        private readonly ISetupService setupSevice;
        public UserManageController(IUserManageService _userManageService, ISetupService _setupService)
        {
            userManageService = _userManageService;
            setupSevice = _setupService;
        }

        [AuthorizeUser(Roles = AppUserRoles.view_users)]
        public ActionResult Users()
        {
            ViewBag.Users = userManageService.GetUsers();
            return View();
        }

        [AuthorizeUser(Roles = AppUserRoles.add_users)]
        public ActionResult AddUser(int? id)
        {
            AddUserVM userVM = new AddUserVM();
            userVM.DOB = DateTime.Now;
            userVM.IsActive = true;

            ViewBag.LocationId = new SelectList(setupSevice.GetLocations(), "LocationID", "LocationName");


            if (id > 0)
            {
                var user = userManageService.GetUserById(Convert.ToInt32(id));
                int userRoleId = 0;
                var userRole = userManageService.GetUserRoleByUserId(Convert.ToInt32(id));
                userRoleId = userRole == null ? 0 : userRole.RoleId;
                ViewBag.LocationId = new SelectList(setupSevice.GetLocations(), "LocationID", "LocationName", user.LastLocationId);


                if (user != null)
                {
                    userVM = new AddUserVM
                    {
                        UserID = user.ID,
                        RoleId = userRoleId,
                        FullName = user.FullName,
                        Email = user.Email,
                        Password = PMS.Common.Security.StringCipher.Decrypt(user.Password),
                        ConfirmPassword = PMS.Common.Security.StringCipher.Decrypt(user.Password),
                        Gender = user.Gender,
                        DOB = Convert.ToDateTime(user.DOB),
                        Phone = user.Phone,
                        IsActive = user.IsActive,
                        Address = user.Address,
                        Department = user.Department,
                        Designation = user.Designation
                    };
                }
                else
                {
                    TempData["error"] = "User not found to update;";
                    return RedirectToAction("Users");
                }
            }

            userVM.RolesList = userManageService.GetRoles();

            return View(userVM);
        }

        [AuthorizeUser(Roles = AppUserRoles.add_users)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddUser(AddUserVM userVM)
        {
            try
            {
                bool IsActive = (Request.Form["IsActive"] != null);
                userVM.IsActive = IsActive;

                if (userVM.UserID > 0)
                {
                    userVM.UpdatedBy = PMS.Common.Globals.User.Email;
                    userVM.UpdatedDate = DateTime.Now;

                    userManageService.UpdateUser(userVM);
                    TempData["success"] = "User updated successfully.";
                }
                else
                {
                    userVM.CreatedBy = PMS.Common.Globals.User.Email;
                    userVM.CreatedDate = DateTime.Now;

                    if (userManageService.AddUser(userVM).ID > 0)
                    {
                        TempData["success"] = "User saved successfully.";
                    }
                    else
                    {
                        TempData["error"] = "User not saved.";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
            }

            return RedirectToAction("Users");
        }

        [AuthorizeUser(Roles = AppUserRoles.delete_users)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteUser(int id)
        {
            if (userManageService.DeleteUser(id))
                TempData["success"] = "User deleted successfully.";
            else
                TempData["error"] = "Error : Unable to delete user at this moment.";

            return RedirectToAction("Users");
        }

        [AuthorizeUser(Roles = AppUserRoles.view_roles)]
        public ActionResult Roles()
        {
            ViewBag.LocationId = new SelectList(setupSevice.GetLocations(), "LocationID", "LocationName");
            ViewBag.Roles = userManageService.GetRoles();
            return View();


        }

        [AuthorizeUser(Roles = AppUserRoles.add_roles)]
        public ActionResult AddRole(int? id)
        {

            ViewBag.LocationId = new SelectList(setupSevice.GetLocations(), "LocationID", "LocationName");


            AddRoleVM roleVM = new AddRoleVM();

            if (id > 0)
            {
                var role = userManageService.GetRoleById(Convert.ToInt32(id));

                roleVM.RoleID = role.RoleId;
                roleVM.RoleName = role.RoleName;
                roleVM.RoleDescription = role.RoleDescription;
            }

            return View(roleVM);
        }

        [AuthorizeUser(Roles = AppUserRoles.add_roles)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddRole(AddRoleVM roleVM)
        {
            if (roleVM.RoleID > 0)
            {
                roleVM.UpdatedBy = PMS.Common.Globals.User.Email;
                roleVM.UpdatedDate = DateTime.Now;

                userManageService.UpdateRole(roleVM);
                TempData["success"] = "Role updated successfully.";
            }
            else
            {
                roleVM.CreatedBy = PMS.Common.Globals.User.Email;
                roleVM.CreatedDate = DateTime.Now;

                if (userManageService.AddRole(roleVM).RoleId > 0)
                {
                    TempData["success"] = "Role saved successfully.";
                }
                else
                {
                    TempData["error"] = "Role not saved.";
                }
            }

            return RedirectToAction("Roles");
        }

        [AuthorizeUser(Roles = AppUserRoles.delete_roles)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteRole(int id)
        {
            if (userManageService.DeleteRole(id))
                TempData["success"] = "Role deleted successfully.";
            else
                TempData["error"] = "Error : Unable to delete role at this moment.";

            return RedirectToAction("Roles");
        }

        [AuthorizeUser(Roles = AppUserRoles.manage_role_rights)]
        public ActionResult RoleRights(int id)
        {
            RoleRightsVM model = new RoleRightsVM
            {
                RoleId = id,
                UserMasterId = Common.Globals.User.ID,
                MenuList = userManageService.GetMenus(id)
            };

            return View(model);
        }

        [AuthorizeUser(Roles = AppUserRoles.manage_role_rights)]
        [HttpPost]
        public ActionResult RoleRights(RoleRightsVM model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    model.CreatedBy = Common.Globals.User.Email;
                    model.CreatedDate = DateTime.Now;

                    if (userManageService.SaveRoleRights(model))
                    {
                        TempData["success"] = "Role rights saved successfully.";
                    }
                    else
                    {
                        TempData["error"] = "Error : Unable to save role rights at this moment.";
                    }
                }
                else
                {
                    TempData["error"] = "Error : Model error.";
                }
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
            }

            return RedirectToAction("Roles");
        }
        public ActionResult AddLocationRights(int id)
        {
            var model = new LocatonRightsVM();
            var Locations = userManageService.GetAssignedLocation(id);
            if (Locations != null)
            {
                model = Locations;
            }
            ViewBag.UserID = id;
            ViewBag.res = setupSevice.GetAllLocations();
            return View(model);
        }
        [HttpPost]
        public ActionResult AddLocationRights(int ID, List<string> AssignedLocation)
        {
            var AssignedLocations = AssignedLocation != null ? string.Join(",", AssignedLocation) : "";
            try
            {
                if (ModelState.IsValid)
                {
                    if (userManageService.SaveLocationRights(ID, AssignedLocations))
                    {
                        TempData["success"] = "Location rights saved successfully.";
                    }
                    else
                    {
                        TempData["error"] = "Error : Unable to save location rights at this moment.";
                    }
                }
                else
                {
                    TempData["error"] = "Error : Model error.";
                }
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
            }

            return RedirectToAction("Users");
        }

    }
}