using Intuit.Ipp.LinqExtender;
using PMS.Classes;
using PMS.Common;
using PMS.Common.Classes;
using PMS.Common.Filters;
using PMS.DTO.ViewModels;
using PMS.DTO.ViewModels.ApiViewModels;
using PMS.DTO.ViewModels.BedSpacePlacementViewModels;
using PMS.DTO.ViewModels.BookingViewModels;
using PMS.DTO.ViewModels.PersonManageViewModels;
using PMS.DTO.ViewModels.SetupViewModels;
using PMS.EF;
using PMS.Repository.UnitOfWork;
using PMS.Services.Services.BedSpacePlace;
using PMS.Services.Services.Booking;
using PMS.Services.Services.BusStop;
using PMS.Services.Services.Contracts;
using PMS.Services.Services.Feedback;
using PMS.Services.Services.Inspection;
using PMS.Services.Services.Person;
using PMS.Services.Services.Service;
using PMS.Services.Services.Setup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PMS.Controllers
{
    [AuthorizeUser]
    public class BookingController : BaseController
    {
        private readonly UnitOfWork<PMSEntities> uow;
        private readonly IBookingService bookingService;
        private readonly IInspectionService inspectionService;
        private readonly IContractManageService contractManageService;
        private readonly IBedSpacePlacementService placementService;
        private readonly IPersonService personService;

        public BookingController(UnitOfWork<PMSEntities> _uow, IBookingService _bookingService, IInspectionService _inspectionService, IContractManageService _contractManageService, IBedSpacePlacementService _placementService, IPersonService _personService)
        {
            uow = _uow;
            bookingService = _bookingService;
            inspectionService = _inspectionService;
            contractManageService = _contractManageService;
            placementService = _placementService;
            personService = _personService;
        }

        [AuthorizeUser(Roles = AppUserRoles.View_BookingList)]
        public ActionResult BookingList(BookingsBinding request)
        {
            var today = DateTime.Today;

            if (request.FromDate == null || request.ToDate == null)
            {
                // FromDate: always last year's September 1
                request.FromDate = new DateTime(today.Year - 1, 9, 1);

                var aprilFirstThisYear = new DateTime(today.Year, 4, 1);

                if (today >= aprilFirstThisYear)
                {
                    // After April 1 → push ToDate to next year's Sep 1
                    request.ToDate = new DateTime(today.Year + 1, 9, 1);
                }
                else
                {
                    // Before April 1 → current year's Sep 1
                    request.ToDate = new DateTime(today.Year, 9, 1);
                }
            }

            ViewBag.FromDate = request.FromDate.HasValue ? request.FromDate.Value.ToString("dd/MMM/yyyy") : null;
            ViewBag.ToDate = request.ToDate.HasValue ? request.ToDate.Value.ToString("dd/MMM/yyyy") : null;
            ViewBag.error = TempData["error"];
            ViewBag.success = TempData["success"];
            return View();
        }

        public object LoadBookingList(BookingsBinding request)
        {
            var Booking = new BookingsResponse();
            ViewBag.FromDate = request.FromDate.HasValue ? request.FromDate.Value.ToString("dd/MMM/yyyy") : null;
            ViewBag.ToDate = request.ToDate.HasValue ? request.ToDate.Value.ToString("dd/MMM/yyyy") : null;
            Booking = bookingService.GetPMSBookings(request);
            var result = Json(new { draw = request.draw, recordsFiltered = Booking.RecordsFiltered, recordsTotal = Booking.TotalRecords, data = Booking.BookingList.ToList() });
            return result;
        }

        public void ExportBookingReport(string QueryBy, string query = null, DateTime? FromDate = null, DateTime? ToDate = null)
        {
            QueryBy = "Excel";
            var booking = bookingService.GetPMSExportBookings(QueryBy, query, FromDate, ToDate);
            var report = booking.BookingList;
            var data = report.Select(x => new
            {
                Location = x.LocationName,
                x.BookingNumber,
                x.Title,
                x.MyriadID,
                x.FullName,
                x.Gender,
                CheckInDate = x.CheckInDate.ToString("dd/M/yyyy"),
                CheckOutDate = x.CheckOutDate.HasValue ? x.CheckOutDate.Value.ToString("dd/M/yyyy") : null,
                Occupancy = x.Commitment,
                x.RoomType,
                x.Status,
                x.Email,
                x.University,
                x.Phone,
                x.Channel,
                x.Source,
                x.UniReferenceNo,
                x.PaymentType,
                x.AccessibilityRequest,
                BookingDate = x.BookingDate.ToString("dd/M/yyyy"),
                x.HearFrom,
                x.Nationality,
                x.TenantPassportNumber,
                x.GuardianFullName,
                x.GuardianPhone,
                x.GuardianEmail,
                x.GuardianRelation,
                x.PrefereableFloor,
                x.PrefereableView,
                x.Religions,
                x.Nationalities,
                x.Universities,
                x.AgeRange

            });
            ExcelHelper.ExportToExcel(Response, data, "Booking List - PMS");
            return;
        }

        [AuthorizeUser(Roles = AppUserRoles.Add_Booking)]
        public ActionResult AddBooking(int? personId, int? bookingId)
        {
            var model = new AddBookingVM();
            if (personId.HasValue)
            {
                ViewBag.StudentId = new SelectList(personService.GetPersons().Select(x => new { x.PersonID, FullName = x.Code + ": " + x.FullName }), "PersonID", "FullName", personId);
                bool exist = uow.Context.People.Any(x => x.IsEnable == true && x.PersonID == personId);
                if (exist)
                {
                    model.CheckInDate = DateTime.Today;
                    model.CheckOut = DateTime.Today;
                    model.PriceConfigList = bookingService.GetWebsitePriceConfigurations();
                    model.PersonID = personId.Value;

                    if (bookingId.HasValue && bookingId > 0)
                    {
                        var booking = bookingService.GetBookingByID(bookingId.Value);
                        if (booking != null && booking.PersonID == personId)
                        {
                            model.BookingID = booking.BookingID;
                            model.PriceConfigID = booking.PriceConfigID;
                            model.CheckInDate = booking.CheckInDate;
                            model.CheckOut = booking.CheckOutDate;
                            model.Requests = booking.AccessibilityRequest;
                        }
                    }
                    return View(model);
                }
            }

            else
            {
                ViewBag.StudentId = new SelectList(personService.GetPersons().Select(x => new { x.PersonID, FullName = x.Code + ": " + x.FullName }), "PersonID", "FullName");
                model.CheckInDate = DateTime.Today;
                model.CheckOut = DateTime.Today;
                model.PriceConfigList = bookingService.GetWebsitePriceConfigurations();
                return View(model);
            }

            TempData["error"] = "Please select add booking action for person to place a booking.";
            return RedirectToAction("Persons", "PersonManage");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeUser(Roles = AppUserRoles.Add_Booking)]
        public ActionResult AddBooking(AddBookingVM model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (model.BookingID > 0)
                    {
                        model.UpdatedBy = PMS.Common.Globals.User.Email;
                        model.UpdatedDate = DateTime.Now;

                        bookingService.UpdateBooking(model);
                        TempData["success"] = "Booking updated successfully.";
                        return RedirectToAction("BookingList");
                    }
                    else
                    {
                        model.CreatedBy = PMS.Common.Globals.User.Email;
                        model.CreatedDate = DateTime.Now;

                        if (bookingService.AddBooking(model).BookingID > 0)
                        {
                            TempData["success"] = "Booking saved successfully.";

                        }
                    }
                }
                else
                {
                    TempData["error"] = "Model Error.";
                    return RedirectToAction("AddBooking", new { personId = model.PersonID, bookingId = model.BookingID });
                }
            }

            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
            }

            return RedirectToAction("Persons", "PersonManage");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeUser(Roles = AppUserRoles.Delete_Booking)]
        public ActionResult DeleteBooking(int id)
        {
            try
            {
                if (bookingService.DeleteBooking(id))
                {
                    TempData["success"] = "Booking deleted successfully.";
                }
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
            }

            return RedirectToAction("BookingList");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeUser(Roles = AppUserRoles.Cancel_Booking)]
        public ActionResult CancelBooking(int id)
        {
            try
            {
                if (bookingService.CancelBooking(id))
                {
                    TempData["success"] = "Booking status changed successfully.";
                }
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
            }

            return RedirectToAction("BookingList");
        }

        public ActionResult GetCommitDetail(int id)
        {
            var model = bookingService.GetPriceConfigDetailByID(id);
            return View(model);
        }

        public ActionResult ImportBookingFromExcel(ImportFromExcelVM model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    FileUpload upload = new FileUpload();
                    FileUploadResult result = upload.Upload(model.fileBase, Globals.UploadDirectory);
                    if (result.Success)
                    {
                        List<AddBookingVM> savedBookingList = new List<AddBookingVM>();
                        List<AddBookingVM> notSavedBookingList = new List<AddBookingVM>();
                        ExcelHelper helper = new ExcelHelper();
                        var list = helper.GetBookingData(result.LocalFilePath);
                        if (list.Count > 0)
                        {
                            foreach (var booking in list)
                            {
                                try
                                {
                                    if (bookingService.AddImportBooking(booking).PersonID > 0)
                                    {
                                        savedBookingList.Add(booking);
                                        continue;
                                    }
                                    else
                                    {
                                        notSavedBookingList.Add(booking);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    notSavedBookingList.Add(booking);
                                }
                            }

                            ViewBag.SavedPersons = savedBookingList;

                            ViewBag.NotSavedPersons = notSavedBookingList;

                            ViewBag.success = "Following is the imported Bookings detail.";

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

        public ActionResult GetPlacementDetail(int id)
        {
            var model = bookingService.GetPlacementDetailID(id);
            return View(model);
        }

        public ActionResult PersonGuest()
        {
            var personGuestList = placementService.GetGuestCounts();
            ViewBag.success = TempData["success"];
            ViewBag.error = TempData["error"];
            return View(personGuestList);
        }

        [HttpGet]
        public ActionResult AddGuestCount()
        {
            ViewBag.StudentId = new SelectList(personService.GetCheckInPersons().Select(x => new { x.PersonID, FullName = x.Code + ": " + x.FullName }), "PersonID", "FullName");
            return View();
        }

        [HttpPost]
        public ActionResult AddGuestCount(GuestCountVm personGuest)
        {
            if (personGuest.BedSpacePlacementID > 0)
            {
                bool save = bookingService.SavePersonGuest(personGuest);
                if (save)
                {
                    TempData["success"] = "Head count added successfully";
                }
                else
                {
                    TempData["error"] = "Something went wrong";
                }
            }
            return RedirectToAction("PersonGuest");
        }
        public ActionResult CheckOutPersonGuest(GuestDetailVm heeadCountVM)
        {
            if (heeadCountVM.ID > 0)
            {
                bool res1 = bookingService.CheckOutGuest(heeadCountVM);
                if (res1 != false)
                {
                    TempData["success"] = "Guest check out successfully";
                }
                else
                {
                    TempData["error"] = "Something went Wrong";
                }
            }
            return RedirectToAction("PersonGuest");
        }
        [AllowAnonymous]
        public ActionResult UploadReceipt(string encIds)
        {
            var model = new UploadReceiptVM();
            string decryptUrl = PMS.Common.Security.StringCipher.DecryptFeedback(encIds);
            string[] hList = decryptUrl.Split(',');
            model.PersonId = Convert.ToInt16(hList[0]);
            model.BookingId = Convert.ToInt16(hList[1]);
            bool resp = bookingService.Checkupload(model.PersonId, model.BookingId);
            if (resp == true)
            {
                return RedirectToAction("Alreadyupload");
            }
            var res = bookingService.GetBookingData(model.PersonId, model.BookingId);
            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        public ActionResult UploadReceipt(UploadReceiptVM uploadReceiptVM)
        {
            var res = bookingService.AddReceipt(uploadReceiptVM);
            if (res == true)
            {
                TempData["success"] = "University Added Successfully!";
                return RedirectToAction("Receiptuploadsuccess");
            }
            return RedirectToAction("UploadReceipt");
        }

        [AllowAnonymous]
        public ActionResult Receiptuploadsuccess()
        {

            return View();
        }

        [AllowAnonymous]
        public ActionResult Alreadyupload()
        {

            return View();
        }
        public ActionResult ViewReceipt(int BookingID)
        {
            var model = new UploadReceiptVM();
            var receipt = bookingService.GetReceipt(BookingID);

            model = receipt;
            return View(model);
        }
    }
}