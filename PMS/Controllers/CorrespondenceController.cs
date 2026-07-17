using PMS.Classes;
using PMS.Common.Classes;
using PMS.Common.Filters;
using PMS.DTO.ViewModels.CorrespondenceViewModels;
using PMS.EF;
using PMS.Services.Services.Correspondence;
using PMS.Services.Services.Email;
using PMS.Services.Services.Person;
using PMS.Services.Services.Setup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PMS.Controllers
{
    [AuthorizeUser]
    public class CorrespondenceController : BaseController
    {
        private readonly ICorrespondenceService correspondenceService;
        private readonly IEmailService emailService;
        private ISetupService setupService;
        private readonly IPersonService personService;

        public CorrespondenceController(ICorrespondenceService _correspondenceService, IEmailService _emailService, ISetupService _setupService, IPersonService _personService)
        {
            correspondenceService = _correspondenceService;
            emailService = _emailService;
            setupService = _setupService;
            personService = _personService;
        }

        [AuthorizeUser(Roles = AppUserRoles.View_EmailSenders)]
        public ActionResult EmailSenders()
        {
            ViewBag.Senders = correspondenceService.GetEmailSenders();

            return View();
        }

        [AuthorizeUser(Roles = AppUserRoles.Export_EmailSenders)]
        public void ExportEmailSenders()
        {
            var data = correspondenceService.GetEmailSenders();
            ExcelHelper.ExportToExcel(Response, data, "EmailSendersList");
        }

        [AuthorizeUser(Roles = AppUserRoles.Add_EmailSenders)]
        public ActionResult AddEmailSenders(int? id)
        {
            AddEmailSendersVM model = new AddEmailSendersVM();

            if (id > 0)
            {
                var sender = correspondenceService.GetEmailSenderById(Convert.ToInt32(id));
                if (sender != null)
                {
                    model = new AddEmailSendersVM
                    {
                        EmailSenderID = sender.EmailSenderID,
                        EmailSenderName = sender.EmailSenderName,
                        EmailSenderDescription = sender.Description,
                        FromAddress = sender.FromAddress,
                        FromName = sender.FromName,
                        EmailPassword = sender.EmailSenderPassword,
                        ConfirmEmailPassword = sender.EmailSenderPassword,
                        CC = sender.CC,
                        BCC = sender.BCC,
                        ReplyToAddress = sender.ReplyToAddress,
                        EmailSignature = sender.EmailSignature,
                        IsActive = sender.IsActive
                    };
                }
            }
            else
            {
                model.IsActive = true;
            }

            return View(model);
        }
        

        [HttpPost, ValidateAntiForgeryToken, ValidateInput(false)]
        public ActionResult AddEmailSenders(AddEmailSendersVM model)
        {
            try
            {
                bool IsActive = (Request.Form["IsActive"] != null);
                model.IsActive = IsActive;

                ModelState.Remove("IsActive");

                if (ModelState.IsValid)
                {
                    if (model.EmailSenderID == 0)
                    {
                        model.CreatedBy = PMS.Common.Globals.User.Email;
                        model.CreatedDate = DateTime.Now;

                        if (correspondenceService.AddEmailSender(model).EmailSenderID > 0)
                        {
                            TempData["success"] = "Email sender saved successfully.";
                            return RedirectToAction("EmailSenders");
                        }
                        else
                        {
                            ViewBag.error = "Something went wrong. Sender not saved.";
                        }
                    }
                    else
                    {
                        model.UpdatedBy = PMS.Common.Globals.User.Email;
                        model.UpdatedDate = DateTime.Now;

                        correspondenceService.UpdateEmailSender(model);
                        TempData["success"] = "Email sender updated successfully.";
                        return RedirectToAction("EmailSenders");
                    }
                }
                else
                {
                    ViewBag.error = "Model error.";
                }
            }
            catch (Exception ex)
            {
                ViewBag.error = ex.Message;
            }

            return View(model);
        }

        [HttpPost]
        [AuthorizeUser(Roles = AppUserRoles.Delete_EmailSenders)]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteEmailSender(int id)
        {
            if (correspondenceService.DeleteEmailSender(id))
                TempData["success"] = "Sender deleted successfully.";
            else
                TempData["error"] = "Error : Unable to delete sender at this moment.";

            return RedirectToAction("EmailSenders");
        }

        [AuthorizeUser(Roles = AppUserRoles.View_EmailMessages)]
        public ActionResult EmailMessages()
        {
            ViewBag.Messages = correspondenceService.GetEmailMessages();

            return View();
        }


        [AuthorizeUser(Roles = AppUserRoles.Export_EmailMessages)]
        public void ExportEmailMessages()
        {
            var data = correspondenceService.GetEmailMessages();
            ExcelHelper.ExportToExcel(Response, data, "EmailMessagesList");
        }


        [AuthorizeUser(Roles = AppUserRoles.Add_EmailMessages)]
        public ActionResult AddEmailMessages(int? id)
        {
            AddEmailMessageVM model = new AddEmailMessageVM();
            //ViewBag.ActionId = new SelectList(correspondenceService.GetCorrespondenceActions(), "Id", "ActionName");

            if (id > 0)
            {
                var message = correspondenceService.GetEmailMessageById(Convert.ToInt32(id));
                if (message != null)
                {
                    ViewBag.ActionId = new SelectList(correspondenceService.GetCorrespondenceActions(), "Id", "ActionName", message.ActionId);
                    ViewBag.LocationId = new SelectList(setupService.GetLocations(), "LocationID", "LocationName",message.LocationId);

                    model = new AddEmailMessageVM
                    {
                        EmailMessageID = message.EmailMessageID,
                        EmailSenderID = message.EmailMessageSenderID,
                        EmailMessageName = message.EmailMessageName,
                        EmailMessageDescription = message.EmailMessageDescription,
                        EmailMessageSubject = message.EmailMessageSubject,
                        EmailMessageBody = message.EmailMessageBody,
                        IsActive = message.IsActive,
                        LocationId = message.LocationId,
                    };
                }
            }
            else
            {
                model.IsActive = true;
                ViewBag.ActionId = new SelectList(correspondenceService.GetCorrespondenceActions(), "Id", "ActionName");
                ViewBag.LocationId = new SelectList(setupService.GetLocations(), "LocationID", "LocationName");
            }

            model.EmailMessageSendersList = correspondenceService.SendersList();
            return View(model);
        }


        [AuthorizeUser(Roles = AppUserRoles.Add_EmailMessages)]

        [HttpPost, ValidateAntiForgeryToken, ValidateInput(false)]
        public ActionResult AddEmailMessages(AddEmailMessageVM model)
        {
            try
            {
                bool IsActive = (Request.Form["IsActive"] != null);
                model.IsActive = IsActive;

                ModelState.Remove("IsActive");

                if (ModelState.IsValid)
                {
                    if (model.EmailMessageID == 0)
                    {
                        model.CreatedBy = PMS.Common.Globals.User.Email;
                        model.CreatedDate = DateTime.Now;

                        if (correspondenceService.AddEmailMessage(model).EmailMessageID > 0)
                        {
                            TempData["success"] = "Email message saved successfully.";
                            return RedirectToAction("EmailMessages");
                        }
                        else
                        {
                            ViewBag.error = "Something went wrong. Message not saved.";
                        }
                    }
                    else
                    {
                        model.UpdatedBy = PMS.Common.Globals.User.Email;
                        model.UpdatedDate = DateTime.Now;

                        correspondenceService.UpdateEmailMessage(model);
                        TempData["success"] = "Email message updated successfully.";
                        return RedirectToAction("EmailMessages");
                    }
                }
                else
                {
                    ViewBag.error = "Model error.";
                }
            }
            catch (Exception ex)
            {
                ViewBag.error = ex.Message;
            }

            model.EmailMessageSendersList = correspondenceService.SendersList();
            ViewBag.ActionId = new SelectList(correspondenceService.GetCorrespondenceActions(), "Id", "ActionName", model.ActionId);

            return View(model);
        }

        [HttpPost]
        [AuthorizeUser(Roles = AppUserRoles.Delete_EmailMessages)]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteEmailMessage(int id)
        {
            if (correspondenceService.DeleteEmailMessage(id))
                TempData["success"] = "Message deleted successfully.";
            else
                TempData["error"] = "Error : Unable to delete message at this moment.";

            return RedirectToAction("EmailMessages");
        }
      

        [AuthorizeUser(Roles = AppUserRoles.View_EmailSettings)]
        public ActionResult EmailSettings()
        {
            //AddEmailSettingsVM model = correspondenceService.GetEmailSettings().FirstOrDefault();
            List<AddEmailSettingsVM> model = correspondenceService.GetEmailSettings();
            if (model == null)
            {
                //model = new AddEmailSettingsVM();
                return View();
            }

            return View(model);
        }

        [AuthorizeUser(Roles = AppUserRoles.View_EmailSettings)]
        public ActionResult AddEmailSettings(int? id)
        {
            ViewBag.LocationId = new SelectList(setupService.GetLocations(), "LocationID", "LocationName");
            AddEmailSettingsVM model = new AddEmailSettingsVM();

            if (id > 0)
            {
                var emailSetting = correspondenceService.GetEmailSettingsById(Convert.ToInt32(id));

                if (emailSetting != null)
                {
                    model = new AddEmailSettingsVM
                    {
                        EmailSettingsID = id ?? 0,
                        EmailServer = emailSetting.EmailServer,
                        EmailServerPort = emailSetting.EmailServerPort,
                        EnableEmail = emailSetting.EnableEmail,
                        UseSSL = emailSetting.UseSSL
                    };

                }
                else
                {
                    TempData["error"] = "Emai Setting not found to update;";
                    return RedirectToAction("AddEmailSettings");
                }
            }
            return View(model);

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeUser(Roles = AppUserRoles.Update_EmailSettings)]
        public ActionResult AddEmailSettings(AddEmailSettingsVM model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (model.EmailSettingsID == 0)
                    {
                        model.CreatedBy = PMS.Common.Globals.User.Email;
                        model.CreatedDate = DateTime.Now;

                        if (correspondenceService.AddEmailSettings(model).EmailSettingsID > 0)
                        {
                            TempData["success"] = "Email settings saved successfully.";

                            return RedirectToAction("EmailSettings");
                        }
                        else
                        {
                            ViewBag.error = "Something went wrong. Email settings not saved.";
                        }
                    }
                    else
                    {
                        model.UpdatedBy = PMS.Common.Globals.User.Email;
                        model.UpdatedDate = DateTime.Now;

                        correspondenceService.UpdateEmailSettings(model);
                        TempData["success"] = "Email settings updated successfully.";
                        return RedirectToAction("EmailSettings");
                    }
                }
                else
                {
                    ViewBag.error = "Model error.";
                }
            }
            catch (Exception ex)
            {
                ViewBag.error = ex.Message;
            }

            return View(model);
        }

        [HttpPost]
        [AuthorizeUser(Roles = AppUserRoles.Update_EmailSettings)]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteEmailSettings(int id)
        {
            if (correspondenceService.DeleteEmailSettings(id))
                TempData["success"] = "Settings deleted successfully.";
            else
                TempData["error"] = "Error : Unable to delete Schedule at this moment.";

            return RedirectToAction("EmailSettings");
        }


        [AuthorizeUser(Roles = AppUserRoles.TestEmailSendersSettings)]
        public ActionResult SendTestEmail(string ToEmail, string EmailBody, string Subject, int SenderEmail)
        {

            var testemail = emailService.SendEmail(Subject, EmailBody, false, ToEmail, SenderEmail, true);

            return Json(new { data = testemail }, JsonRequestBehavior.AllowGet);
        }


        [AuthorizeUser(Roles = AppUserRoles.View_EmailSchedulers)]
        public ActionResult EmailSchedulers()
        {
            ViewBag.Schedulers = correspondenceService.GetEmailSchedulers();

            return View();
        }


        [AuthorizeUser(Roles = AppUserRoles.Add_EmailScheduler)]
        public ActionResult AddEmailSchedule(int? id)
        {
            AddEmailSchedulerVM model = new AddEmailSchedulerVM();
            if (id > 0)
            {
                var emailScheduler = correspondenceService.GetEmailSchedulerById(Convert.ToInt32(id));
                if (emailScheduler != null)
                {
                    model = new AddEmailSchedulerVM
                    {
                        ScheduleName = emailScheduler.ScheduleName,
                        Type = emailScheduler.Type,
                        SubType = emailScheduler.SubType,
                        TaskName = emailScheduler.TaskName,
                        Recurrence = emailScheduler.Recurrence,
                        LastRun = emailScheduler.LastRun,
                        NextRun = emailScheduler.NextRun,
                        ExecutionTime = emailScheduler.ExecutionTime,
                        IsActive = emailScheduler.IsActive,
                        EmailSenderID = emailScheduler.EmailSenderID,
                        EmailMessageBody = emailScheduler.EmailMessageBody,
                        CreatedDate = emailScheduler.CreatedDate,
                        CreatedBy = emailScheduler.CreatedBy,
                        UpdatedDate = emailScheduler.UpdatedDate,
                        UpdatedBy = emailScheduler.UpdatedBy,
                        SendTo = emailScheduler.SendTo,
                        LocationId = emailScheduler.LocationId ?? 0
                    };

                    ViewBag.SendTo = new SelectList(personService.GetPersons().Select(x => new { x.Email, EmailDisplay = x.Code + ": " + x.Email }), "Email", "EmailDisplay");
                    ViewBag.SelectedEmails = model.SendTo.ToArray();

                }
            }
            else
            {
                ViewBag.SendTo = new MultiSelectList(personService.GetPersons().Select(x => new { x.Email, EmailDisplay = x.Code + ": " + x.Email }), "Email", "EmailDisplay");

                model.IsActive = true;
            }
            ViewBag.LocationId = new SelectList(setupService.GetLocations(), "LocationID", "LocationName");

            model.EmailMessageSendersList = correspondenceService.SendersList();

            return View(model);
        }


        [AuthorizeUser(Roles = AppUserRoles.Add_EmailScheduler)]
        [HttpPost, ValidateAntiForgeryToken, ValidateInput(false)]
        public ActionResult AddEmailSchedule(AddEmailSchedulerVM model)
        {
            try
            {
                bool IsActive = (Request.Form["IsActive"] != null);
                bool Recurrence = (Request.Form["Recurrence"] != null);
                model.IsActive = IsActive;
                model.Recurrence = Recurrence;

                ModelState.Remove("IsActive");
                ModelState.Remove("Recurrence");

                if (ModelState.IsValid)
                {
                    if (model.SendTo != null && model.SendTo.Count > 0)
                    {
                        model.SendToEmails = string.Join(",", model.SendTo);
                    }
                    if (model.ID == 0)
                    {
                        model.CreatedBy = PMS.Common.Globals.User.Email;
                        model.CreatedDate = DateTime.Now;

                        if (correspondenceService.AddEmailSchedule(model).ID > 0)
                        {
                            TempData["success"] = "Email schedule saved successfully.";
                            return RedirectToAction("EmailSchedulers");
                        }
                        else
                        {
                            ViewBag.error = "Something went wrong. Sender not saved.";
                        }
                    }
                    else
                    {
                        model.UpdatedBy = PMS.Common.Globals.User.Email;
                        model.UpdatedDate = DateTime.Now;

                        correspondenceService.UpdateEmailSchedule(model);
                        TempData["success"] = "Email schedule updated successfully.";
                        return RedirectToAction("EmailSchedulers");
                    }
                }
                else
                {
                    ViewBag.error = "Model error.";
                }
            }
            catch (Exception ex)
            {
                ViewBag.error = ex.Message;
            }

            return View(model);
        }

        [HttpPost]
        [AuthorizeUser(Roles = AppUserRoles.Delete_EmailScheduler)]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteEmailSchedule(int id)
        {
            if (correspondenceService.DeleteEmailSchedule(id))
                TempData["success"] = "Schedule deleted successfully.";
            else
                TempData["error"] = "Error : Unable to delete Schedule at this moment.";

            return RedirectToAction("EmailSchedulers");
        }

        public ActionResult EmailCheck(int? id)
        {
            AddEmailSchedulerVM model = new AddEmailSchedulerVM();
            if (id > 0)
            {
                var emailScheduler = correspondenceService.ManuallySendEmail(Convert.ToInt32(id));
                TempData["success"] = "Emails send successfully.";
                return RedirectToAction("EmailSchedulers");
            }
            else
            {
                ViewBag.SendTo = new MultiSelectList(personService.GetPersons().Select(x => new { x.Email, EmailDisplay = x.Code + ": " + x.Email }), "Email", "EmailDisplay");

                model.IsActive = true;
            }
            model.EmailMessageSendersList = correspondenceService.SendersList();

            return View(model);
        }


    }
}