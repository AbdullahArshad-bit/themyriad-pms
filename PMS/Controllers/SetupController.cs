using PMS.Common.Filters;
using PMS.DTO.ViewModels.SetupViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PMS.Repository.UnitOfWork;
using PMS.Services.Services.Setup;
using PMS.Common;
using PMS.Common.Classes;
using PMS.Classes;
using PMS.DTO.ViewModels.PersonManageViewModels;
using PMS.Services.Services.Person;
using PMS.EF;
using Intuit.Ipp.Data;
using PMS.Services.Services.ChartOfAccounts;
using PMS.Services.Services.LocationContext;

namespace PMS.Controllers
{
    public class SetupController : BaseController
    {
        private readonly ISetupService setupService;
        private readonly UnitOfWork<EF.PMSEntities> uow;
        private readonly IChartOfAccountsService ChartOfAccountsService;
        private readonly ILocationContextService locationContextService;

        public SetupController(ISetupService _setupService, UnitOfWork<EF.PMSEntities> _uow, IChartOfAccountsService _chartOfAccountsService, ILocationContextService _locationContextService)
        {
            setupService = _setupService;
            uow = _uow;
            ChartOfAccountsService = _chartOfAccountsService;
            locationContextService = _locationContextService;
        }

        [AuthorizeUser(Roles = AppUserRoles.view_room_feaure)]
        public ActionResult RoomFeatures()
        {
            ViewBag.Features = setupService.GetAllRoomFeatures();
            return View();
        }

        [AuthorizeUser(Roles = AppUserRoles.add_room_feature)]
        public ActionResult AddRoomFeature(int? id)
        {
            AddAllRoomFeatureVM model = new AddAllRoomFeatureVM();

            if (id > 0)
            {
                var feature = setupService.GetAllRoomFeatureByID(Convert.ToInt32(id));
                model.AllRoomFeatureID = feature.AllRoomFeatureId;
                model.FeatureName = feature.FeatureName;
                model.Ar_FeatureName = feature.Ar_FeatureName;
                model.ImageUrl = feature.ImageUrl;
            }
            return View(model);
        }

        [AuthorizeUser(Roles = AppUserRoles.add_room_feature)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddRoomFeature(AddAllRoomFeatureVM model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (model.AllRoomFeatureID == 0)
                    {
                        if (setupService.GetAllRoomFeatures().Where(x => x.FeatureName.ToLower() == model.FeatureName.ToLower()).FirstOrDefault() != null)
                        {
                            ModelState.AddModelError("FeatureName", "Feature Already Exist with Same Name.");
                            return View(model);
                        }

                        if (setupService.AddAllRoomFeature(model).AllRoomFeatureId > 0)
                        {
                            ModelState.Clear();
                            model = new AddAllRoomFeatureVM();
                            ViewBag.success = "Feature saved successfully.";
                            return RedirectToAction("RoomFeatures");
                        }
                        else
                        {
                            ViewBag.error = "Error : unable to save feature.";
                        }
                    }
                    else
                    {
                        if (setupService.GetAllRoomFeatureByID(model.AllRoomFeatureID).FeatureName.ToLower() != model.FeatureName.ToLower())
                        {
                            if (setupService.GetAllRoomFeatures().Where(x => x.FeatureName.ToLower() == model.FeatureName.ToLower()).FirstOrDefault() != null)
                            {
                                ModelState.AddModelError("FeatureName", "Feature Already Exist with Same Name.");
                                return View(model);
                            }
                        }
                        if (setupService.UpdateAllRoomFeature(model).AllRoomFeatureId > 0)
                        {
                            ViewBag.success = "Feature saved successfully.";
                            return RedirectToAction("RoomFeatures");
                        }
                        else
                        {
                            ViewBag.error = "Error : unable to update feature.";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.error = ex.Message;
            }

            return View(model);
        }

        [AuthorizeUser(Roles = AppUserRoles.delete_room_feature)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteRoomFeature(int id)
        {
            try
            {
                if (setupService.GetAllRoomTypeFeatures().Where(x => x.AllRomFeatureID == id).FirstOrDefault() != null)
                {
                    TempData["error"] = "You can not delete this feature is in use.";
                    var error = TempData["error"];
                    return RedirectToAction("RoomFeatures", error);

                    //return Json(new { status = true, data = ViewBag.error },JsonRequestBehavior.AllowGet);
                }

                if (setupService.DeleteAllRoomFeature(id))
                {
                    ViewBag.success = "Feature deleted successfully.";
                }
                else
                {
                    ViewBag.error = "Error : unable to delete feature.";
                }
            }
            catch (Exception ex)
            {
                ViewBag.error = ex.Message;
            }
            return RedirectToAction("RoomFeatures");
        }

        [AuthorizeUser(Roles = AppUserRoles.view_room_types)]
        public ActionResult RoomTypes()
        {
            ViewBag.RoomTypes = setupService.GetRoomTypes();
            return View();
        }

        [AuthorizeUser(Roles = AppUserRoles.add_room_types)]
        public ActionResult AddRoomType(int? id)
        {
            AddRoomTypeVM roomTypeVM = new AddRoomTypeVM();

            roomTypeVM.AllRoomFeaturesList = setupService.GetAllRoomFeatures();
            ViewBag.LocationId = new SelectList(setupService.GetLocations(), "LocationID", "LocationName");
            if (id != null)
            {
                var roomType = setupService.GetRoomTypeByID(Convert.ToInt32(id));
                if (roomType != null)
                {
                    roomTypeVM.SelectedFeatures = setupService.GetRoomTypeFeaturesByRoomTypeID(roomType.RoomTypeID)
                        .Select(x => x.AllRomFeatureID).ToList();

                    roomTypeVM.RoomTypeID = roomType.RoomTypeID;
                    roomTypeVM.RoomCode = roomType.RoomCode;
                    roomTypeVM.RoomName = roomType.RoomName;
                    roomTypeVM.RoomArea = roomType.RoomArea;
                    roomTypeVM.RoomDescription = roomType.RoomDescription;
                    roomTypeVM.Ar_RoomName = roomType.Ar_RoomName;
                    roomTypeVM.Ar_RoomDescription = roomType.Ar_RoomDescription;
                    roomTypeVM.BedSpace = roomType.BedSpace;
                    roomTypeVM.Actual_Price = roomType.Actual_Price;
                    roomTypeVM.thumbnail = roomType.Thumbnail;
                    roomTypeVM.Ar_thumbnail = roomType.Ar_Thumbnail;
                    roomTypeVM.RoomInstruction = roomType.RoomInstruction;
                    roomTypeVM.Ar_RoomInstruction = roomType.Ar_RoomInstruction;
                    ViewBag.LocationId = new SelectList(setupService.GetLocations(), "LocationID", "LocationName", roomType.LocationId);
                }
            }
            return View(roomTypeVM);
        }

        [AuthorizeUser(Roles = AppUserRoles.add_room_types)]
        [HttpPost]
        public ActionResult AddRoomType(AddRoomTypeVM roomTypeVM)
        {
            if (ModelState.IsValid)
            {
                ViewBag.LocationId = new SelectList(setupService.GetLocations(), "LocationID", "LocationName", roomTypeVM.LocationId);

                if (roomTypeVM.RoomTypeID > 0)
                {
                    bool exist = uow.GenericRepository<EF.RoomType>().GetAll(x => x.IsEnable == true && x.RoomCode == roomTypeVM.RoomCode && x.RoomTypeID != roomTypeVM.RoomTypeID).Count > 0;

                    roomTypeVM.AllRoomFeaturesList = setupService.GetAllRoomFeatures();
                    if (!exist)
                    {
                        //if (setupService.GetRoomTypeByIDandLocation(roomTypeVM.RoomTypeID, roomTypeVM.LocationId).RoomCode.ToLower() == roomTypeVM.RoomName.ToLower() || setupService.GetRoomTypeByIDandLocation(roomTypeVM.RoomTypeID, roomTypeVM.LocationId).RoomName.ToLower() == roomTypeVM.RoomName.ToLower())
                        //{
                        //    if (setupService.GetRoomTypes().Where(x => x.RoomName.ToLower() == roomTypeVM.RoomName.ToLower()).FirstOrDefault() != null)
                        //    {
                        //        ModelState.AddModelError("RoomName", "Room Type Already Exist with Same Name.");
                        //        roomTypeVM.AllRoomFeaturesList = setupService.GetAllRoomFeatures();

                        //        roomTypeVM.SelectedFeatures = setupService.GetRoomTypeFeaturesByRoomTypeID(roomTypeVM.RoomTypeID)
                        //.Select(x => x.AllRomFeatureID).ToList();

                        //        return View(roomTypeVM);
                        //    }
                        //}   
                        roomTypeVM.UpdatedBy = Globals.User.Email;
                        roomTypeVM.UpdatedDate = DateTime.Now;

                        setupService.UpdateRoomType(roomTypeVM);
                        TempData["success"] = "Room Type updated successfully.";
                        roomTypeVM = new AddRoomTypeVM();
                        return RedirectToAction("RoomTypes");
                    }
                    else
                    {
                        ViewBag.error = "Error : Same room code is already available.";
                    }
                }
                else
                {
                    bool exist = uow.GenericRepository<EF.RoomType>().GetAll(x => x.IsEnable == true && x.RoomCode == roomTypeVM.RoomCode).Count > 0;
                    if (!exist)
                    {
                        if (setupService.GetRoomTypes().Where(x => x.RoomName.ToLower() == roomTypeVM.RoomName.ToLower()).Where(x => x.LocationId == roomTypeVM.LocationId).FirstOrDefault() != null)
                        {
                            roomTypeVM.AllRoomFeaturesList = setupService.GetAllRoomFeatures();
                            ModelState.AddModelError("RoomName", "Room Type Already Exist with Same Name.");
                            return View(roomTypeVM);
                        }
                        roomTypeVM.CreatedBy = Globals.User.Email;
                        roomTypeVM.CreatedDate = DateTime.Now;

                        if (setupService.AddRoomType(roomTypeVM).RoomTypeID > 0)
                        {
                            TempData["success"] = "Room Type saved successfully.";
                            roomTypeVM = new AddRoomTypeVM();

                            return RedirectToAction("RoomTypes");
                        }
                        else
                            TempData["error"] = "Something went wrong. Room Type not saved.";
                    }
                    else
                    {
                        ViewBag.error = "Error : Same room code is already available.";
                    }
                }
            }

            roomTypeVM.AllRoomFeaturesList = setupService.GetAllRoomFeatures();

            return View(roomTypeVM);
        }

        [AuthorizeUser(Roles = AppUserRoles.upload_room_typeImages)]
        public ActionResult UploadRoomTypeImages(int RoomTypeid)
        {
            var images = setupService.GetRoomTypeDetails();
            ViewBag.ImagesDetail = images.Where(x => x.RoomTypeID == RoomTypeid).ToList();
            ViewBag.RoomTypeID = RoomTypeid;
            return View();
        }
        [HttpPost]
        public ActionResult UploadRoomTypeImages(RoomTypeDetailsVM roomTypeDetailsVM)
        {
            EF.RoomTypeDetail roomTypeDetail = new EF.RoomTypeDetail
            {
                RoomTypeID = roomTypeDetailsVM.RoomTypeID,
                Description = roomTypeDetailsVM.ImageSource.FileName
            };

            if (roomTypeDetailsVM.ImageSource != null)
            {
                ImageResult result = new ImageResult();
                Common.ImageUpload upload = new Common.ImageUpload()
                {
                    Width = 1200,
                    Height = 675,
                    Quality = 100
                };
                result = upload.RenameUploadFileNew(roomTypeDetailsVM.ImageSource);

                if (!result.Success)
                    return View();
                roomTypeDetailsVM.ImageUrl = result.ImageName;
            }
            roomTypeDetail.ImageUrl = roomTypeDetailsVM.ImageUrl;
            uow.GenericRepository<EF.RoomTypeDetail>().Insert(roomTypeDetail);
            uow.SaveChanges();

            return Json(new { success = true, data = roomTypeDetail }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult DeleteRoomTypeImagebyId(int id)
        {
            var db = uow.Context;
            var imageurl = db.RoomTypeDetails.Where(x => x.ID == id).FirstOrDefault();
            if (imageurl == null)
            {
                return HttpNotFound();
            }
            db.RoomTypeDetails.Remove(imageurl);
            db.SaveChanges();
            return Json(new { status = true }, JsonRequestBehavior.AllowGet);
        }

        [AuthorizeUser(Roles = AppUserRoles.delete_room_types)]
        [HttpPost]
        public ActionResult DeleteRoomType(int id)
        {
            try
            {
                var check = setupService.GetPriceConfigs().Where(x => x.RoomTypeID == id).ToList();

                if (check.Count > 0)
                {
                    TempData["error"] = "You can not delete this Room Type is in use.";
                    var error = TempData["error"];
                    return RedirectToAction("RoomTypes", error);

                    //return Json(new { status = true, data = ViewBag.error },JsonRequestBehavior.AllowGet);
                }

                if (setupService.DeleteRoomType(id))
                    TempData["success"] = "Room Type deleted successfully.";
                else

                    TempData["error"] = "Something went wrong. Room Type not deleted.";
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
            }
            return RedirectToAction("RoomTypes");
        }


        [AuthorizeUser(Roles = AppUserRoles.view_terms)]
        public ActionResult Terms()
        {
            ViewBag.Terms = setupService.GetTerms();
            return View();
        }


        [AuthorizeUser(Roles = AppUserRoles.add_terms)]
        public ActionResult AddTerm(int? id)
        {
            AddTermVM termVM = new AddTermVM();
            //termVM.TermStartDate = DateTime.Today;
            //termVM.TermEndDate = DateTime.Today;
            ViewBag.LocationId = new SelectList(setupService.GetLocations(), "LocationID", "LocationName");
            ViewBag.FrequencyId = new SelectList(setupService.GetFrequency(), "Id", "Name");
            ViewBag.UniversityId = new SelectList(setupService.GetAllUniversityList(), "Id", "UniversityName");

            if (id != null)
            {
                var term = setupService.GetTermsByID(Convert.ToInt32(id));
                if (term != null)
                {
                    termVM.TermID = term.TermID;

                    if (term.TermStartDate != null)
                        termVM.TermStartDate = Convert.ToDateTime(term.TermStartDate);

                    if (term.TermEndDate != null)
                        termVM.TermEndDate = Convert.ToDateTime(term.TermEndDate);

                    termVM.TermName = term.TermName;
                    termVM.Ar_TermName = term.AR_TermName;
                    termVM.TermDescription = term.TermDescription;
                    termVM.Ar_TermDescription = term.Ar_TermDescription;
                    termVM.Min_Duration = term.Min_Duration;
                    termVM.Room_Occupancy = term.Room_Occupancy;
                    termVM.Ar_Room_Occupancy = term.Ar_Room_Occupancy;
                    termVM.Room_Standared = term.Room_Standared;
                    termVM.Ar_Room_Standared = term.Ar_Room_Standared;
                    termVM.DurationType = term.DurationType;
                    termVM.IsPublished = term.IsPublished ?? false;

                    ViewBag.LocationId = new SelectList(setupService.GetLocations(), "LocationID", "LocationName", term.LocationId);
                    ViewBag.FrequencyId = new SelectList(setupService.GetFrequency(), "Id", "Name", term.FrequencyId);
                    ViewBag.UniversityId = new SelectList(setupService.GetAllUniversityList(), "Id", "UniversityName", term.UniversityId);
                }
            }
            return View(termVM);
        }

        [AuthorizeUser(Roles = AppUserRoles.add_terms)]
        [HttpPost]
        public ActionResult AddTerm(AddTermVM termVM)
        {
            if (ModelState.IsValid)
            {
                if (termVM.TermID > 0)
                {
                    //if (setupService.GetTermsByID(termVM.TermID).TermName.ToLower() != termVM.TermName.ToLower())
                    //{
                    //    if (setupService.GetTerms().Where(x => x.TermName.ToLower() == termVM.TermName.ToLower()).FirstOrDefault() != null)
                    //    {
                    //        ModelState.AddModelError("TermName", "Term Already Exist With Same Name.");
                    //        return View(termVM);
                    //    }
                    //}
                    termVM.UpdatedBy = Globals.User.Email;
                    termVM.UpdatedDate = DateTime.Now;

                    setupService.UpdateTerm(termVM);
                    TempData["success"] = "Term updated successfully.";
                    termVM = new AddTermVM();

                    return RedirectToAction("Terms");
                }
                else
                {
                    //if (setupService.GetTerms().Where(x => x.TermName.ToLower() == termVM.TermName.ToLower()).FirstOrDefault() != null)
                    //{
                    //    ModelState.AddModelError("TermName", "Term Already Exist With Same Name.");
                    //    ViewBag.LocationId = new SelectList(setupService.GetLocations(), "LocationID", "LocationName", termVM.LocationId);

                    //    return View(termVM);
                    //}

                    termVM.CreatedBy = Globals.User.Email;
                    termVM.CreatedDate = DateTime.Now;

                    if (setupService.AddTerm(termVM).TermID > 0)
                    {
                        TempData["success"] = "Term saved successfully.";
                        termVM = new AddTermVM();

                        return RedirectToAction("Terms");
                    }
                    else
                        TempData["error"] = "Something went wrong. Term not saved.";
                }
            }
            ViewBag.LocationId = new SelectList(setupService.GetLocations(), "LocationID", "LocationName", termVM.LocationId);
            ViewBag.FrequencyId = new SelectList(setupService.GetFrequency(), "Id", "Name", termVM.FrequencyId);
            return View(termVM);
        }

        public ActionResult ImportTermsFromExcel(ImportTermFromExcelVM model, HttpPostedFileBase file)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    FileUpload upload = new FileUpload();
                    FileUploadResult result = upload.Upload(model.fileBase, Globals.UploadDirectory);
                    if (result.Success)
                    {
                        List<AddTermVM> savedTermList = new List<AddTermVM>();
                        List<AddTermVM> notSavedTermList = new List<AddTermVM>();
                        ExcelHelper helper = new ExcelHelper();
                        var list = helper.GetTermData(result.LocalFilePath);
                        if (list.Count > 0)
                        {
                            foreach (var term in list)
                            {
                                try
                                {
                                    // Check if a term with the same TermName already exists in the database
                                    var existingTerm = uow.GenericRepository<EF.Term>().Table.FirstOrDefault(t => t.TermName == term.TermName);
                                    if (existingTerm == null)
                                    {
                                        // If the term does not exist, add it
                                        setupService.AddTerm(term, file);
                                        savedTermList.Add(term);
                                        continue;
                                    }
                                    else
                                    {
                                        // Optionally, log or add to a "not saved" list for reporting
                                        notSavedTermList.Add(term);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    notSavedTermList.Add(term);
                                }
                            }

                            ViewBag.savedTerms = savedTermList;

                            ViewBag.NotSavedTerms = notSavedTermList;

                            ViewBag.success = "Following is the imported terms detail.";

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

            return RedirectToAction("Terms");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeUser(Roles = AppUserRoles.delete_terms)]
        public ActionResult DeleteTerm(int id)
        {
            try
            {
                if (setupService.GetPriceConfigs().Where(x => x.TermID == id).ToList().Count > 0)
                {
                    TempData["error"] = "You can not delete Term is in use.";
                    var error = TempData["error"];
                    return RedirectToAction("Terms", "Setup");
                }
                if (setupService.DeleteTerm(id))
                    TempData["success"] = "Term deleted successfully.";
                else

                    TempData["error"] = "Something went wrong. Term not deleted.";
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
            }

            return RedirectToAction("Terms");
        }

        [AuthorizeUser(Roles = AppUserRoles.view_price_config)]
        public ActionResult PriceConfig(int? id)
        {
            AddPriceConfigVM priceConfigVM = new AddPriceConfigVM
            {
                RoomTypesList = setupService.GetRoomTypes(),
                TermsList = setupService.GetTermsForDropDown()
            };
            ViewBag.LocationId = new SelectList(setupService.GetLocations(), "LocationID", "LocationName");
            ViewBag.Currency = new SelectList(setupService.GetCurrency(), "Name", "Name");

            if (id > 0)
            {
                var price = setupService.GetPriceConfigByID(Convert.ToInt32(id));
                var room = setupService.GetRoomTypeByID(Convert.ToInt32(price.RoomTypeID));
                priceConfigVM.RoomName = room.RoomName;

                if (price != null)
                {
                    priceConfigVM.LocationId = price.LocationId;
                    priceConfigVM.PriceConfigID = price.PriceConfigID;
                    priceConfigVM.RoomTypeID = price.RoomTypeID;
                    priceConfigVM.TermID = price.TermID;
                    priceConfigVM.Price = price.Price;
                    priceConfigVM.Deposit = price.InitialDeposit;
                    priceConfigVM.CleaningCharge = price.CleaningCharge;
                    priceConfigVM.Currency = price.Currency;
                    priceConfigVM.IsAvailable = price.IsAvailable;
                    priceConfigVM.OrderBy = price.OrderBy;
                    ViewBag.TermId = new SelectList("");
                    ViewBag.LocationId = new SelectList(setupService.GetLocations(), "LocationID", "LocationName", price.LocationId);
                    ViewBag.Currency = new SelectList(setupService.GetCurrency(), "Name", "Name", priceConfigVM.Currency);
                }
            }
            else
            {
                ViewBag.Currency = new SelectList(setupService.GetCurrency(), "Name", "Name");
                ViewBag.PriceConfig = setupService.GetPriceConfigVM();
            }
            return View(priceConfigVM);
        }

        [AuthorizeUser(Roles = AppUserRoles.view_price_config)]
        public ActionResult UpdatePriceConfig(int id)
        {
            AddPriceConfigVM priceConfigVM = new AddPriceConfigVM
            {
                RoomTypesList = setupService.GetRoomTypes(),
                TermsList = setupService.GetTermsForDropDown()
            };
            ViewBag.LocationId = new SelectList(setupService.GetLocations(), "LocationID", "LocationName");
            ViewBag.Currency = new SelectList(setupService.GetCurrency(), "Name", "Name");

            if (id > 0)
            {
                var price = setupService.GetPriceConfigByID(Convert.ToInt32(id));
                var room = setupService.GetRoomTypeByID(Convert.ToInt32(price.RoomTypeID));
                priceConfigVM.RoomName = room.RoomName;

                if (price != null)
                {
                    priceConfigVM.LocationId = price.LocationId;
                    priceConfigVM.PriceConfigID = price.PriceConfigID;
                    priceConfigVM.RoomTypeID = price.RoomTypeID;
                    priceConfigVM.TermID = price.TermID;
                    priceConfigVM.Price = price.Price;
                    priceConfigVM.Deposit = price.InitialDeposit;
                    priceConfigVM.CleaningCharge = price.CleaningCharge;
                    priceConfigVM.Currency = price.Currency;
                    priceConfigVM.IsAvailable = price.IsAvailable;
                    priceConfigVM.OrderBy = price.OrderBy;
                    ViewBag.RoomTypeID = new SelectList(setupService.GetRoomTypes().Where(x => x.LocationId == price.LocationId).ToList(), "RoomTypeID", "RoomName", price.RoomTypeID);
                    ViewBag.TermId = new SelectList(setupService.GetTerms().Where(x => x.LocationId == price.LocationId).ToList(), "TermID", "TermName", price.TermID);
                    ViewBag.LocationId = new SelectList(setupService.GetLocations(), "LocationID", "LocationName", price.LocationId);
                    ViewBag.Currency = new SelectList(setupService.GetCurrency(), "Name", "Name", price.Currency);
                }
            }
            else
                ViewBag.PriceConfig = setupService.GetPriceConfigVM();

            ModelState.Clear();
            return View(priceConfigVM);
        }

        [AuthorizeUser(Roles = AppUserRoles.add_price_config)]
        [HttpPost]
        public ActionResult AddPriceConfig(AddPriceConfigVM priceConfigVM)
        {
            bool IsAvailable = (Request.Form["IsActive"] != null);
            priceConfigVM.IsAvailable = IsAvailable;

            //if (ModelState.IsValid == true)
            //{
                if (priceConfigVM.PriceConfigID > 0)
                {
                    priceConfigVM.UpdatedBy = Globals.User.Email;
                    priceConfigVM.UpdatedDate = DateTime.Now;

                    setupService.UpdatePriceConfig(priceConfigVM);
                    TempData["success"] = "Price config updated successfully.";
                    priceConfigVM = new AddPriceConfigVM();

                    return RedirectToAction("PriceConfig");
                }
                else
                {
                    priceConfigVM.CreatedBy = Globals.User.Email;
                    priceConfigVM.CreatedDate = DateTime.Now;
                    priceConfigVM.IsAvailable = true;

                    if (setupService.AddPriceConfig(priceConfigVM).PriceConfigID > 0)
                    {
                        TempData["success"] = "Price config saved successfully.";
                        priceConfigVM = new AddPriceConfigVM();

                        return RedirectToAction("PriceConfig");
                    }
                    else
                        TempData["error"] = "Something went wrong. Price config not saved.";
                }
            //}
            return RedirectToAction("PriceConfig");
        }

       
        public ActionResult ImportPriceConfigFromExcel(AddPriceConfigVM model, HttpPostedFileBase file)
        {
            if (model.fileBase != null)
            {
                try
                {
                    FileUpload upload = new FileUpload();
                    FileUploadResult result = upload.Upload(model.fileBase, Globals.UploadDirectory);

                    if (result.Success)
                    {
                        List<AddPriceConfigVM> savedPriceConfigList = new List<AddPriceConfigVM>();
                        List<AddPriceConfigVM> notSavedPriceConfigList = new List<AddPriceConfigVM>();

                        ExcelHelper helper = new ExcelHelper();
                        var list = helper.GetPriceConfigData(result.LocalFilePath);

                        foreach (var priceConfig in list)
                        {
                            if (setupService.TryAddPriceConfig(priceConfig, out string reason))
                            {
                                savedPriceConfigList.Add(priceConfig);
                            }
                            else
                            {
                                priceConfig.ErrorMessage = reason; // Add error details to the model
                                notSavedPriceConfigList.Add(priceConfig);
                            }
                        }

                        ViewBag.savedPriceConfig = savedPriceConfigList;
                        ViewBag.notSavedPriceConfig = notSavedPriceConfigList;
                        ViewBag.success = "Price configurations have been imported successfully.";
                        return View();
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

            return RedirectToAction("PriceConfig");
        }




        [AuthorizeUser(Roles = AppUserRoles.add_price_config)]
        [HttpPost]
        public ActionResult AddPriceConfigList(List<AddPriceConfigVM> list)
        {
            try
            {
                foreach (var priceConfigVM in list)
                {
                    if (setupService.GetPriceConfigs().Where(x => x.TermID == priceConfigVM.TermID).Where(x => x.RoomTypeID == priceConfigVM.RoomTypeID).Count() > 0)
                    {
                        //ModelState.AddModelError("TermName", "Term Already Exist With Same Room.");
                        //return View(priceConfigVM);
                        return Json(new { status = false, data = "Term Already Exist With Same Room." }, JsonRequestBehavior.AllowGet);
                    }
                    priceConfigVM.CreatedBy = Globals.User.Email;
                    priceConfigVM.CreatedDate = DateTime.Now;

                    setupService.AddPriceConfiglist(priceConfigVM);

                }
                return Json(new { status = true, data = "Price config saved successfully." }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public ActionResult GetTermsAndRoomsBYLocationId(int id)
        {
            try
            {
                var Terms = setupService.GetTerms().Where(x => x.LocationId == id).Select(x => new { x.TermID, TermName = (x.TermDescription == null || x.UniversityId == null ? x.TermName.ToString() : x.TermName.ToString() + " - " + x.TermDescription.ToString() + " - " + x.University?.Prefix ?? "") }).ToList();
                var Rooms = setupService.GetRoomTypes().Where(x => x.LocationId == id).Select(x => new { x.RoomName, x.RoomTypeID }).ToList();
                return Json(new { Status = true, Terms = Terms, Rooms = Rooms }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { Status = false, error = ex.StackTrace }, JsonRequestBehavior.AllowGet);
            }
        }

        [AuthorizeUser(Roles = AppUserRoles.delete_price_config)]
        public ActionResult DeletePriceConfig(int id)
        {
            try
            {
                if (setupService.DeletePriceConfig(id))
                    TempData["success"] = "Price config. deleted successfully.";
                else

                    TempData["error"] = "Something went wrong. Price config. not deleted.";
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
            }

            return RedirectToAction("PriceConfig");
        }

        [AuthorizeUser(Roles = AppUserRoles.living_areas)]
        public ActionResult LivingAreas()
        {
            ViewBag.Locations = setupService.GetLocations();
            return View();
        }

        [AuthorizeUser(Roles = AppUserRoles.living_areas)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddLocation(AddLocationVM locationVM)
        {
            EF.Location location = new EF.Location();
            if (ModelState.IsValid)
            {
                if (locationVM.LocationID > 0)
                {
                    locationVM.UpdatedBy = Globals.User.Email;
                    locationVM.UpdatedDate = DateTime.Now;

                    location = setupService.UpdateLocation(locationVM);
                }
                else
                {
                    locationVM.CreatedBy = Globals.User.Email;
                    locationVM.CreatedDate = DateTime.Now;

                    location = setupService.AddLocation(locationVM);
                }
            }
            else
            {
                return Json(new DTO.ApiResponse<object>
                {
                    Success = false,
                    Code = 400,
                    Message = "Model error.",
                    Data = null
                }, JsonRequestBehavior.AllowGet);
            }
            var respose = new DTO.ApiResponse<object>
            {
                Success = true,
                Code = 200,
                Message = "Location saved successfully."
            };

            return Json(respose, JsonRequestBehavior.AllowGet);
        }

        [AuthorizeUser(Roles = AppUserRoles.living_areas)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteLocation(int id)
        {

            if (setupService.GetPriceConfigs().Where(x => x.LocationId == id).ToList().Count > 0 || setupService.GetRoomTypes().Where(x => x.LocationId == id).ToList().Count > 0
                || setupService.GetTerms().Where(x => x.LocationId == id).ToList().Count > 0)
            {

                TempData["error"] = "You can not delete this Location is in use.";
                var error = TempData["error"];
                return Json(new { status = false, data = error }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                bool ret = false;

                if (ModelState.IsValid)
                {
                    ret = setupService.DeleteLocation(id);
                }
                return Json(new DTO.ApiResponse<bool>
                {
                    Success = true,
                    Code = 200,
                    Message = "Location deleted successfully.",
                    Data = ret
                }, JsonRequestBehavior.AllowGet);
            }
        }

        [AuthorizeUser(Roles = AppUserRoles.living_areas)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddProject(AddProjectVM projectVM)
        {
            EF.Project project = new EF.Project();
            if (ModelState.IsValid)
            {
                if (projectVM.ProjectID > 0)
                {
                    projectVM.UpdatedBy = Globals.User.Email;
                    projectVM.UpdatedDate = DateTime.Now;

                    project = setupService.UpdateProject(projectVM);
                }
                else
                {
                    projectVM.CreatedBy = Globals.User.Email;
                    projectVM.CreatedDate = DateTime.Now;

                    project = setupService.AddProject(projectVM);
                }
            }
            else
            {
                return Json(new DTO.ApiResponse<object>
                {
                    Success = false,
                    Code = 400,
                    Message = "Model error.",
                    Data = null
                }, JsonRequestBehavior.AllowGet);
            }
            return Json(new DTO.ApiResponse<object>
            {
                Success = true,
                Code = 200,
                Message = "Project saved successfully.",
            }, JsonRequestBehavior.AllowGet);
        }

        [AuthorizeUser(Roles = AppUserRoles.living_areas)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteProject(int id)
        {
            bool ret = false;
            if (ModelState.IsValid)
            {
                ret = setupService.DeleteProject(id);
            }
            return Json(new DTO.ApiResponse<bool>
            {
                Success = true,
                Code = 200,
                Message = "Project deleted successfully.",
                Data = ret
            }, JsonRequestBehavior.AllowGet);
        }

        [AuthorizeUser(Roles = AppUserRoles.living_areas)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddBuilding(AddBuildingVM buildingVM)
        {
            EF.Building building = new EF.Building();
            if (ModelState.IsValid)
            {
                if (buildingVM.BuildingID > 0)
                {
                    bool exist = uow.GenericRepository<EF.Building>().GetAll(x => x.IsEnable == true && x.BuildingName == buildingVM.BuildingName &&
                    x.ProjectID == buildingVM.ProjectID &&
                    x.BuildingID != buildingVM.BuildingID).Count > 0;

                    if (!exist)
                    {
                        buildingVM.UpdatedBy = Globals.User.Email;
                        buildingVM.UpdatedDate = DateTime.Now;

                        building = setupService.UpdateBuilding(buildingVM);
                        return Json(new DTO.ApiResponse<object>
                        {
                            Success = true,
                            Code = 200,
                            Message = "Building saved successfully.",
                        }, JsonRequestBehavior.AllowGet);
                    }
                    else
                    {
                        return Json(new DTO.ApiResponse<object>
                        {
                            Success = false,
                            Code = 400,
                            Message = "Error : Duplicate building name in same project.",
                            Data = null
                        }, JsonRequestBehavior.AllowGet);
                    }
                }
                else
                {
                    bool exist = uow.GenericRepository<EF.Building>().GetAll(x => x.IsEnable == true && x.BuildingName == buildingVM.BuildingName &&
                    x.ProjectID == buildingVM.ProjectID).Count > 0;

                    if (!exist)
                    {
                        buildingVM.CreatedBy = Globals.User.Email;
                        buildingVM.CreatedDate = DateTime.Now;

                        building = setupService.AddBuilding(buildingVM);

                        return Json(new DTO.ApiResponse<object>
                        {
                            Success = true,
                            Code = 200,
                            Message = "Building saved successfully.",
                        }, JsonRequestBehavior.AllowGet);
                    }
                    else
                    {
                        return Json(new DTO.ApiResponse<object>
                        {
                            Success = false,
                            Code = 400,
                            Message = "Error : Duplicate building name in same project.",
                            Data = null
                        }, JsonRequestBehavior.AllowGet);
                    }
                }
            }
            else
            {
                return Json(new DTO.ApiResponse<object>
                {
                    Success = false,
                    Code = 400,
                    Message = "Model error.",
                    Data = null
                }, JsonRequestBehavior.AllowGet);
            }

        }

        [AuthorizeUser(Roles = AppUserRoles.living_areas)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteBuilding(int id)
        {
            bool ret = false;
            if (ModelState.IsValid)
            {
                ret = setupService.DeleteBuilding(id);
            }
            return Json(new DTO.ApiResponse<bool>
            {
                Success = true,
                Code = 200,
                Message = "Building deleted successfully.",
                Data = ret
            }, JsonRequestBehavior.AllowGet);
        }

        [AuthorizeUser(Roles = AppUserRoles.living_areas)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddFloor(AddFloorVM floorVM)
        {
            EF.Floor floor = new EF.Floor();
            if (ModelState.IsValid)
            {
                if (floorVM.FloorID > 0)
                {
                    bool exist = uow.GenericRepository<EF.Floor>().Table.Any(x => x.IsEnable == true && x.FloorName == floorVM.FloorName &&
                    x.BuildingID == floorVM.BuildingID &&
                    x.FloorID != floorVM.FloorID);

                    if (!exist)
                    {
                        floorVM.UpdatedBy = Globals.User.Email;
                        floorVM.UpdatedDate = DateTime.Now;

                        floor = setupService.UpdateFloor(floorVM);
                    }
                    else
                    {
                        return Json(new DTO.ApiResponse<object>
                        {
                            Success = false,
                            Code = 400,
                            Message = "Error : Duplicate floor name in same block.",
                            Data = null
                        }, JsonRequestBehavior.AllowGet);
                    }
                }
                else
                {
                    bool exist = uow.GenericRepository<EF.Floor>().Table.Any(x => x.IsEnable == true && x.FloorName == floorVM.FloorName &&
                    x.BuildingID == floorVM.BuildingID);

                    if (!exist)
                    {
                        floorVM.CreatedBy = Globals.User.Email;
                        floorVM.CreatedDate = DateTime.Now;

                        floor = setupService.AddFloor(floorVM);
                    }
                    else
                    {
                        return Json(new DTO.ApiResponse<object>
                        {
                            Success = false,
                            Code = 400,
                            Message = "Error : Duplicate floor name in same block.",
                            Data = null
                        }, JsonRequestBehavior.AllowGet);
                    }
                }
            }
            else
            {
                return Json(new DTO.ApiResponse<object>
                {
                    Success = false,
                    Code = 400,
                    Message = "Model error.",
                    Data = null
                }, JsonRequestBehavior.AllowGet);
            }

            return Json(new DTO.ApiResponse<object>
            {
                Success = true,
                Code = 200,
                Message = "Floor saved successfully.",
            }, JsonRequestBehavior.AllowGet);
        }

        [AuthorizeUser(Roles = AppUserRoles.living_areas)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteFloor(int id)
        {
            bool ret = false;
            if (ModelState.IsValid)
            {
                ret = setupService.DeleteFloor(id);
            }
            return Json(new DTO.ApiResponse<bool>
            {
                Success = true,
                Code = 200,
                Message = "Floor deleted successfully.",
                Data = ret
            }, JsonRequestBehavior.AllowGet);
        }

        [AuthorizeUser(Roles = AppUserRoles.living_areas)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddRoom(AddRoomVM roomVM)
        {
            EF.Room room = new EF.Room();
            if (ModelState.IsValid)
            {
                if (roomVM.RoomID > 0)
                {
                    roomVM.UpdatedBy = Globals.User.Email;
                    roomVM.UpdatedDate = DateTime.Now;

                    room = setupService.UpdateRoom(roomVM);
                }
                else
                {
                    roomVM.CreatedBy = Globals.User.Email;
                    roomVM.CreatedDate = DateTime.Now;

                    room = setupService.AddRoom(roomVM);
                }
            }
            else
            {
                return Json(new DTO.ApiResponse<object>
                {
                    Success = false,
                    Code = 400,
                    Message = "Model error.",
                    Data = null
                }, JsonRequestBehavior.AllowGet);
            }

            return Json(new DTO.ApiResponse<object>
            {
                Success = true,
                Code = 200,
                Message = "Room saved successfully.",
                //Data = room
            }, JsonRequestBehavior.AllowGet);
        }

       


        [AuthorizeUser(Roles = AppUserRoles.living_areas)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteRoom(int id)
        {
            bool ret = false;
            if (ModelState.IsValid)
            {
                ret = setupService.DeleteRoom(id);
            }
            return Json(new DTO.ApiResponse<bool>
            {
                Success = true,
                Code = 200,
                Message = "Room deleted successfully.",
                Data = ret
            }, JsonRequestBehavior.AllowGet);
        }

        [AuthorizeUser(Roles = AppUserRoles.living_areas)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddBed(AddBedSpaceVM bedSpaceVM)
        {
            EF.BedSpace bedSpace = new EF.BedSpace();
            bool Status = (Request.Form["Status"] != null);
            bedSpaceVM.Status = Status;
            ModelState.Remove("Status");


            if (ModelState.IsValid)
            {
                if (bedSpaceVM.BedSpaceID > 0)
                {
                    bedSpaceVM.UpdatedBy = Globals.User.Email;
                    bedSpaceVM.UpdatedDate = DateTime.Now;

                    bedSpace = setupService.UpdateBedSpace(bedSpaceVM);
                }
                else
                {
                    bedSpaceVM.CreatedBy = Globals.User.Email;
                    bedSpaceVM.CreatedDate = DateTime.Now;

                    bedSpace = setupService.AddBedSpace(bedSpaceVM);
                }
            }
            else
            {
                return Json(new DTO.ApiResponse<object>
                {
                    Success = false,
                    Code = 400,
                    Message = "Model error.",
                    Data = null
                }, JsonRequestBehavior.AllowGet);
            }

            return Json(new DTO.ApiResponse<object>
            {
                Success = true,
                Code = 200,
                Message = "Bed space saved successfully.",
                //Data = bedSpace
            }, JsonRequestBehavior.AllowGet);
        }

        [AuthorizeUser(Roles = AppUserRoles.living_areas)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteBed(int id)
        {
            bool ret = false;
            if (ModelState.IsValid)
            {
                ret = setupService.DeleteBedSpace(id);
            }
            return Json(new DTO.ApiResponse<bool>
            {
                Success = true,
                Code = 200,
                Message = "Bed space deleted successfully.",
                Data = ret
            }, JsonRequestBehavior.AllowGet);
        }


        [AuthorizeUser(Roles = AppUserRoles.living_areas)]
        public ActionResult GetModal([System.Web.Http.FromBody] GetTableVM tableVM)
        {
            switch (tableVM.View)
            {
                case "location":
                    {
                        AddLocationVM locationVM = new AddLocationVM();
                        if (tableVM.Id > 0)
                        {
                            var loc = setupService.GetLocationByID(Convert.ToInt32(tableVM.Id));
                            if (loc != null)
                            {
                                locationVM.LocationID = loc.LocationID;
                                locationVM.LocationName = loc.LocationName;
                                locationVM.Ar_LocationName = loc.Ar_LocationName;
                                locationVM.LocationDescription = loc.LocationDescription;
                                locationVM.Prefix = loc.Prefix;
                            }
                        }

                        return PartialView("_PartialLocationsModal", locationVM);
                    }
                case "project":
                    {
                        AddProjectVM projectVM = new AddProjectVM();
                        projectVM.LocationID = tableVM.LinkId;
                        if (tableVM.Id > 0)
                        {
                            var project = setupService.GetProjectByID(Convert.ToInt32(tableVM.Id));
                            if (project != null)
                            {
                                projectVM.LocationID = project.LocationID;
                                projectVM.ProjectID = project.ProjectID;
                                projectVM.ProjectName = project.ProjectName;
                                projectVM.ProjectCity = project.ProjectCity;
                                projectVM.ProjectState = project.ProjectState;
                                projectVM.ProjectZip = project.ProjectZip;
                                projectVM.ProjectAddress = project.ProjectAddress;
                                projectVM.ProjectDescription = project.ProjectDescription;
                            }
                        }

                        return PartialView("_PartialProjectModal", projectVM);
                    }
                case "building":
                    {
                        AddBuildingVM buildingVM = new AddBuildingVM();
                        buildingVM.ProjectID = tableVM.LinkId;
                        if (tableVM.Id > 0)
                        {
                            var building = setupService.GetBuildingByID(Convert.ToInt32(tableVM.Id));
                            if (building != null)
                            {
                                buildingVM.BuildingID = building.BuildingID;
                                buildingVM.BuildingName = building.BuildingName;
                                buildingVM.BuildingDescription = building.BuildingDescription;
                            }
                        }

                        return PartialView("_PartialBuildingModal", buildingVM);
                    }
                case "floor":
                    {
                        AddFloorVM floorVM = new AddFloorVM();
                        floorVM.BuildingID = tableVM.LinkId;
                        if (tableVM.Id > 0)
                        {
                            var floor = setupService.GetFloorByID(Convert.ToInt32(tableVM.Id));
                            if (floor != null)
                            {
                                floorVM.FloorID = floor.FloorID;
                                floorVM.FloorName = floor.FloorName;
                                floorVM.FloorDescription = floor.FloorDescription;
                            }
                        }

                        return PartialView("_PartialFloorModal", floorVM);
                    }
                case "room":
                    {
                        AddRoomVM roomVM = new AddRoomVM();
                        roomVM.FloorID = tableVM.LinkId;
                        roomVM.RoomTypeList = setupService.GetRoomTypes();

                        if (tableVM.Id > 0)
                        {
                            var room = setupService.GetRoomByID(Convert.ToInt32(tableVM.Id));
                            if (room != null)
                            {
                                roomVM.RoomID = room.RoomID;
                                roomVM.RoomTypeID = Convert.ToInt32(room.RoomTypeID);
                                roomVM.RoomGender = room.RoomGender;
                                roomVM.RoomName = room.RoomName;
                                roomVM.RoomSize = room.RoomSize;
                                roomVM.RoomDescription = room.RoomDescription;
                                roomVM.RoomLockId = room.RoomLockId ?? 0;
                            }
                        }

                        return PartialView("_PartialRoomModal", roomVM);
                    }
                case "bed":
                    {
                        AddBedSpaceVM bedSpaceVM = new AddBedSpaceVM();
                        bedSpaceVM.RoomID = tableVM.LinkId;

                        if (tableVM.Id > 0)
                        {
                            var bed = setupService.GetBedSpaceByID(Convert.ToInt32(tableVM.Id));
                            if (bed != null)
                            {
                                bedSpaceVM.BedSpaceID = bed.BedSpaceID;
                                bedSpaceVM.BedSpaceName = bed.BedName;
                                bedSpaceVM.BedSpaceDescription = bed.BedDescription;
                                bedSpaceVM.BedSpaceGender = bed.RoomGender;
                                bedSpaceVM.BedSpaceAddress = bed.BedAddress;
                                bedSpaceVM.Status = bed.Status;
                            }
                        }

                        return PartialView("_PartialBedModal", bedSpaceVM);
                    }
                default:
                    throw new Exception("View not found.");
            }
        }


        [AuthorizeUser(Roles = AppUserRoles.living_areas)]
        public ActionResult GetTable([System.Web.Http.FromBody] GetTableVM tableVM)
        {
            switch (tableVM.View)
            {
                case "location":
                    {
                        ViewBag.Locations = setupService.GetLocations();
                        return PartialView("_PartialLocationsTable");
                    }
                case "project":
                    {
                        ViewBag.Projects = setupService.GetProjects(tableVM.Id);
                        return PartialView("_PartialProjectTable");
                    }
                case "building":
                    {
                        ViewBag.Buildings = setupService.GetBuildings(tableVM.Id);
                        return PartialView("_PartialBuildingTable");
                    }
                case "floor":
                    {
                        ViewBag.Floors = setupService.GetFloors(tableVM.Id);
                        return PartialView("_PartialFloorTable");
                    }
                case "room":
                    {
                        ViewBag.Rooms = setupService.GetRooms(tableVM.Id);
                        return PartialView("_PartialRoomTable");
                    }
                case "bed":
                    {
                        ViewBag.BedSpaces = setupService.GetBedSpaces(tableVM.Id);
                        return PartialView("_PartialBedTable");
                    }
                default:
                    throw new Exception("View not found.");
            }
        }


        public ActionResult LocationSettings(int LocationId)
        {
            LocationSettingsVM model = setupService.GetLocationSettingsByLocationid(LocationId);
            if (model == null)
            {
                model = new LocationSettingsVM()
                {
                    LocationId = LocationId
                };
            }

            ViewBag.Def_Acc_Rec = new SelectList(ChartOfAccountsService.GetReceivableAccounts(), "Id", "Name", model.Def_Acc_Rec);
            ViewBag.Def_Acc_Pay = new SelectList(ChartOfAccountsService.GetPayableAccounts(), "Id", "Name", model.Def_Acc_Pay);
            ViewBag.Def_Acc_Adv_Pay = new SelectList(ChartOfAccountsService.GetLiablitiesAccounts(), "Id", "Name", model.Def_Acc_Adv_Pay);
            ViewBag.Def_Acc_Discount = new SelectList(ChartOfAccountsService.GetDiscountAccounts(), "Id", "Name", model.Def_Acc_Discount);
            

            return View(model);
        }
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult LocationSettings(LocationSettingsVM locationSettingsVM)
        {
            EF.LocationSetting locationSetting = new EF.LocationSetting();
            if (ModelState.IsValid)
            {
                bool success = setupService.AddOrEditLocationSettings(locationSettingsVM);
                if (success)
                {
                    return RedirectToAction("LivingAreas");
                }

            }

            return RedirectToAction("LivingAreas");

        }

        [AuthorizeUser(Roles = AppUserRoles.View_UniversitesList)]
        [HttpGet]
        public ActionResult GetUniversitesList()
        {
            ViewBag.Universities = setupService.GetAllUniversities();

            return View();
        }

        [AuthorizeUser(Roles = AppUserRoles.add_Universites)]
        [HttpGet]
        public ActionResult AddUniversity(int? Id)
        {
            var model = new UniversityVM();
            ViewBag.LocationId = new SelectList(setupService.GetLocations(), "LocationID", "LocationName");
            model.IsActive = true;
            if (Id != null)
            {
                var university = setupService.GetUniversityById((int)Id);
                if (university != null)
                {
                    model = university;
                }
                ViewBag.LocationId = new SelectList(setupService.GetLocations(), "LocationID", "LocationName", model.LocationId);
            }
            return View(model);
        }

        [AuthorizeUser(Roles = AppUserRoles.add_Universites)]
        [HttpPost]
        public ActionResult AddUniversity(UniversityVM universityVM)
        {
            try
            {
                if (universityVM.Id > 0)
                {
                    ModelState.Remove("ThumbnailImage");
                }
                bool IsActive = (Request.Form["IsActive"] != null);
                if (universityVM.Id > 0)
                {
                    universityVM.IsActive = IsActive;
                    var res = setupService.UpdateUniversity(universityVM);

                    if (res == true)
                    {
                        TempData["success"] = "University Updated Successfully!";
                        return RedirectToAction("GetUniversitesList");
                    }

                }
                else
                {
                    universityVM.IsActive = IsActive;
                    var res = setupService.AddNewUniversity(universityVM);

                    if (res == true)
                    {
                        TempData["success"] = "University Added Successfully!";
                        return RedirectToAction("GetUniversitesList");
                    }

                }
            }
            catch (Exception ex)
            {

                throw;
            }
            ViewBag.LocationId = new SelectList(setupService.GetLocations(), "LocationID", "LocationName");

            return RedirectToAction("AddUniversity");

        }

        [HttpPost]
        public ActionResult DeleteUni(int id)
        {
            var res = setupService.DeleteUniversity(id);
            if (res == true)
            {
                TempData["success"] = "University Deleted Successfully!";

                return RedirectToAction("GetUniversitesList");

            }
            else
            {
                TempData["error"] = "University could not be deleted!";

                return RedirectToAction("GetUniversitesList");


            }

        }


        public ActionResult GetUniversitiesByLocId(int Id)
        {
            try
            {

                var data = setupService.GetUniversityListByLoactionId(Id, "en-");

                return Json(new
                {
                    Success = true,
                    Code = 200,
                    Data = data
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    Success = false,
                    Code = 404,
                    Message = ex,
                    Data = ex
                }, JsonRequestBehavior.AllowGet);
            }
        }

        //GetTermsBYRoomTypeID
        [HttpGet]
        public JsonResult GetTermsBYRoomTypeID(int roomTypeId)
        {
            try
            {
                var Terms = setupService.GetTermsByRoomTypeID(roomTypeId);


                return Json(new { Status = true, Terms = Terms }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {

                return Json(new { Status = false, error = ex.StackTrace }, JsonRequestBehavior.AllowGet);

            }
        }
      
        public JsonResult GetAssignedLocations()
        {
            // Use GetUserAssignedLocationIds() to always show all locations the user has access to
            // This is for the dropdown, so it should show all available locations, not just the selected one
            var assignedLocationIds = locationContextService.GetUserAssignedLocationIds();
            var location = new SelectList(setupService.GetLocationsByID(assignedLocationIds), "LocationID", "LocationName");

            return Json(new { data = location }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult RoomsExcel()
        {
            return View();
        }
        public ActionResult ImportRoomsFromExcel(ImportRoomFromExcelVM model, HttpPostedFileBase file)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    FileUpload upload = new FileUpload();
                    FileUploadResult result = upload.Upload(model.fileBase, Globals.UploadDirectory);
                    if (result.Success)
                    {
                        List<AddRoomVM> savedRoomList = new List<AddRoomVM>();
                        List<AddRoomVM> notSavedRoomList = new List<AddRoomVM>();
                        ExcelHelper helper = new ExcelHelper();
                        var list = helper.GetRoomsData(result.LocalFilePath);
                        if (list.Count > 0)
                        {
                            foreach (var room in list)
                            {
                                try
                                {
                                    // Check if a room with the same Room name already exists with same room type in the database
                                    var existingRoom = uow.GenericRepository<EF.Room>().Table.FirstOrDefault(r => r.RoomName == room.RoomName && r.RoomTypeID == room.RoomTypeID);
                                    if (existingRoom == null)
                                    {
                                        // If the room does not exist, add it
                                        setupService.ExcelAddRoom(room, file);
                                        savedRoomList.Add(room);
                                        continue;
                                    }
                                    else
                                    {
                                        // Optionally, log or add to a "not saved" list for reporting
                                        notSavedRoomList.Add(room);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    notSavedRoomList.Add(room);
                                }
                            }

                            ViewBag.savedRooms = savedRoomList;

                            ViewBag.NotSavedRooms = notSavedRoomList;

                            ViewBag.success = "Following is the imported rooms detail.";

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

            return RedirectToAction("RoomsExcel");
        }


        public ActionResult BedsExcel()
        {
            return View();
        }
        public ActionResult ImportBedsFromExcel(ImportBedFromExcelVM model, HttpPostedFileBase file)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    FileUpload upload = new FileUpload();
                    FileUploadResult result = upload.Upload(model.fileBase, Globals.UploadDirectory);
                    if (result.Success)
                    {
                        List<AddBedSpaceVM> savedBedList = new List<AddBedSpaceVM>();
                        List<AddBedSpaceVM> notSavedBedList = new List<AddBedSpaceVM>();
                        ExcelHelper helper = new ExcelHelper();
                        var list = helper.GetBedsData(result.LocalFilePath);
                        if (list.Count > 0)
                        {
                            foreach (var bed in list)
                            {
                                try
                                {
                                    // Check if a room with the same Room name already exists with same room type in the database
                                    var existingBed = uow.GenericRepository<EF.BedSpace>().Table.FirstOrDefault(b => b.RoomID == bed.RoomID && b.BedName == bed.BedSpaceName);
                                    if (existingBed == null)
                                    {
                                        // If the room does not exist, add it
                                        setupService.ExcelAddBedSpace(bed, file);
                                        savedBedList.Add(bed);
                                        continue;
                                    }
                                    else
                                    {
                                        // Optionally, log or add to a "not saved" list for reporting
                                        notSavedBedList.Add(bed);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    notSavedBedList.Add(bed);
                                }
                            }

                            ViewBag.savedBeds = savedBedList;

                            ViewBag.NotSavedBeds = notSavedBedList;

                            ViewBag.success = "Following is the imported rooms detail.";

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

            return RedirectToAction("BedsExcel");
        }
        public JsonResult GetLastLocation()
        {
            var lastlocation = setupService.GetLastLocation();
            return Json(new { data = lastlocation }, JsonRequestBehavior.AllowGet);
        }


    }
}