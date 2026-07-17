using PMS.Common.Classes;
using PMS.DTO;
using PMS.DTO.ViewModels;
using PMS.DTO.ViewModels.ApiViewModels;
using PMS.DTO.ViewModels.NewsViewModels;
using PMS.DTO.ViewModels.PaymentViewModels;
using PMS.DTO.ViewModels.ReportingViewModels;
using PMS.DTO.ViewModels.SetupViewModels;
using PMS.DTO.ViewModels.TaxViewModels;
using PMS.EF;
using PMS.Repository.UnitOfWork;
using PMS.Services.Services.AuditLogs;
using PMS.Services.Services.Notifications;
using PMS.Services.Services.Payment;
using PMS.Services.Services.Person;
using PMS.Services.Services.Service;
using PMS.Services.Services.Tax;
using PMS.Services.Services.Tex;
using PMS.Services.Services.VoucherSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Metadata.Edm;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;
using TheMyriad.DTO.DTO_Mapings;
using static PMS.Common.Classes.Enumeration;
using static System.Data.Entity.Infrastructure.Design.Executor;
using PMS.Services.Services.CreditNote;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using PMS.Services.Services.Setup;
using System.Data.Entity.Core.Objects;
using PMS.Services.Services.LocationContext;

namespace PMS.Services.Services.Invoicings
{
    public class InvoicingService : IInvoicingService
    {
        private static readonly object InvoiceCodeLock = new object();
        private readonly UnitOfWork<PMSEntities> uow;
        private readonly IAuditLogsService auditLogsService;
        private readonly INotificationService notificationService;
        private readonly ITaxService taxService;
        private readonly IServicesService servicesService;
        private readonly IPersonService personService;
        private readonly IVoucherService voucherService;
        private readonly IPaymentService paymentService;
        private readonly ICreditNoteService creditNoteService;
        private readonly ILocationContextService locationContextService;
        public InvoicingService(UnitOfWork<PMSEntities> _uow, IAuditLogsService _auditLogsService, INotificationService _notificationService, ITaxService _taxService, IServicesService _servicesService,
            IPersonService _personService, IVoucherService _voucherService, IPaymentService _paymentService, ICreditNoteService _creditNoteService
            , ILocationContextService _locationContextService)
        {
            uow = _uow;
            auditLogsService = _auditLogsService;
            notificationService = _notificationService;
            taxService = _taxService;
            servicesService = _servicesService;
            personService = _personService;
            voucherService = _voucherService;
            paymentService = _paymentService;
            creditNoteService = _creditNoteService;
            locationContextService = _locationContextService;
        }


        #region GET Methods

        public InvoicingsResponse GetAll(InvoicingBinding request, string QueryBY, string searchValue, string start, string length, string query = null, string orderBy = null, string orderDir = "asc", DateTime? FromDate = null, DateTime? ToDate = null, int? InvoiceTypeId = null)
        {
            try
            {
                var assignedLocationIds = locationContextService.GetAssignedLocationIds();

                // Initialize IQueryable with base query - Apply filters early for better performance
                IQueryable<EF.V_GetInvoiceList> baseQuery = uow.GenericRepository<EF.V_GetInvoiceList>().Table
 .Where(x =>
      (assignedLocationIds.Contains((int)x.LocationId))
 ).Where(x => (!FromDate.HasValue || EntityFunctions.TruncateTime(x.InvoiceDate) >= EntityFunctions.TruncateTime(FromDate.Value)) &&
                   (!ToDate.HasValue || EntityFunctions.TruncateTime(x.InvoiceDate) <= EntityFunctions.TruncateTime(ToDate.Value)))
       .Where(x => (!InvoiceTypeId.HasValue || x.InvoiceTypeId == InvoiceTypeId.Value));


                // Apply search filters BEFORE projection to reduce data transfer
                if (!string.IsNullOrEmpty(request?.search?.value) && !string.IsNullOrEmpty(request.search.column) && request.query == null)
                {
                    string searchVal = request.search.value.ToLower();
                    switch (request.search.column.ToLower())
                    {
                        case "location":
                            baseQuery = baseQuery.Where(x => x.Location != null && x.Location.ToLower().Contains(searchVal));
                            break;
                        case "code":
                            baseQuery = baseQuery.Where(x => x.Code != null && x.Code.ToLower().Contains(searchVal));
                            break;
                        case "myriadid":
                            baseQuery = baseQuery.Where(x => x.MyriadID != null && x.MyriadID.ToLower().Contains(searchVal));
                            break;
                        case "fullname":
                            baseQuery = baseQuery.Where(x => x.FullName != null && x.FullName.ToLower().Contains(searchVal));
                            break;
                        case "remarks":
                            baseQuery = baseQuery.Where(x => x.Remarks != null && x.Remarks.ToLower().Contains(searchVal));
                            break;
                        case "servicename":
                            baseQuery = baseQuery.Where(x => x.ServiceName != null && x.ServiceName.ToLower().Contains(searchVal));
                            break;
                        case "netamount":
                            if (decimal.TryParse(searchVal, out decimal amount))
                            {
                                baseQuery = baseQuery.Where(x => x.NetAmount == amount);
                            }
                            break;
                        case "status":
                            if (bool.TryParse(searchVal, out bool status))
                            {
                                baseQuery = baseQuery.Where(x => x.Status == status);
                            }
                            break;
                        case "ispaid":
                            if (bool.TryParse(searchVal, out bool isPaid))
                            {
                                baseQuery = baseQuery.Where(x => x.isPaid == isPaid);
                            }
                            break;
                        case "createdby":
                            baseQuery = baseQuery.Where(x => x.CreatedBy != null && x.CreatedBy.ToLower().Contains(searchVal));
                            break;
                        case "approvedby":
                            baseQuery = baseQuery.Where(x => x.ApprovedBy != null && x.ApprovedBy.ToLower().Contains(searchVal));
                            break;
                    }
                }
                else if (!string.IsNullOrEmpty(searchValue))
                {
                    // General search - Fixed logic and performance
                    string searchVal = searchValue.ToLower();
                    baseQuery = baseQuery.Where(x =>
                        (x.Location != null && x.Location.ToLower().Contains(searchVal)) ||
                        (x.Code != null && x.Code.ToLower().Contains(searchVal)) ||
                        (x.MyriadID != null && x.MyriadID.ToLower().Contains(searchVal)) ||
                        (x.FullName != null && x.FullName.ToLower().Contains(searchVal)) ||
                        (x.Remarks != null && x.Remarks.ToLower().Contains(searchVal)) ||
                        //(x.ServiceName != null /*&& !string.IsNullOrEmpty(x.ServiceName) */&& x.ServiceName.ToLower().Contains(searchVal)) ||
                        (x.CreatedBy != null && x.CreatedBy.ToLower().Contains(searchVal)) ||
                        (x.ApprovedBy != null && x.ApprovedBy.ToLower().Contains(searchVal)));
                }

                // Apply ordering BEFORE projection
                IQueryable<EF.V_GetInvoiceList> orderedQuery = ApplyOrdering(baseQuery, orderBy, orderDir);

                // Get total count before projection for pagination
                int totalRecords = 0;
                if (string.IsNullOrEmpty(QueryBY))
                {
                    totalRecords = orderedQuery.Count();
                }

                // Apply pagination BEFORE projection
                if (!string.IsNullOrEmpty(QueryBY))
                {
                    // No pagination for export scenarios
                }
                else
                {
                    orderedQuery = orderedQuery
                        .Skip(Int32.Parse(start))
                        .Take(Int32.Parse(length));
                }

                // Project to ViewModel - Do this LAST to minimize data transfer
                var invoicing = orderedQuery.Select(x => new InvoiceViewModel
                {
                    Id = x.Id,
                    Code = x.Code,
                    Location = x.Location,
                    LocationId = x.LocationId,
                    MyriadID = x.MyriadID,
                    FullName = x.FullName,
                    InvoiceDate = x.InvoiceDate,
                    DueDate = x.DueDate,
                    CreatedDate = x.CreatedDate,
                    Remarks = x.Remarks,
                    ServiceName =/* string.IsNullOrEmpty(x.ServiceName) ? "N/A" : */x.ServiceName,
                    StudentId = x.StudentId,
                    NetAmount = x.NetAmount,
                    Status = x.Status,
                    isPaid = x.isPaid,
                    Refunded = x.Refunded,
                    InvoiceTypeId = x.InvoiceTypeId,
                    ParentInvoiceId = x.ParentInvoiceId,
                    CreatedBy = x.CreatedBy,
                    ApprovedBy = x.ApprovedBy,
                    TotalDiscountAmount = x.TotalDiscountAmount,
                    FromDate = x.FromDate,
                    ToDate = x.ToDate,
                    PendingBalance = x.PendingBalance,
                    TotalBalanceOfResident = x.TotalBalanceOfResident,
                    VoucherId = x.VoucherId ?? 0,
                    VoucherCode = x.VoucherCode

                });

                List<string> selectedColumn = request?.SelectedColumns ?? new List<string>();
                if (selectedColumn.Any())
                {
                    invoicing = ApplyColumnFiltering(invoicing, selectedColumn);
                }

                var result = new InvoicingsResponse();
                result.InvoicingList = invoicing.ToList();

                if (string.IsNullOrEmpty(QueryBY))
                {
                    result.TotalRecords = totalRecords;
                    result.RecordsFiltered = totalRecords;
                }

                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception occurred in GetAll: {ex.Message} - StackTrace: {ex.StackTrace}");
                throw;
            }
        }

        private IQueryable<EF.V_GetInvoiceList> ApplyOrdering(IQueryable<EF.V_GetInvoiceList> query, string orderBy, string orderDir)
        {
            bool ascending = orderDir?.ToLower() == "asc";

            switch (orderBy?.ToLower())
            {
                case "code":
                    return ascending ? query.OrderBy(x => x.Code) : query.OrderByDescending(x => x.Code);
                case "location":
                    return ascending ? query.OrderBy(x => x.Location) : query.OrderByDescending(x => x.Location);
                case "myriadid":
                    return ascending ? query.OrderBy(x => x.MyriadID) : query.OrderByDescending(x => x.MyriadID);
                case "fullname":
                    return ascending ? query.OrderBy(x => x.FullName) : query.OrderByDescending(x => x.FullName);
                case "invoicedate":
                    return ascending ? query.OrderBy(x => x.InvoiceDate) : query.OrderByDescending(x => x.InvoiceDate);
                case "fromdate":
                    return ascending ? query.OrderBy(x => x.FromDate) : query.OrderByDescending(x => x.FromDate);
                case "todate":
                    return ascending ? query.OrderBy(x => x.ToDate) : query.OrderByDescending(x => x.ToDate);
                case "duedate":
                    return ascending ? query.OrderBy(x => x.DueDate) : query.OrderByDescending(x => x.DueDate);
                case "remarks":
                    return ascending ? query.OrderBy(x => x.Remarks) : query.OrderByDescending(x => x.Remarks);
                case "servicename":
                    return ascending ? query.OrderBy(x => x.ServiceName) : query.OrderByDescending(x => x.ServiceName);
                case "netamount":
                    return ascending ? query.OrderBy(x => x.NetAmount) : query.OrderByDescending(x => x.NetAmount);
                case "pendingbalance":
                    return ascending ? query.OrderBy(x => x.PendingBalance) : query.OrderByDescending(x => x.PendingBalance);
                case "status":
                    return ascending ? query.OrderBy(x => x.Status) : query.OrderByDescending(x => x.Status);
                case "ispaid":
                    return ascending ? query.OrderBy(x => x.isPaid) : query.OrderByDescending(x => x.isPaid);
                case "createdby":
                    return ascending ? query.OrderBy(x => x.CreatedBy) : query.OrderByDescending(x => x.CreatedBy);
                case "approvedby":
                    return ascending ? query.OrderBy(x => x.ApprovedBy) : query.OrderByDescending(x => x.ApprovedBy);
                case "createddate":
                    return ascending ? query.OrderBy(x => x.CreatedDate) : query.OrderByDescending(x => x.CreatedDate);
                case "id":
                    return ascending ? query.OrderBy(x => x.Id) : query.OrderByDescending(x => x.Id);
                default:
                    return query.OrderByDescending(x => x.CreatedDate);
            }
        }

        private IQueryable<InvoiceViewModel> ApplyColumnFiltering(IQueryable<InvoiceViewModel> query, List<string> selectedColumns)
        {
            List<string> allColumns = new List<string>
    {
        "Code", "Location", "MyriadID", "FullName", "InvoiceDate", "DueDate", "CreatedDate",
        "Remarks", "ServiceName", "NetAmount", "Status", "isPaid", "CreatedBy", "ApprovedBy",
        "TotalBalanceOfResident", "PendingBalance"
    };

            List<string> unselectedColumns = allColumns.Except(selectedColumns).ToList();

            return query.Select(x => new InvoiceViewModel
            {
                Id = x.Id,
                Code = unselectedColumns.Contains("Code") ? x.Code : default,
                Location = unselectedColumns.Contains("Location") ? x.Location : default,
                MyriadID = unselectedColumns.Contains("MyriadID") ? x.MyriadID : default,
                FullName = unselectedColumns.Contains("FullName") ? x.FullName : default,
                InvoiceDate = unselectedColumns.Contains("InvoiceDate") ? x.InvoiceDate : default,
                DueDate = unselectedColumns.Contains("DueDate") ? x.DueDate : default,
                CreatedDate = unselectedColumns.Contains("CreatedDate") ? x.CreatedDate : default,
                Remarks = unselectedColumns.Contains("Remarks") ? x.Remarks : default,
                ServiceName = unselectedColumns.Contains("ServiceName") ? x.ServiceName : default,
                StudentId = x.StudentId,
                NetAmount = unselectedColumns.Contains("NetAmount") ? x.NetAmount : default,
                Status = unselectedColumns.Contains("Status") ? x.Status : default,
                isPaid = unselectedColumns.Contains("isPaid") ? x.isPaid : default,
                Refunded = x.Refunded,
                InvoiceTypeId = x.InvoiceTypeId,
                ParentInvoiceId = x.ParentInvoiceId,
                CreatedBy = unselectedColumns.Contains("CreatedBy") ? x.CreatedBy : default,
                ApprovedBy = unselectedColumns.Contains("ApprovedBy") ? x.ApprovedBy : default,
                TotalDiscountAmount = x.TotalDiscountAmount,
                FromDate = x.FromDate,
                ToDate = x.ToDate,
                PendingBalance = unselectedColumns.Contains("PendingBalance") ? x.PendingBalance : default,
                TotalBalanceOfResident = unselectedColumns.Contains("TotalBalanceOfResident") ? x.TotalBalanceOfResident : default
            });
        }

        public List<Invoicing> GetInvoicesByStudentId(int studentId)
        {
            using (var context = new PMSEntities())
            {
                var invoices = context.Invoicings.Include("InvoicingDetails")
                    .Where(i => i.StudentId == studentId
                                && i.Code != null
                                && !i.Code.StartsWith("R-")
                                && i.Refunded == null)
                    .ToList();

                return invoices;
            }
        }


        public OutputInvoicingVM GetStudentOccupancy(int serviceId, int residentId, int invoiceTypeId)
        {
            var db = uow.Context;

            var data = (from booking in db.Bookings

                        join pl in db.BedSpacePlacements
                        on booking.BookingID equals pl.BookingID
                        into plc
                        from placement in plc.DefaultIfEmpty()


                        join pr in db.PriceConfigs
                        on booking.PriceConfigID equals pr.PriceConfigID
                        into pri
                        from priceConfig in pri.DefaultIfEmpty()

                        join t in db.Terms
                        on priceConfig.TermID equals t.TermID
                        into tr
                        from term in tr.DefaultIfEmpty()



                        where booking.IsEnable == true && booking.IsCancel != true && booking.PersonID == residentId
                        && placement != null && placement.IsEnable == true
                        orderby placement.CreatedDate descending
                        select new
                        {
                            occupancyName = term.TermName + " - " + term.TermDescription,
                            occupancyId = priceConfig.PriceConfigID,
                            depositPrice = priceConfig.InitialDeposit,
                            price = priceConfig.Price,
                            checkInDate = placement.MoveIn,
                            checkOutDate = (DateTime?)placement.MoveOut,
                            termID = term.TermID,
                            frequencyID = term.FrequencyId,
                            locationId = booking.LocationID

                        }).FirstOrDefault();
            if (invoiceTypeId == (int)InvoiceTypes.Deposit)
            {
                data = uow.GenericRepository<EF.Booking>().Table.Where(x => x.IsEnable == true && x.IsCancel != true && x.PersonID == residentId).OrderByDescending(x => x.CreatedDate)
                    .Select(x => new
                    {
                        occupancyName = x.PriceConfig.Term.TermName + " - " + x.PriceConfig.Term.TermDescription,
                        occupancyId = x.PriceConfig.PriceConfigID,
                        depositPrice = x.PriceConfig.InitialDeposit,
                        price = x.PriceConfig.Price,
                        checkInDate = x.CheckInDate,
                        checkOutDate = x.CheckOutDate,
                        termID = x.PriceConfig.TermID,
                        frequencyID = x.PriceConfig.Term.FrequencyId,
                        locationId = x.LocationID


                    }).FirstOrDefault();
            }

            var occupancyId = 0;
            var taxId = 0;
            var occupancy = "";
            decimal DepositPrice = 0;
            decimal Price = 0;
            int TermID = 0;
            int FrequencyID = 0;
            int LocationID = 0;
            DateTime? checkInDate = null;
            DateTime? checkOutDate = null;
            if (data != null)
            {
                occupancy = data.occupancyName;
                occupancyId = data.occupancyId;
                DepositPrice = data.depositPrice;
                Price = data.price;
                checkInDate = data.checkInDate;
                checkOutDate = data.checkOutDate;
                TermID = data.termID;
                FrequencyID = data.frequencyID ?? 2;
                LocationID = data.locationId ?? 0;


            }
            bool isDailyRate = occupancy.Contains("Daily Rate");


            var service = uow.GenericRepository<EF.Service>().GetById(serviceId);
            if (service == null)
            {
                throw new Exception("ServiceType Is Not Exist!");
            }
            decimal ServicePrice = 0;
            if (service.TypeId == (int)ServiceTypes.Deposit)
                ServicePrice = DepositPrice;
            else if (service.TypeId == (int)ServiceTypes.RentalCharges && service.AccountId == (int)COA.Cleaning)
            {
                if (isDailyRate)
                {
                    var cleaningServiceAmount = 95.14m;
                    ServicePrice = cleaningServiceAmount / 30.0m; // Convert monthly fee to daily rate
                }
                else
                {
                    ServicePrice = service.ServiceAmount; // Use monthly fee
                }
                taxId = service.TaxId ?? 0;
            }
            else if (service.TypeId == (int)ServiceTypes.RentalCharges)
            {
                ServicePrice = Price;
                taxId = service.TaxId ?? 0;
            }

            else
                ServicePrice = service.ServiceAmount;

            var response = new OutputInvoicingVM
            {
                Name = occupancy,
                ServiceTypeId = service.TypeId,
                Occupancy = occupancy,
                ServicePrice = ServicePrice,
                OccupancyId = occupancyId,
                CheckOutDate = checkOutDate,
                CheckInDate = checkInDate,
                TaxId = taxId,
                ServiceId = serviceId,
                TermId = TermID,
                FrequencyId = FrequencyID,
                LocationID = LocationID,
            };
            return response;

        }

        public List<OutputInvoicingVM> GetAllDepositOptions(int serviceId, int residentId)
        {
            var today = DateTime.Now.Date;
            var bookings = uow.GenericRepository<EF.Booking>().Table
                .Where(x => x.IsEnable == true && x.IsCancel != true && x.PersonID == residentId && x.CheckOutDate >= today)
                .OrderByDescending(x => x.CreatedDate)
                .Select(x => new
                {
                    occupancyName = x.PriceConfig.Term.TermName,
                    occupancyId = x.PriceConfig.PriceConfigID,
                    depositPrice = x.PriceConfig.InitialDeposit,
                    price = x.PriceConfig.Price,
                    checkInDate = x.CheckInDate,
                    checkOutDate = x.CheckOutDate,
                    termID = x.PriceConfig.TermID,
                    frequencyID = x.PriceConfig.Term.FrequencyId,
                    locationId = x.LocationID
                }).ToList();

            var service = uow.GenericRepository<EF.Service>().GetById(serviceId);

            return bookings.Select(b => new OutputInvoicingVM
            {
                Occupancy = b.occupancyName,
                OccupancyId = b.occupancyId,
                ServicePrice = b.depositPrice,
                CheckInDate = b.checkInDate,
                CheckOutDate = b.checkOutDate,
                TermId = b.termID,
                FrequencyId = b.frequencyID ?? 2,
                LocationID = b.locationId ?? 0,
                ServiceId = serviceId,
                TaxId = service?.TaxId ?? 0
            }).ToList();
        }

        public List<InvoicingDetailVM> GetInvoiceDetail(int InvoiceId)
        {
            var Invoice = uow.GenericRepository<Invoicing>().GetById(InvoiceId);
            var priceConfig = uow.GenericRepository<PriceConfig>().Table.AsNoTracking().Where(x => x.TermID == Invoice.TermID).Select(x => x.PriceConfigID).FirstOrDefault();
            var detail = uow.GenericRepository<InvoicingDetail>().Table.AsNoTracking().Where(x => x.InvoicingId == InvoiceId).ToList();

            List<InvoicingDetailVM> list = new List<InvoicingDetailVM>();
            foreach (var item in detail)
            {
                var StudentBooking = GetStudentOccupancy(item.ServiceId, Invoice.StudentId, Invoice.InvoiceTypeId ?? 0);
                var service = uow.GenericRepository<EF.Service>().Table.Where(x => x.ServiceId == item.ServiceId).FirstOrDefault();
                InvoicingDetailVM model = new InvoicingDetailVM();
                model.LocationId = Invoice.LocationId;
                model.Id = item.Id;
                model.InvvoicingId = item.InvoicingId;
                model.PriceConfig = priceConfig;
                model.ServiceId = item.ServiceId;
                model.ServiceName = item.ServiceName;
                model.TaxesIds = item.TaxesIds;
                model.TaxesAmount = item.TaxAmount;
                model.ToDate = item.ToDate;
                model.FromDate = item.FromDate;
                model.TaxesName = item.TaxesName;
                model.FromDateString = item.FromDate != null ? item.FromDate?.ToString("dd/MM/yyyy") : "N/A";
                model.ToDateString = item.ToDate != null ? item.ToDate?.ToString("dd/MM/yyyy") : "N/A";
                model.BaseServicePrice = StudentBooking.ServicePrice;
                model.TotalAmount = item.TotalAmount;
                model.Price = item.Price;
                model.Description = item.Description;
                model.DiscountPercentage = item.DiscountPercentage;
                model.DiscountAmount = item.DiscountAmount;
                list.Add(model);
            }
            return list;
        }
        public int? GetServiceType(int invoiceDetailId)
        {
            var serviceId = uow.GenericRepository<InvoicingDetail>().Table.AsNoTracking().Where(x => x.Id == invoiceDetailId).Select(x => x.ServiceId).FirstOrDefault();

            if (serviceId == 0) return null;

            var serviceTypeId = uow.GenericRepository<PMS.EF.Service>().Table.AsNoTracking().Where(x => x.ServiceId == serviceId).Select(x => x.TypeId).FirstOrDefault();
            return serviceTypeId;
        }

        public List<InvoicingDetailVM> GetFeeAssessmentInvoiceDetail(int InvoiceId)
        {
            var Invoice = uow.GenericRepository<Invoicing>().GetById(InvoiceId);
            var detail = uow.GenericRepository<InvoicingDetail>().GetAll(x => x.InvoicingId == InvoiceId);

            DateTime maxFromDate = detail.Max(d => d.FromDate.GetValueOrDefault());
            DateTime maxToDate = detail.Max(d => d.ToDate.GetValueOrDefault());

            var maxDateItem = detail.OrderByDescending(d => d.ToDate.GetValueOrDefault()).FirstOrDefault();

            List<InvoicingDetailVM> list = new List<InvoicingDetailVM>();

            if (maxDateItem != null)
            {
                var StudentBooking = GetStudentOccupancy(maxDateItem.ServiceId, Invoice.StudentId, Invoice.InvoiceTypeId ?? 0);
                var service = uow.GenericRepository<EF.Service>().Table.Where(x => x.ServiceId == maxDateItem.ServiceId).FirstOrDefault();

                InvoicingDetailVM model = new InvoicingDetailVM
                {
                    Id = maxDateItem.Id,
                    InvvoicingId = maxDateItem.InvoicingId,
                    ServiceId = maxDateItem.ServiceId,
                    ServiceName = maxDateItem.ServiceName,
                    TaxesIds = maxDateItem.TaxesIds,
                    FromDate = maxToDate.AddDays(1),
                    ToDate = maxToDate.AddDays(31),
                    TaxesName = maxDateItem.TaxesName,
                    FromDateString = maxDateItem.FromDate?.ToString("dd/MM/yyyy") ?? "N/A",
                    ToDateString = maxDateItem.ToDate?.ToString("dd/MM/yyyy") ?? "N/A",
                    BaseServicePrice = StudentBooking.ServicePrice,
                    TotalAmount = maxDateItem.TotalAmount,
                    Price = maxDateItem.Price,
                    Description = maxDateItem.Description,
                    DiscountPercentage = maxDateItem.DiscountPercentage,
                    DiscountAmount = maxDateItem.DiscountAmount
                };

                Invoice.Code = GetMaxInvoiceCodeString(Invoice.LocationId, Invoice.InvoiceTypeId ?? 1);

                list.Add(model);
            }
            maxDateItem.Id = 0;
            maxDateItem.InvoicingId = 0;
            Invoice.Id = 0;
            return list;
        }

        private DateTime? ParseDate(string dateString)
        {
            if (DateTime.TryParseExact(dateString, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                return date;
            }

            return null;
        }

        public Invoicing GetById(int? Id)
        {
            return uow.GenericRepository<Invoicing>().GetById(Id);
        }

        public Invoicing GetLastInoviceByStudentIdId(int? StudentId)
        {
            var lastInvoice = uow.GenericRepository<EF.Invoicing>()
                .Table
                .Where(i => i.StudentId == StudentId && i.InvoiceTypeId == (int)InvoiceTypes.Rental && i.IsApproved == true)
                .OrderByDescending(i => i.InvoiceDate)
                .ThenByDescending(i => i.Id)
                .FirstOrDefault();
            if (lastInvoice != null)
            {
                lastInvoice.InvoiceDate = DateTime.Now.Date;
            }
            return lastInvoice;
        }

        public LastInvoiceCheckResult GetLastMultipleInvoicesStatusWithInvoice(int? studentId, DateTime fromDate, DateTime toDate)
        {
            var lastInvoice = uow.GenericRepository<Invoicing>().Table
                      .Where(i => i.StudentId == studentId && i.ParentInvoiceId == null && i.IsApproved == true && i.BilledUpToDate != null && i.InvoiceTypeId == (int)InvoiceTypes.Rental)
                          .OrderByDescending(i => i.BilledUpToDate)
                          .FirstOrDefault();

            if (lastInvoice != null)
            {
                var lastBilledUpToDate = lastInvoice.BilledUpToDate.Value.Date;

                // Conflict only when the entire requested range is already billed.
                // Keep partial-overlap ranges eligible so fee assessment can start
                // from BilledUpToDate + 1 day and create only the missing period.
                if (toDate.Date <= lastBilledUpToDate)
                {
                    return new LastInvoiceCheckResult
                    {
                        Status = "DateConflicted",
                        LastInvoiceId = lastInvoice.Id
                    };
                }

                return new LastInvoiceCheckResult
                {
                    Status = "Invoiced",
                    LastInvoiceId = lastInvoice.Id
                };
            }

            return new LastInvoiceCheckResult
            {
                Status = "NoInvoice"
            };
        }
        public List<InvoicingDetail> GetInvoiceDetailsByInvoiceId(int invoiceId)
        {
            return uow.GenericRepository<InvoicingDetail>()
                .Table
                .Where(d => d.InvoicingId == invoiceId)
                .ToList();
        }

        public Invoicing GetInoviceForReverse(int id)
        {
            return uow.GenericRepository<EF.Invoicing>().Table.FirstOrDefault(i => i.Id == id);
        }

        public List<InvoicingDetail> GetInvoiceDetailsForReverse(int invoiceId)
        {
            var details = uow.GenericRepository<InvoicingDetail>().Table.AsNoTracking().Where(x => x.InvoicingId == invoiceId).ToList()
                //.GetAll(x => x.InvoicingId == invoiceId)
                .Select(detailItem => new InvoicingDetail()
                {
                    Id = detailItem.Id,
                    ServiceId = detailItem.ServiceId,
                    ServiceName = detailItem.ServiceName,
                    Price = -detailItem.Price,
                    Description = detailItem.Description,
                    TaxesIds = detailItem.TaxesIds,
                    TaxesName = detailItem.TaxesName,
                    TaxAmount = -detailItem.TaxAmount,
                    TotalAmount = -detailItem.TotalAmount,
                    FromDate = detailItem.FromDate,
                    ToDate = detailItem.ToDate,
                    DiscountPercentage = detailItem.DiscountPercentage,
                    DiscountAmount = -detailItem.DiscountAmount
                })
                .ToList();

            return details;
        }

        public static InvoicingDetail GetReverseInvoicingRevenueDetail1(InvoicingDetail list)
        {
            list.InvoicingRevenueDetails = new List<InvoicingRevenueDetail>();

            DateTime? Date1 = list.FromDate;
            DateTime? Date2 = list.ToDate;

            if (Date1 == null || Date2 == null)
            {
                Date1 = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                Date2 = Date1.Value.AddDays(31);
            }

            List<DateTime> Dates = new List<DateTime>();
            while (Date1 < Date2)
            {
                Dates.Add(Date1.Value);
                Date1 = Date1.Value.AddDays(1);
            }

            list.InvoicingRevenueDetails = Dates.GroupBy(x => x.Month)
                .Select(x => new InvoicingRevenueDetail
                {
                    Month = x.FirstOrDefault().Date,
                    Days = x.Count(),
                    Revenue = (((decimal)list.TotalAmount / Dates.Count()) * x.Count()),
                    DeferredRevenue = (((decimal)list.TotalAmount / Dates.Count()) * Dates.GroupBy(y => y.Month)
                        .Where(y => y.FirstOrDefault().Date > x.FirstOrDefault().Date)
                        .Sum(y => y.Count()))
                })
                .ToList();

            return list;
        }

        public InvoicingsResponse Getcalculation()
        {
            try
            {
                var invoicingQuery = uow.GenericRepository<EF.V_GetInvoiceList>().Table
                    .Select(x => new InvoiceViewModel
                    {
                        Id = x.Id,
                        PendingBalance = x.PendingBalance,
                        TotalBalanceOfResident = x.TotalBalanceOfResident
                    });

                var invoicing = invoicingQuery.ToList();
                var response = new InvoicingsResponse
                {
                    InvoicingList = invoicing,
                    TotalRecords = invoicing.Count
                };

                return response;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred: {ex.Message}");
                throw;
            }
        }

        public List<InvoicingVM> GetByPersonId(int PersonId)
        {
            var Invoices = uow.GenericRepository<Invoicing>().Table.Where(x => x.StudentId == PersonId && x.IsApproved == true && x.IsPaid != true && x.Refunded == null && x.ParentInvoiceId == null).Select(x => new InvoicingVM
            {
                Id = x.Id,
                Value = x.Code + " - (" + x.NetAmount + ")",
                Amount = x.NetAmount
            }).ToList();
            return Invoices;
        }

        public List<UnpaidInvoiceVM> GetUnpaidInvoicesWithDueByPerson(int personId)
        {
            var invoices = uow.GenericRepository<Invoicing>().Table
                .Where(x => x.StudentId == personId && x.IsApproved == true && x.IsPaid != true && x.Refunded == null && x.ParentInvoiceId == null)
                .Select(x => new { x.Id, x.Code, x.InvoiceDate, x.NetAmount })
                .ToList();

            var result = new List<UnpaidInvoiceVM>();

            foreach (var inv in invoices)
            {
                var ledgerLines = uow.GenericRepository<EF.StudentLedger>().Table
                    .Where(l => l.InvoiceId == inv.Id)
                    .Select(l => new { l.DebitAmount, l.CreditAmount })
                    .ToList();

                var debit = ledgerLines.Sum(l => l.DebitAmount ?? 0);
                var credit = ledgerLines.Sum(l => l.CreditAmount ?? 0);
                var due = debit - credit;

                result.Add(new UnpaidInvoiceVM
                {
                    Id = inv.Id,
                    Code = inv.Code,
                    InvoiceDate = inv.InvoiceDate,
                    InvoiceAmount = inv.NetAmount,
                    AmountDue = due
                });
            }

            return result.OrderBy(x => x.InvoiceDate).ToList();
        }

        public List<InvoiceTypeLookup> GetInvoicingTypes()
        {
            return uow.GenericRepository<InvoiceTypeLookup>().Table.Where(x => x.DisplayToUser == true).ToList();
        }

        public InvoicingVM GetEditInvoiceById(int InvoiceId)
        {
            var Invoice = uow.GenericRepository<Invoicing>().Table.Where(x => x.Id == InvoiceId && x.IsApproved == true && x.IsPaid != true && x.Refunded != false).Select(x => new InvoicingVM
            {
                Id = x.Id,
                Value = x.Code + " - (" + x.NetAmount + ")",
                Amount = x.NetAmount
            }).FirstOrDefault();
            return Invoice;
        }

        public List<InvoicingDetail> GetMultipleInvoiceDetail(Invoicing inv, int InvoiceId, DateTime invoiceStartDate, DateTime invoiceEndDate)
        {
            var Invoice = uow.GenericRepository<Invoicing>().GetById(InvoiceId);
            var details = uow.GenericRepository<InvoicingDetail>()
                .Table.AsNoTracking()
                .Where(x => x.InvoicingId == InvoiceId)
                .Select(d => new
                {
                    d.ServiceId,
                    d.ServiceName,
                    d.Description,
                    d.TaxesIds,
                    d.TaxesName,
                    d.DiscountPercentage,
                    d.DiscountAmount,
                    d.FromDate,
                    d.ToDate
                })
                .ToList();

            if (!details.Any())
                return new List<InvoicingDetail>();

            var invoicingDetails = details.Select(maxDateItem => new InvoicingDetail
            {
                ServiceId = maxDateItem.ServiceId,
                ServiceName = maxDateItem.ServiceName,
                Price = inv.TotalPrice / details.Count,
                Description = maxDateItem.Description,
                TaxesIds = maxDateItem.TaxesIds,
                TaxesName = maxDateItem.TaxesName,
                TaxAmount = inv.TaxAmount / details.Count,
                TotalAmount = inv.NetAmount / details.Count,
                FromDate = ParseDate(invoiceStartDate.ToString("dd/MM/yyyy")),
                ToDate = ParseDate(invoiceEndDate.ToString("dd/MM/yyyy")),
                DiscountPercentage = maxDateItem.DiscountPercentage,
                DiscountAmount = maxDateItem.DiscountAmount
            }).ToList();

            return invoicingDetails;
        }

        #region  ------------------------- ** Invoice Code Generation ** -------------------------

        public string GetMaxInvoiceCodeString(int id, int InvoiceTypeId)
        {
            lock (InvoiceCodeLock)
            {
                using (var db1 = new PMSEntities())
                {
                    var locationPrefix = db1.Locations
                        .AsNoTracking()
                        .Where(x => x.LocationID == id)
                        .Select(x => x.Prefix)
                        .FirstOrDefault();

                    var invoiceTypePrefix = db1.InvoiceTypeLookups
                        .AsNoTracking()
                        .Where(x => x.Id == InvoiceTypeId)
                        .Select(x => x.InvoicePrefix)
                        .FirstOrDefault();

                    var maxcode = GetMaxInvoiceCode(db1, id, InvoiceTypeId);
                    string value = String.Format("{0:D4}", maxcode);
                    var Code = (invoiceTypePrefix ?? string.Empty).Trim() + "-" + locationPrefix + "-" + value;
                    return Code;
                }
            }
        }

        public static int GetMaxInvoiceCode(int id, int InvoiceTypeId)
        {
            using (var db1 = new PMSEntities())
            {
                return GetMaxInvoiceCode(db1, id, InvoiceTypeId);
            }
        }

        private static int GetMaxInvoiceCode(PMSEntities db1, int id, int InvoiceTypeId)
        {
            var maxExisting = db1.Invoicings
                .AsNoTracking()
                .Where(x => x.LocationId == id && x.InvoiceTypeId == InvoiceTypeId && x.Code != null)
                .Select(x => x.Code)
                .AsEnumerable()
                .Select(TryGetTrailingNumber)
                .Where(x => x.HasValue)
                .Select(x => x.Value)
                .DefaultIfEmpty(0)
                .Max();

            return maxExisting + 1;
        }

        private static int? TryGetTrailingNumber(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return null;

            var lastPart = code.Split('-').LastOrDefault();
            if (int.TryParse(lastPart, out int number))
                return number;

            return null;
        }

        #endregion

        public static List<InvoicingDetail> GetInvoicingRevenueDetail(List<InvoicingDetail> list)
        {
            List<InvoicingDetail> List = new List<InvoicingDetail>();
            var DetailList = list;
            foreach (var item in DetailList)
            {
                item.InvoicingRevenueDetails = new List<InvoicingRevenueDetail>();

                var Date1 = item.FromDate.Value;
                var Date2 = item.ToDate.Value;


                List<DateTime> Dates = new List<DateTime>();
                while (Date1 < Date2)
                {

                    Dates.Add(Date1);
                    Date1 = Date1.AddDays(1);
                }


                item.InvoicingRevenueDetails = (Dates.GroupBy(x => x.Month).Select(x => new InvoicingRevenueDetail
                {
                    Month = x.FirstOrDefault().Date,
                    Days = x.Count(),
                    Revenue = (((decimal)item.TotalAmount / Dates.Count()) * x.Count()),
                    DeferredRevenue = (((decimal)item.TotalAmount / Dates.Count()) * Dates.GroupBy(y => y.Month).Where(y => y.FirstOrDefault().Date > x.FirstOrDefault().Date).Sum(y => y.Count()))
                }
                ).ToList());

                List.Add(item);
            };

            return List;
        }

        public static InvoicingDetail GetInvoicingRevenueDetailForFeeAssessment(InvoicingDetail list)
        {

            list.InvoicingRevenueDetails = new List<InvoicingRevenueDetail>();

            var Date1 = list.FromDate.Value;
            var Date2 = list.ToDate.Value;


            List<DateTime> Dates = new List<DateTime>();
            while (Date1 < Date2)
            {

                Dates.Add(Date1);
                Date1 = Date1.AddDays(1);
            }


            list.InvoicingRevenueDetails = (Dates.GroupBy(x => x.Month).Select(x => new InvoicingRevenueDetail
            {
                Month = x.FirstOrDefault().Date,
                Days = x.Count(),
                Revenue = (((decimal)list.TotalAmount / Dates.Count()) * x.Count()),
                DeferredRevenue = (((decimal)list.TotalAmount / Dates.Count()) * Dates.GroupBy(y => y.Month).Where(y => y.FirstOrDefault().Date > x.FirstOrDefault().Date).Sum(y => y.Count()))
            }
            ).ToList());




            return list;
        }

        public List<OutputInvoicingVM> GetActiveInvoicesByPerson(int personId)
        {

            var invoicing = uow.GenericRepository<EF.Invoicing>().Table.Where(x => x.StudentId == personId && x.IsApproved == true).ToList();


            List<OutputInvoicingVM> list = new List<OutputInvoicingVM>();
            foreach (var item in invoicing)
            {
                OutputInvoicingVM model = new OutputInvoicingVM();
                model.Id = item.Id;
                model.Code = item.Code;
                model.InvoiceDate = item.InvoiceDate;
                model.Remarks = item.Remarks;
                model.NetAmount = item.NetAmount;
                model.LocationID = item.LocationId;
                model.Status = item.IsPaid ?? false;


                list.Add(model);

            }
            return list;
        }

        public AddTermVM GetFrequencyById(int Id, int configId)
        {
            var fre = uow.GenericRepository<EF.Booking>().Table.Where(x => x.PersonID == Id && x.PriceConfigID == configId && x.IsEnable == true && x.PriceConfig.IsEnable == true && x.PriceConfig.Term.IsEnable == true && x.IsCancel == false).Select(x => new AddTermVM
            {
                FrequencyId = x.PriceConfig.Term.FrequencyId ?? 0
            }).FirstOrDefault();
            return fre;
        }

        public DepositInvoicesVM GetDepositInvoices(int InvoiceId)
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();


            var res = uow.GenericRepository<EF.Invoicing>().Table.Where(x => x.Id == InvoiceId && assignedLocationIds.Contains((int)x.LocationId)).Select(x => new DepositInvoicesVM
            {
                InvoiceId = x.Id,
                InvoiceDate = x.InvoiceDate.ToString(),
                InvoiceCode = x.Code,
                personId = x.StudentId,
                Remarks = x.Remarks,
                InvoicePrice = x.TotalPrice,
                TaxAmount = x.TaxAmount,
                NetAmount = x.NetAmount,
                TaxIds = x.TaxIds,
                Status = x.IsApproved.ToString(),
                CreatedDate = x.CreatedDate,
                CreatedBy = x.CreatedBy,
                UpdatedDate = x.UpdatedDate,
                LocationId = x.LocationId,
                IsPaid = x.IsPaid,
                InvoiceTypeId = x.InvoiceTypeId,
                Refunded = x.Refunded,
                ParentInvoiceId = x.ParentInvoiceId,
                LocationName = x.Location.LocationName,
                FullName = x.Person.FullName,
                TermID = x.TermID,
                DebitAmount = x.StudentLedgers.Where(sl => sl.InvoiceId == InvoiceId).Sum(sl => sl.DebitAmount),
                CreditAmount = x.StudentLedgers.Where(sl => sl.InvoiceId == InvoiceId).Sum(sl => sl.CreditAmount)
            }).FirstOrDefault();

            return res;
        }

        public List<PaymentVM> ReceiptDetail(int? id)
        {
            var res = uow.GenericRepository<EF.StudentLedger>().Table.Where(x => x.IsApproved == true && x.InvoiceId == id && x.CreditAmount != null).Select(x => new PaymentVM
            {
                TransactionCode = x.Code,
                Amount = x.CreditAmount ?? 0,
                Id = x.Id

            }).ToList();
            return res;
        }

        #endregion

        #region Export 
        public InvoicingsResponse ExportInvoiceReport(string QueryBY, DateTime? FromDate = null, DateTime? ToDate = null, int? InvoiceTypeId = null)
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();
            var previousCommandTimeout = uow.Context.Database.CommandTimeout;
            uow.Context.Database.CommandTimeout = 180;
            try
            {
                IQueryable<EF.V_GetInvoiceList> query = uow.GenericRepository<EF.V_GetInvoiceList>().Table.AsNoTracking()
     .Where(x =>
          (assignedLocationIds.Contains((int)x.LocationId))
      ).Where(x => (!FromDate.HasValue || DbFunctions.TruncateTime(x.InvoiceDate) >= DbFunctions.TruncateTime(FromDate.Value)) &&
                       (!ToDate.HasValue || DbFunctions.TruncateTime(x.InvoiceDate) <= DbFunctions.TruncateTime(ToDate.Value)))

           .Where(x => (!InvoiceTypeId.HasValue || x.InvoiceTypeId == InvoiceTypeId.Value));

                var result = query.Select(x => new InvoiceViewModel
                {
                    Id = x.Id,
                    Code = x.Code,
                    Location = x.Location,
                    MyriadID = x.MyriadID,
                    FullName = x.FullName,
                    InvoiceDate = x.InvoiceDate,
                    DueDate = x.DueDate,
                    CreatedDate = x.CreatedDate,
                    Remarks = x.Remarks,
                    ServiceName = x.ServiceName,
                    StudentId = x.StudentId,
                    NetAmount = x.NetAmount,
                    Status = x.Status,
                    isPaid = x.isPaid,
                    Refunded = x.Refunded,
                    InvoiceTypeId = x.InvoiceTypeId,
                    ParentInvoiceId = x.ParentInvoiceId,
                    CreatedBy = x.CreatedBy,
                    ApprovedBy = x.ApprovedBy,
                    TotalDiscountAmount = x.TotalDiscountAmount,
                    FromDate = x.FromDate,
                    ToDate = x.ToDate,
                    PendingBalance = x.PendingBalance,
                    TotalBalanceOfResident = x.TotalBalanceOfResident
                }).OrderByDescending(x => x.CreatedDate)
        .ToList();

                return new InvoicingsResponse { InvoicingList = result };
            }
            finally
            {
                uow.Context.Database.CommandTimeout = previousCommandTimeout;
            }
        }

        public async Task<InvoicingsResponse> ExportInvoiceReportAsync(string QueryBY, DateTime? FromDate = null, DateTime? ToDate = null, int? InvoiceTypeId = null)
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();
            var previousCommandTimeout = uow.Context.Database.CommandTimeout;
            uow.Context.Database.CommandTimeout = 180;
            try
            {
                IQueryable<EF.V_GetInvoiceList> query = uow.GenericRepository<EF.V_GetInvoiceList>().Table.AsNoTracking()
     .Where(x =>
          (assignedLocationIds.Contains((int)x.LocationId))
     ).Where(x => (!FromDate.HasValue || DbFunctions.TruncateTime(x.InvoiceDate) >= DbFunctions.TruncateTime(FromDate.Value)) &&
                       (!ToDate.HasValue || DbFunctions.TruncateTime(x.InvoiceDate) <= DbFunctions.TruncateTime(ToDate.Value)))
           .Where(x => (!InvoiceTypeId.HasValue || x.InvoiceTypeId == InvoiceTypeId.Value));

                var result = await query.Select(x => new InvoiceViewModel
                {
                    Id = x.Id,
                    Code = x.Code,
                    Location = x.Location,
                    MyriadID = x.MyriadID,
                    FullName = x.FullName,
                    InvoiceDate = x.InvoiceDate,
                    DueDate = x.DueDate,
                    CreatedDate = x.CreatedDate,
                    Remarks = x.Remarks,
                    ServiceName = x.ServiceName,
                    StudentId = x.StudentId,
                    NetAmount = x.NetAmount,
                    Status = x.Status,
                    isPaid = x.isPaid,
                    Refunded = x.Refunded,
                    InvoiceTypeId = x.InvoiceTypeId,
                    ParentInvoiceId = x.ParentInvoiceId,
                    CreatedBy = x.CreatedBy,
                    ApprovedBy = x.ApprovedBy,
                    TotalDiscountAmount = x.TotalDiscountAmount,
                    FromDate = x.FromDate,
                    ToDate = x.ToDate,
                    PendingBalance = x.PendingBalance,
                    TotalBalanceOfResident = x.TotalBalanceOfResident
                }).OrderByDescending(x => x.CreatedDate)
        .ToListAsync();

                return new InvoicingsResponse { InvoicingList = result };
            }
            finally
            {
                uow.Context.Database.CommandTimeout = previousCommandTimeout;
            }
        }


        #endregion

        #region Post and Boolean Methods

        //public bool IsPersonBookedAndPlaced(int personId)
        //{
        //    var db = uow.Context;
        //    try
        //    {
        //        var result = (from b in db.Bookings
        //                      where b.PersonID == personId &&
        //                            b.IsEnable == true &&
        //                            b.IsCancel == false
        //                      let latestBooking = db.Bookings
        //                                         .Where(lb => lb.PersonID == b.PersonID)
        //                                         .OrderByDescending(lb => lb.CreatedDate)
        //                                         .FirstOrDefault()
        //                      let activeBedSpacePlacement = db.BedSpacePlacements
        //                                                 .OrderByDescending(x => x.CreatedDate)
        //                                                 .FirstOrDefault(bs => bs.BookingID == latestBooking.BookingID)
        //                      where activeBedSpacePlacement != null &&
        //                            activeBedSpacePlacement.IsEnable == true &&
        //                            (activeBedSpacePlacement.CheckOut == null || activeBedSpacePlacement.CheckIn == null)
        //                      select new
        //                      {
        //                          PersonID = personId,
        //                          BookingID = latestBooking.BookingID,
        //                          BedSpacePlacementID = activeBedSpacePlacement.BedSpacePlacementID
        //                      }).FirstOrDefault();

        //        if (result != null)
        //        {
        //            Console.WriteLine($"PersonID: {result.PersonID}, BookingID: {result.BookingID}, BedSpacePlacementID: {result.BedSpacePlacementID}");
        //            return true;
        //        }
        //        else
        //        {
        //            var personCode = db.People.Where(p => p.PersonID == personId).Select(p => p.Code).SingleOrDefault();
        //            return false;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return false;
        //    }
        //}

        public bool IsPersonBookedAndPlaced(int personId, DateTime startDate, DateTime endDate)
        {
            var db = uow.Context;

            try
            {
                var result = (from b in db.Bookings
                              where b.PersonID == personId
                && b.IsEnable == true
                && b.IsCancel == false
                              // Ensure booking overlaps with the invoice period
                              // && b.CheckInDate <= endDate // Booking starts before invoice period ends
                              // && (b.CheckOutDate == null || b.CheckOutDate >= startDate) // Booking ends after invoice period starts
                              //orderby b.CreatedDate descending
                              let hasActivePlacement = db.BedSpacePlacements.Where(p =>
                                  p.BookingID == b.BookingID &&
                                  p.IsEnable
                && p.MoveIn <= endDate &&
                                  (p.MoveOut == null || p.MoveOut >= startDate)
                              ).OrderByDescending(x => x.MoveIn)
                            .ThenByDescending(x => x.MoveOut)
                              .FirstOrDefault() // Placement is active during invoice period
                              where hasActivePlacement != null


                              select new
                              {
                                  b.PersonID,
                                  b.BookingID,
                                  CheckInDate = hasActivePlacement.MoveIn,
                                  CheckOutDate = hasActivePlacement.MoveOut
                                  //b.CheckInDate,
                                  //b.CheckOutDate
                              }).FirstOrDefault();

                if (result != null)
                {
                    Console.WriteLine($"PersonID: {result.PersonID}, BookingID: {result.BookingID}, CheckIn: {result.CheckInDate}, CheckOut: {result.CheckOutDate}");
                    return true;
                }
                else
                {
                    var personCode = db.People.Where(p => p.PersonID == personId).Select(p => p.Code).SingleOrDefault();
                    Console.WriteLine($"No valid booking found for PersonID: {personId}, PersonCode: {personCode}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in IsPersonBookedAndPlaced: {ex.Message}");
                return false;
            }
        }

        public bool SaveReverseInvoice(Invoicing invoicing, List<InvoicingDetail> list)
        {
            uow.CreateTransaction();

            // Performance: reduce EF change tracking overhead during bulk insert/update
            var ctx = uow.Context;
            bool prevAutoDetect = ctx.Configuration.AutoDetectChangesEnabled;
            bool prevValidate = ctx.Configuration.ValidateOnSaveEnabled;
            ctx.Configuration.AutoDetectChangesEnabled = false;
            ctx.Configuration.ValidateOnSaveEnabled = false;

            if (invoicing.InvoiceTypeId == (int)InvoiceTypes.Rental)
            {
                foreach (var detail in list)
                {
                    var updatedDetail = GetReverseInvoicingRevenueDetail1(detail);
                    invoicing.InvoicingDetails.Add(updatedDetail);
                }
            }
            else
            {
                foreach (var detail in list)
                {
                    invoicing.InvoicingDetails.Add(detail);
                }
            }

            uow.GenericRepository<Invoicing>().Insert(invoicing);
            uow.SaveChanges();

            var mainInvoice = uow.GenericRepository<Invoicing>().GetById(invoicing.ParentInvoiceId);
            var originalMinFromDate = uow.GenericRepository<InvoicingDetail>().Table.Where(x => x.InvoicingId == mainInvoice.Id && x.FromDate.HasValue)
                                        .Select(x => x.FromDate.Value).DefaultIfEmpty().Min();

            var reverseInvoices = uow.GenericRepository<Invoicing>().Table.Where(x => x.ParentInvoiceId == mainInvoice.Id)
                                        .Select(x => x.Id).ToList();

            var reverseMinFromDate = uow.GenericRepository<InvoicingDetail>().Table.Where(x => reverseInvoices.Contains(x.InvoicingId) && x.FromDate.HasValue)
                                        .Select(x => x.FromDate.Value).DefaultIfEmpty().Min();

            DateTime? billedUpToDate = null;

            if (reverseMinFromDate != DateTime.MinValue)
            {
                var calculatedDate = reverseMinFromDate.AddDays(-1);

                if (originalMinFromDate != DateTime.MinValue)
                {
                    billedUpToDate = calculatedDate < originalMinFromDate ? (DateTime?)null : calculatedDate;
                }
                else
                {
                    billedUpToDate = calculatedDate;
                }
            }

            mainInvoice.BilledUpToDate = billedUpToDate;
            mainInvoice.Refunded = true;

            uow.GenericRepository<Invoicing>().Update(mainInvoice);
            uow.SaveChanges();

            // Restore EF settings
            ctx.Configuration.AutoDetectChangesEnabled = prevAutoDetect;
            ctx.Configuration.ValidateOnSaveEnabled = prevValidate;

            StudentLedger studentLedger = new StudentLedger()
            {
                StudentId = invoicing.StudentId,
                LocationId = invoicing.LocationId,
                Code = invoicing.Code,
                PaymentTypeName = "Invoice",
                PaymentDate = DateTime.Now,
                InvoiceId = invoicing.Id,
                //DebitAmount = invoicing.NetAmount,
                CreditAmount = Math.Abs(invoicing.NetAmount),


                IsApproved = invoicing.IsApproved,
                ApprovedBy = invoicing.ApprovedBy,
                CreatedDate = DateTime.Now,
                CreatedBy = Common.Globals.User.ID,
            };

            uow.GenericRepository<StudentLedger>().Insert(studentLedger);
            uow.SaveChanges();

            var oldinvoice = new Invoicing();
            // Insert Audit Log
            {
                var difference = Common.Classes.Common.DetailedCompare<EF.Invoicing>(oldinvoice, invoicing);
                List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();

                EF.AuditLog auditLog = new EF.AuditLog()
                {
                    AuditType = (int)Enumeration.AuditType.Create,
                    ActionId = (int)Enumeration.CorrespondenceAction.CreateInvoice,
                    PK = invoicing.Id.ToString(),
                    UserId = Common.Globals.User.ID,
                    TableName = "Invoicing",
                    Reference = invoicing.Code,
                    UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                    PersonId = invoicing.StudentId,
                    AuditLogDetails = difference
                };
                auditLogsService.AddAuditLog(auditLog);
            }

            //Create Voucher for Reverse Invoice

            CreateRevInvVoucher(invoicing, list);


            // Send Notification
            var description = "Your Invoice has been created against Invoice Code: " + invoicing.Code;
            notificationService.SendNotification(null, invoicing.StudentId, "Student", "New Invoice", description, "/Student/Invoicings/InvoicingList", PMS.Common.Globals.User.Email);

            uow.Commit();

            return true;
        }

        public bool SaveInvoice(Invoicing invoicing, List<InvoicingDetail> list)
        {
            if (invoicing.Id == 0)
            {
                uow.CreateTransaction();
                // Performance: temporarily disable expensive EF tracking & validation
                var ctx = uow.Context;
                bool prevAutoDetect = ctx.Configuration.AutoDetectChangesEnabled;
                bool prevValidate = ctx.Configuration.ValidateOnSaveEnabled;
                ctx.Configuration.AutoDetectChangesEnabled = false;
                ctx.Configuration.ValidateOnSaveEnabled = false;
                var invoiceCode = GetMaxInvoiceCodeString(invoicing.LocationId, (int)invoicing.InvoiceTypeId);
                invoicing.Code = invoiceCode;
                invoicing.CreatedDate = DateTime.Now;
                invoicing.CreatedBy = Common.Globals.User.ID;
                invoicing.ApprovedBy = invoicing.IsApproved == true ? Common.Globals.User.ID : invoicing.ApprovedBy;
                invoicing.TermID = invoicing.TermID;
                if (invoicing.InvoiceTypeId == (int)InvoiceTypes.Rental)
                {
                    invoicing.InvoicingDetails = GetInvoicingRevenueDetail(list);
                }
                else if (invoicing.InvoiceTypeId == (int)InvoiceTypes.Deposit)
                {
                    IQueryable<V_GetPersonsforDeposit> query = uow.GenericRepository<V_GetPersonsforDeposit>().Table
                   .Where(p => p.PersonID == invoicing.StudentId);
                    if (!query.Any())
                    {
                        throw new Exception("You are not permitted to create this invoice");
                    }
                    invoicing.InvoicingDetails = list;
                }
                else
                {
                    invoicing.InvoicingDetails = list;
                }
                if (invoicing.InvoiceTypeId == (int)InvoiceTypes.Rental)
                {
                    var term = uow.GenericRepository<EF.Term>().Table
               .FirstOrDefault(t => t.TermID == invoicing.TermID);
                    if (term != null && term.FrequencyId == (int)FrequencyStatusLookup.Monthly)
                    {

                        var existingInvoices = uow.GenericRepository<EF.Invoicing>().Table.Where(x => x.StudentId == invoicing.StudentId && x.Refunded != true).ToList();
                        foreach (var invoice in existingInvoices)
                        {
                            var details = uow.GenericRepository<EF.InvoicingDetail>().Table.Where(x => x.InvoicingId == invoice.Id).ToList();
                            foreach (var item in list)
                            {
                                var res = details.Any(x => (x.FromDate <= item.FromDate && item.FromDate <= x.ToDate) ||
                                                                       (x.FromDate <= item.ToDate && item.ToDate <= x.ToDate) ||
                                                                       (item.FromDate <= x.FromDate && x.ToDate <= item.ToDate));
                                if (res == true)
                                {
                                    throw new Exception("Invoice for this Period is already created");
                                }
                            }
                        }
                    }

                    else
                    {
                        var existingInvoices = uow.GenericRepository<EF.Invoicing>().Table.Where(x => x.StudentId == invoicing.StudentId && x.Refunded != true).ToList();
                        foreach (var invoice in existingInvoices)
                        {
                            var details = uow.GenericRepository<EF.InvoicingDetail>().Table.Where(x => x.InvoicingId == invoice.Id).ToList();
                            foreach (var item in list)
                            {
                                var res = details.Any(x => (x.FromDate <= item.FromDate && item.FromDate < x.ToDate) ||
                                                                       (x.FromDate <= item.ToDate && item.ToDate <= x.ToDate) ||
                                                                       (item.FromDate <= x.FromDate && x.ToDate <= item.ToDate));
                                if (res == true)
                                {
                                    throw new Exception("Invoice for this Period is already created");
                                }
                            }
                        }
                    }

                    var bookingDetails = (from booking in uow.GenericRepository<EF.Booking>().Table
                                          join placement in uow.GenericRepository<EF.BedSpacePlacement>().Table
                                          on booking.BookingID equals placement.BookingID
                                          into placements
                                          from latestPlacement in placements.OrderByDescending(p => p.CreatedDate).Take(1)
                                          where booking.PersonID == invoicing.StudentId && latestPlacement.CheckOut == null
                                          orderby latestPlacement.CreatedDate descending
                                          select new { booking, latestPlacement }).FirstOrDefault();

                    if (bookingDetails != null)
                    {
                        // Check if user is checked in for Muscat location (LocationId = Muscat)
                        if (bookingDetails.booking.LocationID == (int)LocationEnum.Muscat && bookingDetails.latestPlacement.CheckIn == null)
                        {
                            throw new Exception("Student must be checked in before creating an invoice. Please check in the student first.");
                        }

                        if (list.Last().ToDate > bookingDetails.booking.CheckOutDate)
                        {
                            throw new Exception("Billing for this period is beyond the contract date.");
                        }

                        var lastInvoice = uow.GenericRepository<Invoicing>().Table
                        .Where(i => i.StudentId == invoicing.StudentId && i.ParentInvoiceId == null && i.BilledUpToDate != null && i.InvoiceTypeId == (int)InvoiceTypes.Rental)
                            .OrderByDescending(i => i.BilledUpToDate)
                            .FirstOrDefault();


                        if (lastInvoice != null)
                        {
                            // We only care about periods after this date
                            if (invoicing.InvoiceDate >= new DateTime(2024, 03, 26))
                            {
                                var missingPeriodStartDate = lastInvoice.BilledUpToDate.Value.Date.AddDays(1);
                                var currentFromDate = list.First().FromDate;

                                if (!currentFromDate.HasValue) throw new Exception("From Date is required.");

                                var missingPeriodEndDate = currentFromDate.Value.Date.AddDays(-1);
                                var bookingCheckIn = bookingDetails.booking.CheckInDate;
                                var bookingCheckOut = bookingDetails.booking.CheckOutDate;

                                // Only enforce continuity when the gap falls within the current booking dates
                                // (e.g. after a new placement, days between placements are not billable on this booking).
                                if (missingPeriodStartDate >= bookingCheckIn && missingPeriodEndDate <= bookingCheckOut)
                                {
                                    if (currentFromDate.Value.Date < missingPeriodStartDate)
                                    {
                                        throw new Exception("Invalid From Date. It cannot be earlier than `Billed Upto Date`: " + missingPeriodStartDate.ToString("yyyy-MM-dd") + ".");
                                    }

                                    if (missingPeriodStartDate <= missingPeriodEndDate)
                                    {
                                        throw new Exception("Billing for the period between " + missingPeriodStartDate.ToString("yyyy-MM-dd") + " and " + missingPeriodEndDate.ToString("yyyy-MM-dd") + " is missing.");
                                    }
                                }
                            }
                        }
                    }

                    else
                    {
                        throw new Exception("No valid booking details found for this student.");
                    }
                }

                if (invoicing.InvoicingDetails != null && invoicing.InvoicingDetails.Any(x => x.ToDate.HasValue))
                {
                    invoicing.BilledUpToDate = invoicing.InvoicingDetails.Where(x => x.ToDate.HasValue).Max(x => x.ToDate);
                }

                uow.GenericRepository<Invoicing>().Insert(invoicing);
                uow.SaveChanges();

                // Restore defaults before downstream logic which may rely on validation/tracking
                ctx.Configuration.AutoDetectChangesEnabled = prevAutoDetect;
                ctx.Configuration.ValidateOnSaveEnabled = prevValidate;

                //Create Voucher for Invoice

                CreateInvoicingVoucher(invoicing, list);

                StudentLedger studentLedger = new StudentLedger()
                {
                    StudentId = invoicing.StudentId,
                    LocationId = invoicing.LocationId,
                    Code = invoicing.Code,
                    PaymentTypeName = "Invoice",
                    PaymentDate = invoicing.InvoiceDate,
                    InvoiceId = invoicing.Id,
                    DebitAmount = invoicing.NetAmount,
                    IsApproved = invoicing.IsApproved,
                    ApprovedBy = invoicing.IsApproved == true ? Common.Globals.User.ID : invoicing.ApprovedBy,
                    CreatedDate = DateTime.Now,
                    CreatedBy = Common.Globals.User.ID,
                };

                uow.GenericRepository<StudentLedger>().Insert(studentLedger);
                uow.SaveChanges();

                // Audit log creation
                var oldinvoice = new Invoicing();
                var difference = Common.Classes.Common.DetailedCompare<EF.Invoicing>(oldinvoice, invoicing);
                List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();

                EF.AuditLog auditLog = new EF.AuditLog()
                {
                    AuditType = (int)Enumeration.AuditType.Create,
                    ActionId = (int)Enumeration.CorrespondenceAction.CreateInvoice,
                    PK = invoicing.Id.ToString(),
                    UserId = Common.Globals.User.ID,
                    TableName = "Invoicing",
                    Reference = invoicing.Code,
                    UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                    PersonId = invoicing.StudentId,
                    AuditLogDetails = difference
                };
                auditLogsService.AddAuditLog(auditLog);

                // Send Notification

                var Description = "Your Invoice has been created against Invoice Code: " + invoicing.Code;
                notificationService.SendNotification(null, invoicing.StudentId, "Student", "New Invoice", Description, "/Student/Invoicings/InvoicingList", PMS.Common.Globals.User.Email);

                //END notification

                // Excluded: do not auto-allocate/split advance payments to this invoice

                uow.Commit();

                return true;
            }
            else
            {
                uow.CreateTransaction();
                // Performance: temporarily disable expensive EF tracking & validation
                var ctx = uow.Context;
                bool prevAutoDetect = ctx.Configuration.AutoDetectChangesEnabled;
                bool prevValidate = ctx.Configuration.ValidateOnSaveEnabled;
                ctx.Configuration.AutoDetectChangesEnabled = false;
                ctx.Configuration.ValidateOnSaveEnabled = false;

                Invoicing oldinvoice = uow.GenericRepository<Invoicing>().GetByIdAsNoTracking(x => x.Id == invoicing.Id);
                Invoicing invoicing1 = uow.GenericRepository<Invoicing>().GetById(invoicing.Id);
                invoicing1.InvoiceDate = invoicing.InvoiceDate;
                //invoicing1.DueDate = invoicing.DueDate;
                invoicing1.StudentId = invoicing.StudentId;
                invoicing1.Remarks = invoicing.Remarks;
                invoicing1.TaxIds = invoicing.TaxIds;
                invoicing1.TotalPrice = invoicing.TotalPrice;
                invoicing1.TaxAmount = invoicing.TaxAmount;
                invoicing1.NetAmount = invoicing.NetAmount;
                invoicing1.TotalDiscountAmount = invoicing.TotalDiscountAmount;
                invoicing1.TermID = invoicing.TermID;
                invoicing1.IsApproved = invoicing.IsApproved;
                invoicing1.ApprovedBy = invoicing.IsApproved == true ? Common.Globals.User.ID : invoicing.ApprovedBy;

                //var lastInvoice = uow.GenericRepository<Invoicing>().Table
                //        .Where(i => i.StudentId == invoicing1.StudentId && i.ParentInvoiceId == null && i.BilledUpToDate != null && i.InvoiceTypeId == (int)InvoiceTypes.Rental)
                //        .OrderByDescending(i => i.BilledUpToDate)
                //        .FirstOrDefault();

                //if (lastInvoice != null)
                //{
                //    if (invoicing.InvoiceDate >= new DateTime(2024, 03, 26))
                //    {
                //        var expectedFromDate = lastInvoice.BilledUpToDate.Value.Date.AddDays(1); // BilledUpToDate
                //        var currentFromDate = list.First().FromDate; // missingPeriodEndDate

                //        if (!currentFromDate.HasValue) throw new Exception("From Date is required.");

                //        if (currentFromDate.Value != expectedFromDate)
                //        {
                //            throw new Exception("Billing for the period between " + expectedFromDate.ToString("yyyy-MM-dd") + " and " + currentFromDate.Value.ToString("yyyy-MM-dd") + " is missing.");
                //        }
                //    }
                //}


                var oldinvoicedetails = oldinvoice.InvoicingDetails.Select(x => new
                {
                    x.Id,
                    x.ServiceId,
                    x.InvoicingId,
                    x.Description,
                    x.Price,
                    x.ServiceName,
                    x.TaxesIds,
                    x.TaxesName,
                    x.TotalAmount,
                    x.DiscountPercentage,
                    x.DiscountAmount,
                }).ToList();
                var oldobj = new JavaScriptSerializer().Serialize(oldinvoicedetails);
                var newobj = new JavaScriptSerializer().Serialize(list);

                //Insert Audit Log
                {
                    var difference = Common.Classes.Common.DetailedCompare<EF.Invoicing>(oldinvoice, invoicing1);
                    List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();

                    EF.AuditLog auditLog = new EF.AuditLog()
                    {
                        OldValue = oldobj,
                        NewValue = newobj,
                        AuditType = (int)Enumeration.AuditType.Update,
                        ActionId = (int)Enumeration.CorrespondenceAction.UpdateInvoice,
                        PK = invoicing1.Id.ToString(),
                        UserId = Common.Globals.User.ID,
                        TableName = "Invoicing",
                        Reference = invoicing1.Code,
                        UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                        PersonId = invoicing.StudentId,
                        AuditLogDetails = difference
                    };
                    auditLogsService.AddAuditLog(auditLog);
                }

                var invoiceref = uow.GenericRepository<StudentLedger>().Table.Where(x => x.InvoiceId == invoicing1.Id && x.PaymentTypeName == "Invoice").FirstOrDefault();
                if (invoiceref != null)
                {
                    uow.GenericRepository<StudentLedger>().Delete(invoiceref);
                }

                StudentLedger studentLedger = new StudentLedger()
                {
                    StudentId = invoicing1.StudentId,
                    LocationId = invoicing1.LocationId,
                    Code = invoicing1.Code,
                    PaymentTypeName = "Invoice",
                    PaymentDate = invoicing.InvoiceDate,
                    InvoiceId = invoicing1.Id,
                    DebitAmount = invoicing1.NetAmount,
                    IsApproved = invoicing.IsApproved,
                    ApprovedBy = invoicing.IsApproved == true ? Common.Globals.User.ID : invoicing.ApprovedBy,
                    CreatedDate = DateTime.Now,
                    CreatedBy = Common.Globals.User.ID,
                };

                uow.GenericRepository<StudentLedger>().Insert(studentLedger);
                var previousdetails = uow.GenericRepository<InvoicingDetail>().Table.Where(x => x.InvoicingId == invoicing.Id).ToList();
                foreach (var item in previousdetails)
                {
                    var calenderList = item.InvoicingRevenueDetails.ToList();
                    foreach (var item2 in calenderList)
                    {
                        uow.GenericRepository<InvoicingRevenueDetail>().Delete(item2);
                    }
                    uow.GenericRepository<InvoicingDetail>().Delete(item);
                }

                // Re-enable EF change tracking/validation before assigning new child collections so EF can detect new entities
                ctx.Configuration.AutoDetectChangesEnabled = prevAutoDetect;
                ctx.Configuration.ValidateOnSaveEnabled = prevValidate;

                if (invoicing.InvoiceTypeId == (int)InvoiceTypes.Rental)
                    invoicing1.InvoicingDetails = GetInvoicingRevenueDetail(list);
                else
                    invoicing1.InvoicingDetails = list;

                if (invoicing1.InvoicingDetails != null && invoicing1.InvoicingDetails.Any(x => x.ToDate.HasValue))
                {
                    invoicing1.BilledUpToDate = invoicing1.InvoicingDetails.Where(x => x.ToDate.HasValue).Max(x => x.ToDate);
                }

                uow.GenericRepository<Invoicing>().Update(invoicing1);
                uow.SaveChanges();

                // Defaults already restored prior to assigning children

                // Update voucher and voucher details for this invoice
                var existingVoucher = uow.GenericRepository<Voucher>().Table.FirstOrDefault(v => v.ReferenceId == invoicing.Id);
                if (existingVoucher != null)
                {
                    UpdateInvoiceVoucher(invoicing, list, auditLogsService);
                }

                else
                {
                    CreateInvoicingVoucher(invoicing, list);
                }

                uow.Commit();

                return true;
            }
        }

        public void ProcessInvoiceAdvancePayments(int invoiceId, decimal invoiceAmount, int studentId, int locationId)
        {
            if (locationId != (int)LocationEnum.Dubai) return;

            decimal remainingInvoiceAmount = invoiceAmount;
            int createdBy = Common.Globals.User.ID;
            DateTime createdDate = DateTime.Now;

            // Fetch all advance payments (payments without InvoiceId) for the student (oldest first)
            var advancePayments = uow.GenericRepository<StudentLedger>().Table
                .Where(x => x.StudentId == studentId
                           && x.InvoiceId == null
                           && x.CreditAmount > 0
                           && x.PaymentTypeName != "Invoice"
                           )
                .OrderBy(x => x.PaymentDate)
                .ThenBy(x => x.CreatedDate)
                .ToList();

            foreach (var advancePayment in advancePayments)
            {
                if (remainingInvoiceAmount <= 0) break;

                decimal originalAdvanceAmount = advancePayment.CreditAmount ?? 0;
                decimal amountToApply = Math.Min(remainingInvoiceAmount, originalAdvanceAmount);

                if (amountToApply <= 0) continue;

                remainingInvoiceAmount -= amountToApply;

                // Declare remainingAdvancePayment variable outside the scope
                StudentLedger remainingAdvancePayment = null;

                // Handle advance payment based on allocation type
                if (amountToApply == originalAdvanceAmount)
                {
                    // Full allocation - attach invoice to original advance payment
                    advancePayment.InvoiceId = invoiceId;
                    advancePayment.Remarks = $"Fully allocated to invoice. Original amount: {originalAdvanceAmount:F2}, Applied: {amountToApply:F2}";
                    uow.GenericRepository<StudentLedger>().Update(advancePayment);
                }
                else
                {
                    // Partial allocation - split the advance payment
                    decimal remainingAdvanceAmount = originalAdvanceAmount - amountToApply;

                    // Update original advance payment (this goes to invoice)
                    advancePayment.InvoiceId = invoiceId;
                    advancePayment.CreditAmount = amountToApply;
                    advancePayment.Remarks = $"Partial allocation to invoice. Original amount: {originalAdvanceAmount:F2}, Applied: {amountToApply:F2}, Remaining: {remainingAdvanceAmount:F2}";
                    uow.GenericRepository<StudentLedger>().Update(advancePayment);

                    // Create new advance payment entry for remaining amount (NO InvoiceId)
                    remainingAdvancePayment = new StudentLedger
                    {
                        PaymentDate = advancePayment.PaymentDate,
                        Code = advancePayment.Code,
                        InvoiceId = null, // This is crucial - remaining amount stays as advance
                        StudentId = studentId,
                        DebitAmount = 0,
                        CreditAmount = remainingAdvanceAmount,
                        Remarks = $"Remaining advance after partial allocation. Original amount: {originalAdvanceAmount:F2}, Applied to invoice: {amountToApply:F2}, Remaining: {remainingAdvanceAmount:F2}",
                        PaymentTypeId = advancePayment.PaymentTypeId,
                        PaymentTypeName = advancePayment.PaymentTypeName,
                        IsApproved = advancePayment.IsApproved,
                        CreatedBy = createdBy,
                        CreatedDate = createdDate,
                        LocationId = locationId,
                        ApprovedBy = advancePayment.ApprovedBy,
                        PaymentReferenceNumber = advancePayment.PaymentReferenceNumber
                    };
                    uow.GenericRepository<StudentLedger>().Insert(remainingAdvancePayment);
                }

                // Create audit log for invoice allocation (original payment update)
                var originalUpdateAuditLog = new EF.AuditLog()
                {
                    AuditType = (int)Enumeration.AuditType.Update,
                    ActionId = (int)Enumeration.CorrespondenceAction.UpdatePayment,
                    PK = advancePayment.Id.ToString(),
                    UserId = createdBy,
                    TableName = "StudentLedger - Advance Payment",
                    Reference = advancePayment.Code,
                    UserName = "System - Auto Allocation",
                    PersonId = studentId,
                    NewValue = amountToApply == originalAdvanceAmount
                        ? $"Fully allocated to invoice {invoiceId}. Amount: {amountToApply:N2}"
                        : $"Partially allocated to invoice {invoiceId}. Original: {originalAdvanceAmount:N2}, Applied: {amountToApply:N2}",
                    TimeStamp = DateTime.Now
                };
                auditLogsService.AddAuditLog(originalUpdateAuditLog);

                // Create audit log for the remaining advance payment (only if partial allocation)
                if (amountToApply != originalAdvanceAmount && remainingAdvancePayment != null)
                {
                    var remainingAuditLog = new EF.AuditLog()
                    {
                        AuditType = (int)Enumeration.AuditType.Create,
                        ActionId = (int)Enumeration.CorrespondenceAction.CreatePayment,
                        PK = remainingAdvancePayment.Id.ToString(),
                        UserId = createdBy,
                        TableName = "StudentLedger - Remaining Advance",
                        Reference = remainingAdvancePayment.Code,
                        UserName = "System - Auto Split",
                        PersonId = studentId,
                        NewValue = $"Payment split from {advancePayment.Code}. Original: {originalAdvanceAmount:N2}, Applied: {amountToApply:N2}, Remaining: {(originalAdvanceAmount - amountToApply):N2}",
                        TimeStamp = DateTime.Now
                    };
                    auditLogsService.AddAuditLog(remainingAuditLog);
                }
            }

            // After processing all advance payments, check if invoice is fully paid
            var invoice = uow.GenericRepository<Invoicing>().GetById(invoiceId);

            // Calculate total amount applied to this invoice
            decimal totalAppliedAmount = invoiceAmount - remainingInvoiceAmount;

            // Mark invoice as paid if fully covered by advance payments
            if (remainingInvoiceAmount <= 0)
            {
                invoice.IsPaid = true;
                uow.GenericRepository<Invoicing>().Update(invoice);

                // Create audit log for invoice payment completion
                var invoicePaidAuditLog = new EF.AuditLog()
                {
                    AuditType = (int)Enumeration.AuditType.Update,
                    ActionId = (int)Enumeration.CorrespondenceAction.UpdateInvoice,
                    PK = invoiceId.ToString(),
                    UserId = createdBy,
                    TableName = "Invoicing - Auto Payment",
                    Reference = invoice.Code,
                    UserName = "System - Auto Payment",
                    PersonId = studentId,
                    NewValue = $"Invoice fully paid through auto-allocation. Invoice Amount: {invoiceAmount:N2}, Applied from Advances: {totalAppliedAmount:N2}",
                    TimeStamp = DateTime.Now
                };
                auditLogsService.AddAuditLog(invoicePaidAuditLog);
            }
            else
            {
                // Create audit log for partial payment
                var partialPaymentAuditLog = new EF.AuditLog()
                {
                    AuditType = (int)Enumeration.AuditType.Update,
                    ActionId = (int)Enumeration.CorrespondenceAction.UpdateInvoice,
                    PK = invoiceId.ToString(),
                    UserId = createdBy,
                    TableName = "Invoicing - Partial Payment",
                    Reference = invoice.Code,
                    UserName = "System - Auto Payment",
                    PersonId = studentId,
                    NewValue = $"Invoice partially paid through auto-allocation. Invoice Amount: {invoiceAmount:N2}, Applied: {totalAppliedAmount:N2}, Remaining: {remainingInvoiceAmount:N2}",
                    TimeStamp = DateTime.Now
                };
                auditLogsService.AddAuditLog(partialPaymentAuditLog);
            }

            // Save all changes
            uow.SaveChanges();
        }



        public bool Approve(int id)
        {
            Invoicing oldinvoice = uow.GenericRepository<Invoicing>().GetByIdAsNoTracking(x => x.Id == id);
            Invoicing invoicing = uow.GenericRepository<Invoicing>().GetById(id);
            if (invoicing == null)
            {
                return false;
            }
            invoicing.IsApproved = true;
            invoicing.ApprovedBy = Common.Globals.User.ID;
            var studentledger = uow.GenericRepository<StudentLedger>().Table.Where(x => x.InvoiceId == id && x.PaymentTypeName == "Invoice").FirstOrDefault();
            if (studentledger != null)
            {
                studentledger.IsApproved = true;
            }
            uow.GenericRepository<StudentLedger>().Update(studentledger);
            uow.GenericRepository<Invoicing>().Update(invoicing);
            uow.SaveChanges();

            //Insert Audit Log
            {
                var difference = Common.Classes.Common.DetailedCompare<EF.Invoicing>(oldinvoice, invoicing);
                List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();

                EF.AuditLog auditLog = new EF.AuditLog()
                {
                    AuditType = (int)Enumeration.AuditType.Update,
                    ActionId = (int)Enumeration.CorrespondenceAction.UpdateInvoice,
                    PK = invoicing.Id.ToString(),
                    UserId = Common.Globals.User.ID,
                    TableName = "Invoicing - Approve",
                    Reference = invoicing.Code,
                    UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                    PersonId = invoicing.StudentId,
                    AuditLogDetails = difference
                };
                auditLogsService.AddAuditLog(auditLog);
            }
            return true;
        }

        //public bool CloneInvoice(DepositInvoicesVM depositInvoicesVM, string source = "deposit")
        //{
        //    var res = uow.GenericRepository<EF.Invoicing>().Table.Where(x => x.Id == depositInvoicesVM.InvoiceId).FirstOrDefault();
        //    res.Refunded = true;
        //    uow.GenericRepository<EF.Invoicing>().Update(res);
        //    uow.SaveChanges();
        //    try
        //    {
        //        var invoicing = new EF.Invoicing()
        //        {
        //            InvoiceDate = DateTime.Now,
        //            //DueDate = DateTime.Now,
        //            Code = "REF-" + depositInvoicesVM.InvoiceCode,
        //            StudentId = depositInvoicesVM.personId,
        //            Remarks = depositInvoicesVM.Remarks,
        //            TotalPrice = depositInvoicesVM.InvoicePrice * -1,
        //            TaxAmount = depositInvoicesVM.TaxAmount * -1,
        //            NetAmount = depositInvoicesVM.NetAmount * -1,
        //            TaxIds = depositInvoicesVM.TaxIds,
        //            IsApproved = true,
        //            CreatedDate = DateTime.Now,
        //            CreatedBy = PMS.Common.Globals.User.ID,
        //            LocationId = depositInvoicesVM.LocationId,
        //            // If source is partnerledger, mark the refund invoice as paid immediately
        //            IsPaid = (source == "partnerledger"),
        //            InvoiceTypeId = (int)Enumeration.InvoiceTypes.Refund,
        //            Refunded = false,
        //            ParentInvoiceId = depositInvoicesVM.InvoiceId,
        //            TermID = depositInvoicesVM.TermID,
        //            TotalDiscountAmount = depositInvoicesVM.TotalDiscountAmount * -1 ?? 0.0m
        //        };
        //        uow.GenericRepository<Invoicing>().Insert(invoicing);
        //        uow.SaveChanges();
        //        StudentLedger studentLedger = new StudentLedger()
        //        {
        //            StudentId = invoicing.StudentId,
        //            LocationId = invoicing.LocationId,
        //            Code = invoicing.Code,
        //            PaymentTypeName = "Invoice",
        //            PaymentDate = DateTime.Now,
        //            InvoiceId = invoicing.Id,
        //            Remarks = invoicing.Remarks,
        //            //DebitAmount = invoicing.NetAmount,
        //            CreditAmount = Math.Abs(invoicing.NetAmount),

        //            IsApproved = invoicing.IsApproved,
        //            ApprovedBy = invoicing.IsApproved == true ? Common.Globals.User.ID : invoicing.ApprovedBy,
        //            CreatedDate = DateTime.Now,
        //            CreatedBy = Common.Globals.User.ID,
        //        };
        //        uow.GenericRepository<StudentLedger>().Insert(studentLedger);
        //        uow.SaveChanges();
        //        var originalDetails = uow.GenericRepository<EF.InvoicingDetail>().Table.Where(x => x.InvoicingId == depositInvoicesVM.InvoiceId).ToList();
        //        var newDetails = new List<EF.InvoicingDetail>();

        //        foreach (var item in originalDetails)
        //        {
        //            var details = new EF.InvoicingDetail()
        //            {
        //                InvoicingId = invoicing.Id,
        //                ServiceId = item.ServiceId,
        //                ServiceName = item.ServiceName,
        //                Price = item.Price * -1,
        //                Description = item.Description,
        //                TaxesIds = item.TaxesIds,
        //                TaxesName = item.TaxesName,
        //                TaxAmount = item.TaxAmount * -1,
        //                TotalAmount = item.TotalAmount * -1,
        //                FromDate = item.FromDate,
        //                ToDate = item.ToDate,
        //                DiscountPercentage = item.DiscountPercentage * -1,
        //                DiscountAmount = item.DiscountAmount * -1
        //            };
        //            uow.GenericRepository<EF.InvoicingDetail>().Insert(details);
        //            newDetails.Add(details);
        //        }
        //        uow.SaveChanges();

        //        var oldinvoice = new Invoicing();
        //        //Insert Audit Log
        //        {
        //            var difference = Common.Classes.Common.DetailedCompare<EF.Invoicing>(oldinvoice, invoicing);

        //            List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();

        //            EF.AuditLog auditLog = new EF.AuditLog()
        //            {
        //                AuditType = (int)Enumeration.AuditType.Create,
        //                ActionId = (int)Enumeration.CorrespondenceAction.CreateInvoice,
        //                PK = invoicing.Id.ToString(),
        //                UserId = Common.Globals.User.ID,
        //                TableName = "Invoicing",
        //                Reference = invoicing.Code,
        //                UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
        //                PersonId = invoicing.StudentId,
        //                AuditLogDetails = difference
        //            };
        //            auditLogsService.AddAuditLog(auditLog);
        //        }

        //        CreateRevInvVoucher(invoicing, newDetails);

        //        // Send Notification

        //        var Description = "Your Invoice has been created against Invoice Code: " + invoicing.Code;
        //        notificationService.SendNotification(null, invoicing.StudentId, "Student", "New Invoice", Description, "/Student/Invoicings/InvoicingList", PMS.Common.Globals.User.Email);

        //        //END notification

        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        return false;
        //    }
        //}

        public bool CloneInvoice(DepositInvoicesVM depositInvoicesVM, string source = "deposit")
        {
            using (var transaction = uow.Context.Database.BeginTransaction())
            {
                try
                {
                    // Lock the original invoice row and check if it's already refunded
                    var res = uow.GenericRepository<EF.Invoicing>().Table.Where(x => x.Id == depositInvoicesVM.InvoiceId && (x.Refunded == null || x.Refunded == false)).FirstOrDefault();

                    if (res == null)
                    {
                        return false;
                    }

                    // Check if a refund invoice already exists for this invoice
                    var existingRefund = uow.GenericRepository<EF.Invoicing>().Table.Where(x => x.ParentInvoiceId == depositInvoicesVM.InvoiceId && x.InvoiceTypeId == (int)Enumeration.InvoiceTypes.Refund).FirstOrDefault();

                    if (existingRefund != null)
                    {
                        // Refund already exists, return false
                        return false;
                    }

                    // Mark original invoice as refunded
                    res.Refunded = true;
                    uow.GenericRepository<EF.Invoicing>().Update(res);

                    uow.SaveChanges();

                    // Create the refund invoice
                    var invoicing = new EF.Invoicing()
                    {
                        InvoiceDate = DateTime.Now,
                        //DueDate = DateTime.Now,
                        Code = "REF-" + depositInvoicesVM.InvoiceCode,
                        StudentId = depositInvoicesVM.personId,
                        Remarks = depositInvoicesVM.Remarks,
                        TotalPrice = depositInvoicesVM.InvoicePrice * -1,
                        TaxAmount = depositInvoicesVM.TaxAmount * -1,
                        NetAmount = depositInvoicesVM.NetAmount * -1,
                        TaxIds = depositInvoicesVM.TaxIds,
                        IsApproved = true,
                        CreatedDate = DateTime.Now,
                        CreatedBy = PMS.Common.Globals.User.ID,
                        LocationId = depositInvoicesVM.LocationId,
                        // If source is partnerledger, mark the refund invoice as paid immediately
                        IsPaid = (source == "partnerledger"),
                        InvoiceTypeId = (int)Enumeration.InvoiceTypes.Refund,
                        Refunded = false,
                        ParentInvoiceId = depositInvoicesVM.InvoiceId,
                        TermID = depositInvoicesVM.TermID,
                        TotalDiscountAmount = depositInvoicesVM.TotalDiscountAmount * -1 ?? 0.0m
                    };

                    uow.GenericRepository<Invoicing>().Insert(invoicing);
                    uow.SaveChanges();

                    // Create student ledger entry
                    StudentLedger studentLedger = new StudentLedger()
                    {
                        StudentId = invoicing.StudentId,
                        LocationId = invoicing.LocationId,
                        Code = invoicing.Code,
                        PaymentTypeName = "Invoice",
                        PaymentDate = DateTime.Now,
                        InvoiceId = invoicing.Id,
                        Remarks = invoicing.Remarks,
                        DebitAmount = null,
                        CreditAmount = Math.Abs(invoicing.NetAmount),
                        IsApproved = invoicing.IsApproved,
                        ApprovedBy = invoicing.IsApproved == true ? Common.Globals.User.ID : invoicing.ApprovedBy,
                        CreatedDate = DateTime.Now,
                        CreatedBy = Common.Globals.User.ID,
                    };

                    uow.GenericRepository<StudentLedger>().Insert(studentLedger);
                    uow.SaveChanges();

                    // Clone invoice details
                    var originalDetails = uow.GenericRepository<EF.InvoicingDetail>().Table.Where(x => x.InvoicingId == depositInvoicesVM.InvoiceId).ToList();

                    var newDetails = new List<EF.InvoicingDetail>();

                    foreach (var item in originalDetails)
                    {
                        var details = new EF.InvoicingDetail()
                        {
                            InvoicingId = invoicing.Id,
                            ServiceId = item.ServiceId,
                            ServiceName = item.ServiceName,
                            Price = item.Price * -1,
                            Description = item.Description,
                            TaxesIds = item.TaxesIds,
                            TaxesName = item.TaxesName,
                            TaxAmount = item.TaxAmount * -1,
                            TotalAmount = item.TotalAmount * -1,
                            FromDate = item.FromDate,
                            ToDate = item.ToDate,
                            DiscountPercentage = item.DiscountPercentage * -1,
                            DiscountAmount = item.DiscountAmount * -1
                        };
                        uow.GenericRepository<EF.InvoicingDetail>().Insert(details);
                        newDetails.Add(details);
                    }
                    uow.SaveChanges();

                    // Insert Audit Log
                    var oldinvoice = new Invoicing();
                    var difference = Common.Classes.Common.DetailedCompare<EF.Invoicing>(oldinvoice, invoicing);

                    EF.AuditLog auditLog = new EF.AuditLog()
                    {
                        AuditType = (int)Enumeration.AuditType.Create,
                        ActionId = (int)Enumeration.CorrespondenceAction.CreateInvoice,
                        PK = invoicing.Id.ToString(),
                        UserId = Common.Globals.User.ID,
                        TableName = "Invoicing",
                        Reference = invoicing.Code,
                        UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                        PersonId = invoicing.StudentId,
                        AuditLogDetails = difference
                    };
                    auditLogsService.AddAuditLog(auditLog);

                    CreateRevInvVoucher(invoicing, newDetails);

                    // Send Notification
                    var Description = "Your Invoice has been created against Invoice Code: " + invoicing.Code;
                    notificationService.SendNotification(null, invoicing.StudentId, "Student", "New Invoice", Description, "/Student/Invoicings/InvoicingList", PMS.Common.Globals.User.Email);

                    // Commit the transaction
                    transaction.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    // Rollback the transaction on error
                    transaction.Rollback();
                    // Log the exception here if you have logging
                    ErrorLogger.WriteToTestingLog("$ERROR:", ex.Message);
                    return false;
                }
            }
        }

        public bool SaveCloneInvoicePayment(DepositInvoicesVM depositInvoicesVM)
        {
            var paymentname = uow.GenericRepository<EF.PaymentType>().Table.Where(x => x.PaymentId == depositInvoicesVM.PaymentTypeId).Select(x => x.PayementName).FirstOrDefault();
            var codeTMM = depositInvoicesVM.InvoiceCode.Split('-').Take(4).Last();
            var codeNO = depositInvoicesVM.InvoiceCode.Split('-').Take(5).Last();
            try
            {
                var paymentCode = "RCT-REF-" + codeTMM + '-' + codeNO;
                var now = DateTime.Now;
                var duplicateWindowStart = now.AddMinutes(-2);

                // Guard against duplicate submits (double-click/retry) without DB unique constraints.
                var normalizedRemarks = (depositInvoicesVM.Remarks ?? string.Empty).Trim();
                var hasRecentDuplicate = uow.GenericRepository<EF.StudentLedger>().Table.Any(x =>
                    x.LookupId == (int)PaymentLookup.PaymentRefund &&
                    x.InvoiceId == depositInvoicesVM.InvoiceId &&
                    x.Code == paymentCode &&
                    x.PaymentTypeId == depositInvoicesVM.PaymentTypeId &&
                    (x.DebitAmount ?? 0) == (depositInvoicesVM.DebitAmount ?? 0) &&
                    (x.Remarks ?? string.Empty).Trim() == normalizedRemarks &&
                    x.CreatedDate >= duplicateWindowStart);

                if (hasRecentDuplicate)
                {
                    throw new InvalidOperationException("Duplicate refund payment request detected. Please refresh and verify payment history before retrying.");
                }

                var payment = new EF.StudentLedger()
                {
                    Code = paymentCode,
                    PaymentDate = now,
                    InvoiceId = depositInvoicesVM.InvoiceId,
                    StudentId = depositInvoicesVM.personId,
                    DebitAmount = depositInvoicesVM.DebitAmount.HasValue ? Math.Abs(depositInvoicesVM.DebitAmount.Value) : 0,
                    CreditAmount = null,
                    Remarks = depositInvoicesVM.Remarks,
                    PaymentTypeId = depositInvoicesVM.PaymentTypeId,
                    CreditNoteId = depositInvoicesVM.CreditNoteId,
                    PaymentTypeName = paymentname,
                    IsApproved = true,
                    CreatedBy = PMS.Common.Globals.User.ID,
                    CreatedDate = now,
                    LocationId = depositInvoicesVM.LocationId,
                    ApprovedBy = PMS.Common.Globals.User.ID,
                    LookupId = (int)PaymentLookup.PaymentRefund

                };
                uow.GenericRepository<EF.StudentLedger>().Insert(payment);
                uow.SaveChanges();
                {
                    var Invoice = uow.GenericRepository<Invoicing>().GetById(depositInvoicesVM.InvoiceId);
                    var TotalPaidAmount = uow.GenericRepository<EF.StudentLedger>().Table.Where(x => x.InvoiceId == depositInvoicesVM.InvoiceId).ToList();
                    var remainingAmount = TotalPaidAmount.Sum(x => x.DebitAmount) - TotalPaidAmount.Sum(x => x.CreditAmount);

                    if (remainingAmount == 0 || depositInvoicesVM.IsPaid == true)
                    {
                        Invoice.IsPaid = true;
                        uow.GenericRepository<Invoicing>().Update(Invoice);
                        uow.SaveChanges();
                    }
                }
                //Create Audit Log
                {
                    EF.AuditLog auditLog = new EF.AuditLog()
                    {
                        AuditType = (int)Enumeration.AuditType.Create,
                        ActionId = (int)Enumeration.CorrespondenceAction.CreatePayment,
                        PK = payment.Id.ToString(),
                        UserId = Common.Globals.User.ID,
                        TableName = "StudentLedger - Payments",
                        Reference = payment.Code,
                        UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                        PersonId = payment.StudentId,
                    };
                    auditLogsService.AddAuditLog(auditLog);
                }
                //Ref Payment Voucher
                paymentService.CreateRevPayVoucher(payment);
                // Send Notification

                var Description = "Your Payment: " + payment.CreditAmount + " has been paid against Invoice: " + payment.Code;
                notificationService.SendNotification(null, payment.StudentId, "Student", "New Payment", Description, "/Student/Payment/PaymentList", PMS.Common.Globals.User.Email);
                //END notification
                return true;
            }
            catch (Exception ex)
            {

                System.Diagnostics.Debug.WriteLine($"SaveCloneInvoicePayment Error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                return false;
            }
        }

        #endregion

        #region FEE ASSESSMENT
        public (bool Success, string SuccessMessage, string ErrorMessage) GenerateInvoices(int[] personIds, DateTime startDate, DateTime endDate)
        {
            // Initialize result collections with estimated capacity
            var personsNotBookedAndPlaced = new List<string>(personIds.Length);
            var personsAlreadyExists = new List<string>(personIds.Length);
            var personsInvoiceGenerated = new List<string>(personIds.Length);
            var personsWithExpiredCheckOutDate = new List<string>(personIds.Length);
            var personsWithMissingPeriod = new List<string>(personIds.Length);
            var personsWithoutCode = new List<string>(personIds.Length);

            // Local cache to avoid repeated tax lookups during a fee assessment batch
            var taxCache = new Dictionary<int, AddTaxVM>();

            AddTaxVM GetTaxFromCache(int taxId)
            {
                if (taxId <= 0)
                    return null;

                if (taxCache.TryGetValue(taxId, out var cached))
                    return cached;

                var tax = taxService.GetTaxById(taxId);
                taxCache[taxId] = tax;
                return tax;
            }

            try
            {
                // Pre-fetch all person codes in one query
                var personCodes = personService.GetPersons()
                    .Where(p => personIds.Contains(p.PersonID))
                    .ToDictionary(p => p.PersonID, p => p.Code);

                foreach (var personId in personIds)
                {
                    // Validate person code first
                    if (!personCodes.TryGetValue(personId, out string personCode) || string.IsNullOrWhiteSpace(personCode))
                    {
                        personsWithoutCode.Add($"PersonID-{personId}");
                        continue;
                    }

                    // Check booking status
                    if (!IsPersonBookedAndPlaced(personId, startDate, endDate))
                    {
                        personsNotBookedAndPlaced.Add(personCode);
                        continue;
                    }

                    var invoiceCheckResult = GetLastMultipleInvoicesStatusWithInvoice(personId, startDate, endDate);
                    if (invoiceCheckResult.Status == "DateConflicted")
                    {
                        // Requested batch overlaps already billed period; do not generate invoice.
                        personsAlreadyExists.Add(personCode);
                        continue;
                    }

                    // Handle existing invoice case
                    if (invoiceCheckResult.Status == "Invoiced")
                    {
                        var lastInvoiceDetails = GetInvoiceDetailsByInvoiceId(invoiceCheckResult.LastInvoiceId);

                        if (lastInvoiceDetails == null || !lastInvoiceDetails.Any())
                        {
                            personsAlreadyExists.Add(personCode);
                            continue;
                        }

                        // Get all occupancies for the person within the date range
                        var studentOccupancies = GetStudentOccupanciesForPerson(personId, startDate, endDate);

                        if (studentOccupancies == null || !studentOccupancies.Any())
                        {
                            personsNotBookedAndPlaced.Add(personCode);
                            continue;
                        }

                        // Block only when move-out is before the selected assessment period
                        if (HasExpiredCheckOutForAssessment(studentOccupancies, startDate))
                        {
                            personsWithExpiredCheckOutDate.Add(personCode);
                            continue;
                        }

                        // Check for missing period (scoped to current placement/booking window)
                        var bookingCheckIn = studentOccupancies.Min(o => o.CheckInDate);
                        var bookingCheckOut = studentOccupancies.Max(o => o.CheckOutDate);
                        var (hasGap, gapStart, gapEnd, isBeforeBatch) = HasMissingBillingPeriod(
                            invoiceCheckResult.LastInvoiceId,
                            lastInvoiceDetails,
                            startDate,
                            endDate,
                            bookingCheckIn,
                            bookingCheckOut);
                        var newInvoicingDetails = new List<InvoicingDetail>();

                        if (hasGap)
                        {
                            if (isBeforeBatch)
                            {
                                personsWithMissingPeriod.Add(personCode);
                                continue; // Skip to next person, don't create invoice for gap before batch
                            }
                            // For each occupancy, create invoice details for the gap period
                            foreach (var occupancy in studentOccupancies)
                            {
                                DateTime effectiveFromDate = gapStart.Value;
                                DateTime effectiveToDate;
                                if (occupancy.CheckOutDate.HasValue && occupancy.CheckOutDate.Value <= gapEnd.Value)
                                {
                                    // Cap to one day prior to move-out when move-out is before the gap end
                                    effectiveToDate = occupancy.CheckOutDate.Value.AddDays(-1);
                                }
                                else
                                {
                                    effectiveToDate = gapEnd.Value;
                                }
                                if (effectiveFromDate <= effectiveToDate)
                                {
                                    var tax = GetTaxFromCache(occupancy.TaxId ?? 0);
                                    decimal servicePrice = CalculateServicePrice(
                                        occupancy.ServicePrice,
                                        effectiveFromDate,
                                        effectiveToDate,
                                        occupancy.FrequencyId);

                                    decimal taxAmount = CalculateTaxAmount(servicePrice, tax);

                                    newInvoicingDetails.Add(new InvoicingDetail
                                    {
                                        ServiceId = occupancy.ServiceId,
                                        ServiceName = occupancy.ServiceName,
                                        Price = servicePrice,
                                        Description = occupancy.Name,
                                        TaxesIds = tax?.TaxId.ToString(),
                                        TaxesName = tax?.TaxName,
                                        TaxAmount = taxAmount,
                                        TotalAmount = servicePrice + taxAmount,
                                        FromDate = effectiveFromDate,
                                        ToDate = effectiveToDate
                                    });
                                }
                            }

                            if (newInvoicingDetails.Any())
                            {
                                CreateAndSaveInvoice(newInvoicingDetails, personId, null, startDate, endDate, studentOccupancies.First());
                                personsInvoiceGenerated.Add(personCode);
                            }
                            else
                            {
                                personsAlreadyExists.Add(personCode);
                            }
                        }
                        else
                        {
                            // No gap, proceed with existing logic
                            var serviceIds = lastInvoiceDetails.Select(d => d.ServiceId).Distinct().ToArray();
                            // Use batch loading instead of individual queries
                            var servicesDict = servicesService.GetServicesByIds(serviceIds);
                            var services = serviceIds.Select(id => servicesDict.ContainsKey(id) ? servicesDict[id] : null).Where(s => s != null).ToList();

                            // Batch get all occupancies
                            var occupancies = serviceIds
                                .Select(id => GetStudentOccupancy(id, personId, 1))
                                .ToList();

                            // Block only when move-out is before the selected assessment period
                            if (occupancies.Any(o => IsCheckOutBeforeAssessmentPeriod(o.CheckOutDate, startDate)))
                            {
                                personsWithExpiredCheckOutDate.Add(personCode);
                                continue;
                            }

                            // Determine earliest checkout date
                            var earliestCheckout = occupancies
                                .Where(o => o.CheckOutDate.HasValue)
                                .Min(o => o.CheckOutDate.Value);
                            var invoiceEndDate = earliestCheckout <= endDate ? earliestCheckout.AddDays(-1) : endDate;

                            var newInvoicingDetailsList = new List<InvoicingDetail>();
                            foreach (var service in services)
                            {
                                var occupancy = occupancies.First(o => o.ServiceId == service.serviceId);

                                // Skip if the computed end date falls before the start date
                                if (startDate > invoiceEndDate)
                                {
                                    continue;
                                }

                                var tax = GetTaxFromCache(occupancy.TaxId ?? 0);

                                decimal servicePrice = CalculateServicePrice(
                                    occupancy.ServicePrice,
                                    startDate,
                                    invoiceEndDate,
                                    occupancy.FrequencyId);

                                decimal taxAmount = CalculateTaxAmount(servicePrice, tax);

                                newInvoicingDetailsList.Add(new InvoicingDetail
                                {
                                    ServiceId = service.serviceId,
                                    ServiceName = service.ServiceName,
                                    Price = servicePrice,
                                    Description = occupancy.Name,
                                    TaxesIds = tax?.TaxId.ToString(),
                                    TaxesName = tax?.TaxName,
                                    TaxAmount = taxAmount,
                                    TotalAmount = servicePrice + taxAmount,
                                    FromDate = startDate,
                                    ToDate = invoiceEndDate
                                });
                            }

                            if (newInvoicingDetailsList.Any())
                            {
                                CreateAndSaveInvoice(newInvoicingDetailsList, personId, services[0], startDate, invoiceEndDate);
                                personsInvoiceGenerated.Add(personCode);
                            }
                        }
                    }
                    // Handle new invoice case
                    else
                    {
                        var studentOccupancies = GetStudentOccupanciesForPerson(personId, startDate, endDate);

                        if (studentOccupancies == null || !studentOccupancies.Any())
                        {
                            personsNotBookedAndPlaced.Add(personCode);
                            continue;
                        }

                        // Block only when move-out is before the selected assessment period
                        if (HasExpiredCheckOutForAssessment(studentOccupancies, startDate))
                        {
                            personsWithExpiredCheckOutDate.Add(personCode);
                            continue;
                        }

                        var newInvoicingDetails = new List<InvoicingDetail>();
                        foreach (var occupancy in studentOccupancies)
                        {
                            DateTime actualStartDate = GetEffectiveDate(occupancy.CheckInDate, startDate);
                            DateTime actualEndDate = GetEffectiveEndDate(occupancy.CheckOutDate, endDate);

                            // Skip if the computed end date falls before the start date
                            if (actualStartDate > actualEndDate)
                            {
                                continue;
                            }

                            var tax = GetTaxFromCache(occupancy.TaxId ?? 0);
                            decimal servicePrice = CalculateServicePrice(
                                occupancy.ServicePrice,
                                actualStartDate,
                                actualEndDate,
                                occupancy.FrequencyId);

                            decimal taxAmount = CalculateTaxAmount(servicePrice, tax);

                            newInvoicingDetails.Add(new InvoicingDetail
                            {
                                ServiceId = occupancy.ServiceId,
                                ServiceName = occupancy.ServiceName,
                                Price = servicePrice,
                                Description = occupancy.Name,
                                TaxesIds = tax?.TaxId.ToString(),
                                TaxesName = tax?.TaxName,
                                TaxAmount = taxAmount,
                                TotalAmount = servicePrice + taxAmount,
                                FromDate = actualStartDate,
                                ToDate = actualEndDate
                            });
                        }

                        if (newInvoicingDetails.Any())
                        {
                            CreateAndSaveInvoice(newInvoicingDetails, personId, null, startDate, endDate, studentOccupancies.First());
                            personsInvoiceGenerated.Add(personCode);
                        }
                    }
                }

                // Build results
                var resultMessages = BuildResultMessages(
                    personsInvoiceGenerated,
                    personsNotBookedAndPlaced,
                    personsWithExpiredCheckOutDate,
                    personsWithMissingPeriod,
                    personsAlreadyExists,
                    personsWithoutCode);

                return (true, resultMessages.SuccessMessage, resultMessages.ErrorMessage);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generating invoices: {ex.Message}");
                return (false, "", "An error occurred while generating invoices");
            }
        }

        // Helper Methods
        private decimal CalculateTaxAmount(decimal price, AddTaxVM tax)
        {
            return tax != null ? Math.Round(price * (tax.TaxPercentage / 100m), 2) : 0m;
        }

        private bool IsCheckOutBeforeAssessmentPeriod(DateTime? checkOutDate, DateTime assessmentStartDate)
        {
            return checkOutDate.HasValue && checkOutDate.Value.Date < assessmentStartDate.Date;
        }

        private bool HasExpiredCheckOutForAssessment(IEnumerable<OutputInvoicingVM> occupancies, DateTime assessmentStartDate)
        {
            return occupancies.Any(o => IsCheckOutBeforeAssessmentPeriod(o.CheckOutDate, assessmentStartDate));
        }

        private DateTime GetEffectiveDate(DateTime? candidate, DateTime defaultDate)
        {
            return (candidate.HasValue && candidate > defaultDate) ? candidate.Value : defaultDate;
        }

        private DateTime GetEffectiveEndDate(DateTime? candidate, DateTime defaultDate)
        {
            // If move-out (candidate) is on or before the selected end date, cap to one day prior to move-out
            if (candidate.HasValue && candidate.Value <= defaultDate)
            {
                return candidate.Value.AddDays(-1);
            }
            return defaultDate;
        }

        private (string SuccessMessage, string ErrorMessage) BuildResultMessages(
            List<string> generated, List<string> notBooked, List<string> expired,
            List<string> missingPeriod, List<string> exists, List<string> noCode)
        {
            var successMessage = new StringBuilder();
            if (generated.Count > 0)
            {
                successMessage.Append($"<strong>Invoices Generated Successfully ({generated.Count} persons):</strong><br/>")
                              .Append(FormatPersonList(generated));
            }

            var errorMessage = new StringBuilder();
            AppendErrorSection(errorMessage, "No Valid Booking/Placement", notBooked);
            AppendErrorSection(errorMessage, "Expired Check-Out Date", expired);
            AppendErrorSection(errorMessage, "Missing Billing Periods", missingPeriod);
            AppendErrorSection(errorMessage, "Already billed for the selected date range", exists);
            AppendErrorSection(errorMessage, "Invalid Person Codes", noCode);

            return (successMessage.ToString(), errorMessage.ToString().TrimEnd("<br/><br/>".ToCharArray()));
        }

        private void AppendErrorSection(StringBuilder sb, string title, List<string> items)
        {
            if (items.Count > 0)
            {
                sb.Append($"<strong>{title} ({items.Count} persons):</strong><br/>")
                  .AppendLine(FormatPersonList(items) + "<br/><br/>");
            }
        }

        private string FormatPersonList(List<string> persons, int maxDisplay = 50)
        {
            if (persons.Count == 0) return "";
            return persons.Count <= maxDisplay
                ? string.Join(", ", persons)
                : string.Join(", ", persons.Take(maxDisplay)) + $" and {persons.Count - maxDisplay} more";
        }

        public List<OutputInvoicingVM> GetStudentOccupanciesForPerson(int residentId, DateTime startDate, DateTime endDate)
        {
            var db = uow.Context;
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();

            var occupanciesList = new List<OutputInvoicingVM>();

            var data = (from booking in db.Bookings
                        join pl in db.BedSpacePlacements
                        on booking.BookingID equals pl.BookingID
                        into plc
                        from placement in plc.DefaultIfEmpty()

                        join pr in db.PriceConfigs
                        on booking.PriceConfigID equals pr.PriceConfigID
                        into pri
                        from priceConfig in pri.DefaultIfEmpty()

                        join t in db.Terms
                        on priceConfig.TermID equals t.TermID
                        into tr
                        from term in tr.DefaultIfEmpty()

                        where booking.IsEnable == true && booking.IsCancel != true && booking.PersonID == residentId
            && placement != null && placement.IsEnable == true && placement.CheckIn != null
            && placement.MoveIn <= endDate
            && (placement.MoveOut == null || placement.MoveOut >= startDate)
                        //&& booking.CheckInDate <= endDate
                        //&& (booking.CheckOutDate == null || booking.CheckOutDate >= startDate)
                        select new
                        {
                            occupancyName = term.TermName + " - " + term.TermDescription,
                            occupancyId = priceConfig.PriceConfigID,
                            price = priceConfig.Price,
                            //checkInDate = booking.CheckInDate,
                            //checkOutDate = booking.CheckOutDate,
                            checkInDate = placement.MoveIn,
                            checkOutDate = (DateTime?)placement.MoveOut,
                            termID = term.TermID,
                            frequencyID = term.FrequencyId,
                            locationId = booking.LocationID
                        })
   .OrderByDescending(x => x.checkOutDate ?? DateTime.MaxValue)
   .ThenByDescending(x => x.checkInDate)
   .Take(1)
   .ToList();

            // Get the services we need
            var serviceRental = uow.GenericRepository<EF.Service>().Table.Where(s => s.ServiceName == "Rental Charges" && assignedLocationIds.Contains((int)s.LocationId)).FirstOrDefault();
            var serviceCleaning = uow.GenericRepository<EF.Service>().Table.Where(s => s.ServiceName == "Cleaning fee" && assignedLocationIds.Contains((int)s.LocationId)).FirstOrDefault();

            if (serviceRental == null)
            {
                throw new Exception("Rental Charges service is not configured!");
            }

            if (serviceCleaning == null)
            {
                throw new Exception("Cleaning Charges service is not configured!");
            }

            foreach (var occupancyData in data)
            {
                var occupancy = occupancyData.occupancyName;
                var occupancyId = occupancyData.occupancyId;
                var price = occupancyData.price;
                var checkInDate = occupancyData.checkInDate;
                var checkOutDate = occupancyData.checkOutDate;
                var termID = occupancyData.occupancyId;
                var frequencyID = occupancyData.frequencyID ?? 2;
                var locationID = occupancyData.locationId ?? 0;

                bool isDailyRate = occupancy.Contains("Daily Rate");

                // Create entry for Rental Charges
                decimal rentalServicePrice = 0;
                int rentalTaxId = 0;

                if (serviceRental.TypeId == (int)ServiceTypes.RentalCharges)
                {
                    rentalServicePrice = price;
                    rentalTaxId = serviceRental.TaxId ?? 0;
                }
                else
                    rentalServicePrice = serviceRental.ServiceAmount;

                occupanciesList.Add(new OutputInvoicingVM
                {
                    Name = occupancy,
                    ServiceTypeId = serviceRental.TypeId,
                    Occupancy = occupancy,
                    ServicePrice = rentalServicePrice,
                    OccupancyId = occupancyId,
                    CheckOutDate = checkOutDate,
                    CheckInDate = checkInDate,
                    TaxId = rentalTaxId,
                    TermId = termID,
                    FrequencyId = frequencyID,
                    LocationID = locationID,
                    ServiceId = serviceRental.ServiceId,
                    ServiceName = serviceRental.ServiceName
                });

                // Create entry for Cleaning Charges
                decimal cleaningServicePrice = 0;
                int cleaningTaxId = 0;

                if (serviceCleaning.TypeId == (int)ServiceTypes.RentalCharges && serviceCleaning.AccountId == (int)COA.Cleaning)
                {
                    if (isDailyRate)
                    {
                        var cleaningServiceAmount = 95.14m;
                        cleaningServicePrice = cleaningServiceAmount / 30.0m; // Convert monthly fee to daily rate
                    }
                    else
                    {
                        cleaningServicePrice = serviceCleaning.ServiceAmount;
                    }
                    cleaningTaxId = serviceCleaning.TaxId ?? 0;
                }
                else
                {
                    cleaningServicePrice = serviceCleaning.ServiceAmount;
                    cleaningTaxId = serviceCleaning.TaxId ?? 0;
                }

                occupanciesList.Add(new OutputInvoicingVM
                {
                    Name = occupancy,
                    ServiceTypeId = serviceCleaning.TypeId,
                    Occupancy = occupancy,
                    ServicePrice = cleaningServicePrice,
                    OccupancyId = occupancyId,
                    CheckOutDate = checkOutDate,
                    CheckInDate = checkInDate,
                    TaxId = cleaningTaxId,
                    TermId = termID,
                    FrequencyId = frequencyID,
                    LocationID = locationID,
                    ServiceId = serviceCleaning.ServiceId,
                    ServiceName = serviceCleaning.ServiceName
                });
            }

            return occupanciesList;
        }

        private (bool HasGap, DateTime? GapStart, DateTime? GapEnd, bool IsBeforeBatch) HasMissingBillingPeriod(
            int lastInvoiceId,
            List<InvoicingDetail> lastInvoiceDetails,
            DateTime startDate,
            DateTime endDate,
            DateTime? bookingCheckIn = null,
            DateTime? bookingCheckOut = null)
        {
            // Only check for periods after March 26, 2024
            if (DateTime.Today < new DateTime(2024, 03, 26))
                return (false, null, null, false);

            if (lastInvoiceDetails == null || !lastInvoiceDetails.Any())
                return (false, null, null, false);

            // Use invoice-level BilledUpToDate so partial reversals are respected.
            DateTime? lastBilledUpToDate = null;
            if (lastInvoiceId > 0)
            {
                lastBilledUpToDate = uow.GenericRepository<Invoicing>()
                    .Table.AsNoTracking()
                    .Where(i => i.Id == lastInvoiceId)
                    .Select(i => i.BilledUpToDate)
                    .FirstOrDefault();
            }

            // Fallback for legacy records where BilledUpToDate is not set.
            if (!lastBilledUpToDate.HasValue)
            {
                lastBilledUpToDate = lastInvoiceDetails
                    .OrderByDescending(d => d.ToDate)
                    .FirstOrDefault()
                    ?.ToDate;
            }

            if (!lastBilledUpToDate.HasValue)
                return (false, null, null, false);

            // Calculate potential gap start (day after billed-up-to boundary)
            var gapStart = lastBilledUpToDate.Value.Date.AddDays(1);

            // Days between placements are not billable on the current booking.
            if (bookingCheckIn.HasValue)
            {
                var checkIn = bookingCheckIn.Value.Date;
                if (gapStart < checkIn)
                    gapStart = checkIn;
            }

            if (bookingCheckOut.HasValue)
            {
                var checkOut = bookingCheckOut.Value.Date;
                if (gapStart > checkOut)
                    return (false, null, null, false);
            }

            // Check if gap is before batch starts
            if (gapStart < startDate)
            {
                var preBatchGapEnd = startDate.AddDays(-1);
                if (bookingCheckOut.HasValue && preBatchGapEnd > bookingCheckOut.Value.Date)
                    preBatchGapEnd = bookingCheckOut.Value.Date;

                if (bookingCheckIn.HasValue && preBatchGapEnd < bookingCheckIn.Value.Date)
                    return (false, null, null, false);

                if (gapStart <= preBatchGapEnd)
                    return (true, gapStart, preBatchGapEnd, true);

                return (false, null, null, false);
            }

            // Check for gap within batch range
            var gapEnd = endDate;
            if (bookingCheckOut.HasValue && gapEnd > bookingCheckOut.Value.Date)
                gapEnd = bookingCheckOut.Value.Date;

            if (gapStart <= gapEnd && gapStart > startDate)
                return (true, gapStart, gapEnd, false);

            return (false, null, null, false);
        }
        // Calculates the service price based on frequency and date range
        private decimal CalculateServicePrice(decimal basePrice, DateTime startDate, DateTime endDate, int frequencyId)
        {
            // Get the total days (inclusive)
            int totalDays = (endDate - startDate).Days + 1;

            // Price calculation based on frequency
            switch (frequencyId)
            {
                case (int)FrequencyStatusLookup.Daily:
                    //daily frequency
                    return Math.Round(basePrice * totalDays, 2);

                case (int)FrequencyStatusLookup.Weekly:
                    //weekly frequency, calculate the number of weeks (rounded up) and multiply
                    decimal weeks = Math.Ceiling(totalDays / 7.0m);
                    return Math.Round(basePrice * weeks, 2);

                case (int)FrequencyStatusLookup.Monthly:
                    // For monthly frequency, handle based on actual month lengths
                    return CalculateMonthlyRate(basePrice, startDate, endDate);

                default:
                    // If frequency is not recognized, fall back to daily rate
                    return Math.Round(basePrice * totalDays, 2);
            }
        }

        // Calculates the monthly rate based on actual days in each month
        private decimal CalculateMonthlyRate(decimal baseMonthlyPrice, DateTime startDate, DateTime endDate)
        {
            // If start and end dates are in the same month of the same year
            if (startDate.Year == endDate.Year && startDate.Month == endDate.Month)
            {
                // Check if it's a full month
                var daysInMonth = DateTime.DaysInMonth(startDate.Year, startDate.Month);

                // If start day is 1 and end day is the last day of the month
                if (startDate.Day == 1 && endDate.Day == daysInMonth)
                {
                    // Full month, use the full price
                    return baseMonthlyPrice;
                }
                else
                {
                    // Partial month, calculate proportion based on actual days in this month
                    var proportion = (decimal)(endDate.Day - startDate.Day + 1) / daysInMonth;
                    return Math.Round(baseMonthlyPrice * proportion, 2);
                }
            }

            // If dates span multiple months
            else
            {
                decimal totalPrice = 0;
                var currentDate = startDate;

                // Handle the first partial month
                var daysInFirstMonth = DateTime.DaysInMonth(startDate.Year, startDate.Month);
                var daysInFirstMonthPeriod = daysInFirstMonth - startDate.Day + 1;
                var firstMonthProportion = (decimal)daysInFirstMonthPeriod / daysInFirstMonth;
                totalPrice += baseMonthlyPrice * firstMonthProportion;

                // Move to the first day of next month
                currentDate = new DateTime(startDate.Year, startDate.Month, 1).AddMonths(1);

                // Handle full months in between
                while (currentDate.Year < endDate.Year ||
                      (currentDate.Year == endDate.Year && currentDate.Month < endDate.Month))
                {
                    totalPrice += baseMonthlyPrice;
                    currentDate = currentDate.AddMonths(1);
                }

                if (currentDate.Year == endDate.Year && currentDate.Month == endDate.Month && endDate.Day < DateTime.DaysInMonth(endDate.Year, endDate.Month))
                {
                    var daysInLastMonth = DateTime.DaysInMonth(endDate.Year, endDate.Month);
                    var lastMonthProportion = (decimal)endDate.Day / daysInLastMonth;
                    totalPrice += baseMonthlyPrice * lastMonthProportion;
                }

                // If the end date is the last day of its month, add a full month
                else if (currentDate.Year == endDate.Year && currentDate.Month == endDate.Month)
                {
                    totalPrice += baseMonthlyPrice;
                }

                return Math.Round(totalPrice, 2);
            }
        }

        // Helper method to create and save invoice
        private void CreateAndSaveInvoice(List<InvoicingDetail> invoicingDetails, int personId, dynamic firstService, DateTime invoiceStartDate, DateTime invoiceEndDate, dynamic firstOccupancy = null)
        {
            decimal totalPrice = invoicingDetails.Sum(d => d.Price);
            decimal totalTaxAmount = invoicingDetails.Sum(d => d.TaxAmount ?? 0m);
            decimal netAmount = invoicingDetails.Sum(d => d.TotalAmount ?? 0m);

            var occupancyForDetails = firstOccupancy ?? GetStudentOccupancy(firstService.serviceId, personId, 1);

            string singleTaxId = invoicingDetails
                .Where(d => !string.IsNullOrEmpty(d.TaxesIds))
                .Select(d => d.TaxesIds)
                .FirstOrDefault();

            var inv = new Invoicing()
            {
                Code = "",
                CreatedDate = DateTime.Now,
                InvoiceDate = DateTime.Today,
                DueDate = DateTime.Today,
                CreatedBy = Common.Globals.User.ID,
                TermID = occupancyForDetails.OccupancyId,
                StudentId = personId,
                Remarks = "Auto Generated Invoice",
                TotalPrice = totalPrice,
                NetAmount = netAmount,
                TaxAmount = totalTaxAmount,
                TaxIds = singleTaxId,
                IsApproved = true,
                LocationId = occupancyForDetails?.LocationID ?? 0,
                ApprovedBy = Common.Globals.User.ID,
                InvoiceTypeId = 1,
                TotalDiscountAmount = 0,
            };

            SaveMultipleInvoices(inv, invoicingDetails);
        }
        public bool SaveMultipleInvoices(Invoicing invoicing, List<InvoicingDetail> list)
        {
            uow.CreateTransaction();
            var invoiceCode = GetMaxInvoiceCodeString(invoicing.LocationId, (int)invoicing.InvoiceTypeId);
            invoicing.Code = invoiceCode;

            if (invoicing.InvoiceTypeId == (int)InvoiceTypes.Rental)
            {
                foreach (var detail in list)
                {
                    var revenueDetail = GetInvoicingRevenueDetailForFeeAssessment(detail);
                    invoicing.InvoicingDetails.Add(revenueDetail);
                }
            }
            else
            {
                foreach (var detail in list)
                {
                    invoicing.InvoicingDetails.Add(detail);
                }
            }

            if (invoicing.InvoicingDetails != null && invoicing.InvoicingDetails.Any(x => x.ToDate.HasValue))
            {
                invoicing.BilledUpToDate = invoicing.InvoicingDetails.Where(x => x.ToDate.HasValue).Max(x => x.ToDate);
            }

            uow.GenericRepository<Invoicing>().Insert(invoicing);
            uow.SaveChanges();

            //Voucher Creation
            CreateInvoicingVoucher(invoicing, list);

            StudentLedger studentLedger = new StudentLedger()
            {
                StudentId = invoicing.StudentId,
                LocationId = invoicing.LocationId,
                Code = invoicing.Code,
                PaymentTypeName = "Invoice",
                PaymentDate = DateTime.Now,
                InvoiceId = invoicing.Id,
                DebitAmount = invoicing.NetAmount,
                IsApproved = invoicing.IsApproved,
                ApprovedBy = invoicing.ApprovedBy,
                CreatedDate = DateTime.Now,
                CreatedBy = Common.Globals.User.ID,
            };

            uow.GenericRepository<StudentLedger>().Insert(studentLedger);
            uow.SaveChanges();

            var oldinvoice = new Invoicing();
            //Insert Audit Log
            {
                var difference = Common.Classes.Common.DetailedCompare<EF.Invoicing>(oldinvoice, invoicing);
                List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();

                EF.AuditLog auditLog = new EF.AuditLog()
                {
                    AuditType = (int)Enumeration.AuditType.Create,
                    ActionId = (int)Enumeration.CorrespondenceAction.CreateInvoice,
                    PK = invoicing.Id.ToString(),
                    UserId = Common.Globals.User.ID,
                    TableName = "Invoicing",
                    Reference = invoicing.Code,
                    UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                    PersonId = invoicing.StudentId,
                    AuditLogDetails = difference
                };
                auditLogsService.AddAuditLog(auditLog);
            }

            // Send Notification
            var Description = "Your Invoice has been created against Invoice Code: " + invoicing.Code;
            notificationService.SendNotification(null, invoicing.StudentId, "Student", "New Invoice", Description, "/Student/Invoicings/InvoicingList", PMS.Common.Globals.User.Email);

            uow.Commit();
            return true;
        }

        #endregion

        #region invoicing Vouchers
        public void CreateInvoicingVoucher(Invoicing invoicing, List<InvoicingDetail> list)
        {
            var request = new VoucherCreationRequest
            {
                VoucherType = VoucherType.Invoice,
                BaseVoucherData = new BaseVoucherData
                {
                    VoucherDate = invoicing.InvoiceDate,
                    ReferenceId = invoicing.Id,
                    StudentId = invoicing.StudentId,
                    Remarks = invoicing.Remarks,
                    CreatedDate = invoicing.CreatedDate,
                    CreatedBy = invoicing.CreatedBy,
                    LocationId = invoicing.LocationId,
                    TransactionType = "Invoice"
                },
                InvoicingData = invoicing,
                InvoicingDetails = list,
                IsRefund = false
            };

            voucherService.CreateVoucherWithDetails(request, auditLogsService);
        }

        public void UpdateInvoiceVoucher(Invoicing invoicing, List<InvoicingDetail> list, IAuditLogsService auditLogsService)
        {
            var existingVoucher = uow.GenericRepository<Voucher>().Table.FirstOrDefault(v => v.ReferenceId == invoicing.Id);

            if ((invoicing.Refunded == false || invoicing.Refunded == null) && invoicing.NetAmount >= 0)
            {
                if (existingVoucher != null)
                {
                    // Create request object for the update method
                    var updateRequest = new VoucherCreationRequest
                    {
                        VoucherType = VoucherType.Invoice,
                        BaseVoucherData = new BaseVoucherData
                        {
                            VoucherDate = invoicing.InvoiceDate,
                            StudentId = invoicing.StudentId,
                            Remarks = invoicing.Remarks,
                            UpdatedBy = Common.Globals.User.ID,
                            LocationId = invoicing.LocationId,
                            TransactionType = "Invoice"
                        },
                        InvoicingData = invoicing,
                        InvoicingDetails = list
                    };

                    // Use the new update method
                    voucherService.UpdateVoucherWithDetails(existingVoucher.VoucherId, updateRequest, auditLogsService);
                }
            }
            else
            {
                if (existingVoucher != null)
                {
                    var updateRequest = new VoucherCreationRequest
                    {
                        VoucherType = VoucherType.RevOrRefInvoice,
                        BaseVoucherData = new BaseVoucherData
                        {
                            VoucherDate = invoicing.InvoiceDate,
                            ReferenceId = invoicing.Id,
                            StudentId = invoicing.StudentId,
                            Remarks = invoicing.Remarks,
                            CreatedDate = invoicing.CreatedDate,
                            CreatedBy = invoicing.CreatedBy,
                            LocationId = invoicing.LocationId,
                            TransactionType = "Invoice"
                        },
                        InvoicingData = invoicing,
                        InvoicingDetails = list,
                        IsRefund = true
                    };

                    // Use the new update method
                    voucherService.UpdateVoucherWithDetails(existingVoucher.VoucherId, updateRequest, auditLogsService);
                }
            }
        }
        //Create Voucher for Reverse/Refund Invoice
        public void CreateRevInvVoucher(Invoicing invoicing, List<InvoicingDetail> list)
        {
            var request = new VoucherCreationRequest
            {
                VoucherType = VoucherType.RevOrRefInvoice,
                BaseVoucherData = new BaseVoucherData
                {
                    VoucherDate = invoicing.InvoiceDate,
                    ReferenceId = invoicing.Id,
                    StudentId = invoicing.StudentId,
                    Remarks = invoicing.Remarks,
                    CreatedDate = invoicing.CreatedDate,
                    CreatedBy = invoicing.CreatedBy,
                    LocationId = invoicing.LocationId,
                    TransactionType = "Invoice"
                },
                InvoicingData = invoicing,
                InvoicingDetails = list,
                IsRefund = true
            };

            voucherService.CreateVoucherWithDetails(request, auditLogsService);
        }

        #endregion

        #region API'S

        //Services for api
        public ApiResponse<List<OutputInvoicingVM>> GetInvoicesById(int Id)
        {
            var response = new ApiResponse<List<OutputInvoicingVM>>();
            try
            {
                var studentId = uow.GenericRepository<UserMaster>().Table.Where(x => x.ID == Id).Select(x => x.PersonID)
                .FirstOrDefault();

                var data = GetActiveInvoicesByPerson(studentId ?? 0);
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

        //Services for api
        public ApiResponse<OutputInvoicingVM> GetUnpaidInvoice(int Id)
        {
            var response = new ApiResponse<OutputInvoicingVM>();
            try
            {
                var data = uow.GenericRepository<Invoicing>().Table.Where(x => x.Id == Id && x.IsPaid != true && x.IsApproved == true).Select(x => new OutputInvoicingVM
                {
                    Id = x.Id,
                    Code = x.Code,
                    NetAmount = x.NetAmount
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

        #endregion
        public (bool Success, string Message) ProcessRefundedInvoicePayment(DepositInvoicesVM depositInvoicesVM)
        {
            // Get assigned location ids for payment type lookup
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();

            bool paymentSaved = false;
            int? creditNoteId = null;
            try
            {
                // Detect full credit note scenario (from view or logic)
                bool isFullCreditNote = false;
                if ((depositInvoicesVM.IsCreditNote && depositInvoicesVM.CreditNoteAmount > 0 && (depositInvoicesVM.DebitAmount == 0 || depositInvoicesVM.DebitAmount == null)))
                {
                    isFullCreditNote = true;
                }

                // Scenario 3: Full Credit Note (no payment, only credit note, payment entry with 0 and CRD-01)
                if (isFullCreditNote)
                {
                    // 1. Create credit note for full amount
                    bool creditNoteSaved = SaveCreditNote(depositInvoicesVM);
                    if (!creditNoteSaved)
                    {
                        return (false, "Credit note creation failed!");
                    }
                    // 2. Get the just-created credit note id from the service
                    creditNoteId = creditNoteService.GetLatestCreatedCreditNoteId(depositInvoicesVM.personId, depositInvoicesVM.LocationId, depositInvoicesVM.CreditNoteAmount, (int)Enumeration.Status.Approved);

                    // 3. Set up payment entry with 0 amount, CRD-01 payment type, and credit note id
                    depositInvoicesVM.DebitAmount = 0;
                    var paymentType = uow.GenericRepository<PaymentType>().Table.Where(x => x.KeyCode == "CRD-01" && assignedLocationIds.Contains((int)x.LocationId)).FirstOrDefault();
                    if (paymentType != null)
                    {
                        depositInvoicesVM.PaymentTypeId = paymentType.PaymentId;
                        depositInvoicesVM.PaymentTypeName = paymentType.PayementName;
                    }
                    depositInvoicesVM.IsCreditNote = true;
                    depositInvoicesVM.CreditNoteAmount = depositInvoicesVM.NetAmount;
                    // Attach credit note id if possible
                    if (creditNoteId.HasValue)
                        depositInvoicesVM.CreditNoteId = creditNoteId.Value;
                    paymentSaved = SaveCloneInvoicePayment(depositInvoicesVM);
                    if (paymentSaved)
                        return (true, "Full credit note and payment entry generated successfully.");
                    else
                        return (false, "Payment entry for full credit note failed!");
                }

                // Scenario 2: Partial payment + partial credit note
                if (depositInvoicesVM.IsCreditNote && depositInvoicesVM.CreditNoteAmount > 0 && depositInvoicesVM.DebitAmount > 0)
                {
                    // 1. Create credit note for the specified amount
                    bool creditNoteSaved = SaveCreditNote(depositInvoicesVM);
                    if (!creditNoteSaved)
                    {
                        return (false, "Credit note creation failed!");
                    }
                    // 2. Get the just-created credit note id from the service
                    creditNoteId = creditNoteService.GetLatestCreatedCreditNoteId(depositInvoicesVM.personId, depositInvoicesVM.LocationId, depositInvoicesVM.CreditNoteAmount, (int)Enumeration.Status.Approved);
                    // 3. Attach credit note id to payment
                    if (creditNoteId.HasValue)
                        depositInvoicesVM.CreditNoteId = creditNoteId.Value;
                    paymentSaved = SaveCloneInvoicePayment(depositInvoicesVM);
                    if (paymentSaved && creditNoteSaved)
                        return (true, "Payment and credit note generated successfully.");
                    else if (paymentSaved)
                        return (false, "Payment processed but credit note creation failed!");
                    else
                        return (false, "Payment not updated. Please try again later.");
                }

                // Scenario 1: Full payment only (no credit note)
                paymentSaved = SaveCloneInvoicePayment(depositInvoicesVM);
                if (paymentSaved)
                    return (true, "Payment generated successfully.");
                else
                    return (false, "Payment not updated. Please try again later.");
            }
            catch (InvalidOperationException ex)
            {
                return (false, ex.Message);
            }
            catch (Exception ex)
            {
                return (false, "An error occurred: " + ex.Message);
            }
        }

        private bool SaveCreditNote(DepositInvoicesVM vm)
        {
            var creditNoteVm = new StudentCreditNoteVm
            {
                LocationId = vm.LocationId,
                TypeId = (int)CrdNoteTypeLookup.Refund,
                Code = creditNoteService.GetCode(vm.LocationId, (int)CrdNoteTypeLookup.Refund),
                StudentId = vm.personId,
                Amount = vm.CreditNoteAmount,
                Reason = vm.CreditNoteRemarks,
                Status = (int)Enumeration.Status.Approved,
                IsUtilized = false,
                PaymentTypeId = null,
                CreatedDate = DateTime.Now,
                CreatedById = PMS.Common.Globals.User.ID
            };
            return creditNoteService.Add(creditNoteVm);
        }

        #region credit card process of deposit invoice

        public void CreateDepositInvoiceAndPayment(BookingVM bookingVM, int personId)
        {
            var ctx = uow.Context;
            bool prevAutoDetect = ctx.Configuration.AutoDetectChangesEnabled;
            bool prevValidate = ctx.Configuration.ValidateOnSaveEnabled;

            try
            {
                ctx.Configuration.AutoDetectChangesEnabled = false;
                ctx.Configuration.ValidateOnSaveEnabled = false;

                int onlineBookingUserId = GetOnlineBookingUserId();


                // Find deposit service
                var depositService = GetDepositService(bookingVM);
                if (depositService == null)
                {
                    throw new Exception($"No deposit service found for amount {bookingVM.Amount} at location {bookingVM.LocationID}");
                }

                // Calculate tax
                var taxInfo = CalculateTax(depositService, bookingVM.Amount);
                decimal netAmount = bookingVM.Amount + taxInfo.TaxAmount;

                // Create invoice
                var depositInvoice = CreateDepositInvoice(bookingVM, personId, taxInfo, netAmount, onlineBookingUserId);

                // Create invoice detail
                var invoicingDetail = CreateInvoiceDetail(depositInvoice, depositService, bookingVM, taxInfo, netAmount);

                // Create ledger entries
                CreateInvoiceLedger(depositInvoice, personId, netAmount, bookingVM, onlineBookingUserId);

                // Create auditlog entries
                CreateAuditLogForOnlineInvoice(depositInvoice);

                // Create Invoice vouchers
                CreateOnlineInvoicingVoucher(depositInvoice, new List<EF.InvoicingDetail> { invoicingDetail }, onlineBookingUserId);

                // Create payment ledger
                var paymentLedger = paymentService.CreateDepositPaymentLedger(
                    depositInvoice.Id, personId, netAmount,
                    bookingVM.LocationID ?? 0, bookingVM.PaymentMethodID, bookingVM.TranRef, onlineBookingUserId);

                uow.GenericRepository<EF.StudentLedger>().Insert(paymentLedger);
                uow.SaveChanges();

                // Mark invoice as paid
                depositInvoice.IsPaid = true;
                uow.GenericRepository<EF.Invoicing>().Update(depositInvoice);
                uow.SaveChanges();

                // Create auditlog entries
                paymentService.CreateAuditLogForOnlinePayment(paymentLedger);

                // Create Payment vouchers
                paymentService.CreateOnlinePayVoucher(paymentLedger, onlineBookingUserId);


            }
            finally
            {
                ctx.Configuration.AutoDetectChangesEnabled = prevAutoDetect;
                ctx.Configuration.ValidateOnSaveEnabled = prevValidate;
            }
        }

        private int GetOnlineBookingUserId()
        {
            // Get email from web.config
            string onlineBookingUserEmail = System.Configuration.ConfigurationManager.AppSettings["OnlineBookingUserEmail"];

            if (string.IsNullOrEmpty(onlineBookingUserEmail))
            {
                throw new Exception("OnlineBookingUserEmail not configured in web.config AppSettings");
            }

            var onlineBookingUser = uow.GenericRepository<EF.UserMaster>().Table
                .FirstOrDefault(u => u.Email == onlineBookingUserEmail);

            if (onlineBookingUser == null)
            {
                throw new Exception($"Online booking user not found with email: {onlineBookingUserEmail}");
            }

            return onlineBookingUser.ID;
        }

        private EF.Service GetDepositService(BookingVM bookingVM)
        {
            return uow.GenericRepository<EF.Service>().Table
                .Where(s => s.LocationId == bookingVM.LocationID &&
                           s.ServiceAmount == bookingVM.Amount &&
                           s.ServiceName.Contains("Deposit") &&
                           s.IsActive == true && s.IsEnable == true)
                .FirstOrDefault();
        }

        private (decimal TaxAmount, int? TaxId, string TaxName) CalculateTax(EF.Service service, decimal amount)
        {
            decimal taxAmount = 0;
            int? taxId = null;
            string taxName = null;

            if (service.TaxId.HasValue)
            {
                var tax = uow.GenericRepository<EF.Tax>().Table
                    .Where(t => t.TaxId == service.TaxId && t.IsActive == true && t.IsEnable == true)
                    .FirstOrDefault();

                if (tax != null)
                {
                    taxId = tax.TaxId;
                    taxName = tax.TaxName;
                    taxAmount = (amount * tax.TaxPercentage) / 100;
                }
            }

            return (taxAmount, taxId, taxName);
        }

        private EF.Invoicing CreateDepositInvoice(BookingVM bookingVM, int personId,
            (decimal TaxAmount, int? TaxId, string TaxName) taxInfo, decimal netAmount, int createdByUserId)
        {
            var invoiceCode = GetMaxInvoiceCodeString(bookingVM.LocationID ?? 0, (int)Enumeration.InvoiceTypes.Deposit);

            var depositInvoice = new EF.Invoicing
            {
                InvoiceDate = DateTime.Now,
                Code = invoiceCode,
                StudentId = personId,
                Remarks = "Online MEMO invoice",
                TotalPrice = bookingVM.Amount,
                TaxAmount = taxInfo.TaxAmount,
                NetAmount = netAmount,
                TaxIds = taxInfo.TaxId?.ToString(),
                IsApproved = true,
                CreatedDate = DateTime.Now,
                CreatedBy = createdByUserId,
                LocationId = bookingVM.LocationID ?? 0,
                ApprovedBy = createdByUserId,
                InvoiceTypeId = (int)Enumeration.InvoiceTypes.Deposit,
                TermID = bookingVM.RoomTypePriceID,
                TotalDiscountAmount = 0.00m
            };

            uow.GenericRepository<EF.Invoicing>().Insert(depositInvoice);
            uow.SaveChanges();

            return depositInvoice;
        }

        private EF.InvoicingDetail CreateInvoiceDetail(EF.Invoicing invoice, EF.Service service, BookingVM bookingVM,
            (decimal TaxAmount, int? TaxId, string TaxName) taxInfo, decimal netAmount)
        {
            string termDescription = GetTermDescription(bookingVM.RoomTypePriceID);

            var invoicingDetail = new EF.InvoicingDetail
            {
                InvoicingId = invoice.Id,
                ServiceId = service.ServiceId,
                ServiceName = service.ServiceName,
                Price = bookingVM.Amount,
                Description = termDescription,
                TaxesIds = taxInfo.TaxId?.ToString(),
                TaxesName = taxInfo.TaxName,
                TaxAmount = taxInfo.TaxAmount,
                TotalAmount = netAmount,
                DiscountAmount = 0.00m
            };

            uow.GenericRepository<EF.InvoicingDetail>().Insert(invoicingDetail);
            uow.SaveChanges();

            return invoicingDetail;
        }

        private void CreateInvoiceLedger(EF.Invoicing invoice, int personId, decimal netAmount, BookingVM bookingVM, int createdByUserId)
        {
            var invoiceLedger = new EF.StudentLedger
            {
                PaymentDate = DateTime.Now,
                Code = invoice.Code,
                InvoiceId = invoice.Id,
                StudentId = personId,
                DebitAmount = netAmount,
                CreditAmount = null,
                Remarks = invoice.Remarks,
                PaymentTypeName = "Invoice",
                IsApproved = true,
                CreatedBy = createdByUserId,
                CreatedDate = DateTime.Now,
                LocationId = bookingVM.LocationID,
                ApprovedBy = createdByUserId
            };

            uow.GenericRepository<EF.StudentLedger>().Insert(invoiceLedger);
            uow.SaveChanges();
        }

        private string GetTermDescription(int roomTypePriceId)
        {
            var priceConfig = uow.GenericRepository<EF.PriceConfig>().Table
                .FirstOrDefault(p => p.PriceConfigID == roomTypePriceId);

            if (priceConfig != null)
            {
                var term = uow.GenericRepository<EF.Term>().Table
                    .FirstOrDefault(t => t.TermID == priceConfig.TermID);

                return term != null
                    ? $"{term.TermName} - {term.TermDescription}"
                    : $"Term ID: {priceConfig.TermID}";
            }

            return $"PriceConfig ID: {roomTypePriceId} not found";
        }

        private void CreateAuditLogForOnlineInvoice(EF.Invoicing invoice)
        {
            EF.AuditLog auditLog = new EF.AuditLog()
            {
                AuditType = (int)Enumeration.AuditType.Create,
                ActionId = (int)Enumeration.CorrespondenceAction.CreateInvoice,
                PK = invoice.Id.ToString(),
                UserId = invoice.CreatedBy, // System user for online bookings
                TableName = "Invoicing",
                Reference = invoice.Code,
                UserName = "System - Online Booking",
                PersonId = invoice.StudentId,
                TimeStamp = DateTime.Now,
            };

            auditLogsService.AddAuditLog(auditLog);
        }

        public void CreateOnlineInvoicingVoucher(Invoicing invoicing, List<InvoicingDetail> list, int createdByUserId)
        {
            var request = new VoucherCreationRequest
            {
                VoucherType = VoucherType.Invoice,
                BaseVoucherData = new BaseVoucherData
                {
                    VoucherDate = invoicing.InvoiceDate,
                    ReferenceId = invoicing.Id,
                    StudentId = invoicing.StudentId,
                    Remarks = invoicing.Remarks,
                    CreatedDate = invoicing.CreatedDate,
                    CreatedBy = invoicing.CreatedBy,
                    LocationId = invoicing.LocationId,
                    TransactionType = "Invoice",
                    UserName = invoicing.CreatedBy == createdByUserId ? "System - Online Booking" : Common.Globals.User.Name + " - " + Common.Globals.User.Email
                },
                InvoicingData = invoicing,
                InvoicingDetails = list,
                IsRefund = false,
                IsOnlineBooking = true
            };

            voucherService.CreateVoucherWithDetails(request, auditLogsService);
        }

        #endregion

    }
}
