using PMS.Common.Filters;
using PMS.EF;
using PMS.Repository.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using System.Web.Mvc;
using PMS.Services.Services.Person;
using PMS.Services.Services.Reporting;
using PMS.Common.Classes;
using PMS.Services.Services.Service;
using PMS.Services.Services.PaymentTypes;
using PMS.Services.Services.Setup;
using PMS.DTO.ViewModels.ReportingViewModels;
using PMS.Services.Services.Booking;
using Microsoft.Reporting.WebForms;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net;
using System.Data;
using System.Threading.Tasks;
using PMS.Classes;
using PMS.Services.Services.UserManage;
using PMS.Services.Services.Ticket;
using PMS.Services.Services.VehicleRoutes;
using PMS.Services.Services.Vehicle;
using PMS.Services.Services.Schedule;
using PMS.Services.Services.BedSpacePlace;
using PMS.Services.Services.LocationContext;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using PMS.DTO.ViewModels.ApiViewModels;
using System.Configuration;
using Intuit.Ipp.Data;

namespace PMS.Controllers
{
    [AuthorizeUser]
    public class ReportingController : BaseController
    {
        // GET: Reporting
        private readonly IUnitOfWork<PMSEntities> uow;
        private readonly IReportingService reportingService;
        private readonly IServicesService servicesService;
        private readonly IPersonService personService;
        private readonly IPaymentTypesService paymentTypesService;
        private readonly ISetupService setupService;
        private readonly IBookingService bookinService;
        private readonly IUserManageService userService;
        private readonly ITicketService ticketService;
        private readonly IVehicleRoutesService routeService;
        private readonly IVehicleService vehicleService;
        private IScheduleService scheduleService;
        private readonly IBedSpacePlacementService placementService;
        private readonly ILocationContextService locationContextService;


        public ReportingController(IUnitOfWork<PMSEntities> _uow, IReportingService _reportingService, IPersonService _personService, IServicesService _servicesService,
            IPaymentTypesService _paymentTypesServices_, ISetupService _setupService, IBookingService _bookinService,
            IUserManageService _userService, ITicketService _ticketService, IVehicleRoutesService _routeService, IVehicleService _vehicleService,
            IScheduleService _scheduleService, IBedSpacePlacementService _placementService, ILocationContextService _locationContextService)
        {
            reportingService = _reportingService;
            uow = _uow;
            personService = _personService;
            servicesService = _servicesService;
            paymentTypesService = _paymentTypesServices_;
            setupService = _setupService;
            bookinService = _bookinService;
            userService = _userService;
            ticketService = _ticketService;
            routeService = _routeService;
            vehicleService = _vehicleService;
            scheduleService = _scheduleService;
            placementService = _placementService;
            locationContextService = _locationContextService;
        }

        [AuthorizeUser(Roles = AppUserRoles.View_Ageing_Report)]
        public ActionResult AgeingReport(DateTime? FromDate, DateTime? ToDate, int? StudentId = 0)
        {
            if (FromDate == null)
            {
                FromDate = new DateTime(2021, 9, 1);

                ToDate = DateTime.Now.Date;
            }
            ViewBag.FromDate = FromDate.HasValue ? FromDate.Value.ToString("dd/MMM/yyyy") : null;
            ViewBag.ToDate = ToDate.HasValue ? ToDate.Value.ToString("dd/MMM/yyyy") : null;
            ViewBag.StudentId = new SelectList(personService.GetPersons().Select(x => new { x.PersonID, FullName = x.Code + ": " + x.FullName }), "PersonID", "FullName", StudentId);

            return View();
        }

        public object loadAgeingReportbyAjax(AgeingReportBinding request, string draw, int studentid = 0)
        {
            ViewBag.FromDate = request.FromDate.HasValue ? request.FromDate.Value.ToString("dd/MMM/yyyy") : null;
            ViewBag.ToDate = request.ToDate.HasValue ? request.ToDate.Value.ToString("dd/MMM/yyyy") : null;
            var ageingReport = reportingService.GetAgeingReport(request, request.FromDate, request.ToDate, int.Parse(request.start), int.Parse(request.length), studentid, request.search.value).ToList();
            var totalrecords = ageingReport.Select(x => x.TotalRecords).FirstOrDefault();
            var result = Json(new { draw = draw, recordsFiltered = totalrecords, recordsTotal = totalrecords, data = ageingReport });
            return result;
        }

        public void ExportAgeingReport(DateTime? FromDate, DateTime? ToDate, int? studentid = 0)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            var _returnModel = reportingService.ExportAgeingReport(FromDate, ToDate, studentid).ToList();

            var data = _returnModel.Select(x => new
            {
                x.Title,
                x.MyriadID,
                x.FullName,
                x.Gender,
                x.BedSpace,
                x.Room,
                x.RoomType,
                x.Commitment,
                MoveIn = x.MoveIn.HasValue ? x.MoveIn.Value.ToString("dd/M/yyyy") : null,
                MoveOut = x.MoveOut.HasValue ? x.MoveOut.Value.ToString("dd/M/yyyy") : null,
                CheckIn = x.CheckIn.HasValue ? x.CheckIn.Value.ToString("dd/M/yyyy") : null,
                CheckOut = x.CheckOut.HasValue ? x.CheckOut.Value.ToString("dd/M/yyyy") : null,
                x.Email,
                x.Phone,
                //BSPCreatedDate = x.BSPCreatedDate.HasValue ? x.BSPCreatedDate.Value.ToString("dd/M/yyyy") : null,
                x.University,
                x.Status,
                x.OutstandingAmount,
                x.Last0To7DaysBalance,
                x.Last8To30DaysBalance,
                x.Last31To60DaysBalance,
                x.Last61To120DaysBalance,
                x.Last121To180DaysBalance,
                x.Above180DaysBalance,
                x.LiabilityBalance,
            }).Where(x => x.OutstandingAmount != 0).ToList();

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("AgeingReport");

                worksheet.Cells["A1"].LoadFromCollection(data, true);

                int[] columnsToFormat = { 17, 18, 19, 20, 21, 22, 23 };

                foreach (int col in columnsToFormat)
                {
                    worksheet.Column(col).Style.Numberformat.Format = "0.00;(0.00)";
                }

                worksheet.Cells.AutoFitColumns();

                var fileName = "AgeingReport - PMS.xlsx";
                Response.Clear();
                Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                Response.AddHeader("content-disposition", $"attachment; filename={fileName}");
                Response.BinaryWrite(package.GetAsByteArray());
                Response.End();
            }
        }

        public ActionResult AccountingVouchersReport(DateTime? fromDate = null, DateTime? toDate = null, string reportType = null)
        {
            if (fromDate == null || toDate == null)
            {
                fromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                toDate = DateTime.Now.Date;
            }
            ViewBag.FromDate = fromDate?.ToString("dd/MMM/yyyy") ?? "";
            ViewBag.ToDate = toDate?.ToString("dd/MMM/yyyy") ?? "";
            ViewBag.ReportType = reportType;
            return View();
        }

        public object loadAccountingVouchersReportbyAjax(AccountingVoucherBinding request, string draw)
        {
            ViewBag.FromDate = request.FromDate.HasValue ? request.FromDate.Value.ToString("dd/MMM/yyyy") : null;
            ViewBag.ToDate = request.ToDate.HasValue ? request.ToDate.Value.ToString("dd/MMM/yyyy") : null;
            var vouchers = reportingService.GetAccountingVouchers(request, request.FromDate, request.ToDate, int.Parse(request.start), int.Parse(request.length), request.search.value).ToList();
            var totalrecords = vouchers.Select(x => x.TotalRecords).FirstOrDefault();
            var result = Json(new { draw = draw, recordsFiltered = totalrecords, recordsTotal = totalrecords, data = vouchers });
            return result;
        }


        public void ExportAccountingVouchersReport(AccountingVoucherBinding request)
        {
            string sortColumn = "VoucherDate";
            string sortDirection = "ASC";

            if (request.orderBy != null && !string.IsNullOrEmpty(request.orderBy))
            {
                sortColumn = request.orderBy;
                sortDirection = request.orderDir ?? "ASC";
            }

            var result = reportingService.ExportAccountingVoucher(request.FromDate, request.ToDate, request.ReportType, 0, int.MaxValue, request.search?.value, sortColumn, sortDirection);

            var data = result.Select(x => new
            {
                VoucherDate = x.VoucherDate.Value.ToString("dd/MM/yyyy"),
                x.VoucherTypeName,
                x.MyriadID,
                x.VoucherNumber,
                x.LedgerName,
                x.LedgerAmount,
                x.LedgerAmountDrCr,
                x.VoucherNarration
            }
            ).ToList();

            string reportName = $"Accounting Vouchers {request.ReportType} Report - PMS";

            ExcelHelper.ExportToExcel(Response, data, reportName);
        }


        [AuthorizeUser(Roles = AppUserRoles.View_AccountLiability_Report)]
        public ActionResult AccountLiability(int? StudentId = 0, int? AccountId = 0)
        {
            var model = reportingService.GetAccountLiabilityReport(StudentId, AccountId).ToList();
            ViewBag.StudentId = new SelectList(personService.GetPersons().Select(x => new { x.PersonID, FullName = x.Code + ": " + x.FullName }), "PersonID", "FullName", StudentId);
            ViewBag.AccountId = new SelectList(reportingService.GetChartOfAccounts(), "Id", "Name", AccountId);

            return View(model);
        }


        [AuthorizeUser(Roles = AppUserRoles.View_Booking_Report)]
        public ActionResult BookingReport(DateTime? FromDate, DateTime? ToDate)
        {
            if (FromDate == null || ToDate == null)
            {
                FromDate = DateTime.Now.AddDays(-7).Date;
                ToDate = DateTime.Now.Date;
            }
            ViewBag.FromDate = FromDate.HasValue ? FromDate.Value.ToString("dd/MMM/yyyy") : null;
            ViewBag.ToDate = ToDate.HasValue ? ToDate.Value.ToString("dd/MMM/yyyy") : null;
            var model = reportingService.GetBookingReport(FromDate, ToDate).OrderByDescending(x => x.BookingDate).ToList();
            return View(model);
        }


        [AuthorizeUser(Roles = AppUserRoles.View_Contracts_Report)]
        public ActionResult ContractsExpiringIn30Days(int? IsSignedFilter = 2)
        {
            var model = reportingService.GetContractsExpiringIn30Days(IsSignedFilter).ToList();
            ViewBag.Status = IsSignedFilter;
            return View(model);
        }


        [AuthorizeUser(Roles = AppUserRoles.View_ComplaintHistory_Report)]
        public ActionResult ComplaintHistoryReport(int? StatusId = 0, int? CreatedBy = 0, int? UpdatedBy = 0)
        {
            var model = reportingService.GetComplaintHistoryReport(StatusId, CreatedBy, UpdatedBy);
            ViewBag.statusId = new SelectList(ticketService.GetActiveStatus(), "Id", "Name");
            ViewBag.createdBy = new SelectList(userService.GetActiveUsers().Where(x => x.IsStudent != true), "ID", "FullName", CreatedBy);
            ViewBag.updatedBy = new SelectList(userService.GetActiveUsers().Where(x => x.IsStudent != true), "ID", "FullName", UpdatedBy);

            return View(model);
        }


        [AuthorizeUser(Roles = AppUserRoles.Cancel_Seat)]
        [HttpPost]
        public ActionResult CancelSeat(int id)
        {
            bool res = reportingService.CancelSeat(id);
            try
            {
                if (res == true)
                {
                    TempData["success"] = "Seat cancelled successfully.";
                }
                else
                {
                    TempData["error"] = "Seat not updated. Please try again later.";
                }
            }
            catch (Exception ex)
            {

                TempData["error"] = ex.Message;
            }

            return RedirectToAction("TransportationBookingReport");
        }


        [AuthorizeUser(Roles = AppUserRoles.Export_Booking_Report)]
        public void ExportBookingReport(DateTime? FromDate, DateTime? ToDate)
        {
            var report = reportingService.GetBookingReport(FromDate, ToDate).OrderBy(x => x.BookingDate);
            var data = report.Select(x => new
            {
                x.BookingNumber,
                x.FullName,
                x.Gender,
                x.DOB,
                BookingDate = x.BookingDate.ToString("dd/MM/yyyy"),
                x.CheckInDate,
                x.CheckOutDate,
                Occupancy = x.Duration,
                RoomType = x.RoomName,
                x.Email,
                x.University,
                x.Phone,
                x.UniReferenceNo,
                x.Price,
                x.Deposit,
                x.Currency,
                x.PaymentType,
                Requests = x.AccessibilityRequest,
                x.Nationality,
                x.TenantPassportNumber,
                x.GuardianFullName,
                x.GuardianPhone,
                x.GuardianEmail,
                x.GuardianRelation,
                x.PreferableFloor,
                x.PreferableView,
                x.Religions,
                x.Nationalities,
                x.Universities,
                x.AgeRange


            }).ToList();
            ExcelHelper.ExportToExcel(Response, data, "Booking Report - PMS");
        }


        [AuthorizeUser(Roles = AppUserRoles.View_History_Forcast_Report)]
        public ActionResult HistoryForcastReport(DateTime? FromDate, DateTime? ToDate)
        {
            if (FromDate == null || ToDate == null)
            {
                FromDate = DateTime.Now.AddDays(-7).Date;
                ToDate = DateTime.Now.Date;
            }
            var model = reportingService.GetHistoryForcastReport(FromDate, ToDate);
            ViewBag.FromDate = FromDate.GetValueOrDefault().ToString("dd/MMM/yyyy");
            ViewBag.ToDate = ToDate.GetValueOrDefault().ToString("dd/MMM/yyyy");
            return View(model);
        }
        public ActionResult HistoryForcastReportAjax(DateTime? FromDate, DateTime? ToDate)
        {
            if (FromDate == null)
            {
                FromDate = DateTime.Now.AddDays(-7).Date;
                ToDate = DateTime.Now.Date;
            }
            ViewBag.FromDate = FromDate.HasValue ? FromDate.Value.ToString("dd/MMM/yyyy") : null;
            ViewBag.ToDate = ToDate.HasValue ? ToDate.Value.ToString("dd/MMM/yyyy") : null;
            var _returnModel = reportingService.GetHistoryReport(FromDate, ToDate).ToList();
            var result = Json(new { draw = 1, recordsTotal = _returnModel.Count, recordsFiltered = 50, data = _returnModel, MaxJsonLength = Int32.MaxValue }, JsonRequestBehavior.AllowGet);
            result.MaxJsonLength = int.MaxValue;
            return result;
        }

        [AuthorizeUser(Roles = AppUserRoles.View_Occupancy_Forecast_Report)]
        public ActionResult OccupancyForecastReport(DateTime? FromDate, DateTime? ToDate)
        {
            if (FromDate == null || ToDate == null)
            {
                FromDate = DateTime.Now.Date;
                ToDate = DateTime.Now.Date;
            }
            var model = reportingService.GetOccupancyForecastReport(FromDate, ToDate);

            ViewBag.FromDate = FromDate.GetValueOrDefault().ToString("dd/MMM/yyyy");
            ViewBag.ToDate = ToDate.GetValueOrDefault().ToString("dd/MMM/yyyy");

            return View(model);
        }

        public ActionResult OccupancyForecastReportAjax(DateTime? FromDate, DateTime? ToDate)
        {
            if (FromDate == null)
            {
                FromDate = DateTime.Now.Date;
                ToDate = DateTime.Now.Date;
            }
            ViewBag.FromDate = FromDate.HasValue ? FromDate.Value.ToString("dd/MMM/yyyy") : null;
            ViewBag.ToDate = ToDate.HasValue ? ToDate.Value.ToString("dd/MMM/yyyy") : null;
            var _returnModel = reportingService.GetOccupancyForecastReport(FromDate, ToDate).ToList();
            var result = Json(new { draw = 1, recordsTotal = _returnModel.Count, recordsFiltered = 50, data = _returnModel, MaxJsonLength = Int32.MaxValue }, JsonRequestBehavior.AllowGet);
            result.MaxJsonLength = int.MaxValue;
            return result;
        }

        [AuthorizeUser(Roles = AppUserRoles.View_InHouseByUniversity_Report)]
        public ActionResult InHouseByUniversityReport(DateTime? fromDate, DateTime? toDate, int? checkout = null, string universityIds = null, int? studentid = 0)
        {
            // Set default dates if not provided
            if (fromDate == null || toDate == null)
            {
                fromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                toDate = DateTime.Now.Date;
            }

            List<int> assignedLocationids = locationContextService.GetAssignedLocationIds();

            // Get default values from web.config
            int defaultCheckoutStatus = int.Parse(ConfigurationManager.AppSettings["DefaultCheckoutStatus"]);
            //int defaultUniversityId = int.Parse(ConfigurationManager.AppSettings["DefaultUniversityId"]);
            int defaultUniversityId = assignedLocationids?.Contains(17) == true ? int.Parse(ConfigurationManager.AppSettings["DefaultUniversityId"]) : 0;


            // Use defaults if no values provided
            int finalCheckout = checkout ?? defaultCheckoutStatus;

            // Handle university selection logic
            int[] finalUniversityIds; // For data retrieval
            string[] selectedUniversityIds; // For dropdown display

            if (string.IsNullOrEmpty(universityIds) || universityIds == "0")
            {
                if (universityIds == "0")
                {
                    var allUniversities = setupService.GetAllUniversityList();
                    finalUniversityIds = allUniversities.Select(u => u.Id).ToArray();
                    selectedUniversityIds = new string[] { "0" };
                }
                else
                {
                    // Initial load - use default
                    finalUniversityIds = new int[] { defaultUniversityId };
                    selectedUniversityIds = new string[] { defaultUniversityId.ToString() };
                }
            }
            else
            {
                // Specific universities selected
                int[] uniList = universityIds.Split(',').Select(int.Parse).ToArray();
                finalUniversityIds = uniList;
                selectedUniversityIds = uniList.Select(u => u.ToString()).ToArray();
            }

            // Set ViewBag values
            ViewBag.FromDate = fromDate?.ToString("dd/MMM/yyyy");
            ViewBag.ToDate = toDate?.ToString("dd/MMM/yyyy");
            ViewBag.StudentId = new SelectList(
                personService.GetPersons()
                    .Select(x => new { x.PersonID, FullName = x.Code + ": " + x.FullName }),
                "PersonID", "FullName", studentid);
            ViewBag.CheckOut = finalCheckout;

            // Create university list with "All Universities" option
            var universities = setupService.GetAllUniversityList().ToList();

            // Add "All Universities" option at the beginning
            var universityListWithAll = new List<object>();
            universityListWithAll.Add(new { Id = "0", UniversityName = "-- All Universities --" });
            universityListWithAll.AddRange(universities.Select(u => new { Id = u.Id.ToString(), u.UniversityName }));

            ViewBag.UniversityId = new MultiSelectList(universityListWithAll, "Id", "UniversityName", selectedUniversityIds);

            // Pass the array to the service
            var model = reportingService.GetInHouseByUniversity(fromDate, toDate, finalUniversityIds, finalCheckout, studentid);
            return View(model);
        }


        [AuthorizeUser(Roles = AppUserRoles.Liability_Balance_Report)]
        public ActionResult LiabilityBalanceReport(DateTime? FromDate, DateTime? ToDate, int? StudentId = 0)
        {
            if (FromDate == null)
            {
                FromDate = new DateTime(DateTime.Now.Year, 1, 1);
                ToDate = DateTime.Now.Date;
            }
            if (ToDate == null)
            {
                ToDate = DateTime.Now.Date;
            }
            ViewBag.FromDate = FromDate.HasValue ? FromDate.Value.ToString("dd/MMM/yyyy") : null;
            ViewBag.ToDate = ToDate.HasValue ? ToDate.Value.ToString("dd/MMM/yyyy") : null;
            var model = reportingService.GetLiabilityBalanceReport(FromDate, ToDate, StudentId).ToList();
            ViewBag.StudentId = new SelectList(personService.GetPersons().Select(x => new { x.PersonID, FullName = x.Code + ": " + x.FullName }), "PersonID", "FullName", StudentId);

            return View(model);
        }

        [AuthorizeUser(Roles = AppUserRoles.Liability_Balance_Report)]
        public ActionResult LoadDetailLiabilityBalances(DateTime? FromDate, DateTime? ToDate, int StudentId)
        {
            var model = reportingService.GetDetailLiabilityBalancesReport(FromDate, ToDate, StudentId);
            return PartialView("_DetailLiabilityBalances", model);
        }

        [AuthorizeUser(Roles = AppUserRoles.View_Manager_Daily_Report)]
        public ActionResult ManagerDailyReport(DateTime? FilterDate)
        {
            if (FilterDate == null)
            {
                FilterDate = DateTime.Now.Date;
            }
            var model = reportingService.GetManagerDailyReport(FilterDate);
            ViewBag.FilterDate = FilterDate.GetValueOrDefault().ToString("dd/MMM/yyyy");
            return View(model);
        }

        [AuthorizeUser(Roles = AppUserRoles.View_PaymentDetail_Report)]
        public async Task<ActionResult> PaymentsDetailReport(DateTime? FromDate, DateTime? ToDate, int? PaymentId = 0, int? StudentId = 0, int? userid = 0, int? type = 0)
        {
            if (FromDate == null || ToDate == null)
            {
                FromDate = DateTime.Now.AddDays(-7).Date;
                ToDate = DateTime.Now.Date;
            }
            var model = await reportingService.GetPaymentsDetailReportAsync(FromDate, ToDate, PaymentId, StudentId, userid, type);
            ViewBag.FromDate = FromDate.GetValueOrDefault().ToString("dd/MMM/yyyy");
            ViewBag.ToDate = ToDate.GetValueOrDefault().ToString("dd/MMM/yyyy");
            ViewBag.PaymentId = new SelectList(paymentTypesService.GetPayment().Where(x => x.IsActive == true), "PaymentId", "PaymentName");
            ViewBag.StudentId = new SelectList(personService.GetPersons().Select(x => new { x.PersonID, FullName = x.Code + ": " + x.FullName }), "PersonID", "FullName", StudentId);
            ViewBag.userid = new SelectList(userService.GetActiveUsers().Where(x => x.IsStudent != true), "ID", "FullName", userid);
            ViewBag.Type = type;
            ViewBag.TotalAmount = model.Sum(x => x.NetAmount);

            return View(model);
        }

        [AuthorizeUser(Roles = AppUserRoles.View_Customize_Detailed_Reports)]
        public ActionResult ReportingView(DateTime? FromDate, DateTime? ToDate, int? studentid = 0)
        {
            if (FromDate == null)
            {
                FromDate = DateTime.Now.AddDays(-7).Date;
                ToDate = DateTime.Now.Date;
            }
            ViewBag.FromDate = FromDate.HasValue ? FromDate.Value.ToString("dd/MMM/yyyy") : null;
            ViewBag.ToDate = ToDate.HasValue ? ToDate.Value.ToString("dd/MMM/yyyy") : null;
            ViewBag.StudentId = new SelectList(personService.GetPersons().Select(x => new { x.PersonID, FullName = x.Code + ": " + x.FullName }), "PersonID", "FullName", studentid);

            return View();
        }
        public ActionResult loadGenericReportbyAjax(DateTime? FromDate, DateTime? ToDate, int? studentid = 0)
        {
            if (FromDate == null)
            {
                FromDate = DateTime.Now.AddDays(-7).Date;

                ToDate = DateTime.Now.Date;
            }
            ViewBag.FromDate = FromDate.HasValue ? FromDate.Value.ToString("dd/MMM/yyyy") : null;
            ViewBag.ToDate = ToDate.HasValue ? ToDate.Value.ToString("dd/MMM/yyyy") : null;
            var _returnModel = reportingService.GetReport(FromDate, ToDate, studentid).ToList();
            var result = Json(new { draw = 1, recordsTotal = _returnModel.Count, recordsFiltered = 50, data = _returnModel, MaxJsonLength = Int32.MaxValue }, JsonRequestBehavior.AllowGet);
            result.MaxJsonLength = int.MaxValue;
            return result;
        }

        [AuthorizeUser(Roles = AppUserRoles.View_Resident_Trail_Balance_Report)]
        public ActionResult ResidentTrialBalanceReport(DateTime? FromDate, DateTime? ToDate, int? checkout = 0, int? studentid = 0)
        {
            if (FromDate == null)
            {
                FromDate = new DateTime(2021, 9, 1);

                ToDate = DateTime.Now.Date;
            }
            ViewBag.FromDate = FromDate.HasValue ? FromDate.Value.ToString("dd/MMM/yyyy") : null;
            ViewBag.ToDate = ToDate.HasValue ? ToDate.Value.ToString("dd/MMM/yyyy") : null;
            ViewBag.CheckOut = checkout;
            ViewBag.StudentId = new SelectList(personService.GetPersons().Select(x => new { x.PersonID, FullName = x.Code + ": " + x.FullName }), "PersonID", "FullName", studentid);
            return View();
        }

        public object loadResidentTrialBalancebyAjax(ResidentDetailBinding request, string draw, int checkout = 0, int studentid = 0)
        {
            ViewBag.FromDate = request.FromDate.HasValue ? request.FromDate.Value.ToString("dd/MMM/yyyy") : null;
            ViewBag.ToDate = request.ToDate.HasValue ? request.ToDate.Value.ToString("dd/MMM/yyyy") : null;
            var residentdetailbalance = reportingService.GetResidentTrailBalanceReport(request, request.FromDate, request.ToDate, int.Parse(request.start), int.Parse(request.length), request.isSummaryReport, checkout, studentid, request.search.value).ToList();
            var totalrecords = residentdetailbalance.Select(x => x.TotalRecords).FirstOrDefault();
            var result = Json(new { draw = draw, recordsFiltered = totalrecords, recordsTotal = totalrecords, data = residentdetailbalance });
            return result;
        }

        public ActionResult LoadResidentGrandTotal(ResidentDetailBinding request, int checkout = 0, int studentid = 0)
        {
            var grandTotal = reportingService.GetGrandTotal(request.FromDate, request.ToDate, checkout, studentid);
            return Json(grandTotal, JsonRequestBehavior.AllowGet);
        }

        public void ExportResidentTrialBalanceReport(DateTime? FromDate, DateTime? ToDate, int? checkout = 0, int? studentid = 0)
        {

            var _returnModel = reportingService.ExportResidentTrailBalanceReport(FromDate, ToDate, checkout, studentid).ToList();
            var data = _returnModel.Select(x => new
            {
                x.RoomNo,
                x.BedSpace,
                x.MyriadID,
                x.Name,
                MoveIn = x.MoveIn.HasValue ? x.MoveIn.Value.ToString("dd/M/yyyy") : null,
                MoveOut = x.MoveOut.HasValue ? x.MoveOut.Value.ToString("dd/M/yyyy") : null,
                CheckIn = x.CheckIn.HasValue ? x.CheckIn.Value.ToString("dd/M/yyyy") : null,
                CheckOut = x.CheckOut.HasValue ? x.CheckOut.Value.ToString("dd/M/yyyy") : null,
                x.OpeningBalance,
                x.DebitAmount,
                x.CreditAmount,
                x.Balance,
                x.ExclusiveOpeningBalance,
                LastEntryDate = x.LastEntryDate.HasValue ? x.LastEntryDate.Value.ToString("dd/M/yyyy") : null,
            });
            ExcelHelper.ExportToExcel(Response, data, "ResidentTrialBalanceReport  - PMS");
            return;
        }

        [AuthorizeUser(Roles = AppUserRoles.View_Resident_Detail_Trail_Balance_Report)]
        public ActionResult ResidentDetailTrialBalanceReport(DateTime? FromDate, DateTime? ToDate, int? studentid = 0)
        {
            if (FromDate == null)
            {
                FromDate = new DateTime(2021, 9, 1);

                ToDate = DateTime.Now.Date;
            }
            ViewBag.FromDate = FromDate.HasValue ? FromDate.Value.ToString("dd/MMM/yyyy") : null;
            ViewBag.ToDate = ToDate.HasValue ? ToDate.Value.ToString("dd/MMM/yyyy") : null;
            ViewBag.StudentId = new SelectList(personService.GetPersons().Select(x => new { x.PersonID, FullName = x.Code + ": " + x.FullName }), "PersonID", "FullName", studentid);

            return View();
        }

        public object loadResidentDetailTrialBalancebyAjax(ResidentDetailBinding request, string draw, int studentid = 0)
        {
            var resident = new ResidentDetailBalanceResponse();

            ViewBag.FromDate = request.FromDate.HasValue ? request.FromDate.Value.ToString("dd/MMM/yyyy") : null;
            ViewBag.ToDate = request.ToDate.HasValue ? request.ToDate.Value.ToString("dd/MMM/yyyy") : null;
            var residentdetailbalance = reportingService.ResidentDetailTrialBalanceReport(request, request.FromDate, request.ToDate, int.Parse(request.start), int.Parse(request.length), request.isSummaryReport, studentid, request.search.value).ToList();
            var totalrecords = residentdetailbalance.Select(x => x.TotalRecords).FirstOrDefault();
            var result = Json(new { draw = draw, recordsFiltered = totalrecords, recordsTotal = totalrecords, data = residentdetailbalance });
            return result;
        }

        public void ExportResidentDetailTrialBalanceReport(DateTime? FromDate, DateTime? ToDate, int? studentid = 0)
        {
            ViewBag.FromDate = FromDate.HasValue ? FromDate.Value.ToString("dd/MMM/yyyy") : null;
            ViewBag.ToDate = ToDate.HasValue ? ToDate.Value.ToString("dd/MMM/yyyy") : null;
            var _returnModel = reportingService.ExportResidentDetailTrialBalanceReport(FromDate, ToDate, studentid).ToList();
            var data = _returnModel.Select(x => new
            {
                x.MyriadID,
                x.Name,
                x.RoomNo,
                x.BedSpace,
                x.TypeOfInvoice,
                InvoiceID = x.InvoiceCode,
                InvoiceDate = x.InvoiceDate.ToString("dd/M/yyyy"),
                x.InvoiceAmount,
                FromDate = x.FromDate.HasValue ? x.FromDate.Value.ToString("dd/M/yyyy") : null,
                ToDate = x.ToDate.HasValue ? x.ToDate.Value.ToString("dd/M/yyyy") : null,
                x.TypeOfReceipt,
                x.ReceiptNumber,
                DateOfReceipt = x.DateOfReceipt.HasValue ? x.DateOfReceipt.Value.ToString("dd/M/yyyy") : null,
                x.AmountReceived,
                x.BalanceReceivable,
                AgingOfBalanceInDays = x.AgingInDays,

            });
            ExcelHelper.ExportToExcel(Response, data, "Resident Detail Trial Balance Report  - PMS");
            return;
        }
        [AuthorizeUser(Roles = AppUserRoles.View_RevenueDetail_Report)]
        public ActionResult RevenueDetailReport(DateTime? FromDate, DateTime? ToDate, int? isDefferd = 0, int RoomTypeID = 0, int TermID = 0)
        {
            var Termlist = new SelectList("");
            if (FromDate == null || ToDate == null)
            {
                FromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                ToDate = DateTime.Now.Date;
            }

            //if (FromDate == null)
            //    ViewBag.FromDate = DateTime.Now.ToString("yyyy-MM-dd");
            //else
            //    ViewBag.FromDate = FromDate.Value.ToString("yyyy-MM-dd");

            //if (ToDate == null)
            //    ViewBag.ToDate = DateTime.Now.ToString("yyyy-MM-dd");
            //else
            //    ViewBag.ToDate = ToDate.Value.ToString("yyyy-MM-dd");


            //if (Date == null)

            //    ViewBag.Date = DateTime.Now.ToString("yyyy-MM");
            //else
            //    ViewBag.Date = Date.Value.ToString("yyyy-MM");

            var model = reportingService.GetRevenueDetailReport(FromDate, ToDate, isDefferd, TermID);
            ViewBag.FromDate = FromDate.GetValueOrDefault().ToString("dd/MMM/yyyy");
            ViewBag.ToDate = ToDate.GetValueOrDefault().ToString("dd/MMM/yyyy");
            ViewBag.IsDeffered = isDefferd;
            ViewBag.RoomTypeID = new SelectList(setupService.GetRoomTypes(), "RoomTypeID", "RoomName", RoomTypeID);

            if (TermID != 0)
                Termlist = new SelectList(setupService.GetTermsByRoomTypeID(RoomTypeID), "TermID", "TermName", TermID);

            ViewBag.TermID = Termlist;
            ViewBag.TotalAmount = model.Sum(x => x.Revenue);

            return View(model);
        }

        [AuthorizeUser(Roles = AppUserRoles.View_RoomInventory_Report)]
        public ActionResult RoomInventoryReport()
        {
            var model = reportingService.GetRoomInventoryReport().ToList();
            return View(model);
        }

        [AuthorizeUser(Roles = AppUserRoles.View_ServiceDetail_Report)]
        public ActionResult ServicesDetailReport(DateTime? FromDate, DateTime? ToDate, int? ServiceId = 0, int? studentid = 0, int TermID = 0, int? userid = 0, int? type = 0)
        {

            if (FromDate == null || ToDate == null)
            {
                FromDate = DateTime.Now.AddDays(-7).Date;
                ToDate = DateTime.Now.Date;
            }
            var model = reportingService.GetServicesDetailReport(FromDate, ToDate, ServiceId, studentid, TermID, userid, type);
            ViewBag.FromDate = FromDate.GetValueOrDefault().ToString("dd/MMM/yyyy");
            ViewBag.ToDate = ToDate.GetValueOrDefault().ToString("dd/MMM/yyyy");
            ViewBag.TermID = new SelectList(bookinService.GetPriceConfigurations(), "Value", "Text", TermID);
            ViewBag.StudentId = new SelectList(personService.GetPersons().Select(x => new { x.PersonID, FullName = x.Code + ": " + x.FullName }), "PersonID", "FullName", studentid);
            ViewBag.serviceid = new SelectList(servicesService.GetServices().Where(x => x.IsActive == true), "ServiceId", "ServiceName");
            ViewBag.userid = new SelectList(userService.GetActiveUsers().Where(x => x.IsStudent != true), "ID", "FullName", userid);
            ViewBag.Type = type;
            ViewBag.TotalAmount = model.Sum(x => x.ServicePrice);
            return View(model);
        }

        [AuthorizeUser(Roles = AppUserRoles.view_End_Of_Shift_Report)]
        public ActionResult ShiftEndReport(DateTime? FromDate, DateTime? ToDate, int PaymentId = 0, int userid = 0)
        {
            var model = new List<ShiftEndReportVM>();
            if (PaymentId != 0 || userid != 0 || (FromDate != null && ToDate != null))
            {
                model = reportingService.GetShiftEndReport(FromDate, ToDate, PaymentId, userid);
            }
            ViewBag.FromDate = FromDate.HasValue ? FromDate.Value.ToString("dd/MMM/yyyy") : null;
            ViewBag.ToDate = ToDate.HasValue ? ToDate.Value.ToString("dd/MMM/yyyy") : null;
            ViewBag.PaymentId = new SelectList(paymentTypesService.GetPayment().Where(x => x.IsActive == true), "PaymentId", "PaymentName");
            ViewBag.userid = new SelectList(userService.GetActiveUsers().Where(x => x.IsStudent != true), "ID", "FullName", userid);
            ViewBag.TotalAmount = model.Sum(x => x.Amount);

            return View(model);
        }

        [AuthorizeUser(Roles = AppUserRoles.View_Swapped_Report)]
        public ActionResult SwappBedSpacesReport(DateTime? FromDate, DateTime? ToDate, int? BedSpaceID = 0)
        {
            if (FromDate == null || ToDate == null)
            {
                FromDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                ToDate = DateTime.Now.Date;
            }
            var model = reportingService.GetSwappedBedSpacesReport(FromDate, ToDate, BedSpaceID);

            ViewBag.FromDate = FromDate.GetValueOrDefault().ToString("dd/MMM/yyyy");
            ViewBag.ToDate = ToDate.GetValueOrDefault().ToString("dd/MMM/yyyy");
            ViewBag.BedSpaceID = new SelectList(placementService.GetAllBedSpaces(), "Value", "Text", BedSpaceID);

            return View(model);
        }

        [AuthorizeUser(Roles = AppUserRoles.Tax_Detail_Report)]
        public ActionResult TaxDetailReport(DateTime? FromDate, DateTime? ToDate, int? AccountId = 0)
        {
            if (FromDate == null || ToDate == null)
            {
                FromDate = DateTime.Now.AddDays(-7).Date;
                ToDate = DateTime.Now.Date;
            }
            var model = reportingService.GetTaxDetailReport(FromDate, ToDate, AccountId);

            ViewBag.FromDate = FromDate.GetValueOrDefault().ToString("dd/MMM/yyyy");
            ViewBag.ToDate = ToDate.GetValueOrDefault().ToString("dd/MMM/yyyy");
            ViewBag.AccountId = new SelectList(reportingService.GetChartOfAccounts(), "Id", "Name", AccountId);

            return View(model);
        }

        [AuthorizeUser(Roles = AppUserRoles.View_TransportationBooking_Report)]
        public ActionResult TransportationBookingReport(DateTime? FromDate, DateTime? ToDate, int? DepartureTimeId = 0, int? VehicleId = 0, int? StudentId = 0, int? RouteId = 0)
        {
            if (FromDate == null || ToDate == null)
            {
                FromDate = DateTime.Now.AddDays(-7);
                ToDate = DateTime.Now.Date;
            }
            var model = reportingService.GetTransportationBookingReport(FromDate, ToDate, DepartureTimeId, VehicleId, StudentId, RouteId);
            ViewBag.FromDate = FromDate.GetValueOrDefault().ToString("dd/MMM/yyyy");
            ViewBag.ToDate = ToDate.GetValueOrDefault().ToString("dd/MMM/yyyy");
            ViewBag.DepartureTimeId = new SelectList(scheduleService.GetActiveTimes(), "Id", "Name");
            ViewBag.VehicleId = new SelectList(vehicleService.GetVehicles(), "BusId", "RegistrationNumber");
            ViewBag.StudentId = new SelectList(personService.GetPersons().Select(x => new { x.PersonID, FullName = x.Code + ": " + x.FullName }), "PersonID", "FullName", StudentId);
            ViewBag.RouteId = new SelectList(routeService.GetAll(), "RouteID", "Routename");
            ViewBag.success = TempData["success"];
            ViewBag.error = TempData["error"];
            return View(model);
        }

        [AuthorizeUser(Roles = AppUserRoles.View_Voucher_Report)]
        public ActionResult VoucherReport(DateTime? FromDate, DateTime? ToDate, int? accountId = 0, int? studentid = 0)
        {
            if (FromDate == null)
            {
                FromDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);


                ToDate = DateTime.Now.Date;
            }
            ViewBag.FromDate = FromDate.HasValue ? FromDate.Value.ToString("dd/MMM/yyyy") : null;
            ViewBag.ToDate = ToDate.HasValue ? ToDate.Value.ToString("dd/MMM/yyyy") : null;
            ViewBag.StudentId = new SelectList(personService.GetPersons().Select(x => new { x.PersonID, FullName = x.Code + ": " + x.FullName }), "PersonID", "FullName", studentid);
            ViewBag.AccountId = new SelectList(reportingService.GetChartOfAccounts(), "Id", "Name", accountId);

            return View();
        }

        public object loadVoucherReportbyAjax(VoucherBinding request, string draw, int accountId = 0, int studentid = 0)
        {
            var resident = new VoucherReportResponse();

            ViewBag.FromDate = request.FromDate.HasValue ? request.FromDate.Value.ToString("dd/MMM/yyyy") : null;
            ViewBag.ToDate = request.ToDate.HasValue ? request.ToDate.Value.ToString("dd/MMM/yyyy") : null;
            var vouchers = reportingService.GetVoucherReport(request, request.FromDate, request.ToDate, int.Parse(request.start), int.Parse(request.length), request.isSummaryReport, accountId, studentid, request.search.value).ToList();
            var totalrecords = vouchers.Select(x => x.TotalRecords).FirstOrDefault();
            var result = Json(new { draw = draw, recordsFiltered = totalrecords, recordsTotal = totalrecords, data = vouchers });
            return result;
        }

        public object loadVoucherSummaryReportbyAjax(DateTime? fromDate, DateTime? toDate)
        {
            ViewBag.FromDate = fromDate.HasValue ? fromDate.Value.ToString("dd/MMM/yyyy") : null;
            ViewBag.ToDate = toDate.HasValue ? toDate.Value.ToString("dd/MMM/yyyy") : null;
            var summaryData = reportingService.GetVoucherSummaryReport(fromDate, toDate).ToList();
            var result = Json(new { data = summaryData });
            return result;
        }

        public void ExportVoucherSummaryReport(DateTime? fromDate, DateTime? toDate)
        {
            ViewBag.FromDate = fromDate.HasValue ? fromDate.Value.ToString("dd/MMM/yyyy") : null;
            ViewBag.ToDate = toDate.HasValue ? toDate.Value.ToString("dd/MMM/yyyy") : null;

            var summaryData = reportingService.GetVoucherSummaryReport(fromDate, toDate).ToList();

            var data = summaryData.Select(x => new
            {
                AccountName = x.AccountName,
                TotalDebit = x.TotalDebit,
                TotalCredit = x.TotalCredit,
                TotalNet = x.TotalNet
            });

            ExcelHelper.ExportToExcel(Response, data, "Voucher Summary Report - PMS");
            return;
        }


        public void ExportVoucherReport(DateTime? FromDate, DateTime? ToDate, int? accountId = 0, int? studentid = 0)
        {
            ViewBag.FromDate = FromDate.HasValue ? FromDate.Value.ToString("dd/MMM/yyyy") : null;
            ViewBag.ToDate = ToDate.HasValue ? ToDate.Value.ToString("dd/MMM/yyyy") : null;

            var _returnModel = reportingService.GetVoucherReportForExcel(FromDate, ToDate, accountId, studentid).ToList();

            var data = _returnModel.Select(x => new
            {
                VoucherCode = x.Code,
                TransactionCode = x.TransactionCode,
                TransactionDate = x.VoucherDate.ToString("dd/M/yyyy"),
                x.MyriadID,
                x.FullName,
                x.AccountName,
                x.Remarks,
                DebitAmount = x.DebitAmount ?? 0.00m,
                CreditAmount = x.CreditAmount ?? 0.00m
            });

            ExcelHelper.ExportToExcel(Response, data, "Voucher Trial Balance Report - PMS");
            return;
        }



        #region RDLC PDF Reports

        public ActionResult GenerateInvoiceDetailReport(int Id)
        {
            try
            {
                string username = User.Identity.Name;
                string strMimeType = "";
                List<ReportDataSource> subreportDataSources = new List<ReportDataSource>();
                subreportDataSources.Add(new ReportDataSource("SubInvoiceReportDST", reportingService.GetSubInvoiceReport(Id)));
                ReportDataSource mainDatasource = new ReportDataSource("InvoiceDetailDST", reportingService.GetInvoiceDetailReport(Id, username));
                var bytes = RdlcHelper<PaymentDetailReportVM>.LocalReportMultiDatasouce(mainDatasource, subreportDataSources, "/Reports/InvoiceDetailReport.rdlc", out strMimeType);
                var result = new HttpResponseMessage(HttpStatusCode.OK);
                Stream stream = new MemoryStream(bytes);
                result.Content = new StreamContent(stream);
                result.Content.Headers.ContentType = new MediaTypeHeaderValue(strMimeType);
                return File(bytes, strMimeType);
            }
            catch (Exception ex)
            {
                return View();
            }
        }

        //public ActionResult GetPaymentTransactionDetailReport(int Id)
        //{
        //    try
        //    {
        //        string strMimeType = "";
        //        var list = reportingService.GetPaymentTransactionDetailReport(Id);
        //        var bytes = RdlcHelper<PaymentTransactionDetailReportVM>.LocalReport(list, "/Reports/PaymentTransactionDetailReport.rdlc", "PaymentTransactionDetailDST", out strMimeType);
        //        var result = new HttpResponseMessage(HttpStatusCode.OK);
        //        Stream stream = new MemoryStream(bytes);
        //        result.Content = new StreamContent(stream);
        //        result.Content.Headers.ContentType = new MediaTypeHeaderValue(strMimeType);

        //        return File(bytes, strMimeType);
        //    }
        //    catch (Exception ex)
        //    {
        //        return View();
        //    }
        //}

        public ActionResult GetPaymentTransactionDetailReport(int Id)
        {
            try
            {
                string strMimeType = "";
                var list = reportingService.GetPaymentTransactionDetailReport(Id);

                var user = Session["User"] as dynamic;
                bool isStudent = user != null && user.IsStudent == true;
                ReportParameter param = new ReportParameter("UserIsStudent", isStudent.ToString());

                var bytes = RdlcHelper<PaymentTransactionDetailReportVM>.LocalReport(
                    list,
                    "/Reports/PaymentTransactionDetailReport.rdlc",
                    "PaymentTransactionDetailDST",
                    out strMimeType,
                    new List<ReportParameter> { param }  
                );

                return File(bytes, strMimeType);
            }
            catch (Exception ex)
            {
                return View();
            }
        }


        public ActionResult GetResidentTrialBalanceDetailPDFReport(DateTime? FromDate, DateTime? ToDate, int start, int length, int? studentid = 0)
        {
            try
            {
                string strMimeType = "";
                var list = reportingService.GetResidentTrialBalanceDetailPDFReport(FromDate, ToDate, start, length, studentid);
                var bytes = RdlcHelper<ResidentTrialBalanceVM>.LocalReport(list, "/Reports/ResidentTrialBalanceDetailReport.rdlc", "ResidentTrialBalanceDST", out strMimeType);
                var result = new HttpResponseMessage(HttpStatusCode.OK);
                Stream stream = new MemoryStream(bytes);
                result.Content = new StreamContent(stream);
                result.Content.Headers.ContentType = new MediaTypeHeaderValue(strMimeType);

                return File(bytes, strMimeType);
            }
            catch (Exception ex)
            {
                return View();
            }


        }

        public ActionResult GetServicesDetailPDFReport(DateTime? FromDate, DateTime? ToDate, int? ServiceId = 0, int? studentid = 0, int TermID = 0, int? userid = 0, int? type = 0)
        {
            try
            {
                string strMimeType = "";
                var list = reportingService.GetServicesDetailReport(FromDate, ToDate, ServiceId, studentid, TermID, userid, type);
                var bytes = RdlcHelper<ServicesDetailVM>.LocalReport(list, "/Reports/ServicesDetailReport.rdlc", "ServicesDetailDST", out strMimeType);
                var result = new HttpResponseMessage(HttpStatusCode.OK);
                Stream stream = new MemoryStream(bytes);
                result.Content = new StreamContent(stream);
                result.Content.Headers.ContentType = new MediaTypeHeaderValue(strMimeType);

                return File(bytes, strMimeType);
            }
            catch (Exception ex)
            {
                return View();
            }


        }

        public async Task<ActionResult> GetPaymentsDetailPDFReport(DateTime? FromDate, DateTime? ToDate, int? PaymentId, int? StudentId, int? userid, int? type)
        {
            try
            {
                string strMimeType = "";
                var list = await reportingService.GetPaymentDetailPDFReportAsync(FromDate, ToDate, PaymentId, StudentId, userid, type);
                var bytes = RdlcHelper<PaymentDetailReportVM>.LocalReport(list, "/Reports/PaymentsDetailReport.rdlc", "PaymentsDetailDST", out strMimeType);
                var result = new HttpResponseMessage(HttpStatusCode.OK);
                Stream stream = new MemoryStream(bytes);
                result.Content = new StreamContent(stream);
                result.Content.Headers.ContentType = new MediaTypeHeaderValue(strMimeType);

                return File(bytes, strMimeType);
            }
            catch (Exception ex)
            {
                return View();
            }


        }

        public ActionResult GetRevenueDetailPDFReport(DateTime? fromDate, DateTime? toDate, int RoomTypeID = 0, int TermID = 0, int? isDefferd = 0)
        {
            try
            {
                string strMimeType = "";
                var list = reportingService.GetRevenueDetailReport(fromDate, toDate, isDefferd, TermID);
                var bytes = RdlcHelper<RevenueDetailVM>.LocalReport(list, "/Reports/RevenueDetailReport.rdlc", "RevenueDetailDST", out strMimeType);
                var result = new HttpResponseMessage(HttpStatusCode.OK);
                Stream stream = new MemoryStream(bytes);
                result.Content = new StreamContent(stream);
                result.Content.Headers.ContentType = new MediaTypeHeaderValue(strMimeType);

                return File(bytes, strMimeType);
            }
            catch (Exception ex)
            {
                return View();
            }


        }

        public ActionResult GetBookingDetailPdfReport(DateTime? FromDate, DateTime? ToDate)
        {
            try
            {
                string strMimeType = "";
                var list = reportingService.GetBookingReport(FromDate, ToDate);
                var bytes = RdlcHelper<BookingReportVM>.LocalReport(list, "/Reports/BookingDetailReport.rdlc", "BookingDetailDST", out strMimeType);
                var result = new HttpResponseMessage(HttpStatusCode.OK);
                Stream stream = new MemoryStream(bytes);
                result.Content = new StreamContent(stream);
                result.Content.Headers.ContentType = new MediaTypeHeaderValue(strMimeType);

                return File(bytes, strMimeType);
            }
            catch (Exception ex)
            {
                return View();
            }

        }

        public ActionResult GetRoomInventoryPDFReport()
        {
            try
            {
                string strMimeType = "";
                var list = reportingService.GetRoomInventoryReport();
                var bytes = RdlcHelper<RoomInventoryVM>.LocalReport(list, "/Reports/RoomInventoryReport.rdlc", "RoomInventoryDST", out strMimeType);
                var result = new HttpResponseMessage(HttpStatusCode.OK);
                Stream stream = new MemoryStream(bytes);
                result.Content = new StreamContent(stream);
                result.Content.Headers.ContentType = new MediaTypeHeaderValue(strMimeType);

                return File(bytes, strMimeType);
            }
            catch (Exception ex)
            {
                return View();
            }
        }

        public ActionResult GetAccountLiabilityPDFReport(int? StudentId = 0, int? AccountId = 0)
        {
            try
            {
                string strMimeType = "";
                var list = reportingService.GetAccountLiabilityReport(StudentId, AccountId);
                var bytes = RdlcHelper<AccountLiabilityVM>.LocalReport(list, "/Reports/AccountLiabilityReport.rdlc", "AccountLiabilityDST", out strMimeType);
                var result = new HttpResponseMessage(HttpStatusCode.OK);
                Stream stream = new MemoryStream(bytes);
                result.Content = new StreamContent(stream);
                result.Content.Headers.ContentType = new MediaTypeHeaderValue(strMimeType);

                return File(bytes, strMimeType);
            }
            catch (Exception ex)
            {
                return View();
            }
        }

        public ActionResult TaxDetailPDFReport(DateTime? FromDate, DateTime? ToDate, int? AccountId = 0)
        {
            try
            {
                string strMimeType = "";
                var list = reportingService.GetTaxDetailReport(FromDate, ToDate, AccountId);
                var bytes = RdlcHelper<TaxDetailVM>.LocalReport(list, "/Reports/TaxDetailReport.rdlc", "TaxDetailDST", out strMimeType);
                var result = new HttpResponseMessage(HttpStatusCode.OK);
                Stream stream = new MemoryStream(bytes);
                result.Content = new StreamContent(stream);
                result.Content.Headers.ContentType = new MediaTypeHeaderValue(strMimeType);

                return File(bytes, strMimeType);
            }
            catch (Exception ex)
            {
                return View();
            }
        }

        public ActionResult HistoryForcastPDFReport(DateTime? FromDate, DateTime? ToDate)
        {
            try
            {
                string strMimeType = "";
                var list = reportingService.GetHistoryForcastReport(FromDate, ToDate);
                var bytes = RdlcHelper<HistoryForcastVm>.LocalReport(list, "/Reports/HistoryForcastReport.rdlc", "HistoryForcastDST", out strMimeType);
                var result = new HttpResponseMessage(HttpStatusCode.OK);
                Stream stream = new MemoryStream(bytes);
                result.Content = new StreamContent(stream);
                result.Content.Headers.ContentType = new MediaTypeHeaderValue(strMimeType);

                return File(bytes, strMimeType);
            }
            catch (Exception ex)
            {
                return View();
            }
        }

        public ActionResult ManagerDailyPDFReport(DateTime? FilterDate)
        {
            try
            {
                string strMimeType = "";
                var list = reportingService.GetManagerDailyReport(FilterDate);
                var bytes = RdlcHelper<ManagerDailyVM>.LocalReport(list, "/Reports/ManagerDailyReport.rdlc", "DailyManagerDST", out strMimeType);
                var result = new HttpResponseMessage(HttpStatusCode.OK);
                Stream stream = new MemoryStream(bytes);
                result.Content = new StreamContent(stream);
                result.Content.Headers.ContentType = new MediaTypeHeaderValue(strMimeType);

                return File(bytes, strMimeType);
            }
            catch (Exception ex)
            {
                return View();
            }
        }

        #endregion

    }

}
