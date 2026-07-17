using PMS.Common.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace PMS.DTO.ViewModels
{
    public class BreadCrumb
    {
        public string Link { get; set; }
        public string Title { get; set; }
    }

    public class ImageUploadVM
    {
        [MaxFileSize(1 * 1024 * 1024, ErrorMessage = "Maximum allowed image size is {0}MB")]
        [AllowdExtensions(ErrorMessage = "Only png, jpg, jpeg image files are allowed.", Extensions = "png,jpg,jpeg")]
        public HttpPostedFileBase ImageFile { get; set; }
        public List<string> SavedImages { get; set; }
    }

    public class SelectListVM
    {
        public string Text { get; set; }
        public string Value { get; set; }
        public int? OrderBy { get; set; }
        public int LocationId { get; set; }
        public string RoomTypeName { get; set; }
        public string BuildingName { get; set; }
        public string FloorName { get; set; }
        public string BedName { get; set; }
        public string RoomName { get; set; }
        public int? FrequencyId { get; set; }
        public int BedSpaceID { get; set; }
        public int? MinDuration { get; set; }
        public DateTime? TermEndDate { get; set; }




    }
    public class DropDownViewModel
    {
        public string Name { get; set; }
        public int Id { get; set; }
    }

    // Partner Ledger Composite ViewModels
    public class LedgerTransactionViewModel
    {
        public DateTime Date { get; set; }
        public string Reference { get; set; }
        public string Type { get; set; } // Invoice, Payment, Credit Note, Credit Applied, Opening
        public string Description { get; set; }
        public decimal? Debit { get; set; }
        public decimal? Credit { get; set; }
        public string CreditNoteReference { get; set; } // For credit applied rows
        public decimal RunningBalance { get; set; }
        // Added for Partner Ledger columns
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string PaymentType { get; set; }
        public string Status { get; set; }
        // Added for Refund action wiring
        public int? InvoiceId { get; set; }
        public int? InvoiceTypeId { get; set; }
        public bool? IsPaid { get; set; }
        public bool? Refunded { get; set; }
        public int? ParentInvoiceId { get; set; }
    }

    public class CreditNoteDashboardViewModel
    {
        public string CreditNoteCode { get; set; }
        public DateTime IssueDate { get; set; }
        public decimal OriginalAmount { get; set; }
        public decimal AppliedAmount { get; set; }
        public decimal AvailableBalance { get; set; }
        public string Status { get; set; } // AVAILABLE, FULLY APPLIED
        public string AppliedTo { get; set; } // Invoice reference(s)
    }

    public class LedgerSummaryViewModel
    {
        public decimal TotalInvoices { get; set; }
        public decimal TotalRefundInvoices { get; set; }
        public decimal TotalPayments { get; set; }
        public decimal TotalRefundPayments { get; set; }
        public decimal CreditNotesIssued { get; set; }
        public decimal CreditsApplied { get; set; }
        public decimal AvailableCredits { get; set; }
        public decimal TotalBalance { get; set; }
    }

    public class PartnerLedgerPageViewModel
    {
        public List<LedgerTransactionViewModel> LedgerTransactions { get; set; }
        public List<CreditNoteDashboardViewModel> CreditNotes { get; set; }
        public LedgerSummaryViewModel Summary { get; set; }
        // Filters
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public bool Status { get; set; }
        public int PersonId { get; set; }
        public string PersonName { get; set; }
        public string CreditLimit { get; set; }
        public string PaymentTerms { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal AvailableCreditBalance { get; set; }
        public string PersonCode { get; set; }
        public string LocationName { get; set; }
        public int LocationId { get; set; }
        public string CurrencySymbol { get; set; }
    }

}
