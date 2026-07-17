using PMS.Common.Classes;
using PMS.EF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.DTO.ViewModels
{
    public class VoucherDetailVM
    {
        public int VoucherId { get; set; }
        public int AccountId { get; set; }
        public string AccountName { get; set; }
        public decimal? DebitAmount { get; set; }
        public decimal? CreditAmount { get; set; }
        public string Remarks { get; set; }
    }
    public class VoucherBinding
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
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
    public class VouchersResponse
    {
        public List<VoucherDetailVM> VoucherList { get; set; }
        public int TotalRecords { get; set; }
        public int RecordsFiltered { get; set; }
    }

    public class VoucherCreationRequest
    {
        public VoucherType VoucherType { get; set; }
        public BaseVoucherData BaseVoucherData { get; set; }
        public Invoicing InvoicingData { get; set; }
        public ICollection<InvoicingDetail> InvoicingDetails { get; set; }
        public StudentLedger PaymentData { get; set; }
        public StudentCreditNote CreditNoteData { get; set; }
        public bool IsRefund { get; set; } = false;
        public bool IsCreditNotePayment { get; set; } = false;
        public bool IsAdvancePayment { get; set; } = false;
        public bool IsOnlineBooking { get; set; } = false;

    }

    public class BaseVoucherData
    {
        public DateTime VoucherDate { get; set; }
        public int ReferenceId { get; set; }
        public int StudentId { get; set; }
        public string Remarks { get; set; }
        public string TransactionType { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int CreatedBy { get; set; }
        public int? UpdatedBy { get; set; }
        public int LocationId { get; set; }
        public string UserName { get; set; }

    }

}
