using PMS.EF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.DTO.ViewModels
{
    public class InvoicingVM
    {
        public int Id { get; set; }
        public string Value { get; set; }
        public decimal Amount { get; set; }
    }
    public class OutputInvoicingVM
    {
        public int Id { get; set; }
        public bool Status { get; set; }
        public decimal NetAmount { get; set; }
        public string Remarks { get; set; }
        public DateTime InvoiceDate { get; set; }
        public DateTime? CheckInDate { get; set; }
        public DateTime? CheckOutDate { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string ServiceType { get; set; }
        public int? ServiceTypeId { get; set; }
        public bool IsDepositService { get; set; }
        public string Occupancy { get; set; }
        public int ServiceId { get; set; }
        public string ServiceName { get; set; }
        public decimal ServicePrice { get; set; }
        public int OccupancyId { get; set; }
        public int? TaxId { get; set; }
        public int TermId { get; set; }
        public int FrequencyId { get; set; }
        public int LocationID { get; set; }
    }
    public class InvoicingDetailVM
    {
        public int Id { get; set; }
        public int InvvoicingId { get; set; }
        public int PriceConfig { get; set; }
        public int ServiceId { get; set; }
        public string ServiceName { get; set; }
        public decimal Price { get; set; }
        public string Description { get; set; }
        public string TaxesIds { get; set; }
        public string TaxesName { get; set; }
        public decimal? TaxesAmount { get; set; }
        public decimal? DiscountPercentage { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal? TotalAmount { get; set; }
        public DateTime? ToDate { get; set; }
        public DateTime? FromDate { get; set; }
        public string FromDateString { get; set; }
        public string ToDateString { get; set; }
        public decimal BaseServicePrice { get; set; }
        public int OccupancyId { get; set; }
        public int LocationId { get; set; }
    }

    public class UnpaidInvoiceVM
    {
        public int Id { get; set; }
        public DateTime InvoiceDate { get; set; }
        public string Code { get; set; }
        public decimal InvoiceAmount { get; set; }
        public decimal AmountDue { get; set; }
    }
    public class InvoicingDetailCalenderVM
    {
        public DateTime Month { get; set; }
        public int Days { get; set; }
        public int DetailId { get; set; }
    }

    public class InvoiceViewModel
    {
        public long Id { get; set; }
        public string Code { get; set; }
        public string Location { get; set; }
        public int LocationId { get; set; }
        public string MyriadID { get; set; }
        public string FullName { get; set; }
        public DateTime InvoiceDate { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime CreatedDate { get; set; }
        public int StudentId { get; set; }
        public string Remarks { get; set; }
        public decimal? NetAmount { get; set; }
        public bool Status { get; set; }
        public bool? isPaid { get; set; }
        public bool? Refunded { get; set; }
        public int? InvoiceTypeId { get; set; }
        public int? ParentInvoiceId { get; set; }
        public string CreatedBy { get; set; }
        public string ApprovedBy { get; set; }
        public decimal? TotalDiscountAmount { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public decimal? PendingBalance { get; set; }
        public decimal? TotalBalanceOfResident { get; set; }
        public string ServiceName { get; set; }
        public int VoucherId { get; set; }
        public string VoucherCode { get; set; }
        public int PriceConfigID { get; set; }
    }
    public class InvoicingBinding
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? InvoiceTypeId { get; set; }
        public string query { get; set; }
        public string start { get; set; }
        public string length { get; set; }
        public string draw { get; set; }
        public Search search { get; set; }
        public string orderBy { get; set; }
        public string orderDir { get; set; }
        public List<string> SelectedColumns { get; set; }
        //public string QueryBy { get; set; }

    }
    public class InvoicingsResponse
    {
        public List<InvoiceViewModel> InvoicingList { get; set; }
        public int TotalRecords { get; set; }
        public int RecordsFiltered { get; set; }
    }
    public class Search
    {
        public string value { get; set; }
        public string regex { get; set; }
        public string column { get; set; }
    }

    public class LastInvoiceCheckResult
    {
        public string Status { get; set; } // "Invoiced", "NoInvoice", or "DateConflicted"
        public int LastInvoiceId { get; set; }
    }

}
