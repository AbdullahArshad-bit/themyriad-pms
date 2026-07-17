using PMS.Common.Filters;
using PMS.DTO.ViewModels.BedSpacePlacementViewModels;
using PMS.EF;
using PMS.Repository.UnitOfWork;
using PMS.Services.Services.BedSpacePlace;
using PMS.Services.Services.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PMS.Services.Services.Setup;
using PMS.Services.Services.Inspection;
using PMS.Common.Classes;
using PMS.Services.Services.UserManage;
using PMS.DTO.ViewModels.ContractViewModels;
using System.Data.Entity.Core.Objects;
using System.Threading.Tasks;
using PMS.Classes;
using Org.BouncyCastle.Asn1.X509;
using PMS.DTO.ViewModels.BookingViewModels;
using PMS.DTO.ViewModels.PersonManageViewModels;
using PMS.Services.Services.Booking;
using PMS.Common;
using System.Data.Entity;
using PMS.Services.Services.Invoicings;
using PMS.Services.Services.Service;
using System.Transactions;
using PMS.Services.Services.LocationContext;

namespace PMS.Controllers
{
    [AuthorizeUser]
    public class BedSpacePlacementController : BaseController
    {
        private readonly UnitOfWork<PMSEntities> uow;
        private readonly IBedSpacePlacementService placementService;
        private readonly ISetupService setupService;
        private readonly IServicesService _service;
        private readonly IInspectionService inspectionService;
        private readonly IContractManageService contractManageService;
        private readonly IUserManageService UserManageService;
        private readonly IBookingService _bookingService;
        private readonly ILocationContextService locationContextService;

        public BedSpacePlacementController(IBedSpacePlacementService _placementService, UnitOfWork<PMSEntities> _uow, ISetupService _setupService, IInspectionService _inspectionService
            , IContractManageService _contractManageService, IUserManageService _UserManageService, IBookingService bookingService, IServicesService service
            , ILocationContextService _locationContextService)
        {
            placementService = _placementService;
            uow = _uow;
            setupService = _setupService;
            inspectionService = _inspectionService;
            contractManageService = _contractManageService;
            UserManageService = _UserManageService;
            _bookingService = bookingService;
            _service = service;
            locationContextService = _locationContextService;

        }


        #region Get

        [AuthorizeUser(Roles = AppUserRoles.View_BedSpacePlacement)]
        public ActionResult PlacementsList(int? personId, int? id, string success, PlacementsBinding request)
        {
            if (request.ToDate == null)
            {
                var today = DateTime.Now.Date;

                request.ToDate = today;
            }

            ViewBag.FromDate = request.FromDate.HasValue ? request.FromDate.Value.ToString("dd/MMM/yyyy") : null;
            ViewBag.ToDate = request.ToDate.HasValue ? request.ToDate.Value.ToString("dd/MMM/yyyy") : null;
            ViewBag.Inspection = new SelectList(inspectionService.getInspections(), "InspectionID", "InspectionName");
            ViewBag.ContractId = new SelectList(contractManageService.GetContracts().Where(x => x.IsPublish == true && x.IsActive == true), "ContractID", "ContractName");
            ViewBag.Terms = new SelectList(_bookingService.GetPriceConfigurations(), "Value", "Text");
            ViewBag.Commitments = new SelectList(_bookingService.GetPriceConfigurations(), "Value", "Text");
            ViewBag.Services = new SelectList(_service.GetServices().Where(x => x.IsActive == true && x.ServiceTypeId == (int)Enumeration.ServiceTypes.RentalCharges), "ServiceId", "ServiceName");
            ViewBag.id = id;
            ViewBag.personId = personId;
            if (!string.IsNullOrEmpty(success))
            {
                ViewBag.success = TempData["success"] = success;
            }
            return View();
        }

        public object PlacementsListAjax(PlacementsBinding request)
        {
            var Placement = new PlacementsResponse();
            Placement = placementService.GetPlacements(request, "", request.search.value, request.start, request.length, request.query, request.FromDate, request.ToDate, Convert.ToInt32(request.personId), Convert.ToInt32(request.id), request.orderBy, request.orderDir);
            var result = Json(new { draw = request.draw, recordsFiltered = Placement.RecordsFiltered, recordsTotal = Placement.TotalRecords, data = Placement.PlacementList.ToList() });
            return result;

        }

        [HttpGet]
        [AuthorizeUser(Roles = AppUserRoles.Migrate_BedSpacePlacement)]
        public ActionResult BedSpacePlacementMigration(int PlacementId)
        {
            if (PlacementId == null || PlacementId == 0)
            {
                return RedirectToAction("PlacementsList");
            }

            else
            {
                var placement = placementService.GetBedSpacePlacementById(PlacementId);
                var locationId = uow.GenericRepository<EF.Booking>().Table
                    .Where(x => x.BookingID == placement.BookingID)
                    .Select(x => x.LocationID)
                    .FirstOrDefault();

                BedSpacePlacementMigrationVM model = new BedSpacePlacementMigrationVM()
                {
                    PlacementHistory = placementService.GetMigrationHistoryByPlacementId(PlacementId),
                    PlacementId = PlacementId,
                    LocationId = locationId??0,
                    IsCheckedIn = placement.CheckIn != null
                };

                if (TempData["error"] != null)
                {
                    ViewBag.error = TempData["error"];
                }

                ViewBag.BedSpaceId = new SelectList(placementService.GetAvailableBedSpaces(), "Value", "Text");
                return View(model);
            }
        }

        #endregion

        #region Import/Export
        public ActionResult ImportPlacementFromExcel(ImportFromExcelVM model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    FileUpload upload = new FileUpload();
                    FileUploadResult result = upload.Upload(model.fileBase, Globals.UploadDirectory);
                    if (result.Success)
                    {
                        List<AddBedSpacePlacementVM> savedPlacementList = new List<AddBedSpacePlacementVM>();
                        List<AddBedSpacePlacementVM> notSavedPlacementList = new List<AddBedSpacePlacementVM>();
                        ExcelHelper helper = new ExcelHelper();
                        var list = helper.GetPlacementData(result.LocalFilePath);
                        if (list.Count > 0)
                        {
                            foreach (var placement in list)
                            {
                                try
                                {
                                    if (placementService.ImportBedSpacePlacement(placement).BedSpacePlacementID > 0)
                                    {
                                        savedPlacementList.Add(placement);
                                        continue;
                                    }
                                    else
                                    {
                                        notSavedPlacementList.Add(placement);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    notSavedPlacementList.Add(placement);
                                }
                            }

                            ViewBag.SavedPlacements = savedPlacementList;

                            ViewBag.NotSavedPlacements = notSavedPlacementList;

                            ViewBag.success = "Following is the imported Placements detail.";

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

            return RedirectToAction("Booking");
        }

        public async Task<ActionResult> ExportPlacementReport(DateTime? FromDate, DateTime? ToDate, bool inHouse, int termID = 0, string query = null)
        {
            if (query == "null") query = null;

            string QueryBy = "Excel";

            var place = await placementService.GetPlacementsExportAsync(QueryBy, query, FromDate, ToDate);
            var report = place.PlacementList;

            var assignedLocationIds = locationContextService.GetAssignedLocationIds();
            if (inHouse)
                report = report.Where(x => x.CheckIn != null && x.CheckOut == null).ToList();
            var studentContractPlacementIds = await uow.Context.StudentContracts
                .Select(con => con.PlacementId)
                .ToListAsync();

            var filteredReport = report
                .Where(x => assignedLocationIds.Contains((int)x.LocationID) &&
                (string.IsNullOrEmpty(query) || !studentContractPlacementIds.Contains(x.BedSpacePlacementID)) &&
                (FromDate == null || x.BedSpacePlacementCreatedDate.Date >= FromDate.Value.Date) &&
                (termID == 0 || x.PriceConfigID == termID) &&
                (ToDate == null || x.BedSpacePlacementCreatedDate.Date <= ToDate.Value.Date))
                .Select(x => new
                {
                    x.LocationName,
                    x.Title,
                    x.FullName,
                    x.Gender,
                    x.Block,
                    x.BedSpace,
                    x.Room,
                    x.RoomType,
                    RateCode = x.Commitment,
                    RateAmount = x.Price,
                    BilledUpto = x.BilledUpto.HasValue ? x.BilledUpto.Value.ToString("dd/M/yyyy") : null,
                    MoveIn = x.MoveIn.ToString("dd/M/yyyy"),
                    MoveOut = x.MoveOut.ToString("dd/M/yyyy"),
                    CheckIn = x.CheckIn.HasValue ? x.CheckIn.Value.ToString("dd/M/yyyy h:mm tt") : null,
                    CheckOut = x.CheckOut.HasValue ? x.CheckOut.Value.ToString("dd/M/yyyy h:mm tt") : null,
                    x.Email,
                    x.Phone,
                    DOB = x.DOB.ToString("dd/M/yyyy"),
                    x.University,
                    x.Requests,
                    x.BedSpacePlacementCreatedDate
                }).ToList();

            // Determine the file name based on the query
            string fileName = query == "NoContract" ? "Contracts to be Generated PMS" : "All Bed Space Placements PMS";

            // Export the data to Excel
            ExcelHelper.ExportToExcel(Response, filteredReport, fileName);

            return File(Response.OutputStream, "application/vnd.ms-excel", fileName + ".xlsx");
        }

        #endregion

        #region CRUD

        [AuthorizeUser(Roles = AppUserRoles.Add_BedSpacePlacement)]
        public ActionResult AddPlacements(int bookingId, int? placementId)
        {
            if (bookingId > 0)
            {
                var exist = uow.GenericRepository<EF.Booking>().Table.Where(x => x.IsEnable == true && x.BookingID == bookingId).FirstOrDefault();
                if (exist != null)
                {
                    AddBedSpacePlacementVM model = new AddBedSpacePlacementVM
                    {
                        MoveIn = DateTime.Today,
                        MoveOut = DateTime.Today,
                        BookingID = bookingId,
                        AllBedSpacesList = placementService.GetAvailableBedSpaces(),
                        BedSpacesList = placementService.GetAvailableBedSpacesForRoomType(exist.PriceConfig.RoomType.RoomName),

                        Requests = exist.AccessibilityRequest
                    };
                    if (placementId > 0)
                    {
                        //update existing placement if found
                        var placement = placementService.GetBedSpacePlacementById(Convert.ToInt32(placementId));
                        if (placement != null)
                        {
                            if (placement.CheckOut != null)
                            {
                                TempData["error"] = "You can not edit a placement when a person checks out.";
                                return RedirectToAction("PlacementsList");
                            }

                            model.BedSpacePlacementID = placement.BedSpacePlacementID;
                            model.BookingID = placement.BookingID;
                            model.BedSpaceID = placement.BedSpaceID;
                            model.MoveIn = placement.MoveIn;
                            model.MoveOut = placement.MoveOut;
                            model.Requests = placement.AccessibilityRequest;
                            var bedspace = placementService.GetBedSpaceByID(placement.BedSpaceID);
                            model.BedSpacesList.Add(bedspace);

                        }

                        else
                        {
                            TempData["error"] = "Please select placement to update.";
                            return RedirectToAction("PlacementsList");
                        }
                    }

                    else
                    {
                        //add new placement
                        bool exist1 = uow.GenericRepository<EF.BedSpacePlacement>().Table.Any(x => x.IsEnable == true && x.BookingID == bookingId);
                        if (exist1)
                        {
                            TempData["error"] = "Bed space already allocated for this booking.";
                            return RedirectToAction("BookingList", "Booking");
                        }
                        var latestBed = placementService.AssignBedSpaceToPerson(bookingId, model);
                    }


                    if (exist.CheckOutDate.HasValue)
                    {
                        model.Duration = (exist.CheckOutDate.Value - exist.CheckInDate).Days;
                    }
                    else
                    {
                        model.Duration = 1; // Default duration if CheckOutDate is not set
                    }

                    return View(model);
                }

                else
                {
                    //wrong booking id
                    TempData["error"] = "Please select add bed space placement action for booking.";
                }
            }

            return RedirectToAction("BookingList", "Booking");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeUser(Roles = AppUserRoles.Add_BedSpacePlacement)]
        public ActionResult AddPlacements(AddBedSpacePlacementVM model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (model.BedSpacePlacementID > 0)
                    {
                        model.UpdatedBy = PMS.Common.Globals.User.Email;
                        model.UpdatedDate = DateTime.Now;

                        placementService.UpdateBedSpacePlacement(model);
                        TempData["success"] = "Bed space placement updated successfully.";
                    }

                    else
                    {
                        model.CreatedBy = PMS.Common.Globals.User.Email;
                        model.CreatedDate = DateTime.Now;

                        if (placementService.AddBedSpacePlacement(model).BedSpacePlacementID > 0)
                        {
                            TempData["success"] = "Bed space placement created successfully.";
                        }

                        else
                        {
                            TempData["error"] = "Bed space placement not created. Please try again later.";
                        }
                    }
                }

                else
                {
                    TempData["error"] = "Model Error";
                }
            }

            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
            }

            return RedirectToAction("PlacementsList");
        }

        [AuthorizeUser(Roles = AppUserRoles.Update_Placement_Dates)]
        public ActionResult EditPlacementDates(int bookingId, int? placementId)
        {
            if (bookingId > 0)
            {
                var exist = uow.GenericRepository<EF.Booking>().Table.Where(x => x.IsEnable == true && x.BookingID == bookingId).FirstOrDefault();
                if (exist != null)
                {
                    AddBedSpacePlacementVM model = new AddBedSpacePlacementVM
                    {
                        MoveIn = DateTime.Today,
                        MoveOut = DateTime.Today,
                        CheckIn = DateTime.Today,
                        CheckOut = DateTime.Today,
                        BookingID = bookingId,

                    };
                    if (placementId > 0)
                    {
                        //update existing placement if found
                        var placement = placementService.GetBedSpacePlacementById(Convert.ToInt32(placementId));
                        if (placement != null)
                        {
                            model.BedSpacePlacementID = placement.BedSpacePlacementID;
                            model.BookingID = placement.BookingID;
                            model.BedSpaceID = placement.BedSpaceID;
                            model.MoveIn = placement.MoveIn;
                            model.MoveOut = placement.MoveOut;
                            model.CheckIn = placement.CheckIn ?? DateTime.MinValue;
                            model.CheckOut = placement.CheckOut ?? DateTime.MinValue;
                        }

                        else
                        {
                            TempData["error"] = "Please select placement to update.";
                            return RedirectToAction("PlacementsList");
                        }
                    }

                    return View(model);
                }

                else
                {
                    //wrong booking id
                    TempData["error"] = "Please select add bed space placement action for booking.";
                }
            }

            return RedirectToAction("BookingList", "Booking");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeUser(Roles = AppUserRoles.Update_Placement_Dates)]
        public ActionResult EditPlacementDates(AddBedSpacePlacementVM model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    model.UpdatedBy = PMS.Common.Globals.User.Email;
                    model.UpdatedDate = DateTime.Now;

                    placementService.UpdateBedSpacePlacementDate(model);
                    TempData["success"] = "Bed space placement updated successfully.";
                }

                else
                {
                    TempData["error"] = "Model Error";
                }
            }

            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
            }

            return RedirectToAction("PlacementsList");
        }
        [HttpPost]
        [AuthorizeUser(Roles = AppUserRoles.Delete_BedSpacePlacement)]
        public ActionResult DeletePlacement(int id)
        {
            try
            {
                if (id > 0)
                {
                    var result = placementService.DeleteBedSpacePlacement(id);

                    if (result == true)
                    {
                        TempData["success"] = "Bed space placement deleted successfully.";
                    }
                }

                else
                {
                    ViewBag.error = "Something went wrong, Placement not deleted.";
                }
            }

            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
            }
            return Json(new { status = true, data = "Bed space placement deleted successfully." }, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region POST

        [HttpPost]
        public async Task<ActionResult> CheckIn(int id, DateTime checkIntime, string cardNumber, string encoderNumber)
        {
            try
            {
                if (await placementService.CheckInPlacement(id, checkIntime, cardNumber, encoderNumber))
                {
                    TempData["success"] = "Check in updated successfully.";
                    return Json(new { status = true, success = TempData["success"], error = "" });
                }
                else
                {
                    TempData["error"] = "Encoder timeout or Room is not valid. Check in not updated. Please try again later.";
                    return Json(new { status = false, success = "", error = TempData["error"] });
                }
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return Json(new { status = false, error = TempData["error"] });
            }
        }

        [HttpPost]
        public async Task<ActionResult> CheckOut(int id, DateTime checkOuttime)
        {
            try
            {
                if (await placementService.CheckOutPlacementAsync((int)id, checkOuttime))
                {
                    TempData["success"] = "Check out updated successfully.";
                    return Json(new { status = true, success = TempData["success"], error = "" });
                }
                else
                {
                    TempData["error"] = "Check out date could not be earlier than check-in date.";
                    return Json(new { status = false, success = "", error = TempData["error"] });
                }
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return Json(new { status = false, error = TempData["error"] });
            }
        }

        [HttpPost]
        [AuthorizeUser(Roles = AppUserRoles.Migrate_BedSpacePlacement)]
        public async Task<ActionResult> BedSpacePlacementMigration(BedSpacePlacementMigrationVM migrationVM)
        {
            try
            {
                if (await placementService.SwapBedSpacePlacementAsync(migrationVM))
                {
                    TempData["success"] = "Bed Space has migrated successfully!";
                    return RedirectToAction("PlacementsList");
                }

                TempData["error"] = "You can not change room for this this time please try again later.";
                return RedirectToAction("BedSpacePlacementMigration", new { PlacementId = migrationVM.PlacementId });
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction("BedSpacePlacementMigration", new { PlacementId = migrationVM.PlacementId });
            }
        }

        public async Task<ActionResult> ReissueMSCHcard(int id, string encoderNumber)
        {
            try
            {
                if (await placementService.ReissueCard(id, encoderNumber))
                {
                    TempData["success"] = "Re-Issue card updated successfully.";
                    return Json(new { status = true, success = TempData["success"], error = "" });
                }
                else
                {
                    TempData["error"] = "Encoder timeout or Room is not valid, Re-Issue card in not updated. Please try again.";
                    return Json(new { status = false, success = "", error = TempData["error"] });
                }
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return Json(new { status = false, error = TempData["error"] });
            }
        }


        #endregion


        public ActionResult Calender()

        {
            var buildings = setupService.GetBuildings();

            ViewBag.Buildings = new SelectList(buildings, "BuildingID", "BuildingName");
            return View();
        }


    }
}