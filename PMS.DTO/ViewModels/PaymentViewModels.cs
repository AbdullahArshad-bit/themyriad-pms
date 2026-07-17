using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.DTO.ViewModels.PaymentViewModels
{
    public class PaymentListVM
    {
        public int PaymentId { get; set; }
        public string PaymentName { get; set; }
        public string Code { get; set; }
        public bool IsActive { get; set; }
        public string AccountName { get; set; }
    }
    public class AddPaymentTypeVM
    {
        public int LocationId { get; set; }
        public int PaymentId { get; set; }

        [Required, Display(Name = "Method Name")]
        public string PaymentName { get; set; }

        [Required, Display(Name = "Method Code")]
        public string Code { get; set; }
        public string KeyCode { get; set; }

        [Required, Display(Name = "Active")]
        public bool IsActive { get; set; }

        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }

        [Required, Display(Name = "Chart of Account")]
        public int AccountId { get; set; }
    }
    public class PaymentVM
    {
        public int Id { get; set; }
        public DateTime CreatedDate { get; set; }

        public string TransactionCode { get; set; }
        public string InvoiceId { get; set; }
        public DateTime PaymentDate { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public string Remarks { get; set; }
        public decimal? Amount { get; set; }
        public decimal? CreditAmount { get; set; }
        public decimal? DebitAmount { get; set; }
        public bool Status { get; set; }
        public string PaymentName { get; set; }
        public string Location { get; set; }
        public string MyriadID { get; set; }
        public string FullName { get; set; }
        public bool IsApproved { get; set; }
        public string CreatedBy { get; set; }
        public string ApprovedBy { get; set; }
        public string InvoiceCode {  get; set; } 
        public string PaymentReferenceNumber {  get; set; } 
        public int? CreditNoteId { get; set; }
        public int VoucherId { get; set; }
        public bool HasChildren { get; set; }
        public int? LocationId { get; set; }
        public string Currency { get; set; }
        public bool IsReversedPayment { get; set; }
    }
    public class PaymentBinding
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int StudentId { get; set; }
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
    public class PaymentResponse
    {
        public List<PaymentVM> PaymentingList { get; set; }
        public int TotalRecords { get; set; }
        public int RecordsFiltered { get; set; }
    }

    public class InvoiceAllocationDTO
    {
        public int InvoiceId { get; set; }
        public decimal Amount { get; set; }
    }

    // grouped models removed
    public class Search
    {
        public string value { get; set; }
        public string regex { get; set; }
        public string column { get; set; }
    }
}
