using PMS.EF;
using PMS.Services.Services.UserManage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PMS.Common.Classes;
using PMS.Common.Filters;
using PMS.Services.Services.Account;
using PMS.Areas.Student.Classes;
using System.IO;

namespace PMS.Areas.Student.Controllers
{
    [AuthorizeUser]
    [AllowUserFilter]
    public class UserController : Controller
    {
        private readonly IUserManageService UserManageService;
        private readonly IAccountService AccountService;
        public UserController(IUserManageService _UserManageService,IAccountService _AccountService)
        {
            UserManageService = _UserManageService;
            AccountService = _AccountService;
        }
        // GET: Student/User
        public ActionResult Profile()
        {
            var UserId = PMS.Common.Globals.User.ID;
           var user= UserManageService.GetUserById(UserId);
            ViewBag.error = TempData["error"];
            ViewBag.success = TempData["success"];
            return View(user);
        }
        [HttpPost]
        public ActionResult updateImage(HttpPostedFileBase file)
        {
            var acceptableImages = new[] { ".jpg", ".jpeg", ".png" };
            var fileExtension=Path.GetExtension(file.FileName).ToLower();

            if (!acceptableImages.Contains(fileExtension))
            {
                TempData["error"] = "This type of file is note allowed";
                return RedirectToAction("Profile");
            }

            var response = UserManageService.updateImage(PMS.Common.Globals.User.ID,file);
            if (response == true)
            {

                TempData["success"] = "Picture Uploaded Successfully";
                return RedirectToAction("Profile");
            }
            else
            {
                TempData["error"] = "Something Went Wrong";
                return RedirectToAction("Profile");
            }


        }
        [HttpGet]
        public JsonResult GetProfileImage()
        {
            var response=  UserManageService.GetById(PMS.Common.Globals.User.ID);

            return Json(new { Data = response.Data }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult ChangePassword()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(string oldpassword, string ConfirmPassword)
        {
            if (oldpassword != null && ConfirmPassword != null)
            {
                var restul = AccountService.ChangePassword(oldpassword, ConfirmPassword);
                if (restul == true)
                {
                    ViewBag.success = "Password has Changed Successfully!";
                    return RedirectToAction("Login","Account",new {area="" });

                }
                else
                {
                    ViewBag.error = "Old Password is not correct!";
                }

            }
            return View();
        }

    }
}