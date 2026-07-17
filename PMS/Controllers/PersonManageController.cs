using PMS.Classes;
using PMS.Common;
using PMS.Common.Classes;
using PMS.Common.Filters;
using PMS.DTO.ViewModels.PersonManageViewModels;
using PMS.EF;
using PMS.Services.Services.Person;
using PMS.Services.Services.Booking;
using PMS.Services.Services.Setup;
using PMS.Services.Services.AuditLogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PMS.Repository.UnitOfWork;
using System.Transactions;
using System.Threading.Tasks;
using static PMS.Common.Classes.Enumeration;


namespace PMS.Controllers
{
    [AuthorizeUser]
    public class PersonManageController : BaseController
    {
        private readonly IPersonService personService;
        private readonly ISetupService setupService;
        private readonly IAuditLogsService auditLogsService;
        private readonly IBookingService bookingService;
        private readonly UnitOfWork<EF.PMSEntities> uow;


        public PersonManageController(IPersonService _personService, ISetupService _setupService, IAuditLogsService _auditLogsService, UnitOfWork<EF.PMSEntities> _uow, IBookingService _bookingService)
        {
            personService = _personService;
            setupService = _setupService;
            auditLogsService = _auditLogsService;
            bookingService = _bookingService;
            uow = _uow;
        }

        [AuthorizeUser(Roles = AppUserRoles.View_Profile)]
        public ActionResult Persons()
        {
            ViewBag.error = TempData["error"];
            ViewBag.success = TempData["success"];
            return View();
        }

        public object LoadPersons(PersonBinding person)
        {
            try
            {
                var result = new PersonResponse();
                result = personService.GetPersonsByPages(person, person.search.value, person.start, person.length, "", person.orderBy, person.orderDir, person.SelectedColumns);
                return Json(new { draw = person.draw, recordsFiltered = result.RecordsFiltered, recordsTotal = result.TotalRecords, data = result.person });
            }

            catch (Exception ex)
            {
                // Handle exceptions here (e.g., log the error)
                return Json(new { error = "An error occurred while retrieving data." });
            }
        }
        public void ExportPersonReport()
        {
            var report = personService.GetPersonsExport().ToList();

            bool includeMercury = report.Any(x => x.Location.LocationID == (int)LocationEnum.Dubai);

            if (includeMercury)
            {
                // Project with MercuryID column included
                var data = report.Select(x => new
                {
                    Location = x.Location.LocationName,
                    MyriadID = x.Code,
                    x.FullName,
                    x.ResidentID,
                    x.ReferralCode,
                    x.Title,
                    x.Gender,
                    x.Email,
                    University = x.University.UniversityName,
                    x.Phone,
                    x.VehicleNumber,
                   
                    // Only populate MercuryID for LocationID 17; others get empty string
                    MercuryID = (x.Location.LocationID == (int)LocationEnum.Dubai) ? x.MercuryID : "",
                    PassportNumber = string.IsNullOrEmpty(x.PassportNumber) || x.PassportNumber == "0" ? "" : x.PassportNumber,
                    ProfileNotes = string.IsNullOrEmpty(x.ProfileNotes) || x.ProfileNotes == "0" ? "" : x.ProfileNotes,
                    UniversityStudentID = string.IsNullOrEmpty(x.UniversityStudentID) || x.UniversityStudentID == "0" ? "" : x.UniversityStudentID,
                    EmergencyContactName = x.EmergencyContacts != null && x.EmergencyContacts.Any() ? x.EmergencyContacts.FirstOrDefault().FullName : "",
                    EmergencyContactPhone = x.EmergencyContacts != null && x.EmergencyContacts.Any() ? x.EmergencyContacts.FirstOrDefault().Phone : ""



                }).ToList();

                ExcelHelper.ExportToExcel(Response, data, "All Person - PMS");
            }
            else
            {
                // Project without the MercuryID column at all
                var data = report.Select(x => new
                {
                    Location = x.Location.LocationName,
                    MyriadID = x.Code,
                    x.FullName,
                    x.ResidentID,
                    x.ReferralCode,
                    x.Title,
                    x.Gender,
                    x.Email,
                    University = x.University.UniversityName,
                    x.Phone,
                    x.VehicleNumber
                }).ToList();

                ExcelHelper.ExportToExcel(Response, data, "All Person - PMS");
            }
            return;
        }


        [AuthorizeUser(Roles = AppUserRoles.Add_Profile)]
        public ActionResult AddPerson(int? id)
        {
            PMS.DTO.ViewModels.PersonManageViewModels.AddPersonVM model = new PMS.DTO.ViewModels.PersonManageViewModels.AddPersonVM();
            bool isPersonAdded = id == null;
            ViewBag.LocationId = new SelectList(setupService.GetLocations(), "LocationID", "LocationName");
            ViewBag.UniversityId = new SelectList("");
            if (id > 0)
            {
                var person = personService.GetPersonById(Convert.ToInt32(id));
                var emergencyContact = person.EmergencyContacts.FirstOrDefault();
                ViewBag.LocationId = new SelectList(setupService.GetLocations(), "LocationID", "LocationName", person.LocationId);
                ViewBag.UniversityId = new SelectList(setupService.GetUniversityListByLoactionId((int)person.LocationId, "en-"), "Id", "UniversityName", person.UniversityId);
                if (person != null)
                {
                    model = new AddPersonVM
                    {
                        PersonID = person.PersonID,
                        Title = person.Title,
                        FullName = person.FullName,
                        Email = person.Email,
                        SecondaryEmail = person.SecondaryEmail,
                        Phone = person.Phone,
                        DOB = person.DOB,
                        Gender = person.Gender,
                        Nationality = person.Nationality,
                        LocationId = person.LocationId ?? 0,
                        Code = person.Code,
                        UniversityId = person.UniversityId ?? 0,
                        ResidentID = person.ResidentID,
                        ReferralCode = person.ReferralCode,
                        VehicleNumber = person.VehicleNumber,
                        ImageUrl = person.ImageUrl,
                        GuardianFullName = emergencyContact?.FullName ?? string.Empty,
                        GuardianPhone = emergencyContact?.Phone ?? string.Empty,
                        GuardianOtherEmail = emergencyContact?.Email ?? string.Empty,
                        GuardianRelation = emergencyContact?.Relation ?? string.Empty,
                        CampusEmail = person.CampusEmail,
                        Religion = person.Religion,
                        ResidentWhatsappNumber = person.WhatsappNumber,
                        MercuryID = person.MercuryID,
                        PassportNumber = person.PassportNumber,
                        ProfileNotes = person.ProfileNotes,
                        UniversityStudentID = person.UniversityStudentID


                    };
                };

            }
            else
            {
                model.DOB = DateTime.Today;
            }
            ViewBag.IsPersonAdded = isPersonAdded;
            return View(model);
        }

        [HttpPost]
        [AuthorizeUser(Roles = AppUserRoles.Add_Profile)]
        [ValidateAntiForgeryToken]
        public ActionResult AddPerson(AddPersonVM personVM, HttpPostedFileBase file)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (personVM.PersonID == 0)
                    {
                        personVM.CreatedBy = PMS.Common.Globals.User.Email;
                        personVM.CreatedDate = DateTime.Now;

                        if (personService.AddPerson(personVM, file).PersonID > 0)
                        {
                            TempData["success"] = "Person saved successfully.";

                            ModelState.Clear();
                            personVM = new AddPersonVM
                            {
                                DOB = DateTime.Now
                            };
                            return RedirectToAction("Persons");
                        }
                        else
                        {
                            ViewBag.error = "Something went wrong. Person not saved.";
                        }
                    }
                    else
                    {
                        personVM.UpdatedBy = PMS.Common.Globals.User.Email;
                        personVM.UpdatedDate = DateTime.Now;
                        personService.UpdatePerson(personVM, file);
                        TempData["success"] = "Person updated successfully.";
                        return RedirectToAction("Persons");
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
            ViewBag.UniversityId = new SelectList(setupService.GetUniversityListByLoactionId((int)personVM.LocationId, "en-"), "Id", "UniversityName", personVM.UniversityId);

            ViewBag.LocationId = new SelectList(setupService.GetLocations(), "LocationID", "LocationName", personVM.LocationId);
            return View(personVM);
        }

        public ActionResult GetPersonCodeByLocationId(int id)
        {
            try
            {
                var data = setupService.GetLocationByID(id);
                var Students = personService.GetPersons().Select(x => new { x.PersonID, x.FullName }).ToList();
                //var maxcode = GetMaxPersonCode(id);
                //string value = String.Format("{0:D4}", maxcode);
                //var Code = "PER-" + data.Prefix + "-" + value;

                var Code = personService.GetMaxPersonCode(id);


                return Json(new { Status = true, Students = Students, Code = Code }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {

                return Json(new { Status = false }, JsonRequestBehavior.AllowGet);

            }
        }

        public static int GetMaxPersonCode(int id)
        {
            PMSEntities db1 = new PMSEntities();
            int code = 0;
            if (db1.People.Where(x => x.Code != null && x.LocationId == id).Count() != 0)
            {
                var nowithGRn = Convert.ToDecimal(db1.People.Where(x => x.Code != null && x.LocationId == id).AsEnumerable().Select(x => new { Number = Convert.ToDecimal(x.Code.Split('-')[2]) }).Max(x => x.Number)) + 1;
                code = (int)nowithGRn;
            }
            else

            {
                code = 1;
            }
            return code;
        }

        [HttpPost]
        [AuthorizeUser(Roles = AppUserRoles.Delete_Profile)]
        [ValidateAntiForgeryToken]
        public ActionResult DeletePerson(int id)
        {
            try
            {
                if (personService.DeletePerson(id))
                {
                    TempData["success"] = "Person deleted successfully.";
                }
                else
                {
                    TempData["error"] = "Error : Unable to delete person.";
                }
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
            }

            return RedirectToAction("Persons");
        }

        [HttpGet]
        [AuthorizeUser(Roles = AppUserRoles.View_PersonRelationshipHistory)]

        public ActionResult PersonRelationshipHistory(int Personid)
        {
            var model = auditLogsService.GetAuditHistoryByPersoId(Personid);

            return View(model);
        }

        //public ActionResult ImportFromExcel(ImportFromExcelVM model, HttpPostedFileBase file)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        try
        //        {
        //            FileUpload upload = new FileUpload();
        //            FileUploadResult result = upload.Upload(model.fileBase, Globals.UploadDirectory);
        //            if (result.Success)
        //            {
        //                List<AddPersonVM> savedPersonList = new List<AddPersonVM>();
        //                List<AddPersonVM> notSavedPersonList = new List<AddPersonVM>();
        //                ExcelHelper helper = new ExcelHelper();
        //                var list = helper.GetPersonData(result.LocalFilePath);

        //                if (list.Count > 0)
        //                {
        //                    uow.CreateTransaction();
        //                    try
        //                    {
        //                        foreach (var person in list)
        //                        {
        //                            try
        //                            {
        //                                var newPerson = personService.AddPersonWithoutSaving(person, file);
        //                                if (newPerson.PersonID > 0)
        //                                {
        //                                    uow.GenericRepository<EF.Person>().Insert(newPerson);
        //                                    uow.SaveChanges(); // Save each person individually
        //                                    savedPersonList.Add(person);
        //                                }
        //                                else
        //                                {
        //                                    notSavedPersonList.Add(person);
        //                                }
        //                            }
        //                            catch (Exception ex)
        //                            {
        //                                notSavedPersonList.Add(person);
        //                                // Log error for debugging
        //                                Console.WriteLine($"Error adding person {person.FullName}: {ex.Message}");
        //                            }
        //                        }

        //                        uow.Commit(); // Commit only successful records
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        uow.Rollback(); // Rollback if error occurs
        //                        TempData["error"] = ex.Message;
        //                    }

        //                    ViewBag.SavedPersons = savedPersonList;
        //                    ViewBag.NotSavedPersons = notSavedPersonList;
        //                    ViewBag.success = "Following is the imported persons detail.";

        //                    return View();
        //                }
        //                else
        //                {
        //                    TempData["error"] = "File contains 0 records to import.";
        //                }
        //            }
        //            else
        //            {
        //                TempData["error"] = result.Exception;
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            TempData["error"] = ex.Message;
        //        }
        //    }
        //    else
        //    {
        //        TempData["error"] = "File not found to upload.";
        //    }

        //    return RedirectToAction("Persons");
        //}

        public ActionResult ImportFromExcel(ImportFromExcelVM model, HttpPostedFileBase file)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    FileUpload upload = new FileUpload();
                    FileUploadResult result = upload.Upload(model.fileBase, Globals.UploadDirectory);
                    if (result.Success)
                    {
                        List<AddPersonVM> savedPersonList = new List<AddPersonVM>();
                        List<AddPersonVM> notSavedPersonList = new List<AddPersonVM>();
                        ExcelHelper helper = new ExcelHelper();
                        var list = helper.GetPersonData(result.LocalFilePath);
                        if (list.Count > 0)
                        {
                            foreach (var person in list)
                            {
                                try
                                {
                                    if (personService.AddImportPerson(person, file).PersonID > 0)
                                    {
                                        savedPersonList.Add(person);
                                        continue;
                                    }
                                    else
                                    {
                                        notSavedPersonList.Add(person);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    notSavedPersonList.Add(person);
                                }
                            }

                            ViewBag.SavedPersons = savedPersonList;

                            ViewBag.NotSavedPersons = notSavedPersonList;

                            ViewBag.success = "Following is the imported persons detail.";

                            return View();
                        }
                        else
                        {
                            TempData["error"] = "File contains 0 records to import.";
                        }
                    }
                    else
                    {
                        TempData["error"] = result.Exception;
                    }
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                }
            }
            else
            {
                TempData["error"] = "File not found to upload.";
            }

            return RedirectToAction("Persons");
        }
        public async Task<ActionResult> ImportFromExcelWithBooking(ImportFromExcelVM model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    FileUpload upload = new FileUpload();
                    FileUploadResult result = upload.Upload(model.fileBase, Globals.UploadDirectory);
                    if (result.Success)
                    {
                        ExcelHelper helper = new ExcelHelper();
                        var list = helper.GetPersonDataWithBooking(result.LocalFilePath);
                        if (list.Count > 0)
                        {
                            var importResult = await personService.ImportPersonsWithBookingAsync(list);
                            importResult.BuildErrorSummary();

                            var saved = importResult.SavedPersons.Count;
                            var failed = importResult.NotSavedPersons.Count;
                            var total = importResult.TotalRows;

                            if (failed == 0)
                                ViewBag.summaryType = "success";
                            else if (saved == 0)
                                ViewBag.summaryType = "danger";
                            else
                                ViewBag.summaryType = "warning";

                            ViewBag.summaryMessage = failed == 0
                                ? string.Format("{0} of {1} row(s) imported successfully.", saved, total)
                                : saved == 0
                                    ? string.Format("No rows were saved. {0} of {1} row(s) failed — see reasons below.", failed, total)
                                    : string.Format("Import completed: {0} saved, {1} failed (of {2} total). See details below.", saved, failed, total);

                            return View(importResult);
                        }
                        else
                        {
                            TempData["error"] = "File contains 0 records to import.";
                        }
                    }
                    else
                    {
                        TempData["error"] = result.Exception;
                    }
                }
                catch (Exception ex)
                {
                    TempData["error"] = ex.Message;
                }
            }
            else
            {
                TempData["error"] = "File not found to upload.";
            }

            return RedirectToAction("Persons");
        }


        [AuthorizeUser(Roles = AppUserRoles.View_PersonDocument)]
        public ActionResult UploadPersonDocumentImages(int Personid)
        {
            var images = personService.GetPersonDocuments();
            ViewBag.ImagesDetail = images.Where(x => x.PersonID == Personid).ToList();
            ViewBag.PersonID = Personid;
            return View();
        }

        [HttpPost]
        [AuthorizeUser(Roles = AppUserRoles.Add_PersonDocument)]
        public ActionResult UploadPersonDocumentImages(HttpPostedFileBase file, PersonDocumentsVM personDocumentsVM)
        {

            EF.PersonDocument personDocument = new EF.PersonDocument
            {
                PersonID = personDocumentsVM.PersonID,
                Description = personDocumentsVM.ImageSource.FileName,
                CreatedDate = DateTime.Now,
                CreatedBy = PMS.Common.Globals.User.Email

            };
            if (personDocumentsVM.ImageSource != null)
            {
                file = personDocumentsVM.ImageSource;
                Common.ImageUpload upload = new Common.ImageUpload();

                var result = Common.ImageUpload.SaveFile(file, "StudentDecuments");

                personDocumentsVM.ImageUrl = "/Upload/Files/StudentDecuments/" + result;


            }
            personDocument.ImageUrl = personDocumentsVM.ImageUrl;
            uow.GenericRepository<EF.PersonDocument>().Insert(personDocument);
            uow.SaveChanges();

            //Insert Audit Log
            {
                EF.AuditLog auditLog = new EF.AuditLog()
                {
                    AuditType = (int)Enumeration.AuditType.Create,
                    ActionId = (int)Enumeration.CorrespondenceAction.UploadPersonDocument,
                    PK = personDocument.PersonDocumentID.ToString(),
                    UserId = Common.Globals.User.ID,
                    TableName = "Person Document",
                    UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                    PersonId = personDocument.PersonID,
                    Reference = personDocument.Description

                };
                auditLogsService.AddAuditLog(auditLog);
            }
            return Json(new { success = true, data = personDocument }, JsonRequestBehavior.AllowGet);

        }

        [HttpPost]
        [AuthorizeUser(Roles = AppUserRoles.Delete_PersonDocument)]
        public ActionResult DeletePersonDocumentImagesbyId(int id)
        {
            var personDoc = uow.GenericRepository<PersonDocument>().GetByIdAsNoTracking(x => x.PersonDocumentID == id);

            var db = uow.Context;
            var imageurl = db.PersonDocuments.Where(x => x.PersonDocumentID == id).FirstOrDefault();
            if (imageurl == null)
            {
                return HttpNotFound();
            }
            db.PersonDocuments.Remove(imageurl);
            db.SaveChanges();

            var booking = uow.GenericRepository<Booking>().Table.Where(x => x.PersonID == personDoc.PersonID).FirstOrDefault();

            //Insert Audit Log
            {
                EF.AuditLog auditLog = new EF.AuditLog()
                {
                    AuditType = (int)Enumeration.AuditType.Delete,
                    ActionId = (int)Enumeration.CorrespondenceAction.DeletePersonDocument,
                    PK = personDoc.PersonDocumentID.ToString(),
                    UserId = Common.Globals.User.ID,
                    TableName = "Person Document",
                    UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                    PersonId = personDoc.PersonID,
                    Reference = personDoc.Description

                };
                auditLogsService.AddAuditLog(auditLog);
            }

            return Json(new { status = true }, JsonRequestBehavior.AllowGet);
        }


        [AuthorizeUser(Roles = AppUserRoles.create_resident_Acount)]
        public ActionResult GenrateUser(int id = 0)
        {
            try
            {
                var response = personService.GenrateUser(id);
                TempData["success"] = "User Successfuly Created!";
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.GetBaseException().Message;


            }
            return RedirectToAction("Persons");
        }

        public ActionResult CheckUser(int id)
        {
            var response = personService.CheckUserMaster(id);
            return Json(new { data = response }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ResendEmail(int id)
        {
            try
            {
                var response = personService.ResendEmail(id);
                TempData["success"] = "Email Sent Successfully";
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.GetBaseException().Message;


            }
            return RedirectToAction("Persons");
        }

        [AuthorizeUser(Roles = AppUserRoles.create_resident_Acount)]
        public ActionResult SendPortalCredentialsToInHouseResidents(int? locationId = null)
        {
            try
            {
                var result = personService.SendPortalCredentialsToInHouseResidents(locationId);
                return Json(new { status = true, data = result }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { status = false, message = ex.GetBaseException().Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult GetRefferals(string referralcode)
        {
            if (referralcode != null)
            {
                var res = personService.GetReferrals(referralcode);
                ViewBag.referrals = res;
            }
            else
            {
                ViewBag.referrals = null;
            }
            return View();
        }
    }
}