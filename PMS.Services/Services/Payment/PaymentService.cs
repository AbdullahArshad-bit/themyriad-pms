using Ninject.Activation;
using PMS.Common.Classes;
using PMS.DTO.ViewModels;
using PMS.DTO.ViewModels.ApiViewModels;
using PMS.DTO.ViewModels.PaymentViewModels;
using PMS.EF;
using PMS.Repository.UnitOfWork;
using PMS.Services.Services.AuditLogs;
using PMS.Services.Services.VoucherSystem;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using TheMyriad.DTO.DTO_Mapings;
using System.Data.Entity;
using PMS.Services.Services.PaymentTypes;
using PMS.Services.Services.CreditNote;
using PMS.Services.Services.Notifications;
using PMS.Services.Services.Email;
using PMS.Services.Services.Correspondence;
using PMS.Services.Services.Setup;
using PMS.Services.Services.Person;
using System.IO;
using static PMS.Common.Classes.Enumeration;
using NReco.PdfGenerator;
using System.Xml.Linq;
using iTextSharp.text.pdf;
using iTextSharp.text;
using iTextSharp.tool.xml;
using System.Security.Cryptography;
using PMS.Services.Services.LocationContext;


namespace PMS.Services.Services.Payment
{
    //For Student Portal
    public class PaymentService : IPaymentService
    {
        private static readonly object ReceiptCodeLock = new object();

        private readonly UnitOfWork<PMSEntities> uow;
        private readonly IVoucherService voucherService;
        private readonly IAuditLogsService auditLogsService;
        private readonly IPaymentTypesService paymentTypesService;
        private readonly ICreditNoteService creditNoteService;
        private readonly INotificationService notificationService;
        private readonly IEmailService emailService;
        private readonly ICorrespondenceService correspondenceService;
        private readonly IPersonService personService;
        private readonly ILocationContextService locationContextService;

        public PaymentService(UnitOfWork<PMSEntities> _uow, IVoucherService _voucherService, IAuditLogsService _auditLogsService,
            IPaymentTypesService _paymentTypesService, ICreditNoteService _creditNoteService, INotificationService _notificationService,
            IEmailService _emailService, ICorrespondenceService _correspondenceService, IPersonService _personService, ILocationContextService _locationContextService)
        {
            uow = _uow;
            voucherService = _voucherService;
            auditLogsService = _auditLogsService;
            paymentTypesService = _paymentTypesService;
            creditNoteService = _creditNoteService;
            notificationService = _notificationService;
            emailService = _emailService;
            correspondenceService = _correspondenceService;
            personService = _personService;
            locationContextService = _locationContextService;
        }
        public PaymentResponse GetAll(PaymentBinding request, string QueryBY, string searchValue, string start, string length,
              string query = null, string orderBy = null, string orderDir = "asc", DateTime? FromDate = null, DateTime? ToDate = null, int? StudentId = 0)
        {
            try
            {
                var assignedLocationIds = locationContextService.GetAssignedLocationIds();

                // Initialize IQueryable with base query - Apply filters early for better performance
                IQueryable<EF.V_GetPaymentList> baseQuery = uow.GenericRepository<EF.V_GetPaymentList>().Table
        .Where(x => assignedLocationIds.Contains((int)x.LocationId) &&
                    (x.TransactionType == "Normal" || (x.IsReversedPayment == true && x.ParentId != null)))
        .Where(x => (!FromDate.HasValue || EntityFunctions.TruncateTime(x.PaymentDate) >= EntityFunctions.TruncateTime(FromDate.Value)) &&
                   (!ToDate.HasValue || EntityFunctions.TruncateTime(x.PaymentDate) <= EntityFunctions.TruncateTime(ToDate.Value)) &&
                   (StudentId == 0 || x.StudentId == StudentId));

                // Apply search filters BEFORE projection to reduce data transfer
                if (!string.IsNullOrEmpty(request?.search?.value) && !string.IsNullOrEmpty(request.search.column) && request.query == null)
                {
                    string searchVal = request.search.value.ToLower();
                    switch (request.search.column.ToLower())
                    {
                        case "transactioncode":
                            baseQuery = baseQuery.Where(x => x.TransactionCode != null && x.TransactionCode.ToLower().Contains(searchVal));
                            break;
                        case "location":
                            baseQuery = baseQuery.Where(x => x.Location != null && x.Location.ToLower().Contains(searchVal));
                            break;
                        case "myriadid":
                            baseQuery = baseQuery.Where(x => x.MyriadID != null && x.MyriadID.ToLower().Contains(searchVal));
                            break;
                        case "fullname":
                            baseQuery = baseQuery.Where(x => x.FullName != null && x.FullName.ToLower().Contains(searchVal));
                            break;
                        case "invoicecode":
                            baseQuery = baseQuery.Where(x => x.InvoiceCode != null && x.InvoiceCode.ToLower().Contains(searchVal));
                            break;
                        case "remarks":
                            baseQuery = baseQuery.Where(x => x.Remarks != null && x.Remarks.ToLower().Contains(searchVal));
                            break;
                        case "paymentname":
                            baseQuery = baseQuery.Where(x => x.PaymentName != null && x.PaymentName.ToLower().Contains(searchVal));
                            break;
                        case "paymentreferencenumber":
                            baseQuery = baseQuery.Where(x => x.PaymentReferenceNumber != null && x.PaymentReferenceNumber.ToLower().Contains(searchVal));
                            break;
                        case "amount":
                            if (decimal.TryParse(searchVal, out decimal amount))
                            {
                                baseQuery = baseQuery.Where(x => x.Amount == amount);
                            }
                            break;
                        case "isapproved":
                            if (bool.TryParse(searchVal, out bool isApproved))
                            {
                                baseQuery = baseQuery.Where(x => x.IsApproved == isApproved);
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
                        (x.TransactionCode != null && x.TransactionCode.ToLower().Contains(searchVal)) ||
                        (x.Location != null && x.Location.ToLower().Contains(searchVal)) ||
                        (x.MyriadID != null && x.MyriadID.ToLower().Contains(searchVal)) ||
                        (x.FullName != null && x.FullName.ToLower().Contains(searchVal)) ||
                        (x.InvoiceCode != null && x.InvoiceCode.ToLower().Contains(searchVal)) ||
                        (x.Remarks != null && x.Remarks.ToLower().Contains(searchVal)) ||
                        (x.PaymentName != null && x.PaymentName.ToLower().Contains(searchVal)) ||
                        (x.PaymentReferenceNumber != null && x.PaymentReferenceNumber.ToLower().Contains(searchVal)) ||
                        (x.CreatedBy != null && x.CreatedBy.ToLower().Contains(searchVal)) ||
                        (x.ApprovedBy != null && x.ApprovedBy.ToLower().Contains(searchVal)));
                }

                // Apply ordering BEFORE projection
                IQueryable<EF.V_GetPaymentList> orderedQuery = ApplyOrdering(baseQuery, orderBy, orderDir);

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
                var payments = orderedQuery.Select(x => new PaymentVM
                {
                    Id = x.Id,
                    TransactionCode = x.TransactionCode,
                    Location = x.Location,
                    MyriadID = x.MyriadID,
                    FullName = x.FullName,
                    InvoiceCode = x.InvoiceCode,
                    InvoiceDate = x.InvoiceDate,
                    PaymentDate = x.PaymentDate,
                    CreatedDate = x.CreatedDate,
                    Remarks = x.Remarks,
                    Amount = x.Amount ?? 0,
                    CreditAmount = x.CreditAmount ?? 0,
                    DebitAmount = x.DebitAmount ?? 0,
                    PaymentName = x.PaymentName,
                    PaymentReferenceNumber = x.PaymentReferenceNumber,
                    IsApproved = x.IsApproved,
                    CreatedBy = x.CreatedBy,
                    ApprovedBy = x.ApprovedBy,
                    CreditNoteId = x.CreditNoteId,
                    VoucherId = x.VoucherId ?? 0,
                    LocationId = x.LocationId,
                    IsReversedPayment = x.IsReversedPayment,
                    Currency = x.Currency
                });

                List<string> selectedColumn = request?.SelectedColumns ?? new List<string>();
                if (selectedColumn.Any())
                {
                    payments = ApplyColumnFiltering(payments, selectedColumn);
                }

                var result = new PaymentResponse();
                result.PaymentingList = payments.ToList();

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

        // New: Get only refund payments (DebitAmount > 0, PaymentTypeName <> 'Invoice') for Refund Payments report
        public PaymentResponse GetRefunds(PaymentBinding request, string QueryBY, string searchValue, string start, string length,
            string query = null, string orderBy = null, string orderDir = "asc", DateTime? FromDate = null, DateTime? ToDate = null, int? StudentId = 0)
        {
            try
            {
                var assignedLocationIds = locationContextService.GetAssignedLocationIds();

                // Initialize IQueryable with base query - Apply filters early for better performance
                // Show only parent records (ParentId IS NULL) to avoid duplicates
                IQueryable<EF.V_GetPaymentList> baseQuery = uow.GenericRepository<EF.V_GetPaymentList>().Table
       .Where(x => assignedLocationIds.Contains((int)x.LocationId) &&
                   x.TransactionType == "Refund" && (x.IsReversedPayment != true || x.IsReversedPayment == null))
       .Where(x => (!FromDate.HasValue || EntityFunctions.TruncateTime(x.PaymentDate) >= EntityFunctions.TruncateTime(FromDate.Value)) &&
                  (!ToDate.HasValue || EntityFunctions.TruncateTime(x.PaymentDate) <= EntityFunctions.TruncateTime(ToDate.Value)) &&
                  (StudentId == 0 || x.StudentId == StudentId));

                // Apply search filters BEFORE projection to reduce data transfer
                if (!string.IsNullOrEmpty(request?.search?.value) && !string.IsNullOrEmpty(request.search.column) && request.query == null)
                {
                    string searchVal = request.search.value.ToLower();
                    switch (request.search.column.ToLower())
                    {
                        case "transactioncode":
                            baseQuery = baseQuery.Where(x => x.TransactionCode != null && x.TransactionCode.ToLower().Contains(searchVal));
                            break;
                        case "location":
                            baseQuery = baseQuery.Where(x => x.Location != null && x.Location.ToLower().Contains(searchVal));
                            break;
                        case "myriadid":
                            baseQuery = baseQuery.Where(x => x.MyriadID != null && x.MyriadID.ToLower().Contains(searchVal));
                            break;
                        case "fullname":
                            baseQuery = baseQuery.Where(x => x.FullName != null && x.FullName.ToLower().Contains(searchVal));
                            break;
                        case "invoicecode":
                            baseQuery = baseQuery.Where(x => x.InvoiceCode != null && x.InvoiceCode.ToLower().Contains(searchVal));
                            break;
                        case "remarks":
                            baseQuery = baseQuery.Where(x => x.Remarks != null && x.Remarks.ToLower().Contains(searchVal));
                            break;
                        case "paymentname":
                            baseQuery = baseQuery.Where(x => x.PaymentName != null && x.PaymentName.ToLower().Contains(searchVal));
                            break;
                        case "paymentreferencenumber":
                            baseQuery = baseQuery.Where(x => x.PaymentReferenceNumber != null && x.PaymentReferenceNumber.ToLower().Contains(searchVal));
                            break;
                        case "debitamount":
                            if (decimal.TryParse(searchVal, out decimal amount))
                            {
                                baseQuery = baseQuery.Where(x => x.DebitAmount == amount);
                            }
                            break;
                        case "isapproved":
                            if (bool.TryParse(searchVal, out bool isApproved))
                            {
                                baseQuery = baseQuery.Where(x => x.IsApproved == isApproved);
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
                        (x.TransactionCode != null && x.TransactionCode.ToLower().Contains(searchVal)) ||
                        (x.Location != null && x.Location.ToLower().Contains(searchVal)) ||
                        (x.MyriadID != null && x.MyriadID.ToLower().Contains(searchVal)) ||
                        (x.FullName != null && x.FullName.ToLower().Contains(searchVal)) ||
                        (x.InvoiceCode != null && x.InvoiceCode.ToLower().Contains(searchVal)) ||
                        (x.Remarks != null && x.Remarks.ToLower().Contains(searchVal)) ||
                        (x.PaymentName != null && x.PaymentName.ToLower().Contains(searchVal)) ||
                        (x.PaymentReferenceNumber != null && x.PaymentReferenceNumber.ToLower().Contains(searchVal)) ||
                        (x.CreatedBy != null && x.CreatedBy.ToLower().Contains(searchVal)) ||
                        (x.ApprovedBy != null && x.ApprovedBy.ToLower().Contains(searchVal)));
                }

                // Apply ordering BEFORE projection
                IQueryable<EF.V_GetPaymentList> orderedQuery = ApplyOrdering(baseQuery, orderBy, orderDir);

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
                var payments = orderedQuery.Select(x => new PaymentVM
                {
                    Id = x.Id,
                    TransactionCode = x.TransactionCode,
                    Location = x.Location,
                    MyriadID = x.MyriadID,
                    FullName = x.FullName,
                    InvoiceCode = x.InvoiceCode,
                    InvoiceDate = x.InvoiceDate,
                    PaymentDate = x.PaymentDate,
                    CreatedDate = x.CreatedDate,
                    Remarks = x.Remarks,
                    CreditAmount = x.CreditAmount ?? 0,
                    DebitAmount = x.DebitAmount ?? 0,
                    PaymentName = x.PaymentName,
                    PaymentReferenceNumber = x.PaymentReferenceNumber,
                    IsApproved = x.IsApproved,
                    CreatedBy = x.CreatedBy,
                    ApprovedBy = x.ApprovedBy,
                    CreditNoteId = x.CreditNoteId,
                    VoucherId = x.VoucherId ?? 0,
                    LocationId = x.LocationId,
                    IsReversedPayment = x.IsReversedPayment,
                    Currency = x.Currency
                });

                List<string> selectedColumn = request?.SelectedColumns ?? new List<string>();
                if (selectedColumn.Any())
                {
                    payments = ApplyColumnFiltering(payments, selectedColumn);
                }

                var result = new PaymentResponse();
                result.PaymentingList = payments.ToList();

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

        private IQueryable<EF.V_GetPaymentList> ApplyOrdering(IQueryable<EF.V_GetPaymentList> query, string orderBy, string orderDir)
        {
            bool ascending = orderDir?.ToLower() == "asc";

            switch (orderBy?.ToLower())
            {
                case "transactioncode":
                    return ascending ? query.OrderBy(x => x.TransactionCode) : query.OrderByDescending(x => x.TransactionCode);
                case "location":
                    return ascending ? query.OrderBy(x => x.Location) : query.OrderByDescending(x => x.Location);
                case "myriadid":
                    return ascending ? query.OrderBy(x => x.MyriadID) : query.OrderByDescending(x => x.MyriadID);
                case "fullname":
                    return ascending ? query.OrderBy(x => x.FullName) : query.OrderByDescending(x => x.FullName);
                case "invoicecode":
                    return ascending ? query.OrderBy(x => x.InvoiceCode) : query.OrderByDescending(x => x.InvoiceCode);
                case "invoicedate":
                    return ascending ? query.OrderBy(x => x.InvoiceDate) : query.OrderByDescending(x => x.InvoiceDate);
                case "paymentdate":
                    return ascending ? query.OrderBy(x => x.PaymentDate) : query.OrderByDescending(x => x.PaymentDate);
                case "createddate":
                    return ascending ? query.OrderBy(x => x.CreatedDate) : query.OrderByDescending(x => x.CreatedDate);
                case "remarks":
                    return ascending ? query.OrderBy(x => x.Remarks) : query.OrderByDescending(x => x.Remarks);
                case "amount":
                    return ascending ? query.OrderBy(x => x.Amount) : query.OrderByDescending(x => x.Amount);
                case "debitamount":
                    return ascending ? query.OrderBy(x => x.DebitAmount) : query.OrderByDescending(x => x.DebitAmount);
                case "paymentname":
                    return ascending ? query.OrderBy(x => x.PaymentName) : query.OrderByDescending(x => x.PaymentName);
                case "paymentreferencenumber":
                    return ascending ? query.OrderBy(x => x.PaymentReferenceNumber) : query.OrderByDescending(x => x.PaymentReferenceNumber);
                case "isapproved":
                    return ascending ? query.OrderBy(x => x.IsApproved) : query.OrderByDescending(x => x.IsApproved);
                case "createdby":
                    return ascending ? query.OrderBy(x => x.CreatedBy) : query.OrderByDescending(x => x.CreatedBy);
                case "approvedby":
                    return ascending ? query.OrderBy(x => x.ApprovedBy) : query.OrderByDescending(x => x.ApprovedBy);
                case "id":
                    return ascending ? query.OrderBy(x => x.Id) : query.OrderByDescending(x => x.Id);
                default:
                    return query.OrderByDescending(x => x.CreatedDate);
            }
        }

        private IQueryable<PaymentVM> ApplyColumnFiltering(IQueryable<PaymentVM> query, List<string> selectedColumns)
        {
            List<string> allColumns = new List<string>
    {
        "TransactionCode", "Location", "MyriadID", "FullName", "InvoiceCode", "InvoiceDate", "PaymentDate", "CreatedDate",
        "Remarks", "Amount", "CreditAmount","DebitAmount", "PaymentName", "PaymentReferenceNumber", "IsApproved", "CreatedBy", "ApprovedBy",
        "VoucherId"
    };

            List<string> unselectedColumns = allColumns.Except(selectedColumns).ToList();

            return query.Select(x => new PaymentVM
            {
                Id = x.Id,
                TransactionCode = unselectedColumns.Contains("TransactionCode") ? x.TransactionCode : default,
                Location = unselectedColumns.Contains("Location") ? x.Location : default,
                MyriadID = unselectedColumns.Contains("MyriadID") ? x.MyriadID : default,
                FullName = unselectedColumns.Contains("FullName") ? x.FullName : default,
                InvoiceCode = unselectedColumns.Contains("InvoiceCode") ? x.InvoiceCode : default,
                InvoiceDate = unselectedColumns.Contains("InvoiceDate") ? x.InvoiceDate : default,
                PaymentDate = unselectedColumns.Contains("PaymentDate") ? x.PaymentDate : default,
                CreatedDate = unselectedColumns.Contains("CreatedDate") ? x.CreatedDate : default,
                Remarks = unselectedColumns.Contains("Remarks") ? x.Remarks : default,
                Amount = unselectedColumns.Contains("Amount") ? x.Amount : default,
                CreditAmount = unselectedColumns.Contains("CreditAmount") ? x.CreditAmount : default,
                DebitAmount = unselectedColumns.Contains("DebitAmount") ? x.DebitAmount : default,
                PaymentName = unselectedColumns.Contains("PaymentName") ? x.PaymentName : default,
                PaymentReferenceNumber = unselectedColumns.Contains("PaymentReferenceNumber") ? x.PaymentReferenceNumber : default,
                IsApproved = unselectedColumns.Contains("IsApproved") ? x.IsApproved : default,
                CreatedBy = unselectedColumns.Contains("CreatedBy") ? x.CreatedBy : default,
                ApprovedBy = unselectedColumns.Contains("ApprovedBy") ? x.ApprovedBy : default,
                CreditNoteId = x.CreditNoteId,
                VoucherId = unselectedColumns.Contains("VoucherId") ? x.VoucherId : default
            });
        }

        public PaymentResponse ExportPaymentReport(string QueryBY, DateTime? FromDate = null, DateTime? ToDate = null)
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();

            IQueryable<PaymentVM> query = uow.GenericRepository<EF.V_GetPaymentList>().Table
               .Where(x => assignedLocationIds.Contains((int)x.LocationId) &&
                            (x.TransactionType == "Normal" || (x.IsReversedPayment == true && x.ParentId != null))
                           && (!FromDate.HasValue || EntityFunctions.TruncateTime(x.PaymentDate) >= EntityFunctions.TruncateTime(FromDate.Value))
                           && (!ToDate.HasValue || EntityFunctions.TruncateTime(x.PaymentDate) <= EntityFunctions.TruncateTime(ToDate.Value)))
               .Select(x => new PaymentVM
               {
                   Id = x.Id,
                   TransactionCode = x.TransactionCode,
                   InvoiceCode = x.InvoiceCode,
                   InvoiceDate = x.InvoiceDate,
                   Location = x.Location,
                   MyriadID = x.MyriadID,
                   FullName = x.FullName,
                   PaymentDate = x.PaymentDate,
                   CreatedDate = x.CreatedDate,
                   Remarks = x.Remarks,
                   Amount = x.Amount ?? 0,
                   CreditAmount = x.CreditAmount ?? 0,
                   PaymentName = x.PaymentName,
                   PaymentReferenceNumber = x.PaymentReferenceNumber,
                   IsApproved = x.IsApproved,
                   CreatedBy = x.CreatedBy,
                   ApprovedBy = x.ApprovedBy,
                   CreditNoteId = x.CreditNoteId,
                   VoucherId = x.VoucherId ?? 0
               });

            var Result = new PaymentResponse();
            if (!string.IsNullOrEmpty(QueryBY))
            {
                Result.PaymentingList = query.OrderByDescending(x => x.CreatedDate).ToList();
            }
            return Result;
        }

        public PaymentResponse ExportRefundPaymentReport(string QueryBY, DateTime? FromDate = null, DateTime? ToDate = null)
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();

            IQueryable<PaymentVM> refundQuery = uow.GenericRepository<EF.V_GetPaymentList>().Table
               .Where(x => assignedLocationIds.Contains((int)x.LocationId)
                           && x.TransactionType == "Refund" && (x.IsReversedPayment != true || x.IsReversedPayment == null)
                           && (!FromDate.HasValue || EntityFunctions.TruncateTime(x.PaymentDate) >= EntityFunctions.TruncateTime(FromDate.Value))
                           && (!ToDate.HasValue || EntityFunctions.TruncateTime(x.PaymentDate) <= EntityFunctions.TruncateTime(ToDate.Value)))
               .Select(x => new PaymentVM
               {
                   Id = x.Id,
                   TransactionCode = x.TransactionCode,
                   InvoiceCode = x.InvoiceCode,
                   InvoiceDate = x.InvoiceDate,
                   Location = x.Location,
                   MyriadID = x.MyriadID,
                   FullName = x.FullName,
                   PaymentDate = x.PaymentDate,
                   CreatedDate = x.CreatedDate,
                   Remarks = x.Remarks,
                   Amount = x.Amount ?? 0,
                   // Export refund amount via CreditAmount for consistency
                   CreditAmount = x.DebitAmount ?? 0,
                   PaymentName = x.PaymentName,
                   PaymentReferenceNumber = x.PaymentReferenceNumber,
                   IsApproved = x.IsApproved,
                   CreatedBy = x.CreatedBy,
                   ApprovedBy = x.ApprovedBy,
                   CreditNoteId = x.CreditNoteId,
                   VoucherId = x.VoucherId ?? 0
               });

            var Result = new PaymentResponse();
            if (!string.IsNullOrEmpty(QueryBY))
            {
                Result.PaymentingList = refundQuery.OrderByDescending(x => x.CreatedDate).ToList();
            }
            return Result;
        }

        public List<PaymentVM> GetPayment(int personId)
        {
            var payment = uow.GenericRepository<StudentLedger>().Table.Where(x => x.StudentId == personId && x.PaymentTypeName.Trim() != "Invoice" && x.IsApproved == true).Select(x => new PaymentVM
            {
                Id = x.Id,
                TransactionCode = x.Code,
                Status = x.IsApproved,
                PaymentDate = x.PaymentDate,
                Remarks = x.Remarks,
                Amount = x.CreditAmount ?? 0,
                PaymentName = x.PaymentTypeName == "Thwani Online Payment" ? "Credit Card" : x.PaymentTypeName

            }).ToList();
            return payment;
        }


        #region  ------------------------- ** Receipt Code Generation ** -------------------------
        public string GetMaxCode(int LocationId)
        {
            lock (ReceiptCodeLock)
            {
                var locationPrefix = uow.GenericRepository<Location>().Table
                    .AsNoTracking()
                    .Where(x => x.LocationID == LocationId)
                    .Select(x => x.Prefix)
                    .FirstOrDefault();

                var code = GetMaxReceiptCode(LocationId);
                string value = String.Format("{0:D4}", code);
                var Code = "RCT-" + locationPrefix + "-" + value;
                return Code;
            }
        }

        public string GenerateReceiptCode(int locationId)
        {
            lock (ReceiptCodeLock)
            {
                using (var db1 = new PMSEntities())
                {
                    var locationPrefix = db1.Locations
                        .AsNoTracking()
                        .Where(x => x.LocationID == locationId)
                        .Select(x => x.Prefix)
                        .FirstOrDefault();

                    var maxcode = GetMaxReceiptCode(db1, locationId);
                    string value = String.Format("{0:D4}", maxcode);
                    var Code = "RCT-" + locationPrefix + "-" + value;
                    return Code;
                }
            }
        }

        private int GetMaxReceiptCode(int id)
        {
            return GetMaxReceiptCode(uow.Context, id);
        }

        private static int GetMaxReceiptCode(PMSEntities db, int id)
        {
            var maxExisting = db.StudentLedgers
                .AsNoTracking()
                .Where(x => x.CreditAmount != null && x.Code != null && x.LocationId == id)
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

        // New: Generate refund payment code using InvoiceTypeLookup prefix and new GEN sequence per location
        private string GetMaxPaymentRefundCodeString(int locationId)
        {
            lock (this)
            {
                var db = uow.Context;
                var location = db.Locations.Find(locationId);
                if (location == null) throw new Exception("Invalid location.");

                // Get prefix from InvoiceTypeLookup for PaymentRefund
                var paymentRefundLookupId = (int)PaymentLookup.PaymentRefund;
                var lookup = db.InvoiceTypeLookups.FirstOrDefault(x => x.Id == paymentRefundLookupId);
                if (lookup == null) throw new Exception("Payment refund lookup not configured.");

                // Build code prefix e.g., RCT-REF-GEN-TMD (force -REF if missing in prefix)
                var basePrefix = (lookup.InvoicePrefix ?? string.Empty).Trim();
                if (!basePrefix.ToUpper().Contains("-REF"))
                    basePrefix = (string.IsNullOrWhiteSpace(basePrefix) ? "RCT" : basePrefix) + "-REF";
                var codePrefix = basePrefix + "-GEN-" + location.Prefix;

                // Determine next sequence by either LookupId match or code prefix match
                // Optimized: let DB return only the latest code (server-side ORDER BY + TOP 1)
                var startsWith = codePrefix + "-"; // ensure we don't match plain RCT-...
                // Strictly match only the new refund series for this location
                var topCode = db.StudentLedgers
                    .Where(x => x.LocationId == locationId && x.Code != null && x.Code.StartsWith(startsWith))
                    .OrderByDescending(x => x.Code)
                    .Select(x => x.Code)
                    .FirstOrDefault();

                int next = 1;
                if (!string.IsNullOrEmpty(topCode))
                {
                    var parts = topCode.Split('-');
                    decimal num;
                    next = (parts.Length > 0 && decimal.TryParse(parts.Last(), out num)) ? (int)num + 1 : 1;
                }

                string value = string.Format("{0:D4}", next);
                return codePrefix + "-" + value;
            }
        }

        #endregion


        #region ------------------------- ** All payment vouchers ** -------------------------
        //Create Voucher for Payment
        public void CreatePayVoucher(StudentLedger studentLedger)
        {
            var request = new VoucherCreationRequest
            {
                VoucherType = VoucherType.Payment,
                BaseVoucherData = new BaseVoucherData
                {
                    VoucherDate = studentLedger.PaymentDate,
                    ReferenceId = studentLedger.Id,
                    StudentId = studentLedger.StudentId,
                    Remarks = studentLedger.Remarks,
                    CreatedDate = studentLedger.CreatedDate,
                    CreatedBy = studentLedger.CreatedBy,
                    LocationId = studentLedger.LocationId ?? 0,
                    TransactionType = "Payment"

                },
                PaymentData = studentLedger,
                IsRefund = false
            };

            voucherService.CreateVoucherWithDetails(request, auditLogsService);
        }

        //Create Voucher for CreditNote Payment
        public void CreateCreditNotePaymentVoucher(StudentLedger studentLedger)
        {
            var request = new VoucherCreationRequest
            {
                VoucherType = VoucherType.Payment, // Still use Payment type but with different accounting
                BaseVoucherData = new BaseVoucherData
                {
                    VoucherDate = studentLedger.PaymentDate,
                    ReferenceId = studentLedger.Id,
                    StudentId = studentLedger.StudentId,
                    Remarks = $"Credit Note Payment - {studentLedger.Remarks}",
                    CreatedDate = studentLedger.CreatedDate,
                    CreatedBy = studentLedger.CreatedBy,
                    LocationId = studentLedger.LocationId ?? 0,
                    TransactionType = "Payment"

                },
                PaymentData = studentLedger,
                IsRefund = false,
                IsCreditNotePayment = true
            };
            voucherService.CreateVoucherWithDetails(request, auditLogsService);
        }

        //Create Voucher for Advance Payment
        public void CreateAdvancePaymentVoucher(StudentLedger studentLedger)
        {
            var request = new VoucherCreationRequest
            {
                VoucherType = VoucherType.Payment, // Use Payment type but with advance payment accounting
                BaseVoucherData = new BaseVoucherData
                {
                    VoucherDate = studentLedger.PaymentDate,
                    ReferenceId = studentLedger.Id,
                    StudentId = studentLedger.StudentId,
                    Remarks = $"Advance Payment - {studentLedger.Remarks}",
                    CreatedDate = studentLedger.CreatedDate,
                    CreatedBy = studentLedger.CreatedBy,
                    LocationId = studentLedger.LocationId ?? 0,
                    TransactionType = "Payment"
                },
                PaymentData = studentLedger,
                IsRefund = false,
                IsAdvancePayment = true
            };
            voucherService.CreateVoucherWithDetails(request, auditLogsService);
        }
        public void UpdatePaymentVoucher(StudentLedger studentLedger, IAuditLogsService auditLogsService)
        {
            var existingVoucher = uow.GenericRepository<Voucher>().Table.FirstOrDefault(v => v.ReferenceId == studentLedger.Id);

            if (studentLedger.LookupId == (int)PaymentLookup.PaymentRefund)
            {
                var updateRequest = new VoucherCreationRequest
                {
                    VoucherType = VoucherType.RefundPayment,
                    BaseVoucherData = new BaseVoucherData
                    {
                        VoucherDate = studentLedger.PaymentDate,
                        ReferenceId = studentLedger.Id,
                        StudentId = studentLedger.StudentId,
                        Remarks = studentLedger.Remarks,
                        CreatedDate = studentLedger.CreatedDate,
                        CreatedBy = studentLedger.CreatedBy,
                        LocationId = studentLedger.LocationId ?? 0,
                        TransactionType = "Payment"
                    },
                    PaymentData = studentLedger,
                    IsRefund = true
                };

                voucherService.UpdateVoucherWithDetails(existingVoucher.VoucherId, updateRequest, auditLogsService);
            }
            else
            {
                // Create request object for the update method
                var updateRequest = new VoucherCreationRequest
                {
                    VoucherType = VoucherType.Payment,
                    BaseVoucherData = new BaseVoucherData
                    {
                        VoucherDate = studentLedger.PaymentDate,
                        StudentId = studentLedger.StudentId,
                        Remarks = studentLedger.Remarks,
                        UpdatedBy = Common.Globals.User.ID,
                        LocationId = studentLedger.LocationId ?? 0,
                        TransactionType = "Payment"
                    },
                    PaymentData = studentLedger,
                    IsAdvancePayment = studentLedger.InvoiceId == null

                };

                voucherService.UpdateVoucherWithDetails(existingVoucher.VoucherId, updateRequest, auditLogsService);
            }

            // Use the new update method
            //voucherService.UpdateVoucherWithDetails(existingVoucher.VoucherId, updateRequest, auditLogsService);
        }


        //Create Voucher for Refund Payment
        public void CreateRevPayVoucher(StudentLedger studentLedger)
        {
            var request = new VoucherCreationRequest
            {
                VoucherType = VoucherType.RefundPayment,
                BaseVoucherData = new BaseVoucherData
                {
                    VoucherDate = studentLedger.PaymentDate,
                    ReferenceId = studentLedger.Id,
                    StudentId = studentLedger.StudentId,
                    Remarks = studentLedger.Remarks,
                    CreatedDate = studentLedger.CreatedDate,
                    CreatedBy = studentLedger.CreatedBy,
                    LocationId = studentLedger.LocationId ?? 0,
                    TransactionType = "Payment"
                },
                PaymentData = studentLedger,
                IsRefund = true
            };

            voucherService.CreateVoucherWithDetails(request, auditLogsService);
        }

        #endregion



        public PartnerLedgerPageViewModel GetPartnerLedger(DateTime? FromDate, DateTime? ToDate, int PersonId, bool Status = true)
        {
            var db = uow.Context;
            var query = db.StudentLedgers
                          .Include("Invoicing.InvoicingDetails")
                          .Include("Location")
                          .Include("Person")
                          .Where(x => x.StudentId == PersonId);

            if (Status)
                query = query.Where(x => x.IsApproved == Status);

            var allRecords = query.OrderBy(x => x.PaymentDate).ToList();
            decimal openingBalance = 0;

            // Calculate opening balance - this should match production logic
            if (FromDate != null)
            {
                var prevRecords = allRecords.Where(x => x.PaymentDate.Date < FromDate.Value.Date).ToList();
                decimal prevBalance = 0;
                foreach (var l in prevRecords)
                {
                    // Simple Invoice
                    if (l.PaymentTypeName == "Invoice" && l.DebitAmount != null && l.CreditAmount == null)
                        prevBalance += l.DebitAmount ?? 0;
                    // Refunded Invoice
                    else if (l.PaymentTypeName == "Invoice" && l.DebitAmount == null && l.CreditAmount != null)
                        prevBalance -= l.CreditAmount ?? 0;
                    // Simple Payment
                    // Fixed: Changed condition from l.DebitAmount == null to (l.DebitAmount == null || l.DebitAmount == 0)
                    else if (l.PaymentTypeName != "Invoice" && l.CreditAmount != null && (l.DebitAmount == null || l.DebitAmount == 0))
                        prevBalance -= l.CreditAmount ?? 0;
                    // Refunded Payment
                    else if (l.PaymentTypeName != "Invoice" && l.CreditAmount == null && l.DebitAmount != null)
                        prevBalance += l.DebitAmount ?? 0;
                }
                openingBalance = prevBalance;
            }

            var ledgers = allRecords.Where(x =>
                (FromDate == null || x.PaymentDate.Date >= FromDate.Value.Date) &&
                (ToDate == null || x.PaymentDate.Date <= ToDate.Value.Date)
            ).ToList();

            var allCreditNotes = db.StudentCreditNotes
                .Where(x => x.StudentId == PersonId && x.IsEnable == true && x.Status == (int)Enumeration.Status.Approved)
                .ToList();
            var creditNoteIds = allCreditNotes.Select(cn => cn.ID).ToList();

            var allCreditNoteDetails = db.StudentCreditNoteDetails
                .Where(x => creditNoteIds.Contains(x.StudentCreditNoteId))
                .ToList();

            var transactions = new List<LedgerTransactionViewModel>();

            // Ensure FromDate and ToDate are set
            DateTime? effectiveFromDate = FromDate;
            DateTime? effectiveToDate = ToDate;
            if (!FromDate.HasValue && ledgers.Any())
                effectiveFromDate = ledgers.Min(x => x.PaymentDate);
            if (!ToDate.HasValue && ledgers.Any())
                effectiveToDate = ledgers.Max(x => x.PaymentDate);

            // Add opening balance entry
            transactions.Add(new LedgerTransactionViewModel
            {
                Date = effectiveFromDate ?? (ledgers.FirstOrDefault()?.PaymentDate ?? DateTime.Now),
                Reference = "-",
                Type = "Opening",
                Description = "Opening Balance",
                Debit = null,
                Credit = null,
                CreditNoteReference = null,
                RunningBalance = openingBalance
            });

            // Fetch all invoicing codes for credit note dashboard mapping
            var appliedInvoiceIds = allCreditNoteDetails.Select(d => d.InvoiceId).Distinct().ToList();
            var invoiceIdToCode = db.Invoicings
                .Where(inv => appliedInvoiceIds.Contains(inv.Id))
                .ToDictionary(inv => inv.Id, inv => inv.Code);

            var timeline = new List<dynamic>();

            // Add ledger transactions
            foreach (var l in ledgers)
            {
                if ((effectiveFromDate == null || l.PaymentDate.Date >= effectiveFromDate.Value.Date) &&
                    (effectiveToDate == null || l.PaymentDate.Date <= effectiveToDate.Value.Date))
                {
                    string type = l.PaymentTypeName == "Invoice" ? "Invoice" : (l.CreditNoteId != null ? "Credit Applied" : "Payment");
                    string reference = l.Code;
                    string description = l.Remarks;
                    decimal? debit = l.DebitAmount;
                    decimal? credit = l.CreditAmount;
                    string creditNoteRef = null;

                    if (l.CreditNoteId != null)
                    {
                        var cn = allCreditNotes.FirstOrDefault(x => x.ID == l.CreditNoteId);
                        if (cn != null)
                        {
                            creditNoteRef = cn.Code;
                            reference = l.Code + " (" + cn.Code + ")";
                            var invoiceCode = invoiceIdToCode.ContainsKey(l.InvoiceId ?? 0) ? invoiceIdToCode[l.InvoiceId ?? 0] : (l.InvoiceId?.ToString() ?? "");
                            description = $"Applied {cn.Code} to Invoice {invoiceCode}<br>Credit Note Reference: {cn.Code}";
                        }
                    }

                    timeline.Add(new
                    {
                        Date = l.PaymentDate,
                        Reference = reference,
                        Type = type,
                        Description = description,
                        Debit = debit,
                        Credit = credit,
                        CreditNoteReference = creditNoteRef,
                        LedgerRecord = l // Keep reference to original ledger record
                    });
                }
            }

            // Add credit notes
            foreach (var cn in allCreditNotes)
            {
                if ((effectiveFromDate == null || cn.CreatedDate.Date >= effectiveFromDate.Value.Date) &&
                    (effectiveToDate == null || cn.CreatedDate.Date <= effectiveToDate.Value.Date))
                {
                    if (cn.Amount > (cn.AdjustedAmount ?? 0))
                    {
                        timeline.Add(new
                        {
                            Date = cn.CreatedDate,
                            Reference = cn.Code,
                            Type = "Credit Note",
                            Description = cn.Reason,
                            Debit = (decimal?)null,
                            Credit = cn.Amount - (cn.AdjustedAmount ?? 0),
                            CreditNoteReference = "",
                            LedgerRecord = (object)null
                        });
                    }
                }
            }

            // Add credit note details (only if no corresponding payment record exists)
            foreach (var cnd in allCreditNoteDetails)
            {
                if ((effectiveFromDate == null || cnd.CreatedDate.Date >= effectiveFromDate.Value.Date) &&
                    (effectiveToDate == null || cnd.CreatedDate.Date <= effectiveToDate.Value.Date))
                {
                    var cn = allCreditNotes.FirstOrDefault(x => x.ID == cnd.StudentCreditNoteId);
                    if (cn != null)
                    {
                        // Check if there's already a payment record for this credit note application
                        var existingPayment = ledgers.FirstOrDefault(x => x.CreditNoteId == cn.ID && x.InvoiceId == cnd.InvoiceId);

                        // Only add the credit note detail if there's no corresponding payment record
                        if (existingPayment == null)
                        {
                            timeline.Add(new
                            {
                                Date = cnd.CreatedDate,
                                Reference = "CN-APP-" + cnd.Id,
                                Type = "Credit Applied",
                                Description = $"Applied {cn.Code} to Invoice {(invoiceIdToCode.ContainsKey(cnd.InvoiceId) ? invoiceIdToCode[cnd.InvoiceId] : cnd.InvoiceId.ToString())}<br><span class='credit-note-ref'>Credit Note Reference: {cn.Code}</span>",
                                Debit = (decimal?)null,
                                Credit = cnd.Amount,
                                CreditNoteReference = cn.Code,
                                LedgerRecord = (object)null
                            });
                        }
                    }
                }
            }

            // Sort timeline by date, then by transaction type priority, then by reference to ensure consistent ordering
            var sortedTimeline = timeline.OrderBy(x => x.Date)
                                        .ThenBy(x => x.Type == "Invoice" ? 1 :
                                                   x.Type == "Payment" ? 2 :
                                                   x.Type == "Credit Note" ? 3 : 4)
                                        .ThenBy(x => x.Reference) // Add reference as tie-breaker for same date/type
                                        .ToList();

            decimal runningBalance = openingBalance;

            foreach (var t in sortedTimeline)
            {
                // Get additional transaction details
                DateTime? fromDate = null;
                DateTime? toDate = null;
                string paymentType = null;
                string status = null;

                if (t.Type == "Invoice")
                {
                    var ledger = t.LedgerRecord as StudentLedger;
                    if (ledger != null && ledger.Invoicing != null && ledger.Invoicing.InvoicingDetails.Any())
                    {
                        var detail = ledger.Invoicing.InvoicingDetails.OrderByDescending(d => d.ToDate).FirstOrDefault();
                        fromDate = detail?.FromDate;
                        toDate = detail?.ToDate;
                    }
                    paymentType = ledger?.PaymentTypeName;
                    status = (ledger?.IsApproved ?? false) ? "Approved" : "Unapproved";
                }
                else if (t.Type == "Payment")
                {
                    var ledger = t.LedgerRecord as StudentLedger;
                    paymentType = ledger?.PaymentTypeName;
                    status = (ledger?.IsApproved ?? false) ? "Approved" : "Unapproved";

                    // If Payment has InvoiceId, fetch FromDate/ToDate from related InvoicingDetail
                    if (ledger != null && ledger.InvoiceId.HasValue)
                    {
                        var invoice = db.Invoicings.FirstOrDefault(inv => inv.Id == ledger.InvoiceId.Value);
                        var detail = invoice?.InvoicingDetails.OrderByDescending(d => d.ToDate).FirstOrDefault();
                        fromDate = detail?.FromDate;
                        toDate = detail?.ToDate;
                    }
                }
                else if (t.Type == "Credit Note")
                {
                    var creditNote = allCreditNotes.FirstOrDefault(x => x.CreatedDate == t.Date && x.Code == t.Reference);
                    paymentType = creditNote?.CreditNoteTypeLookup?.Name;
                    status = creditNote != null ? (creditNote.Status == 3 ? "Approved" : (creditNote.Status == 1 ? "Pending" : "Unknown")) : null;
                }
                else if (t.Type == "Credit Applied")
                {
                    // For Credit Applied, try to get the related credit note and invoice
                    var creditNote = allCreditNotes.FirstOrDefault(x => t.Description != null && t.Description.Contains(x.Code));
                    paymentType = creditNote?.CreditNoteTypeLookup?.Name;
                    status = creditNote != null ? (creditNote.Status == 3 ? "Approved" : (creditNote.Status == 1 ? "Pending" : "Unknown")) : null;

                    // Try to get invoice details
                    var invoiceId = 0;
                    var match = System.Text.RegularExpressions.Regex.Match(t.Description ?? "", @"Invoice (\d+)");
                    if (match.Success && int.TryParse(match.Groups[1].Value, out invoiceId))
                    {
                        var invoice = db.Invoicings.FirstOrDefault(inv => inv.Id == invoiceId);
                        var detail = invoice?.InvoicingDetails.OrderByDescending(d => d.ToDate).FirstOrDefault();
                        fromDate = detail?.FromDate;
                        toDate = detail?.ToDate;
                    }
                }

                // FIXED: Single running balance calculation logic
                if (t.Type == "Invoice")
                {
                    var ledger = t.LedgerRecord as StudentLedger;
                    if (ledger != null)
                    {
                        // Simple Invoice (Debit increases balance)
                        if (ledger.DebitAmount != null && ledger.CreditAmount == null)
                        {
                            runningBalance += ledger.DebitAmount ?? 0;
                        }
                        // Refunded Invoice (Credit decreases balance)
                        else if (ledger.DebitAmount == null && ledger.CreditAmount != null)
                        {
                            runningBalance -= ledger.CreditAmount ?? 0;
                        }
                    }
                }
                else if (t.Type == "Payment")
                {
                    var ledger = t.LedgerRecord as StudentLedger;
                    if (ledger != null && ledger.CreditNoteId == null) // Exclude credit note applications
                    {
                        // For advance payments, we need to handle them properly:
                        // - If payment has InvoiceId, it's an allocated payment (reduces balance)
                        // - If payment has no InvoiceId, it's an advance payment (still reduces balance)
                        // Both cases should reduce the running balance

                        // Simple Payment (Credit decreases balance)
                        // Fixed: Changed condition from ledger.DebitAmount == null to (ledger.DebitAmount == null || ledger.DebitAmount == 0)
                        if (ledger.CreditAmount != null && (ledger.DebitAmount == null || ledger.DebitAmount == 0))
                        {
                            // Debug: Check if this is advance payment or allocated payment
                            // Allocated payment: has InvoiceId (e.g., RCT-TMD-10982 with InvoiceId 69716)
                            // Advance payment: no InvoiceId (e.g., RCT-TMD-10983 with InvoiceId NULL)
                            runningBalance -= ledger.CreditAmount ?? 0;
                        }
                        // Refunded Payment (Debit increases balance)
                        else if (ledger.CreditAmount == null && ledger.DebitAmount != null)
                        {
                            runningBalance += ledger.DebitAmount ?? 0;
                        }
                    }
                }
                else if (t.Type == "Credit Note")
                {
                    // Credit notes don't affect running balance until applied
                    // No change to running balance
                }
                else if (t.Type == "Credit Applied")
                {
                    // Credit applications decrease the balance
                    runningBalance -= t.Credit ?? 0;
                }

                transactions.Add(new LedgerTransactionViewModel
                {
                    Date = t.Date,
                    Reference = t.Reference,
                    Type = t.Type,
                    Description = t.Description,
                    Debit = t.Debit,
                    Credit = t.Credit,
                    CreditNoteReference = t.CreditNoteReference,
                    RunningBalance = runningBalance,
                    FromDate = fromDate,
                    ToDate = toDate,
                    PaymentType = paymentType,
                    Status = status,
                    InvoiceId = (t.Type == "Invoice") ? (t.LedgerRecord as StudentLedger)?.InvoiceId : null,
                    InvoiceTypeId = (t.Type == "Invoice") ? (t.LedgerRecord as StudentLedger)?.Invoicing?.InvoiceTypeId : null,
                    IsPaid = (t.Type == "Invoice") ? (t.LedgerRecord as StudentLedger)?.Invoicing?.IsPaid : null,
                    Refunded = (t.Type == "Invoice") ? (t.LedgerRecord as StudentLedger)?.Invoicing?.Refunded : null,
                    ParentInvoiceId = (t.Type == "Invoice") ? (t.LedgerRecord as StudentLedger)?.Invoicing?.ParentInvoiceId : null
                });
            }

            // Credit note dashboard
            var creditNoteDashboard = allCreditNotes.Select(cn => new CreditNoteDashboardViewModel
            {
                CreditNoteCode = cn.Code,
                IssueDate = cn.CreatedDate,
                OriginalAmount = cn.Amount,
                AppliedAmount = cn.AdjustedAmount ?? 0,
                AvailableBalance = cn.Amount - (cn.AdjustedAmount ?? 0),
                Status = (cn.Amount - (cn.AdjustedAmount ?? 0)) == 0 ? "FULLY APPLIED" : "AVAILABLE",
                AppliedTo = string.Join(", ", allCreditNoteDetails.Where(d => d.StudentCreditNoteId == cn.ID).Select(d => invoiceIdToCode.ContainsKey(d.InvoiceId) ? invoiceIdToCode[d.InvoiceId] : d.InvoiceId.ToString()))
            }).ToList();

            // Calculate totals - including advance payments
            var totalInvoices = ledgers.Where(x => x.PaymentTypeName == "Invoice" && x.DebitAmount != null && x.CreditAmount == null).Sum(x => x.DebitAmount ?? 0);
            var totalRefundInvoices = ledgers.Where(x => x.PaymentTypeName == "Invoice" && x.DebitAmount == null && x.CreditAmount != null).Sum(x => x.CreditAmount ?? 0);

            // Total payments should include both allocated payments (with InvoiceId), advance payments (without InvoiceId),
            // and receipts generated against credit notes. Hence, we must not exclude rows with CreditNoteId.
            // Fixed: Include entries where CreditNoteId is not null.
            var totalPayments = ledgers.Where(x => x.PaymentTypeName != "Invoice" && x.CreditAmount != null && (x.DebitAmount == null || x.DebitAmount == 0)).Sum(x => x.CreditAmount ?? 0);
            var totalRefundPayments = ledgers.Where(x => x.PaymentTypeName != "Invoice" && x.CreditAmount == null && x.DebitAmount != null).Sum(x => x.DebitAmount ?? 0);
            var creditsApplied = allCreditNoteDetails.Sum(x => x.Amount);
            var availableCredits = allCreditNotes.Sum(x => x.Amount - (x.AdjustedAmount ?? 0));

            // Calculate total balance
            // Updated: Exclude credit notes from total balance to match running balance
            // Previously creditsApplied were subtracted and availableCredits added back.
            // Now we only consider invoices and payments/refund payments.
            var totalBalance = (totalInvoices + totalRefundPayments) - (totalPayments + totalRefundInvoices);

            var summary = new LedgerSummaryViewModel
            {
                TotalInvoices = totalInvoices,
                TotalRefundInvoices = totalRefundInvoices,
                TotalPayments = totalPayments,
                TotalRefundPayments = totalRefundPayments,
                CreditNotesIssued = allCreditNotes.Sum(x => x.Amount),
                CreditsApplied = creditsApplied,
                AvailableCredits = availableCredits,
                TotalBalance = totalBalance
            };

            var person = db.People.FirstOrDefault(x => x.PersonID == PersonId);
            string currencySymbol = "";
            if (person?.LocationId != null)
            {
                currencySymbol = db.Currencies.Where(c => c.LocationId == person.LocationId).Select(c => c.Name).FirstOrDefault() ?? "";
            }

            return new PartnerLedgerPageViewModel
            {
                LedgerTransactions = transactions.OrderBy(x => x.Date).ToList(),
                CreditNotes = creditNoteDashboard,
                Summary = summary,
                FromDate = effectiveFromDate,
                ToDate = effectiveToDate,
                Status = Status,
                PersonId = PersonId,
                PersonName = person?.FullName,
                PersonCode = person?.Code,
                LocationName = person?.Location?.LocationName,
                LocationId = person?.LocationId ?? 0,
                CreditLimit = "-",
                PaymentTerms = "-",
                OpeningBalance = openingBalance,
                AvailableCreditBalance = creditNoteDashboard.Sum(x => x.AvailableBalance),
                CurrencySymbol = currencySymbol
            };
        }
     
        public decimal CalculateCreditAmountByCreditNote(int invoiceId, int creditNoteId)
        {
            var remainingInvoiceAmount = GetRemainingPayablesByInvoiceId(invoiceId);
            var creditNote = creditNoteService.GetForPaymentById(creditNoteId);
            decimal creditAmount = 0;

            if (creditNote != null)
            {
                if (creditNote.Amount > remainingInvoiceAmount)
                    creditAmount = remainingInvoiceAmount;
                else
                    creditAmount = creditNote.Amount;
            }
            return creditAmount;
        }

        public decimal GetRemainingPayablesByInvoiceId(int invoiceId)
        {
            var invoiceAmount = uow.GenericRepository<StudentLedger>().Table
                .Where(x => x.InvoiceId == invoiceId && x.PaymentTypeName == "Invoice")
                .FirstOrDefault()?.DebitAmount;
            var paidAmount = uow.GenericRepository<StudentLedger>().Table
                .Where(x => x.InvoiceId == invoiceId && x.PaymentTypeName != "Invoice")
                .ToList();
            var totalPayable = paidAmount.Count > 0 ? invoiceAmount - paidAmount.Sum(x => x.CreditAmount) : invoiceAmount;
            return totalPayable ?? 0;
        }

        public void UpdateInvoicePaymentStatus(int? invoiceId)
        {
            if (invoiceId == null) return;

            var invoice = uow.GenericRepository<Invoicing>().GetById(invoiceId);
            var totalPaidAmount = uow.GenericRepository<StudentLedger>().Table
                .Where(x => x.InvoiceId == invoiceId)
                .ToList();
            var remainingAmount = totalPaidAmount.Sum(x => x.DebitAmount ?? 0) - totalPaidAmount.Sum(x => x.CreditAmount ?? 0);

            if (remainingAmount == 0)
            {
                invoice.IsPaid = true;
                uow.GenericRepository<Invoicing>().Update(invoice);
                uow.SaveChanges();
            }
            else
            {
                // Ensure invoice is marked unpaid when there is any remaining balance
                if (invoice.IsPaid == true)
                {
                    invoice.IsPaid = false;
                    uow.GenericRepository<Invoicing>().Update(invoice);
                    uow.SaveChanges();
                }
            }
        }

        public void SendPaymentReceiptEmail(StudentLedger studentLedger)
        {
            var notifyEmail = correspondenceService.GetEmailMessagesByActionId(
                (int)Enumeration.CorrespondenceAction.SendReceiptEmail,
                studentLedger.LocationId ?? 0);

            if (notifyEmail == null) return;

            var locationSetting = uow.GenericRepository<LocationSetting>().Table
                .FirstOrDefault(x => x.LocationId == studentLedger.LocationId);

            var person = uow.GenericRepository<EF.Person>().Table
                .FirstOrDefault(x => x.PersonID == studentLedger.StudentId);

            // Create dictionary for replacements
            var replacements = new Dictionary<string, string>
            {
                {"{{Payment_Date}}", studentLedger.PaymentDate.ToString("dd/MM/yyyy")},
                {"{{Person_Code}}", person?.Code},
                {"{{Full_Name}}", person?.FullName},
                {"{{Transaction_Code}}", studentLedger.Code ?? ""},
                {"{{Invoice_Code}}", studentLedger.Invoicing?.Code ?? "Advance Payment"},
                {"{{Person_PhoneNumber}}", person?.Phone ?? ""},
                {"{{Nationality}}", person?.Nationality ?? ""},
                {"{{Passport_Number}}", person?.PassportNumber ?? ""},
                {"{{Currency}}", locationSetting?.Currency ?? ""},
                {"{{Company_Name}}", locationSetting?.CompanyName ?? ""},
                {"{{VATNo}}", locationSetting?.VATNo ?? ""},
                {"{{TRN}}", locationSetting?.VATNo ?? ""},
                {"{{locationsettings.TRN}}", locationSetting?.VATNo ?? ""},
                {"{{TaxNumberLabel}}", studentLedger.LocationId == (int)LocationEnum.Muscat ? "VAT No:" : "TRN:"},
                {"{{Payment_Method}}", studentLedger.PaymentTypeName ?? ""},
                {"{{Amount}}", studentLedger.CreditAmount?.ToString("N2") ?? "0.00"},
                {"{{Remarks}}", studentLedger.Remarks ?? "No Remarks"},
                {"{{Reference_Number}}", studentLedger.PaymentReferenceNumber ?? ""}
            };

            // Apply replacements
            var body = notifyEmail.EmailMessageBody;
            foreach (var replacement in replacements)
            {
                body = body.Replace(replacement.Key, replacement.Value);
            }

            // Generate PDF
            byte[] pdfBytes = null;
            string pdfFileName = $"Receipt_{studentLedger.Code}_{DateTime.Now:yyyyMMdd}.pdf";

            try
            {
                // Use HtmlToPdf for better rendering (install NReco.PdfGenerator package)
                var htmlToPdf = new NReco.PdfGenerator.HtmlToPdfConverter();
                htmlToPdf.Size = NReco.PdfGenerator.PageSize.A4;
                htmlToPdf.Margins = new NReco.PdfGenerator.PageMargins
                {
                    Top = 15,
                    Bottom = 15,
                    Left = 15,
                    Right = 15
                };

                // Better CSS support
                htmlToPdf.CustomWkHtmlArgs = "--print-media-type --disable-smart-shrinking";

                pdfBytes = htmlToPdf.GeneratePdf(body);
            }
            catch (Exception ex)
            {
                // Fallback to iTextSharp if needed
                try
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        var document = new Document(iTextSharp.text.PageSize.A4, 20, 20, 20, 20);
                        var writer = PdfWriter.GetInstance(document, memoryStream);

                        document.Open();

                        using (var htmlStream = new MemoryStream(Encoding.UTF8.GetBytes(body)))
                        {
                            XMLWorkerHelper.GetInstance().ParseXHtml(
                                writer,
                                document,
                                htmlStream,
                                Encoding.UTF8
                            );
                        }

                        document.Close();
                        pdfBytes = memoryStream.ToArray();
                    }
                }
                catch (Exception fallbackEx)
                {
                    Console.WriteLine($"PDF generation failed: {fallbackEx.Message}");
                    pdfBytes = null;
                }
            }

            // Handle recipients
            var recipients = !string.IsNullOrEmpty(person?.SecondaryEmail)
                ? $"{person.Email},{person.SecondaryEmail}"
                : person?.Email;

            if (!string.IsNullOrEmpty(recipients))
            {
                // Enhanced HTML email body
                var emailBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <title>Payment Receipt</title>
    <style type=""text/css"">
        body {{
            font-family: Arial, sans-serif;
            margin: 0;
            padding: 0;
            background-color: #f4f4f4;
            color: #333;
            line-height: 1.6;
        }}
        .email-container {{
            max-width: 600px;
            margin: 0 auto;
            background-color: #ffffff;
            border-radius: 8px;
            overflow: hidden;
            box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
        }}
        .header {{
            background: linear-gradient(135deg, #c20e1a 0%, #a00d15 100%);
            color: white;
            padding: 30px 20px;
            text-align: center;
        }}
        .header h1 {{
            margin: 0;
            font-size: 24px;
            font-weight: bold;
        }}
        .header p {{
            margin: 10px 0 0 0;
            font-size: 16px;
            opacity: 0.9;
        }}
        .content {{
            padding: 30px 20px;
        }}
        .greeting {{
            font-size: 18px;
            color: #333;
            margin-bottom: 20px;
        }}
        .transaction-details {{
            background-color: #f8f9fa;
            border-left: 4px solid #c20e1a;
            padding: 20px;
            margin: 20px 0;
            border-radius: 0 8px 8px 0;
        }}
        .transaction-details h3 {{
            margin: 0 0 15px 0;
            color: #c20e1a;
            font-size: 18px;
        }}
        .detail-row {{
            display: flex;
            justify-content: space-between;
            margin-bottom: 10px;
            padding: 8px 0;
            border-bottom: 1px solid #e9ecef;
        }}
        .detail-row:last-child {{
            border-bottom: none;
        }}
        .detail-label {{
            font-weight: bold;
            color: #495057;
            min-width: 120px;
        }}
        .detail-value {{
            color: #333;
            text-align: right;
        }}
        .amount-highlight {{
            background-color: #c20e1a;
            color: white;
            padding: 8px 12px;
            border-radius: 4px;
            font-weight: bold;
        }}
        .footer {{
            background-color: #f8f9fa;
            padding: 20px;
            text-align: center;
            border-top: 1px solid #e9ecef;
        }}
        .footer p {{
            margin: 5px 0;
            color: #6c757d;
        }}
        .company-name {{
            color: #c20e1a;
            font-weight: bold;
        }}
        .contact-info {{
            margin-top: 15px;
            font-size: 14px;
        }}
        .contact-info a {{
            color: #c20e1a;
            text-decoration: none;
        }}
        .contact-info a:hover {{
            text-decoration: underline;
        }}
        .attachment-notice {{
            background-color: #e3f2fd;
            border: 1px solid #2196f3;
            border-radius: 4px;
            padding: 15px;
            margin: 20px 0;
            color: #1976d2;
        }}
        .attachment-notice strong {{
            color: #0d47a1;
        }}
        @media only screen and (max-width: 600px) {{
            .email-container {{
                margin: 10px;
                border-radius: 4px;
            }}
            .detail-row {{
                flex-direction: column;
                align-items: flex-start;
            }}
            .detail-value {{
                text-align: left;
                margin-top: 5px;
            }}
        }}
    </style>
</head>
<body>
    <div class=""email-container"">
        <div class=""header"">
            <h1>Payment Confirmation</h1>
            <p>Your payment has been successfully processed</p>
        </div>
        
        <div class=""content"">
            <div class=""greeting"">
                Dear {person?.FullName},
            </div>
            
            <p>Thank you for your payment to <span class=""company-name"">The Myriad Dubai</span>. We have successfully processed your transaction and attached your payment receipt for your records.</p>
            
            <div class=""attachment-notice"">
                <strong>📎 Payment Receipt Attached</strong><br>
                Your detailed payment receipt is attached to this email for your records.
            </div>
            
            <div class=""transaction-details"">
                <h3>📋 Transaction Summary</h3>
                <div class=""detail-row"">
                    <span class=""detail-label"">Transaction #:</span>
                    <span class=""detail-value"">{studentLedger.Code}</span>
                </div>
                <div class=""detail-row"">
                    <span class=""detail-label"">Invoice #:</span>
                    <span class=""detail-value"">{studentLedger.Invoicing?.Code ?? "Advance Payment"}</span>
                </div>
                <div class=""detail-row"">
                    <span class=""detail-label"">Payment Date:</span>
                    <span class=""detail-value"">{studentLedger.PaymentDate:dd/MM/yyyy}</span>
                </div>
                <div class=""detail-row"">
                    <span class=""detail-label"">Payment Method:</span>
                    <span class=""detail-value"">{studentLedger.PaymentTypeName}</span>
                </div>
                <div class=""detail-row"">
                    <span class=""detail-label"">Amount Paid:</span>
                    <span class=""detail-value""><span class=""amount-highlight"">{locationSetting?.Currency} {studentLedger.CreditAmount?.ToString("N2")}</span></span>
                </div>
            </div>
            
            <p>Your payment has been applied to your account. If you have any questions about this transaction or need assistance with your account, please don't hesitate to contact our support team.</p>
            
            <p>We appreciate your business and look forward to serving you at <span class=""company-name"">The Myriad Dubai</span>.</p>
        </div>
        
        <div class=""footer"">
            <p class=""company-name"">The Myriad Dubai</p>
            <div class=""contact-info"">
                <p>📍 Plot: 8120287, Dubai International Academic City, Dubai, UAE</p>
                <p>📧 <a href=""mailto:info@themyriad.com"">info@themyriad.com</a> | 📞 <a href=""tel:+971800MYRIAD"">800 MYRIAD (697423)</a></p>
                <p>🌐 <a href=""https://www.themyriad.com"" target=""_blank"">www.themyriad.com</a></p>
            </div>
        </div>
    </div>
</body>
</html>";

                if (pdfBytes != null)
                {
                    emailService.SendEmailWithAttachment(
                        notifyEmail.EmailMessageSubject?.ToString() ?? "Payment Receipt",
                        emailBody,
                        true,
                        recipients,
                        notifyEmail.EmailMessageSenderID,
                        pdfBytes,
                        pdfFileName,
                        "application/pdf"
                    );
                }
                else
                {
                    // Fallback: Send HTML email without attachment
                    emailService.SendEmail(
                        notifyEmail.EmailMessageSubject?.ToString() ?? "Payment Receipt",
                        body,
                        true,
                        recipients,
                        notifyEmail.EmailMessageSenderID
                    );
                }
            }
        }

        public void SendPaymentNotification(StudentLedger studentLedger)
        {
            string title;
            string description;

            // Reverse payment notification
            if (studentLedger.IsReversedPayment)
            {
                var amount = studentLedger.DebitAmount ?? 0;
                title = "Payment Reversed";
                description = $"Your payment of {amount:N2} has been reversed. Reference: {studentLedger.Code}.";
            }
            // Refund / payout to resident 
            else if (studentLedger.LookupId == (int)PaymentLookup.PaymentRefund && (studentLedger.DebitAmount ?? 0) > 0)
            {
                var amount = studentLedger.DebitAmount ?? 0;
                title = "Refund Payment";
                description = $"A refund payment of {amount:N2} has been processed to your account. Reference: {studentLedger.Code}.";
            }
            // Normal / advance payment 
            else
            {
                var amount = studentLedger.CreditAmount ?? 0;
                title = "New Payment";

                if (studentLedger.InvoiceId != null)
                {
                    description = $"Your payment of {amount:N2} has been paid against invoice: {studentLedger.Code}.";
                }
                else
                {
                    description = $"Your advance payment of {amount:N2} has been received.";
                }
            }

            notificationService.SendNotification(
                null,
                studentLedger.StudentId,
                "Student",
                title,
                description,
                "/Student/Payment/PaymentList",
                PMS.Common.Globals.User.Email);
        }

        public StudentLedger CreatePartnerLedgerPayment(decimal DebitAmount, int PaymentTypeId, string Remarks, int PersonId, int LocationId)
        {

            var payment = new EF.StudentLedger
            {
                PaymentDate = DateTime.Now,
                // Refund payment code should follow new lookup-based sequence like RCT-REF-GEN-TMD-0001
                Code = GetMaxPaymentRefundCodeString(LocationId),
                InvoiceId = null,
                StudentId = PersonId,
                DebitAmount = DebitAmount,
                CreditAmount = null,
                Remarks = Remarks,
                PaymentTypeId = PaymentTypeId,
                PaymentTypeName = paymentTypesService.GetPaymentById(PaymentTypeId).PaymentName,
                IsApproved = true,
                CreatedBy = PMS.Common.Globals.User.ID,
                CreatedDate = DateTime.Now,
                LocationId = LocationId,
                ApprovedBy = PMS.Common.Globals.User.ID,
                // Tag the lookup for this refund payment so future sequences and reporting can filter correctly
                LookupId = (int)PaymentLookup.PaymentRefund
            };

            uow.GenericRepository<EF.StudentLedger>().Insert(payment);
            uow.SaveChanges();

            // Audit Log
            CreateAuditLog(payment);

            // Refund Payment Voucher (as per existing controller logic)
            CreateRevPayVoucher(payment);

            // Notification
            SendPaymentNotification(payment);

            return payment;
        }

        #region ------------------------- ** Audit Log ** -------------------------

        public void CreateAuditLog(StudentLedger studentLedger)
        {
            EF.AuditLog auditLog = new EF.AuditLog()
            {
                AuditType = (int)Enumeration.AuditType.Create,
                ActionId = (int)Enumeration.CorrespondenceAction.CreatePayment,
                PK = studentLedger.Id.ToString(),
                UserId = PMS.Common.Globals.User.ID,
                TableName = "StudentLedger - Payments",
                Reference = studentLedger.Code,
                UserName = PMS.Common.Globals.User.Name + " - " + PMS.Common.Globals.User.Email,
                PersonId = studentLedger.StudentId,
            };
            auditLogsService.AddAuditLog(auditLog);
        }

        #endregion


        public void ProcessPayment(StudentLedger studentLedger)
        {
            // Reduce EF overhead during the hot write path
            var ctx = uow.Context;
            bool prevAutoDetect = ctx.Configuration.AutoDetectChangesEnabled;
            bool prevValidate = ctx.Configuration.ValidateOnSaveEnabled;
            ctx.Configuration.AutoDetectChangesEnabled = false;
            ctx.Configuration.ValidateOnSaveEnabled = false;


            // Validate payment amount if InvoiceId is provided
            if (studentLedger.InvoiceId != null)
            {
                if (!ValidatePaymentAmount(studentLedger.InvoiceId, studentLedger.CreditAmount))
                {
                    throw new Exception("Payment amount exceeds the remaining balance for the invoice.");
                }

                ValidateInvoicePaymentOrder(studentLedger);
            }

            // Get payment type name
            var paymentType = paymentTypesService.GetPaymentById((int)studentLedger.PaymentTypeId).PaymentName;

            // Handle credit note
            if (studentLedger.CreditNoteId != null && studentLedger.CreditNoteId > 0)
            {
                studentLedger.CreditAmount = CalculateCreditAmountByCreditNote(studentLedger.InvoiceId ?? 0, studentLedger.CreditNoteId ?? 0);
                creditNoteService.SaveCreditDetail(studentLedger.InvoiceId ?? 0, studentLedger.CreditAmount ?? 0, studentLedger.CreditNoteId ?? 0);
            }
            else
            {
                studentLedger.CreditNoteId = null;
            }

            // Generate receipt code and set payment details
            var receiptCode = GenerateReceiptCode((int)studentLedger.LocationId);
            studentLedger.Code = receiptCode;
            studentLedger.PaymentTypeName = paymentType;
            studentLedger.ApprovedBy = studentLedger.IsApproved == true ? PMS.Common.Globals.User.ID : studentLedger.ApprovedBy;
            studentLedger.PaymentReferenceNumber = studentLedger.PaymentReferenceNumber;
            studentLedger.LookupId = (int)Enumeration.PaymentLookup.Payment;

            // Save the payment
            uow.GenericRepository<StudentLedger>().Insert(studentLedger);
            uow.SaveChanges();

            // Update invoice status if InvoiceId is provided
            if (studentLedger.InvoiceId != null)
            {
                UpdateInvoicePaymentStatus(studentLedger.InvoiceId);
            }

            // Create voucher
            if (studentLedger.CreditNoteId != null && studentLedger.CreditNoteId > 0)
            {
                CreateCreditNotePaymentVoucher(studentLedger);
            }
            else if (studentLedger.InvoiceId == null)
            {
                CreateAdvancePaymentVoucher(studentLedger);
            }
            else
            {
                CreatePayVoucher(studentLedger);
            }

            // Create audit log
            CreateAuditLog(studentLedger);

            // Restore EF defaults before any downstream async/non-critical ops
            ctx.Configuration.AutoDetectChangesEnabled = prevAutoDetect;
            ctx.Configuration.ValidateOnSaveEnabled = prevValidate;

            // Send email/notification
            // Fire-and-forget email to avoid blocking request
#if !DEBUG
            try { System.Threading.Tasks.Task.Run(() => SendPaymentReceiptEmail(studentLedger)); } catch { }
            SendPaymentNotification(studentLedger);
#endif
        }

        public bool ValidatePaymentAmount(int? invoiceId, decimal? amount)
        {
            if (invoiceId == null || amount == null) return true;

            var checkAmount = uow.GenericRepository<StudentLedger>().Table
                .Where(y => y.InvoiceId == invoiceId && y.PaymentTypeName == "Invoice")
                .FirstOrDefault();

            if (checkAmount != null)
            {
                var totalInvoiceAmount = checkAmount.DebitAmount;
                var totalPaidAmount = uow.GenericRepository<StudentLedger>().Table
                    .Where(x => x.InvoiceId == invoiceId && x.PaymentTypeName != "Invoice")
                    .ToList()
                    .Sum(x => x.CreditAmount);
                var nowPaying = amount + totalPaidAmount;

                return totalInvoiceAmount >= nowPaying;
            }

            return true;
        }

        public void ValidateInvoicePaymentOrder(StudentLedger studentLedger)
        {
            if (studentLedger.InvoiceId == null) return;

            var currentInvoice = uow.GenericRepository<EF.Invoicing>().Table
                .FirstOrDefault(x => x.Id == studentLedger.InvoiceId);

            var olderThanTwoMonths = DateTime.Now.AddMonths(-2);

            var miscInvoices = uow.GenericRepository<EF.Invoicing>().Table
                .Where(x => x.StudentId == studentLedger.StudentId &&
                            x.IsPaid != true &&
                            x.Id != studentLedger.InvoiceId &&
                            x.InvoiceTypeId == (int)InvoiceTypes.Miscellaneous &&
                            x.Refunded == null &&
                            x.InvoiceDate < olderThanTwoMonths)
                .ToList();

            if (miscInvoices.Any())
            {
                var minMiscInvoiceDate = miscInvoices.Min(x => x.InvoiceDate);

                if (currentInvoice.InvoiceDate > minMiscInvoiceDate)
                {
                    throw new Exception("Miscellaneous invoice pending for more than two months.");
                }
            }

            var rentalInvoices = uow.GenericRepository<EF.Invoicing>().Table
                .Where(x => x.StudentId == studentLedger.StudentId &&
                            x.InvoiceDate >= new DateTime(2024, 03, 26) &&
                            x.IsPaid != true && x.Refunded == null &&
                            x.InvoiceTypeId == (int)InvoiceTypes.Rental &&
                            x.Id != studentLedger.InvoiceId &&
                            (studentLedger.DebitAmount != 0 || studentLedger.DebitAmount >= 0))
                .ToList();

            foreach (var invoice in rentalInvoices)
            {
                if (currentInvoice.InvoiceDate > invoice.InvoiceDate)
                {
                    throw new Exception("Please pay previous rental invoices first.");
                }
            }
        }


        public void ProcessPaymentUpdate(StudentLedger studentLedger)
        {
            // Reduce EF overhead during the hot write path
            var ctx = uow.Context;
            bool prevAutoDetect = ctx.Configuration.AutoDetectChangesEnabled;
            bool prevValidate = ctx.Configuration.ValidateOnSaveEnabled;
            ctx.Configuration.AutoDetectChangesEnabled = false;
            ctx.Configuration.ValidateOnSaveEnabled = false;

            try
            {
                var oldPayment = uow.GenericRepository<StudentLedger>().Table.AsNoTracking().FirstOrDefault(x => x.Id == studentLedger.Id);
                if (oldPayment == null)
                {
                    throw new Exception("Payment record not found.");
                }

                var oldInvoiceId = oldPayment.InvoiceId;

                // Perform invoice-specific validations only if InvoiceId is provided
                if (studentLedger.InvoiceId != null)
                {
                    var currentInvoice = uow.GenericRepository<EF.Invoicing>().Table
                        .FirstOrDefault(x => x.Id == studentLedger.InvoiceId);

                    if (currentInvoice == null)
                    {
                        throw new Exception("Invalid Invoice ID provided.");
                    }

                    var olderThanTwoMonths = DateTime.Now.AddMonths(-2);

                    var miscInvoices = uow.GenericRepository<EF.Invoicing>().Table
                        .Where(x => x.StudentId == studentLedger.StudentId &&
                                    x.IsPaid != true &&
                                    x.Refunded == null &&
                                    x.InvoiceTypeId == (int)InvoiceTypes.Miscellaneous &&
                                    x.InvoiceDate < olderThanTwoMonths &&
                                    x.Id != studentLedger.InvoiceId)
                        .ToList();

                    if (miscInvoices.Any())
                    {
                        var minMiscInvoiceDate = miscInvoices.Min(x => x.InvoiceDate);
                        if (currentInvoice.InvoiceDate > minMiscInvoiceDate)
                        {
                            throw new Exception("Miscellaneous invoice pending for more than two months.");
                        }
                    }

                    var rentalInvoices = uow.GenericRepository<EF.Invoicing>().Table
                        .Where(x => x.StudentId == studentLedger.StudentId &&
                                    x.InvoiceDate >= new DateTime(2024, 03, 26) &&
                                    x.IsPaid != true &&
                                    x.Refunded == null &&
                                    x.InvoiceTypeId == (int)InvoiceTypes.Rental &&
                                    !(studentLedger.DebitAmount == 0 || studentLedger.DebitAmount < 0) &&
                                    x.Id != studentLedger.InvoiceId)
                        .ToList();

                    foreach (var invoice in rentalInvoices)
                    {
                        if (currentInvoice.InvoiceDate > invoice.InvoiceDate)
                        {
                            throw new Exception("Please pay previous invoices first.");
                        }
                    }
                }

                // Update payment details
                studentLedger.PaymentTypeName = paymentTypesService.GetPaymentById((int)studentLedger.PaymentTypeId).PaymentName;
                studentLedger.PaymentReferenceNumber = studentLedger.PaymentReferenceNumber;
                studentLedger.ApprovedBy = studentLedger.IsApproved == true ? PMS.Common.Globals.User.ID : studentLedger.ApprovedBy;

                // Preserve LookupId for refund payments
                if (oldPayment.LookupId == (int)PaymentLookup.PaymentRefund)
                {
                    studentLedger.LookupId = oldPayment.LookupId;
                }

                uow.GenericRepository<StudentLedger>().Update(studentLedger);
                uow.SaveChanges();

                // Update or create voucher
                var existingVoucher = uow.GenericRepository<Voucher>().Table.FirstOrDefault(v => v.ReferenceId == studentLedger.Id);

                if (existingVoucher != null)
                {
                    UpdatePaymentVoucher(studentLedger, auditLogsService);
                }
                else
                {
                    if (studentLedger.InvoiceId == null)
                    {
                        // Create Direct Advance Payment Voucher
                        CreateAdvancePaymentVoucher(studentLedger);
                    }
                    else
                    {
                        // Create Direct Advance Payment Voucher
                        CreatePayVoucher(studentLedger);
                    }
                }

                // Update invoice status if InvoiceId has changed or is present
                if (oldInvoiceId != studentLedger.InvoiceId)
                {
                    UpdateInvoicePaymentStatus(oldInvoiceId); // Update the old invoice status
                }

                if (studentLedger.InvoiceId != null)
                {
                    UpdateInvoicePaymentStatus(studentLedger.InvoiceId); // Update the new invoice status
                }

                // Insert Audit Log
                var difference = PMS.Common.Classes.Common.DetailedCompare<EF.StudentLedger>(oldPayment, studentLedger);
                List<EF.AuditLogDetail> auditLogDetails = new List<EF.AuditLogDetail>();

                EF.AuditLog auditLog = new EF.AuditLog()
                {
                    AuditType = (int)Enumeration.AuditType.Update,
                    ActionId = (int)Enumeration.CorrespondenceAction.UpdatePayment,
                    PK = studentLedger.Id.ToString(),
                    UserId = PMS.Common.Globals.User.ID,
                    TableName = "StudentLedger - Payments",
                    Reference = studentLedger.Code,
                    UserName = PMS.Common.Globals.User.Name + " - " + PMS.Common.Globals.User.Email,
                    PersonId = studentLedger.StudentId,
                    AuditLogDetails = difference
                };

                auditLogsService.AddAuditLog(auditLog);
            }
            finally
            {
                // Restore EF defaults
                ctx.Configuration.AutoDetectChangesEnabled = prevAutoDetect;
                ctx.Configuration.ValidateOnSaveEnabled = prevValidate;
            }
        }

        public List<PaymentVM> GetChildPayments(int parentId)
        {
            var children = uow.Context.Set<V_GetPaymentList>().AsNoTracking()
                .Where(x => x.ParentId == parentId)
                .Select(x => new PaymentVM
                {
                    Id = x.Id,
                    TransactionCode = x.TransactionCode,
                    Location = x.Location,
                    MyriadID = x.MyriadID,
                    FullName = x.FullName,
                    InvoiceId = x.InvoiceId == null ? null : x.InvoiceId.ToString(),
                    InvoiceCode = x.InvoiceCode,
                    InvoiceDate = x.InvoiceDate,
                    PaymentDate = x.PaymentDate,
                    CreatedDate = x.CreatedDate,
                    Remarks = x.Remarks,
                    Amount = x.Amount ?? 0,
                    CreditAmount = x.CreditAmount ?? 0,
                    DebitAmount = x.DebitAmount ?? 0,
                    PaymentName = x.PaymentName,
                    PaymentReferenceNumber = x.PaymentReferenceNumber,
                    IsApproved = x.IsApproved,
                    CreatedBy = x.CreatedBy,
                    ApprovedBy = x.ApprovedBy,
                    CreditNoteId = x.CreditNoteId,
                    VoucherId = x.VoucherId ?? 0
                }).ToList();
            return children;
        }

        #region ------------------------- ** credit card process of deposit paymnet ** -------------------------
        public EF.StudentLedger CreateDepositPaymentLedger(int invoiceId, int personId,
       decimal netAmount, int locationId, int paymentMethodId, string TranRef, int createdByUserId)
        {
            try
            {
                var (paymentTypeId, paymentTypeName) = GetPaymentType(paymentMethodId, locationId);
                var paymentCode = GenerateReceiptCode(locationId);

                var paymentLedger = new EF.StudentLedger
                {
                    PaymentDate = DateTime.Now,
                    Code = paymentCode,
                    InvoiceId = invoiceId,
                    StudentId = personId,
                    DebitAmount = null,
                    CreditAmount = netAmount,
                    Remarks = paymentCode,
                    PaymentTypeId = paymentTypeId,
                    PaymentTypeName = paymentTypeName,
                    IsApproved = true,
                    CreatedBy = createdByUserId,
                    CreatedDate = DateTime.Now,
                    LocationId = locationId,
                    ApprovedBy = createdByUserId,
                    PaymentReferenceNumber = TranRef,
                    LookupId = (int)Enumeration.PaymentLookup.Payment
                };

                return paymentLedger;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error creating deposit payment ledger: {ex.Message}", ex);
            }
        }

        private (int PaymentTypeId, string PaymentTypeName) GetPaymentType(int paymentMethodId, int locationId)
        {
            if (paymentMethodId == (int)PaymentMethodType.BankTransfer)
            {
                // Bank Transfer (CBD)
                var bankTransferPayment = uow.GenericRepository<EF.PaymentType>().Table
                    .Where(p => (p.PayementName.Contains("Bank Transfer") || p.PayementName.Contains("CBD"))
                                && p.LocationId == locationId)
                    .FirstOrDefault();

                return (bankTransferPayment?.PaymentId ?? (int)PaymentTypeEnum.TMDBankTransferCBD,
                        bankTransferPayment?.PayementName ?? "Bank Transfer (CBD)");
            }
            else
            {
                // Online Payment Link
                var onlinePayment = uow.GenericRepository<EF.PaymentType>().Table
                    .Where(p => p.PayementName.Contains("Payment Gateway")
                                && p.LocationId == locationId)
                    .FirstOrDefault();

                return (onlinePayment?.PaymentId ?? (int)PaymentTypeEnum.TMDPayGateway,
                        onlinePayment?.PayementName ?? "Payment Gateway");
            }
        }

        public void CreateOnlinePayVoucher(StudentLedger studentLedger, int createdByUserId)
        {
            var request = new VoucherCreationRequest
            {
                VoucherType = VoucherType.Payment,
                BaseVoucherData = new BaseVoucherData
                {
                    VoucherDate = studentLedger.PaymentDate,
                    ReferenceId = studentLedger.Id,
                    StudentId = studentLedger.StudentId,
                    Remarks = studentLedger.Remarks,
                    CreatedDate = studentLedger.CreatedDate,
                    CreatedBy = studentLedger.CreatedBy,
                    LocationId = studentLedger.LocationId ?? 0,
                    TransactionType = "Payment",
                    UserName = studentLedger.CreatedBy == createdByUserId ? "System - Online Booking" : Common.Globals.User.Name + " - " + Common.Globals.User.Email
                },
                PaymentData = studentLedger,
                IsRefund = false,
                IsOnlineBooking = true
            };

            voucherService.CreateVoucherWithDetails(request, auditLogsService);
        }
        public void CreateAuditLogForOnlinePayment(StudentLedger studentLedger)
        {
            EF.AuditLog auditLog = new EF.AuditLog()
            {
                AuditType = (int)Enumeration.AuditType.Create,
                ActionId = (int)Enumeration.CorrespondenceAction.CreatePayment,
                PK = studentLedger.Id.ToString(),
                UserId = studentLedger.CreatedBy,
                TableName = "StudentLedger - Payments",
                Reference = studentLedger.Code,
                UserName = "System - Online Booking",
                PersonId = studentLedger.StudentId,
                TimeStamp = DateTime.Now,
            };
            auditLogsService.AddAuditLog(auditLog);
        }
        #endregion


        #region ------------------------- ** Reverse Payment ** -------------------------

        public StudentLedger ReversePayment(int paymentId, decimal amount, int paymentTypeId, string remarks)
        {
            var ctx = uow.Context;

            var original = ctx.StudentLedgers.FirstOrDefault(x => x.Id == paymentId);
            if (original == null)
                throw new Exception("Payment not found.");

            // Reverse payment is only allowed for TMD (Dubai) location
            if (original.LocationId != (int)LocationEnum.Dubai)
                throw new Exception("Reverse payment is allowed only for TMD location.");

            if (!original.IsApproved)
                throw new Exception("Only approved payments can be reversed.");

            // Do not allow reversing debit/refund payments
            if (original.DebitAmount.HasValue && original.DebitAmount.Value > 0)
                throw new Exception("Only normal credit payments can be reversed.");

            if (original.IsReversedPayment)
                throw new Exception("This payment has already been reversed.");

            if (amount <= 0)
                throw new Exception("Reverse amount must be greater than zero.");

            if (original.CreditAmount.HasValue && amount > original.CreditAmount.Value)
                throw new Exception("Reverse amount cannot exceed the original paid amount.");

            var paymentType = paymentTypesService.GetPaymentById(paymentTypeId);
            if (paymentType == null)
                throw new Exception("Invalid payment method selected.");

            var now = DateTime.Now;

            var reverseLedger = new EF.StudentLedger
            {
                PaymentDate = now,
                Code = "R-" + original.Code,
                InvoiceId = original.InvoiceId,
                StudentId = original.StudentId,
                DebitAmount = amount,
                CreditAmount = null,
                Remarks = remarks,
                PaymentTypeId = paymentTypeId,
                PaymentTypeName = paymentType.PaymentName,
                IsApproved = true,
                CreatedBy = PMS.Common.Globals.User.ID,
                CreatedDate = now,
                LocationId = original.LocationId,
                ApprovedBy = PMS.Common.Globals.User.ID,
                PaymentReferenceNumber = original.PaymentReferenceNumber,
                LookupId = (int)Enumeration.PaymentLookup.PaymentRefund,
                ParentId = original.Id,
                IsReversedPayment = true
            };

            original.IsReversedPayment = true;

            uow.GenericRepository<EF.StudentLedger>().Insert(reverseLedger);
            uow.SaveChanges();

            // Create audit log & voucher, and send notification similar to other refund payments
            CreateAuditLog(reverseLedger);
            CreateRevPayVoucher(reverseLedger);
            SendPaymentNotification(reverseLedger);

            // Update linked invoice payment status after reversal
            if (reverseLedger.InvoiceId != null)
            {
                UpdateInvoicePaymentStatus(reverseLedger.InvoiceId);
            }

            return reverseLedger;
        }

        #endregion


        #region ------------------------- ** Student Portal ** -------------------------
        public decimal InvoicePayableAmount(int Id)
        {
            decimal totalPayable = 0;
            var Invoiceamount = uow.GenericRepository<StudentLedger>().Table.Where(x => x.InvoiceId == Id && x.PaymentTypeName == "Invoice").FirstOrDefault().DebitAmount ?? 0;
            var PaidAmount = uow.GenericRepository<StudentLedger>().Table.Where(x => x.InvoiceId == Id && x.PaymentTypeName != "Invoice").ToList();
            totalPayable = (decimal)(PaidAmount.Count > 0 ? Invoiceamount - PaidAmount.Sum(x => x.CreditAmount) : Invoiceamount);
            return totalPayable;
        }
        #endregion
    }
}
