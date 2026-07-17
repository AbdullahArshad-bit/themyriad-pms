using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.DTO.ViewModels.DashboardViewModel
{
    public class Dashboard
    {
        public int? LocationId { get; set; }
        public Nullable<int> TotalBedSpaces { get; set; }
        public Nullable<int> TotalBedSpaceOccupied { get; set; }
        public double? TotalBedSpaceOccupancyPercent { get; set; }
        public Nullable<int> ReservedButNotCheckedin { get; set; }
        public Nullable<int> BookingsNotAssignedPlacement { get; set; }
        public Nullable<int> TodaysCheckedIn { get; set; }
        public Nullable<int> TodaysCheckedOut { get; set; }
        public Nullable<int> ToBeCheckedinToday { get; set; }
        public Nullable<int> ToBeCheckedOutToday { get; set; }
        public Nullable<int> ContractsToBeSigned { get; set; }
        public Nullable<int> ContractsToBeGenerated { get; set; }
        public Nullable<decimal> TotalPaymentsThisMonth { get; set; }
        public Nullable<decimal> TotalPaymentsThisYear { get; set; }

    }



    public class PendingFeeAssesment
    {
        public Nullable<int> PersonID { get; set; }
        public string FullName { get; set; }
        public string code { get; set; }
        public string LastPaid { get; set; }
        public Nullable<int> LocationId { get; set; }
    } 
    public class unpaidInvoice
    {
        public int? InvoiceId { get; set; }
        public int? StudentId { get; set; }
        public string Personcode { get; set; }
        public string FullName { get; set; }
        public DateTime ?InvoiceDate { get; set; }
        public Nullable<System.DateTime> CheckOut { get; set; }
        public Nullable<System.DateTime> TillDate { get; set; }
        public string PaidStatus { get; set; }
        public string Ispaid { get; set; }
        public Nullable<int> LocationId { get; set; }

    }
}
