using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PMS.Common.Filters;
using PMS.DTO.ViewModels.InspectionViewModels;
using PMS.Services.Services.Inspection;
using PMS.DTO.ViewModels.BedSpacePlacementViewModels;
using PMS.EF;
using System.IO;
using PMS.Common.Classes;

namespace PMS.Controllers
{
    [AuthorizeUser]
    public class InspectionController : BaseController
    {
        private readonly IInspectionService inspectionService;
        public InspectionController(IInspectionService _inspectionService)
        {
            inspectionService = _inspectionService;
        }
        // GET: Inspection
        [AuthorizeUser(Roles = AppUserRoles.view_InspectionRatingList)]
        public ActionResult RatingList()
        {
            ViewBag.Ratings = inspectionService.GetRating();

            return View();
        }

        [AuthorizeUser(Roles = AppUserRoles.add_InspectionRating)]
        public ActionResult AddRatingList(int? id)
        {
            AddRatingListVM model = new AddRatingListVM();
            model.RatingListItem = new List<RatingListItem>();
            model.IsActive = true;

            if (id > 0)
            {
                //edit
                model = inspectionService.GetRatingById(Convert.ToInt32(id));
                if (model == null)
                {
                    TempData["error"] = "Something went wrong, rating list not found to update.";
                    return RedirectToAction("RatingList");
                }
            }
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [AuthorizeUser(Roles = AppUserRoles.add_InspectionRating)]
        public ActionResult AddRatingList(AddRatingListVM model)
        {
            try
            {
                bool IsActive = (Request.Form["IsActive"] != null);
                model.IsActive = IsActive;

                if (model.InspectionRatingListID == 0)
                {
                    //add
                    model.CreatedDate = DateTime.Now;
                    model.CreatedBy = Common.Globals.User.Email;

                    var result = inspectionService.AddRatingList(model);
                    if (result == true)
                    {
                        TempData["success"] = "Rating List added succesfully";
                        return RedirectToAction("RatingList");
                    }
                    else
                    {
                        ViewBag.error = "Something went wrong, rating list not added.";
                    }
                }
                else
                {
                    //update

                    model.UpdatedDate = DateTime.Now;
                    model.UpdatedBy = Common.Globals.User.Email;


                    var result = inspectionService.UpdateRatingList(model);
                    if (result == true)
                    {
                        TempData["success"] = "Rating List updated succesfully";
                        return RedirectToAction("RatingList");
                    }
                    else
                    {
                        ViewBag.error = "Something went wrong, rating list not update.";
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.error = ex.Message;
            }
            return View(model);

        }

        [HttpGet]
        [AuthorizeUser(Roles = AppUserRoles.delete_InspectionRating)]
        public ActionResult DeleteRatingList(int id)
        {
            try
            {
                if (inspectionService.DeleteInspection(id))
                {
                    return Json(new { status = true, data = "Rating list deleted successfully." }, JsonRequestBehavior.AllowGet);
                }
                else
                {

                    return Json(new { status = false, data = "Sorry something went wrong, Rating list not deleted." }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = false, data = "Sorry something went wrong, Rating list not deleted." }, JsonRequestBehavior.AllowGet);

            }

        }

        [AuthorizeUser(Roles = AppUserRoles.view_InspectionList)]
        public ActionResult Inspection()
        {
            var result = inspectionService.getInspections();
            return View(result);
        }
        [AuthorizeUser(Roles = AppUserRoles.add_Inspection)]
        public ActionResult AddNewInspection(int? id)
        {
            AddInspectionsVM model = new AddInspectionsVM();
            model.InspectionFieldForSlect = new List<InspectionFieldForSlect>();



            var InspectionFields = inspectionService.getInspectionFields();
            ViewBag.InspectionFields = InspectionFields;

            var InspectionTypes = inspectionService.getInspectionTypes();
            ViewBag.InspectionTypes = new SelectList(InspectionTypes, "InspectionTypeID", "InspectionTypeName");

            var InspectionComparison = inspectionService.getInspectionToCompareAgainst();
            ViewBag.InspectionComparison = new SelectList(InspectionComparison, "InspectionComapareAgainstID", "InspectionCompareAgainstName");


            model.IsActive = true;
            model.InspectionValidDays = 60;
            if (id > 0)
            {
                model = inspectionService.getInspectionById(Convert.ToInt32(id));
                //edit
                if (model == null)
                {
                    TempData["error"] = "Something went wrong, inspection not found to update.";
                    return RedirectToAction("InspectionField");
                }
            }
            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [AuthorizeUser(Roles = AppUserRoles.add_Inspection)]
        public ActionResult AddNewInspection(AddInspectionsVM model)
        {

            try
            {
                bool IsActive = (Request.Form["IsActive"] != null);
                model.IsActive = IsActive;


                if (model.InspectionID == 0)
                {
                    //add
                    model.CreatedDate = DateTime.Now;
                    model.CreatedBy = Common.Globals.User.Email;

                    var result = inspectionService.AddInspectionsVM(model);
                    if (result == true)
                    {
                        TempData["success"] = "Inspection added succesfully";
                        return RedirectToAction("Inspection");
                    }
                    else
                    {
                        ViewBag.error = "Something went wrong, Inspection not added.";
                    }
                }
                else
                {
                    //update

                    model.UpdatedDate = DateTime.Now;
                    model.UpdatedBy = Common.Globals.User.Email;

                    var result = inspectionService.UpdateInspection(model);
                    if (result == true)
                    {
                        TempData["success"] = "Inspection updated succesfully";
                        return RedirectToAction("Inspection");
                    }
                    else
                    {
                        ViewBag.error = "Something went wrong, Inspection not update.";
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.error = ex.Message;
            }

            var InspectionFields = inspectionService.getInspectionFields();
            ViewBag.InspectionFields = InspectionFields;

            var InspectionTypes = inspectionService.getInspectionTypes();
            ViewBag.InspectionTypes = new SelectList(InspectionTypes, "InspectionTypeID", "InspectionTypeName");

            var InspectionComparison = inspectionService.getInspectionToCompareAgainst();
            ViewBag.InspectionComparison = new SelectList(InspectionComparison, "InspectionComapareAgainstID", "InspectionCompareAgainstName");
            return View(model);
        }

        [AuthorizeUser(Roles = AppUserRoles.view_InspectionFieldsList)]
        public ActionResult InspectionField()
        {
            ViewBag.InspectionFields = inspectionService.getInspectionFields();
            return View();
        }
        [AuthorizeUser(Roles = AppUserRoles.add_InspectionFields)]

        public ActionResult AddInspectionField(int? id)
        {
            AddInspectionFieldsVM model = new AddInspectionFieldsVM();

            var ratingList = inspectionService.GetRating();
            ViewBag.RatingList = new SelectList(ratingList, "InspectionRatingListID", "Name");

            model.IsActive = true;
            model.AllowNotes = true;
            model.AllowImages = true;

            if (id > 0)
            {
                model = inspectionService.getInspectionFieldById(Convert.ToInt32(id));
                //edit
                if (model == null)
                {
                    TempData["error"] = "Something went wrong, inspection field not found to update.";
                    return RedirectToAction("InspectionField");
                }
            }
            return View(model);
        }
        [HttpPost, ValidateAntiForgeryToken]
        [AuthorizeUser(Roles = AppUserRoles.add_InspectionFields)]
        public ActionResult AddInspectionField(AddInspectionFieldsVM model)
        {

            try
            {
                bool IsActive = (Request.Form["IsActive"] != null);
                model.IsActive = IsActive;

                if (model.INspectionFieldID == 0)
                {
                    //add

                    model.CreatedDate = DateTime.Now;
                    model.CreatedBy = Common.Globals.User.Email;

                    var result = inspectionService.AddInspectionFIeld(model);
                    if (result == true)
                    {
                        TempData["success"] = "Inspection Field added succesfully";
                        return RedirectToAction("AddInspectionField", new { id = 0 });
                    }
                    else
                    {
                        TempData["error"] = "Something went wrong, Inspection Field not added.";
                    }
                }
                else
                {

                    //update

                    model.UpdatedDate = DateTime.Now;
                    model.UpdatedBy = Common.Globals.User.Email;

                    var result = inspectionService.UpdateInspectionField(model);
                    if (result == true)
                    {
                        TempData["success"] = "Inspection Field updated succesfully";
                        return RedirectToAction("InspectionField");
                    }
                    else
                    {
                        TempData["error"] = "Something went wrong, Inspection Field not update.";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
            }

            var ratingList = inspectionService.GetRating();
            ViewBag.RatingList = new SelectList(ratingList, "InspectionRatingListID", "Name");

            return View(model);
        }
        [HttpGet]
        [AuthorizeUser(Roles = AppUserRoles.delete_Inspection)]
        public ActionResult DeleteInspection(int id)
        {
            try
            {
                if (inspectionService.DeleteInspections(id))
                {

                    return Json(new { status = true, data = "Inspection deleted successfully." }, JsonRequestBehavior.AllowGet);
                }
                else
                {

                    return Json(new { status = false, data = "Sorry something went wrong, Inspection not deleted." }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = false, data = "Sorry something went wrong, Inspection not deleted." }, JsonRequestBehavior.AllowGet);

            }

        }

        public ActionResult GetRatings(int id)
        {
            ViewBag.viewRatings = inspectionService.getRatingListItemsById(id);
            return PartialView("_PartialViewRatings");
        }

        public ActionResult configureinspectionRules()
        {
            return View();
        }

        [HttpGet]
        [AuthorizeUser(Roles = AppUserRoles.delete_InspectionFields)]
        public ActionResult DeleteInspectionField(int id)
        {
            try
            {
                if (inspectionService.DeleteInspectionField(id))
                {

                    return Json(new { status = true, data = "Inspection Field deleted successfully." }, JsonRequestBehavior.AllowGet);
                }
                else
                {

                    return Json(new { status = false, data = "Sorry something went wrong, Inspection Field not deleted." }, JsonRequestBehavior.AllowGet);

                }
            }
            catch (Exception ex)
            {
                return Json(new { status = false, data = "Sorry something went wrong, Inspection Field not deleted." }, JsonRequestBehavior.AllowGet);

            }

        }
        [HttpPost]
        public ActionResult GenerateNewInspection(GenetateInspectionVM Model)
        {
            GenetateInspectionVM model = new GenetateInspectionVM();
            try
            {
                if (Model.InspectionID > 0)
                {

                    //add
                    model.BedSpaceID = Model.BedSpaceID;
                    model.InspectionID = Model.InspectionID;
                    model.CreatedDate = DateTime.Now;
                    model.CreatedBy = Common.Globals.User.Email;
                    model.UpdatedDate = DateTime.Now;
                    model.UpdatedBy = Common.Globals.User.Email;
                    model.Staff_Status = 1;
                    model.Student_Status = 1;
                    model.Remarks = Model.Remarks;
                    model.Maintenance_Status = 1;
                    model.IsEnable = true;

                    var result = inspectionService.GenerateInspectionsVM(model);
                    if (result == true)
                    {

                        return Json(new { 
                            status = true, 
                            Code = 200,
                            data = "Inspection Generated succesfully" 
                        }, JsonRequestBehavior.AllowGet);

                    }
                    else
                    {


                        return Json(new { 
                            status = false, 
                            Code = 404,
                            data = "Something went wrong, Inspection not Generated." 
                        }, JsonRequestBehavior.AllowGet);

                    }
                }

            }
            catch (Exception ex)
            {
                ViewBag.error = ex.Message;
                return Json(new { status = false, data = "Something went wrong, Inspection not Generated." }, JsonRequestBehavior.AllowGet);

            }
            return Json(new { status = true, data = "Inspection Generated succesfully" }, JsonRequestBehavior.AllowGet);
        }
        public ActionResult EditGenerateNewInspection(UpdateGenetatedInspectionsVM Model)
        {

            AllInspectionsData allInspectionsData = new AllInspectionsData();

            try
            {

                if (Model.ID > 0)
                {

                    GenerateInspection Inspection = inspectionService.UpdateGenerateInspectionsVM(Model);

                    if (Inspection.InspectionID != 0)
                    {

                        var data = inspectionService.getGeneratedInspections().ToList().Where(x => x.InspectionID == Inspection.InspectionID);

                        allInspectionsData.inspectionName = data.Where(x => x.InspectionID == Inspection.InspectionID).FirstOrDefault().InspectionName;
                        allInspectionsData.BedSpace_Name = data.Where(x => x.BedSpaceID == Inspection.BedSpaceID).FirstOrDefault().BedSpaceName;
                        allInspectionsData.GeneratedInspectionID = Model.ID;
                        var InspectionFields = inspectionService.getInspectionFieldsSelected().ToList().Where(x => x.Inspection.InspectionID == Inspection.InspectionID).ToList();


                        //for Fields Detail
                        List<InspectionFieldDetails> IFD = new List<InspectionFieldDetails>();






                        foreach (var IF in InspectionFields)
                        {
                            InspectionFieldDetails inspectionField = new InspectionFieldDetails();


                            //inspection field and field name
                            inspectionField.FieldID = IF.InspectionFieldValue;
                            try
                            {
                                inspectionField.InspectionFieldName = inspectionService.getInspectionFieldById(inspectionField.FieldID).ModelName.ToString();
                                if (inspectionField.InspectionFieldName == null || inspectionField.InspectionFieldName == "")
                                    continue;
                            }
                            catch (Exception)
                            {
                                if (inspectionField.InspectionFieldName == null || inspectionField.InspectionFieldName == "")
                                    continue;

                            }

                            bool rating_check = true;

                            //data for get rattings for fields data
                            var rating = inspectionService.getInspectionFields().Where(x => x.INspectionFieldID == IF.InspectionFieldValue);



                            //for field rating property
                            RatingLists Actual_Field_rating_Lists = new RatingLists();

                            //for rating list if rating
                            List<RatingLists> ratingList = new List<RatingLists>();

                            foreach (var rat in rating)
                            {
                                RatingLists ratings = new RatingLists();

                                int ID = rat.ratingListID;

                                ratings.RatingID = ID;

                                try
                                {
                                    ratings.RatingName = inspectionService.GetRating().Where(x => x.InspectionRatingListID == rat.ratingListID).FirstOrDefault().Name.ToString();
                                    if (ratings.RatingName == null || ratings.RatingName == "")
                                    {
                                        rating_check = false;
                                        continue;
                                    }


                                }
                                catch (Exception ert)
                                {
                                    if (ratings.RatingName == null || ratings.RatingName == "")
                                    {
                                        rating_check = false;
                                        continue;
                                    }

                                }

                                ratings.RatingNote = "";
                                ratings.ImageUrl = "";

                                var items = inspectionService.getRatingListItemsById(ID);

                                List<RatingListItem> listItems = new List<RatingListItem>();

                                listItems.AddRange(items);

                                try
                                {
                                    ratings.actualRatingLists = listItems.ToList();

                                }
                                catch (Exception exe)
                                {

                                }

                                ratingList.Add(ratings);

                            }

                            inspectionField.ratingLists = ratingList;

                            if (ratingList.Count <= 0)
                                rating_check = false;

                            if (!IFD.Any(item => item.FieldID == IF.InspectionFieldValue) && rating_check != false)
                                IFD.Add(inspectionField);
                        }

                        allInspectionsData.Fileds = IFD;




                        return Json(new { status = true, data = allInspectionsData }, JsonRequestBehavior.AllowGet);
                    }
                    else
                    {
                        return Json(new { status = false, data = "Something went wrong, Inspection not Generated." }, JsonRequestBehavior.AllowGet);

                    }

                }
                else
                {
                    return Json(new { status = false, data = "Something went wrong, Inspection not Generated." }, JsonRequestBehavior.AllowGet);
                }
            }

            catch (Exception ex)
            {
                ViewBag.error = ex.Message;
                return Json(new { status = false, data = "Something went wrong, Inspection not Generated." }, JsonRequestBehavior.AllowGet);

            }

        }
        public ActionResult GeneratedInspections()
        {
            ViewBag.Status = new SelectList(inspectionService.getStaus(), "ID", "Name");
            ViewBag.StaffStatus = new SelectList(inspectionService.getStaus(), "ID", "Name");
            ViewBag.StudentStatus = new SelectList(inspectionService.getStaus(), "ID", "Name");

            var result = inspectionService.getGeneratedInspections().ToList();
            return View(result);
        }

        [HttpPost]
        public ActionResult ADDInspectionDetails(List<InspectionRatingDataVM> Model)
        {

            try
            {
                if (Model.Count > 0)
                {
                    foreach (var item in Model)
                    {
                        InspectionRatingDataVM model = new InspectionRatingDataVM();
                        try
                        {
                            if (item.GeneratedInspectionID > 0)
                            {

                                //add
                                model.AssignedFieldID = item.AssignedFieldID;
                                model.ratingListID = item.ratingListID;
                                model.GeneratedInspectionID = item.GeneratedInspectionID;
                                model.SelectetRatinglistitemID = item.SelectetRatinglistitemID;
                                model.RatingNote = item.RatingNote;
                                model.RatingimageUrl = item.RatingimageUrl;
                                model.IsActive = item.IsActive;
                                model.IsEnable = item.IsEnable;

                                var result = inspectionService.ADDInspectionDetails(model);

                            }
                        }
                        catch (Exception erw)
                        {


                        }


                    }
                    return Json(new { status = true, data = "Inspection data saved succesfully" }, JsonRequestBehavior.AllowGet);


                }
                else
                {
                    return Json(new { status = false, data = "Something went wrong, Inspection data not saved succesfully." }, JsonRequestBehavior.AllowGet);

                }


            }
            catch (Exception ex)
            {
                ViewBag.error = ex.Message;
                return Json(new { status = false, data = "Something went wrong, Inspection not Generated." }, JsonRequestBehavior.AllowGet);

            }
           
        }


        [HttpPost]
        public ActionResult UploadFiles()
        {
            // Checking no of files injected in Request object  
            if (Request.Files.Count > 0)
            {
                try
                {
                    //  Get all files from Request object  
                    HttpFileCollectionBase files = Request.Files;
                    for (int i = 0; i < files.Count; i++)
                    {
                        //string path = AppDomain.CurrentDomain.BaseDirectory + "Uploads/";  
                        //string filename = Path.GetFileName(Request.Files[i].FileName);  

                        HttpPostedFileBase file = files[i];
                        string fname;

                        // Checking for Internet Explorer  
                        if (Request.Browser.Browser.ToUpper() == "IE" || Request.Browser.Browser.ToUpper() == "INTERNETEXPLORER")
                        {
                            string[] testfiles = file.FileName.Split(new char[] { '\\' });
                            fname = testfiles[testfiles.Length - 1];
                        }
                        else
                        {
                            fname = file.FileName;
                        }

                        // Get the complete folder path and store the file inside it.  
                        fname = Path.Combine(Server.MapPath("/Assets/Images/Uploads/"), fname);
                        file.SaveAs(fname);
                    }
                    // Returns message that successfully uploaded  
                    return Json("File Uploaded Successfully!");
                }
                catch (Exception ex)
                {
                    return Json("Error occurred. Error details: " + ex.Message);
                }
            }
            else
            {
                return Json("No files selected.");
            }
        }

        public ActionResult MaintenanceRequest()
        {
            var result = inspectionService.MaintenanceRequest();
            return View(result);
        }

        public ActionResult DeleteGenerateInspection(int id)
        {
            try
            {
                if (inspectionService.DeleteGenerateInspection(id))
                {

                    return Json(new { status = true, data = "Inspection deleted successfully." }, JsonRequestBehavior.AllowGet);
                }
                else
                {

                    return Json(new { status = false, data = "Sorry something went wrong, Inspection not deleted." }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = false, data = "Sorry something went wrong, Inspection not deleted." }, JsonRequestBehavior.AllowGet);

            }
        }

        [HttpGet]
        public ActionResult GeneratedInspectionView(int id)
        {

            var result = inspectionService.GenerateViewRequest(id);
            return View(result);
        }



    }
}