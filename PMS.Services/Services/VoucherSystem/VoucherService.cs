using PMS.Common.Classes;
using PMS.DTO.ViewModels;
using PMS.DTO.ViewModels.PaymentViewModels;
using PMS.EF;
using PMS.Repository.UnitOfWork;
using PMS.Services.Services.AuditLogs;
using PMS.Services.Services.VehicleSubscription;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using PMS.Services.Helpers;
using PMS.Services.Services.Service;
using PMS.DTO.ViewModels.ServiceViewModels;
using System.Data.Entity;

namespace PMS.Services.Services.VoucherSystem
{
    public class VoucherService : IVoucherService
    {
        private readonly UnitOfWork<PMSEntities> uow;
        private readonly IServicesService servicesService;



        public VoucherService(UnitOfWork<PMSEntities> _uow, IServicesService _servicesService)
        {
            uow = _uow;
            servicesService = _servicesService;
        }


        public Voucher GetById(int? id)
        {
            return uow.GenericRepository<Voucher>().GetById(id);
        }

        public List<VoucherDetailVM> GetVoucherDetail(int voucherId)
        {
            var voucher = uow.GenericRepository<Voucher>().GetById(voucherId);
            var details = uow.GenericRepository<VoucherDetail>().GetAll(x => x.VoucherId == voucherId);

            List<VoucherDetailVM> list = new List<VoucherDetailVM>();
            foreach (var item in details)
            {
                var account = uow.GenericRepository<EF.ChartOfAccount>().Table.FirstOrDefault(x => x.Id == item.AccountId);
                VoucherDetailVM model = new VoucherDetailVM
                {
                    VoucherId = item.VoucherId,
                    AccountId = item.AccountId,
                    AccountName = account?.Name ?? "N/A",
                    DebitAmount = item.DebitAmount,
                    CreditAmount = item.CreditAmount,
                    Remarks = item.Remarks
                };
                list.Add(model);
            }
            return list;
        }

        public void CreateVoucherWithDetails(VoucherCreationRequest request, IAuditLogsService auditLogsService = null)
        {
            // Create voucher
            var voucher = CreateVoucher(request.BaseVoucherData);
            uow.GenericRepository<Voucher>().Insert(voucher);
            uow.SaveChanges();

            // Pre-load services and taxes for bulk operations to avoid N+1 query problem
            Dictionary<int, AddServiceVM> serviceCache = null;
            Dictionary<int, EF.Tax> taxCache = null;
            
            if (request.VoucherType == VoucherType.Invoice || request.VoucherType == VoucherType.RevOrRefInvoice)
            {
                if (request.InvoicingDetails != null && request.InvoicingDetails.Any())
                {
                    // Batch load all services
                    var serviceIds = request.InvoicingDetails.Select(d => d.ServiceId).Where(id => id > 0).Distinct().ToList();
                    if (serviceIds.Any())
                    {
                        serviceCache = servicesService.GetServicesByIds(serviceIds);
                    }
                    // Ensure cache is never null
                    if (serviceCache == null)
                    {
                        serviceCache = new Dictionary<int, AddServiceVM>();
                    }

                    // Batch load all taxes
                    var taxIds = request.InvoicingDetails
                        .Where(d => !string.IsNullOrEmpty(d.TaxesIds) && int.TryParse(d.TaxesIds, out _))
                        .Select(d => int.Parse(d.TaxesIds))
                        .Distinct()
                        .ToList();
                    
                    if (taxIds.Any())
                    {
                        try
                        {
                            // Use a separate context for read-only operations to avoid lock contention
                            using (var readOnlyContext = new PMSEntities())
                            {
                                readOnlyContext.Configuration.AutoDetectChangesEnabled = false;
                                readOnlyContext.Configuration.ValidateOnSaveEnabled = false;
                                
                                taxCache = readOnlyContext.Taxes
                                    .AsNoTracking()
                                    .Where(t => taxIds.Contains(t.TaxId))
                                    .ToDictionary(t => t.TaxId, t => t);
                            }
                        }
                        catch
                        {
                            // Fallback to unit of work context
                            taxCache = uow.GenericRepository<EF.Tax>().Table
                                .AsNoTracking()
                                .Where(t => taxIds.Contains(t.TaxId))
                                .ToDictionary(t => t.TaxId, t => t);
                        }
                    }
                }
            }

            // Create voucher details based on type
            List<VoucherDetail> voucherDetails;
            switch (request.VoucherType)
            {
                case VoucherType.Invoice:
                case VoucherType.RevOrRefInvoice:
                    voucherDetails = CreateInvoiceVoucherDetails(voucher.VoucherId, request.InvoicingData, request.InvoicingDetails, request.IsRefund, serviceCache, taxCache);
                    break;

                case VoucherType.Payment:
                case VoucherType.RefundPayment:
                    voucherDetails = CreatePaymentVoucherDetails(voucher.VoucherId, request.PaymentData, request.IsRefund, request.IsCreditNotePayment, request.IsAdvancePayment);
                    break;

                case VoucherType.CreditNote:
                    voucherDetails = CreateCreditNoteVoucherDetails(voucher.VoucherId, request.CreditNoteData);
                    break;

                default:
                    throw new ArgumentException("Invalid voucher type");
            }

            foreach (var voucherDetail in voucherDetails)
            {
                uow.GenericRepository<VoucherDetail>().Insert(voucherDetail);
            }
            uow.SaveChanges();

            // Create Audit Log
            if (auditLogsService != null && request.IsOnlineBooking == true)
            {
                EF.AuditLog voucherAuditLog = new EF.AuditLog()
                {
                    AuditType = (int)Enumeration.AuditType.Create,
                    ActionId = (int)Enumeration.CorrespondenceAction.CreateVoucher,
                    PK = voucher.VoucherId.ToString(),
                    UserId = voucher.CreatedBy,
                    TableName = "Voucher",
                    Reference = voucher.Code,
                    UserName = request.BaseVoucherData.UserName,
                    PersonId = request.BaseVoucherData.StudentId,
                };
                auditLogsService.AddAuditLog(voucherAuditLog);
            }
            else
            {
                EF.AuditLog voucherAuditLog = new EF.AuditLog()
                {
                    AuditType = (int)Enumeration.AuditType.Create,
                    ActionId = (int)Enumeration.CorrespondenceAction.CreateVoucher,
                    PK = voucher.VoucherId.ToString(),
                    UserId = voucher.CreatedBy,
                    TableName = "Voucher",
                    Reference = voucher.Code,
                    UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                    PersonId = request.BaseVoucherData.StudentId,
                };
                auditLogsService.AddAuditLog(voucherAuditLog);
            }
        }

        private Voucher CreateVoucher(BaseVoucherData data)
        {
            var voucherCode = GetMaxVoucherCodeString(data.LocationId);
            return new Voucher()
            {
                Code = voucherCode,
                VoucherDate = data.VoucherDate,
                ReferenceId = data.ReferenceId,
                StudentId = data.StudentId,
                Remarks = data.Remarks,
                CreatedDate = data.CreatedDate,
                CreatedBy = data.CreatedBy,
                LocationId = data.LocationId,
                TransactionType = data.TransactionType
            };
        }

        private List<VoucherDetail> CreateInvoiceVoucherDetails(int voucherId, Invoicing invoicing, ICollection<InvoicingDetail> invoicingDetails, bool isRefund = false, Dictionary<int, AddServiceVM> serviceCache = null, Dictionary<int, EF.Tax> taxCache = null)
        {
            var voucherDetails = new List<VoucherDetail>();

            // Pre-load services if cache not provided
            if (serviceCache == null)
            {
                var serviceIds = invoicingDetails.Select(d => d.ServiceId).Where(id => id > 0).Distinct().ToList();
                serviceCache = servicesService.GetServicesByIds(serviceIds);
                // Ensure cache is never null
                if (serviceCache == null)
                {
                    serviceCache = new Dictionary<int, AddServiceVM>();
                }
            }

            // Pre-load taxes if cache not provided
            if (taxCache == null)
            {
                var taxIds = invoicingDetails
                    .Where(d => !string.IsNullOrEmpty(d.TaxesIds) && int.TryParse(d.TaxesIds, out _))
                    .Select(d => int.Parse(d.TaxesIds))
                    .Distinct()
                    .ToList();
                
                if (taxIds.Any())
                {
                    try
                    {
                        // Use a separate context for read-only operations to avoid lock contention
                        using (var readOnlyContext = new PMSEntities())
                        {
                            readOnlyContext.Configuration.AutoDetectChangesEnabled = false;
                            readOnlyContext.Configuration.ValidateOnSaveEnabled = false;
                            
                            taxCache = readOnlyContext.Taxes
                                .AsNoTracking()
                                .Where(t => taxIds.Contains(t.TaxId))
                                .ToDictionary(t => t.TaxId, t => t);
                        }
                    }
                    catch
                    {
                        // Fallback to unit of work context
                        taxCache = uow.GenericRepository<EF.Tax>().Table
                            .AsNoTracking()
                            .Where(t => taxIds.Contains(t.TaxId))
                            .ToDictionary(t => t.TaxId, t => t);
                    }
                }
                else
                {
                    taxCache = new Dictionary<int, EF.Tax>();
                }
            }

            // 1. Add Service entries (Credit)
            foreach (var invoicingDetail in invoicingDetails)
            {
                AddServiceVM service = null;
                
                // Try to get service from cache first
                if (!serviceCache.TryGetValue(invoicingDetail.ServiceId, out service))
                {
                    // Fallback: if not in cache, try individual lookup
                    // This handles cases where batch loading might have failed or missed the service
                    try
                    {
                        service = servicesService.GetServicesById(invoicingDetail.ServiceId);
                        // Add to cache for potential future use in this method
                        if (service != null)
                        {
                            serviceCache[invoicingDetail.ServiceId] = service;
                        }
                    }
                    catch
                    {
                        // If individual lookup also fails, log but continue
                        // We'll skip this service detail
                        System.Diagnostics.Debug.WriteLine($"Warning: Could not load service {invoicingDetail.ServiceId} for voucher detail");
                    }
                }

                // Only create voucher detail if we have the service with AccountId
                if (service != null)
                {
                    var totalAmount = Math.Abs(invoicingDetail.TotalAmount ?? 0M);
                    var taxAmount = Math.Abs(invoicingDetail.TaxAmount ?? 0M);
                    var serviceAmount = Math.Round(totalAmount - taxAmount, 2);

                    var voucherDetail = new VoucherDetail()
                    {
                        VoucherId = voucherId,
                        AccountId = service.AccountId ?? 0,
                        Remarks = invoicingDetail.ServiceName
                    };

                    if (isRefund)
                    {
                        voucherDetail.DebitAmount = serviceAmount;
                        voucherDetail.CreditAmount = null;
                    }
                    else
                    {
                        voucherDetail.DebitAmount = null;
                        voucherDetail.CreditAmount = serviceAmount;
                    }

                    voucherDetails.Add(voucherDetail);
                }
            }

            // 2. Add Tax entries (Credit)
            foreach (var invoicingDetail in invoicingDetails)
            {
                var taxAmount = Math.Abs(invoicingDetail.TaxAmount ?? 0M);
                if (!string.IsNullOrEmpty(invoicingDetail.TaxesIds) && (invoicingDetail.TaxAmount > 0 || invoicingDetail.TaxAmount < 0))
                {
                    if (int.TryParse(invoicingDetail.TaxesIds, out int taxId))
                    {
                        if (taxCache.TryGetValue(taxId, out var tax))
                        {
                            var voucherDetail = new VoucherDetail()
                            {
                                VoucherId = voucherId,
                                AccountId = tax.AccountId ?? 0,
                                Remarks = $"Tax - {tax.TaxName}"
                            };

                            if (isRefund)
                            {
                                voucherDetail.DebitAmount = taxAmount;
                                voucherDetail.CreditAmount = null;
                            }
                            else
                            {
                                voucherDetail.DebitAmount = null;
                                voucherDetail.CreditAmount = taxAmount;
                            }

                            voucherDetails.Add(voucherDetail);
                        }
                    }
                }
            }

            // 3. Add Accounts Receivable entry (Debit)
            var accountReceivableId = GetAccountReceivableId(invoicing.LocationId);
            if (accountReceivableId.HasValue)
            {
                var netAmount = Math.Abs(invoicing.NetAmount);
                var voucherDetail = new VoucherDetail()
                {
                    VoucherId = voucherId,
                    AccountId = accountReceivableId.Value,
                    Remarks = "Accounts Receivable"
                };

                if (isRefund)
                {
                    voucherDetail.DebitAmount = null;
                    voucherDetail.CreditAmount = netAmount;
                }
                else
                {
                    voucherDetail.DebitAmount = netAmount;
                    voucherDetail.CreditAmount = null;
                }

                voucherDetails.Add(voucherDetail);
            }

            return voucherDetails;
        }

        private List<VoucherDetail> CreatePaymentVoucherDetails(int voucherId, StudentLedger studentLedger, bool isRefund = false, bool isCreditNotePayment = false, bool isAdvancePayment = false)
        {
            var voucherDetails = new List<VoucherDetail>();
            var amount = Math.Abs((isRefund ? (studentLedger.DebitAmount ?? 0M) : (studentLedger.CreditAmount ?? 0M)));

            if (isCreditNotePayment)
            {
                // Credit Note Payment Voucher Entries
                // Dr. Liability account (Advance Payment/Unutilized payment)
                var advancePaymentAccountId = GetAdvancePaymentAccountId(studentLedger.LocationId);
                if (advancePaymentAccountId.HasValue)
                {
                    var debitVoucherDetail = new VoucherDetail()
                    {
                        VoucherId = voucherId,
                        AccountId = advancePaymentAccountId.Value,
                        DebitAmount = amount,
                        CreditAmount = null,
                        Remarks = "Credit Note Payment - Liability Account"
                    };
                    voucherDetails.Add(debitVoucherDetail);
                }

                // Cr. Accounts Receivable
                var accountReceivableId = GetAccountReceivableId(studentLedger.LocationId);
                if (accountReceivableId.HasValue)
                {
                    var creditVoucherDetail = new VoucherDetail()
                    {
                        VoucherId = voucherId,
                        AccountId = accountReceivableId.Value,
                        DebitAmount = null,
                        CreditAmount = amount,
                        Remarks = "Credit Note Payment - Accounts Receivable"
                    };
                    voucherDetails.Add(creditVoucherDetail);
                }
            }

            else if (isAdvancePayment)
            {
                // Direct Advance Payment Voucher Entries
                // Dr. Payment Method Account
                var paymentType = uow.GenericRepository<EF.PaymentType>().Table
                    .FirstOrDefault(pt => pt.PaymentId == studentLedger.PaymentTypeId);

                if (paymentType?.AccountId != null)
                {
                    var debitVoucherDetail = new VoucherDetail()
                    {
                        VoucherId = voucherId,
                        AccountId = paymentType.AccountId.Value,
                        DebitAmount = amount,
                        CreditAmount = null,
                        Remarks = $"Advance Payment - {paymentType.PayementName}"
                    };
                    voucherDetails.Add(debitVoucherDetail);
                }

                // Cr. Accounts Receivable
                var advancePaymentAccountId = GetAccountReceivableId(studentLedger.LocationId);
                if (advancePaymentAccountId.HasValue)
                {
                    var creditVoucherDetail = new VoucherDetail()
                    {
                        VoucherId = voucherId,
                        AccountId = advancePaymentAccountId.Value,
                        DebitAmount = null,
                        CreditAmount = amount,
                        Remarks = "Advance Payment - Accounts Receivable"
                    };
                    voucherDetails.Add(creditVoucherDetail);
                }
            }

            else
            {

                // Regular Payment Voucher Entries (existing logic)
                // 1. Add Payment entry (Debit)
                var paymentType = uow.GenericRepository<EF.PaymentType>().Table.FirstOrDefault(pt => pt.PaymentId == studentLedger.PaymentTypeId);
                if (paymentType?.AccountId == null || paymentType.AccountId == 0)
                {
                    throw new InvalidOperationException(
                        $"Payment method '{paymentType?.PayementName}' has no Chart of Account assigned. " +
                        "Please configure in payment types."
                    );
                }
                if (paymentType != null)
                {
                    var paymentVoucherDetail = new VoucherDetail()
                    {
                        VoucherId = voucherId,
                        AccountId = paymentType.AccountId ?? 0,
                        Remarks = $"Payment - {paymentType.PayementName}"
                    };

                    if (isRefund)
                    {
                        paymentVoucherDetail.DebitAmount = null;
                        paymentVoucherDetail.CreditAmount = amount;
                    }
                    else
                    {
                        paymentVoucherDetail.DebitAmount = amount;
                        paymentVoucherDetail.CreditAmount = null;
                    }

                    voucherDetails.Add(paymentVoucherDetail);
                }

                // 2. Add Accounts Receivable entry (Credit)
                var accountReceivableId = GetAccountReceivableId(studentLedger.LocationId);
                if (accountReceivableId.HasValue)
                {
                    var accountsReceivableDetail = new VoucherDetail()
                    {
                        VoucherId = voucherId,
                        AccountId = accountReceivableId.Value,
                        Remarks = isRefund ? "Accounts Receivable - Refund Payment"
                                          : "Accounts Receivable - Payment Received"
                    };

                    if (isRefund)
                    {
                        // For refunds: Debit Accounts Receivable
                        accountsReceivableDetail.DebitAmount = amount;
                        accountsReceivableDetail.CreditAmount = null;
                    }
                    else
                    {
                        // For payments: Credit Accounts Receivable
                        accountsReceivableDetail.DebitAmount = null;
                        accountsReceivableDetail.CreditAmount = amount;
                    }

                    voucherDetails.Add(accountsReceivableDetail);
                }
            }

            return voucherDetails;
        }

        private List<VoucherDetail> CreateCreditNoteVoucherDetails(int voucherId, StudentCreditNote creditNote)
        {
            var voucherDetails = new List<VoucherDetail>();
            var amount = Math.Abs(creditNote.Amount);
            // Determine the credit note type
            var creditNoteType = (CrdNoteTypeLookup)creditNote.Type;
            switch (creditNoteType)
            {
                case CrdNoteTypeLookup.Refund:
                case CrdNoteTypeLookup.Gift:
                case CrdNoteTypeLookup.ReferralGift:
                    // Dr. Revenue (Gift/Referral/Discount account)
                    var revenueAccountId = GetRevenueAccountId(creditNote.LocationId);
                    if (revenueAccountId.HasValue)
                    {
                        voucherDetails.Add(new VoucherDetail
                        {
                            VoucherId = voucherId,
                            AccountId = revenueAccountId.Value,
                            DebitAmount = amount,
                            CreditAmount = null,
                            Remarks = $"Credit Note - {GetCreditNoteTypeDescription(creditNoteType)}"
                        });
                    }
                    // Cr. Credit Note Payment Liability Account (unutilized funds)
                    var unutilizedFundsId = GetAdvancePaymentAccountId(creditNote.LocationId);
                    if (unutilizedFundsId.HasValue)
                    {
                        voucherDetails.Add(new VoucherDetail
                        {
                            VoucherId = voucherId,
                            AccountId = unutilizedFundsId.Value,
                            DebitAmount = null,
                            CreditAmount = amount,
                            Remarks = "Credit Note - Liability"
                        });
                    }
                    break;

                case CrdNoteTypeLookup.AdvancePayment:
                    // Dr. Payment Method Account
                    if (creditNote.PaymentTypeId.HasValue)
                    {
                        var paymentType = uow.GenericRepository<EF.PaymentType>().Table
                            .FirstOrDefault(pt => pt.PaymentId == creditNote.PaymentTypeId.Value);
                        if (paymentType?.AccountId != null)
                        {
                            voucherDetails.Add(new VoucherDetail
                            {
                                VoucherId = voucherId,
                                AccountId = paymentType.AccountId.Value,
                                DebitAmount = amount,
                                CreditAmount = null,
                                Remarks = $"Advance Payment - {paymentType.PayementName}"
                            });
                        }
                    }

                    // Cr. Advance Payment Liability Account (unutilized funds)
                    var advancePaymentAccountId = GetAdvancePaymentAccountId(creditNote.LocationId);
                    if (advancePaymentAccountId.HasValue)
                    {
                        voucherDetails.Add(new VoucherDetail
                        {
                            VoucherId = voucherId,
                            AccountId = advancePaymentAccountId.Value,
                            DebitAmount = null,
                            CreditAmount = amount,
                            Remarks = "Advance Payment Liability"
                        });
                    }
                    break;
                default:
                    throw new ArgumentException($"Invalid credit note type: {creditNoteType}");
            }
            return voucherDetails;
        }

        public void UpdateVoucherWithDetails(int voucherId, VoucherCreationRequest request, IAuditLogsService auditLogsService = null)
        {
            // Get existing voucher for audit
            var existingVoucher = uow.GenericRepository<Voucher>().GetByIdAsNoTracking(x => x.VoucherId == voucherId);
            var oldVoucherForAudit = existingVoucher != null ?
                uow.GenericRepository<Voucher>().GetByIdAsNoTracking(x => x.VoucherId == existingVoucher.VoucherId) : null;

            // Update voucher
            var voucher = uow.GenericRepository<Voucher>().GetById(voucherId);
            if (voucher != null)
            {
                // Update voucher fields
                voucher.VoucherDate = request.BaseVoucherData.VoucherDate;
                voucher.StudentId = request.BaseVoucherData.StudentId;
                voucher.Remarks = request.BaseVoucherData.Remarks;
                voucher.UpdatedDate = DateTime.Now;
                voucher.UpdatedBy = request.BaseVoucherData.UpdatedBy;
                voucher.LocationId = request.BaseVoucherData.LocationId;

                uow.GenericRepository<Voucher>().Update(voucher);

                // Delete existing voucher details
                var existingVoucherDetails = uow.GenericRepository<VoucherDetail>().Table
                    .Where(vd => vd.VoucherId == voucherId).ToList();
                foreach (var voucherDetail in existingVoucherDetails)
                {
                    uow.GenericRepository<VoucherDetail>().Delete(voucherDetail);
                }

                uow.SaveChanges();

                // Pre-load services and taxes for bulk operations to avoid N+1 query problem
                Dictionary<int, AddServiceVM> serviceCache = null;
                Dictionary<int, EF.Tax> taxCache = null;
                
                if (request.VoucherType == VoucherType.Invoice || request.VoucherType == VoucherType.RevOrRefInvoice)
                {
                    if (request.InvoicingDetails != null && request.InvoicingDetails.Any())
                    {
                        // Batch load all services
                        var serviceIds = request.InvoicingDetails.Select(d => d.ServiceId).Where(id => id > 0).Distinct().ToList();
                        if (serviceIds.Any())
                        {
                            serviceCache = servicesService.GetServicesByIds(serviceIds);
                        }
                        // Ensure cache is never null
                        if (serviceCache == null)
                        {
                            serviceCache = new Dictionary<int, AddServiceVM>();
                        }

                        // Batch load all taxes
                        var taxIds = request.InvoicingDetails
                            .Where(d => !string.IsNullOrEmpty(d.TaxesIds) && int.TryParse(d.TaxesIds, out _))
                            .Select(d => int.Parse(d.TaxesIds))
                            .Distinct()
                            .ToList();
                        
                        if (taxIds.Any())
                        {
                            try
                            {
                                // Use a separate context for read-only operations to avoid lock contention
                                using (var readOnlyContext = new PMSEntities())
                                {
                                    readOnlyContext.Configuration.AutoDetectChangesEnabled = false;
                                    readOnlyContext.Configuration.ValidateOnSaveEnabled = false;
                                    
                                    taxCache = readOnlyContext.Taxes
                                        .AsNoTracking()
                                        .Where(t => taxIds.Contains(t.TaxId))
                                        .ToDictionary(t => t.TaxId, t => t);
                                }
                            }
                            catch
                            {
                                // Fallback to unit of work context
                                taxCache = uow.GenericRepository<EF.Tax>().Table
                                    .AsNoTracking()
                                    .Where(t => taxIds.Contains(t.TaxId))
                                    .ToDictionary(t => t.TaxId, t => t);
                            }
                        }
                    }
                }

                // Create new voucher details based on type
                List<VoucherDetail> voucherDetails;
                switch (request.VoucherType)
                {
                    case VoucherType.Invoice:
                        voucherDetails = CreateInvoiceVoucherDetails(voucher.VoucherId, request.InvoicingData, request.InvoicingDetails, false, serviceCache, taxCache);
                        break;
                    case VoucherType.RevOrRefInvoice:
                        voucherDetails = CreateInvoiceVoucherDetails(voucher.VoucherId, request.InvoicingData, request.InvoicingDetails, request.IsRefund, serviceCache, taxCache);
                        break;
                    case VoucherType.Payment:
                    case VoucherType.RefundPayment:
                        voucherDetails = CreatePaymentVoucherDetails(voucher.VoucherId, request.PaymentData, request.IsRefund, request.IsCreditNotePayment, request.IsAdvancePayment);
                        break;
                    default:
                        throw new ArgumentException("Invalid voucher type");
                }

                // Insert new voucher details
                foreach (var voucherDetail in voucherDetails)
                {
                    uow.GenericRepository<VoucherDetail>().Insert(voucherDetail);
                }
                uow.SaveChanges();

                // Create Audit Log for update
                if (auditLogsService != null && oldVoucherForAudit != null)
                {
                    var voucherDifference = Common.Classes.Common.DetailedCompare<EF.Voucher>(oldVoucherForAudit, voucher);
                    EF.AuditLog voucherAuditLog = new EF.AuditLog()
                    {
                        AuditType = (int)Enumeration.AuditType.Update,
                        ActionId = (int)Enumeration.CorrespondenceAction.UpdateVoucher,
                        PK = voucher.VoucherId.ToString(),
                        UserId = Common.Globals.User.ID,
                        TableName = "Voucher",
                        Reference = voucher.Code,
                        UserName = Common.Globals.User.Name + " - " + Common.Globals.User.Email,
                        PersonId = request.BaseVoucherData.StudentId,
                        AuditLogDetails = voucherDifference
                    };
                    auditLogsService.AddAuditLog(voucherAuditLog);
                }
            }
        }

        private int? GetAccountReceivableId(int? locationId)
        {
            if (!locationId.HasValue) return null;
            var accounts = LocationAccountsCacheHelper.GetLocationAccounts(locationId.Value, uow);
            return accounts?.AccountReceivableId;
        }

        private int? GetAccountPayableId(int? locationId)
        {
            if (!locationId.HasValue) return null;
            var accounts = LocationAccountsCacheHelper.GetLocationAccounts(locationId.Value, uow);
            return accounts?.AccountPayableId;
        }

        private int? GetRevenueAccountId(int? locationId)
        {
            if (!locationId.HasValue) return null;
            var accounts = LocationAccountsCacheHelper.GetLocationAccounts(locationId.Value, uow);
            return accounts?.RevenueAccountId;
        }

        private int? GetAdvancePaymentAccountId(int? locationId)
        {
            if (!locationId.HasValue) return null;
            var accounts = LocationAccountsCacheHelper.GetLocationAccounts(locationId.Value, uow);
            return accounts?.AdvancePaymentAccountId;
        }

        public string GetMaxVoucherCodeString(int locationId)
        {
            lock (this)
            {
                PMSEntities db1 = new PMSEntities();
                var location = db1.Locations.Find(locationId);
                var maxcode = GetMaxVoucherCode(locationId);
                string value = String.Format("{0:D4}", maxcode);


                string prefix = "JR";

                var Code = prefix.Trim() + "-" + location.Prefix + "-" + value;
                return Code;
            }
        }

        public static int GetMaxVoucherCode(int locationId)
        {
            using (var db1 = new PMSEntities())
            {
                int code = 0;

                var vouchers = db1.Vouchers.Where(x => x.Code != null && x.LocationId == locationId);

                if (vouchers.Any())
                {
                    var nowithVoucher = Convert.ToDecimal(vouchers.AsEnumerable()
                        .Select(x => new { Number = Convert.ToDecimal(x.Code.Split('-').Last()) })
                        .Max(x => x.Number)) + 1;
                    code = (int)nowithVoucher;
                }
                else
                {
                    code = 1;
                }
                return code;
            }
        }

        private string GetCreditNoteTypeDescription(CrdNoteTypeLookup creditNoteType)
        {
            switch (creditNoteType)
            {
                case CrdNoteTypeLookup.Refund:
                    return "Refund";
                case CrdNoteTypeLookup.Gift:
                    return "Gift";
                case CrdNoteTypeLookup.ReferralGift:
                    return "Referral Gift";
                case CrdNoteTypeLookup.AdvancePayment:
                    return "Advance Payment";
                default:
                    return "Credit Note";
            }
        }
    }
}
