using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.DTO.ViewModels
{
    public class StudentCreditNoteVm
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public int LocationId { get; set; }
        public string Location { get; set; }
        public string Student { get; set; }
        public int TypeId { get; set; }
        public string Type { get; set; }
        public int StudentId { get; set; }
        public int Percentage { get; set; }
        public decimal Amount { get; set; }
        public string Reason { get; set; }
        public int Status { get; set; }
        public bool IsUtilized { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public string ApprovedBy { get; set; }
        public int? PaymentTypeId { get; set; }
        public decimal AdjustedAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public string CurrencyName { get; set; }
        public int? VoucherId { get; set; }
        public int CreatedById { get; set; }

    }
    public class CreditNoteType
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
