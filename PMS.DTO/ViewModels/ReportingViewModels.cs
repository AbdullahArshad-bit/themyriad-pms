using PMS.EF;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.DTO.ViewModels.ReportingViewModels
{
    public class ReportingVM
    {
        public int TotalRecords { get; set; }
        public string Title { get; set; }
        public Nullable<int> PersonID { get; set; }
        public string PersonCode { get; set; }
        public string Fullname { get; set; }
        public string Universiry { get; set; }
        public string Gender { get; set; }
        public string Bedname { get; set; }
        public string RoomName { get; set; }
        public string AccessibilityRequest { get; set; }
        public string BedOccupied { get; set; }
        public string Occupancy { get; set; }
        public string RoomType { get; set; }
        public string BookingNumber { get; set; }
        public string Status { get; set; }
        public DateTime? BookingCreatedDate { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public DateTime? MoveIn { get; set; }
        public DateTime? MoveOut { get; set; }
        public DateTime? CheckIn { get; set; }
        public DateTime? CheckOut { get; set; }
        public string invoiceCode { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public string Services { get; set; }
        public Nullable<decimal> ServiceNetPrice { get; set; }
        public Nullable<decimal> InvoiceNetAmount { get; set; }
        public string Code { get; set; }
        public DateTime? PaymentDate { get; set; }
        public Nullable<decimal> PaymentAmount { get; set; }
        public Nullable<bool> InvoiceStatus { get; set; }
        public string PaymentTypeName { get; set; }
        public string Nationality { get; set; }
        public string email { get; set; }
        public string phone { get; set; }
        public int LocationId { get; set; }
    }
    public class ServicesDetailVM
    {
        public string code { get; set; }
        public string ServiceName { get; set; }
        public string LocationName { get; set; }
        public string PostedBy { get; set; }
        public string ApprovedBy { get; set; }
        public string MyriadID { get; set; }
        public string FullName { get; set; }
        public string InvoiceDate { get; set; }
        public string Remarks { get; set; }
        public decimal? ServicePrice { get; set; }
        public decimal? TaxAmount { get; set; }
        public decimal? ServiceNetAmount { get; set; }
        public Nullable<DateTime> FromDate { get; set; }
        public Nullable<DateTime> ToDate { get; set; }
        public string CompanyName { get; set; }
        public string VATNo { get; set; }
        public int LocationId { get; set; }

    }
    public class RevenueDetailVM
    {
        public string Name { get; set; }
        public string Code { get; set; }
        public decimal Revenue { get; set; }
        public string CompanyName { get; set; }
        public string VATNo { get; set; }
        public int LocationId { get; set; }
    }
    public class ComplaintHistoryVM
    {
        public int TicketId { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Status { get; set; }
        public string CreatedBy { get; set; }
        public string CreatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public string UpdatedDate { get; set; }
        public int LocationId { get; set; }
        public string LocationName { get; set; }

    }
    public class InvoiceDetailReportVM
    {
        public int InvoiceId { get; set; }
        public string Invoice { get; set; }
        public string Location { get; set; }
        public string ResidentID { get; set; }
        public string PhoneNumber { get; set; }
        public string InvoiceDate { get; set; }
        public string Status { get; set; }
        public string Description { get; set; }
        public string Service { get; set; }
        public decimal Amount { get; set; }
        public string TaxesName { get; set; }
        public decimal DiscountPercentage { get; set; }

        public string NetAmount { get; set; }
        public string Remarks { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal? TotalDiscountAmount { get; set; }
        public decimal NetTotal { get; set; }
        public int InvoiceTypeId { get; set; }
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public string CompanyName { get; set; }
        public string VATNo { get; set; }
        public int LocationId { get; set; }
        public bool? IsStudent { get; set; }
    }
    public class SubInvoicePrintReportVM
    {
        public int InvoiceId { get; set; }
        public string code { get; set; }
        public decimal Amount { get; set; }
        public decimal NetAmount { get; set; }
        public int LocationId { get; set; }

    }
    public class PaymentTransactionDetailReportVM
    {
        public int StudentLedgerId { get; set; }
        public string TransactionCode { get; set; }
        public string Location { get; set; }
        public string ResidentID { get; set; }
        public string PhoneNumber { get; set; }
        public string PaymentDate { get; set; }
        public string status { get; set; }
        public string PaymentMethod { get; set; }
        public decimal? Amount { get; set; }
        public string Remarks { get; set; }
        public string CompanyName { get; set; }
        public string VATNo { get; set; }
        public string Code { get; set; }
        public int LocationId { get; set; }
        public bool? IsStudent { get; set; }

    }
    public class ResidentBinding
    {
        public int? studentid { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string query { get; set; }
        public string start { get; set; }
        public string length { get; set; }
        public string draw { get; set; }
        public Search search { get; set; } = new Search();
    }
    public class ResidentResponse
    {
        public int TotalRecords { get; set; }
        public int RecordsFiltered { get; set; }
        public string JsonData { get; set; }
        public List<ResidentTrialBalanceVM> risdentlist { get; set; }
    }

    public class ResidentTrialBalanceVM
    {
        public int locationID { get; set; }

        public int TotalRecords { get; set; }

        public int PersonID { get; set; }
        public string Person { get; set; }
        public string RoomNo { get; set; }
        public string BedSpace { get; set; }
        public string MyriadID { get; set; }
        public string Name { get; set; }
        public DateTime? MoveIn { get; set; }
        public DateTime? MoveOut { get; set; }
        public DateTime? CheckIn { get; set; }
        public DateTime? CheckOut { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal DebitAmount { get; set; }
        public decimal CreditAmount { get; set; }
        public decimal Balance { get; set; }
        public decimal ExclusiveOpeningBalance { get; set; }
        public decimal CalculatedBalance { get; set; }
        public string CompanyName { get; set; }
        public string VATNo { get; set; }
        public DateTime? LastEntryDate { get; set; }
        public int PaymentTypeId { get; set; }
    }
    public class ResidentDetailBinding
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string orderBy { get; set; }
        public string orderDir { get; set; }
        public string query { get; set; }
        public string start { get; set; }
        public string length { get; set; }
        public string draw { get; set; }
        public Search search { get; set; }
        public bool isSummaryReport { get; set; }
        public List<string> SelectedColumns { get; set; }
    }
    public class VoucherReportVM
    {
        public int TotalRecords { get; set; }

        public int VoucherId { get; set; }
        public int LocationId { get; set; }
        public string Code { get; set; }
        public string TransactionCode { get; set; }
        public DateTime TransactionDate { get; set; }

        public int StudentId { get; set; }
        public DateTime VoucherDate { get; set; }
        public string Remarks { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public string FullName { get; set; }
        public string MyriadID { get; set; }
        public string LocationName { get; set; }
        public string AccountName { get; set; }
        public decimal? DebitAmount { get; set; }
        public decimal? CreditAmount { get; set; }
        public int AccountId { get; set; }
    }

    public class VoucherSummaryReportVM
    {
        public int AccountId { get; set; }
        public string AccountName { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public decimal TotalNet { get; set; }
    }
    public class VoucherBinding
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string orderBy { get; set; }
        public string orderDir { get; set; }
        public string query { get; set; }
        public string start { get; set; }
        public string length { get; set; }
        public string draw { get; set; }
        public Search search { get; set; }
        public bool isSummaryReport { get; set; }
        public List<string> SelectedColumns { get; set; }
    }
    public class VoucherReportResponse
    {
        public List<VoucherBinding> voucherreportlist { get; set; }
        public int TotalRecords { get; set; }
        public int RecordsFiltered { get; set; }

    }
    public class AgeingReportBinding
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string orderBy { get; set; }
        public string orderDir { get; set; }
        public string query { get; set; }
        public string start { get; set; }
        public string length { get; set; }
        public string draw { get; set; }
        public Search search { get; set; }
        public List<string> SelectedColumns { get; set; }

    }
    public class AgeingReportBindingResponse
    {
        public List<AgeingReportBinding> ageingReportlist { get; set; }
        public int TotalRecords { get; set; }
        public int RecordsFiltered { get; set; }
    }
    public class ResidentDetailBalanceResponse
    {
        public List<ResidentDetailTrialBalanceVM> residentdetailbalancelist { get; set; }
        public int TotalRecords { get; set; }
        public int RecordsFiltered { get; set; }
    }

    public class Search
    {
        public string value { get; set; }
        public string regex { get; set; }
        public string column { get; set; }
    }

    public class ResidentDetailTrialBalanceVM
    {
        public int TotalRecords { get; set; }

        public int PersonID { get; set; }
        public string MyriadID { get; set; }
        public string Name { get; set; }
        public string BedSpace { get; set; }
        public string RoomNo { get; set; }
        public string TypeOfInvoice { get; set; }
        public string InvoiceCode { get; set; }
        public DateTime InvoiceDate { get; set; }
        public decimal InvoiceAmount { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string TypeOfReceipt { get; set; }
        public string ReceiptNumber { get; set; }
        public DateTime? DateOfReceipt { get; set; }
        public decimal? AmountReceived { get; set; }
        public decimal BalanceReceivable { get; set; }
        public int? AgingInDays { get; set; }
        public int LocationId { get; set; }
    }
    public class PaymentDetailReportVM
    {
        public Nullable<int> StudentId { get; set; }
        public Nullable<int> PaymentId { get; set; }
        public string PostedBy { get; set; }
        public string ApprovedBy { get; set; }
        public string TransactionCode { get; set; }
        public string PaymentType { get; set; }
        public string Location { get; set; }
        public string MyriadID { get; set; }
        public string FullName { get; set; }
        public string TransactionDate { get; set; }
        public string Remarks { get; set; }
        public decimal NetAmount { get; set; }
        public bool IsStudent { get; set; }
        public bool IsApproved { get; set; }
        public string CompanyName { get; set; }
        public string VATNo { get; set; }
        public int LocationId { get; set; }
    }
    public class ShiftEndReportVM
    {
        public string TransactionCode { get; set; }
        public string TransactionDate { get; set; }
        public string Remarks { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; }
        public bool Status { get; set; }
        public string CreatedBy { get; set; }
        public string ApprovedBy { get; set; }
        public int LocationId { get; set; }
        public string LocationName { get; set; }
        public string MyriadID { get; set; }
        public string FullName { get; set; }

    }
    public class RoomInventoryVM
    {
        public string FullName { get; set; }
        public string Code { get; set; }
        public string BedNumber { get; set; }
        public string RoomNumber { get; set; }
        public string RoomType { get; set; }
        public string RoomOccupancy { get; set; }
        public string Dates { get; set; }
        public string Status { get; set; }
        public string University { get; set; }
        public int? LocationId { get; set; }
        public string LocationName { get; set; }

    }
    public class BookingReportVM
    {
        public string BookingNumber { get; set; }
        public string Title { get; set; }
        public string MyriadID { get; set; }
        public string FullName { get; set; }
        public string Gender { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Channel { get; set; }
        public DateTime BookingDate { get; set; }
        public string CheckInDate { get; set; }
        public string Nationality { get; set; }
        public string Duration { get; set; }
        public string RoomName { get; set; }
        public string University { get; set; }
        public string Status { get; set; }
        public string CheckOutDate { get; set; }
        public string Occupancy { get; set; }
        public string Dates { get; set; }
        public int LocationId { get; set; }
        public string LocationName { get; set; }
        public string RoomCode { get; set; }
        public decimal Price { get; set; }
        public decimal Deposit { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string DOB { get; set; }
        public string AccessibilityRequest { get; set; }
        public string HearFrom { get; set; }
        public string HearFromOther { get; set; }
        public string CardLastDigits { get; set; }
        public string TranRef { get; set; }
        public string Message { get; set; }
        public string PaymentType { get; set; }
        public int DurationMonth { get; set; }
        public int Frequency { get; set; }
        public string DurationDescription { get; set; }
        public string Source { get; set; }
        public string UniReferenceNo { get; set; }
        public string TenantPassportNumber { get; set; }
        public string GuardianFullName { get; set; }
        public string GuardianPhone { get; set; }
        public string GuardianEmail { get; set; }
        public string GuardianRelation { get; set; }
        public string PreferableView { get; set; }
        public string PreferableFloor { get; set; }
        public string Religions { get; set; }
        public string Nationalities { get; set; }
        public string Universities { get; set; }
        public string AgeRange { get; set; }

    }

    public class InHouseByUniversityReportVM
    {
        public string LocationName { get; set; }
        public string BookingNumber { get; set; }
        public string MyriadID { get; set; }
        public string Title { get; set; }
        public string FullName { get; set; }
        public string Gender { get; set; }
        public DateTime DOB { get; set; }
        public string Nationality { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string GuardianFullName { get; set; }
        public string GuardianPhone { get; set; }
        public string GuardianEmail { get; set; }
        public string GuardianRelation { get; set; }
        public string TenantPassportNumber { get; set; }
        public DateTime MoveIn { get; set; }
        public DateTime MoveOut { get; set; }
        public DateTime? CheckIn { get; set; }
        public DateTime? CheckOut { get; set; }
        public string RoomType { get; set; }
        public string RoomNo { get; set; }
        public string Channel { get; set; }
        public DateTime BookingDate { get; set; }
        public string University { get; set; }
        public string Source { get; set; }
        public string UniReferenceNo { get; set; }
        public int LocationId { get; set; }
        public string MSIS { get; set; }
    }
    public class AgeingReportVM
    {
        public int LocationID { get; set; }
        public int PersonID { get; set; }
        public string LocationName { get; set; }
        public int TotalRecords { get; set; }

        public string Title { get; set; }
        public string MyriadID { get; set; }
        public string FullName { get; set; }
        public string Gender { get; set; }
        public string BedSpace { get; set; }
        public string Room { get; set; }
        public string RoomType { get; set; }
        public string Commitment { get; set; }
        public string University { get; set; }
        public DateTime? MoveIn { get; set; }
        public DateTime? MoveOut { get; set; }
        public DateTime? CheckIn { get; set; }
        public DateTime? CheckOut { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public DateTime? BSPCreatedDate { get; set; }
        public string Status { get; set; }
        public decimal? OutstandingAmount { get; set; }
        public decimal? Last0To7DaysBalance { get; set; }
        public decimal? Last8To30DaysBalance { get; set; }
        public decimal? Last31To60DaysBalance { get; set; }
        public decimal? Last61To120DaysBalance { get; set; }
        public decimal? Last121To180DaysBalance { get; set; }
        public decimal? Above180DaysBalance { get; set; }
        public decimal? LiabilityBalance { get; set; }
    }

    public class TransportationBookingReportVM
    {
        public int Id { get; set; }
        public string StudentId { get; set; }
        public string VehicleId { get; set; }
        public string RegistrationNumber { get; set; }
        public string DepartureTime { get; set; }
        public string ScheduleDate { get; set; }
        public string FullName { get; set; }
        public string MyriadID { get; set; }
        public string Bus { get; set; }
        public string SeatNumber { get; set; }
        public string RouteName { get; set; }
        public string Status { get; set; }
        //public string DepartureTime { get; set; }
        public TimeSpan Time { get; set; }
        public string ReservationDate { get; set; }
        public int LocationID { get; set; }

    }
    public class AccountLiabilityVM
    {
        public Nullable<int> StudentId { get; set; }
        public Nullable<int> Accountd { get; set; }

        //public string Accountd { get; set; }
        public string MyriadID { get; set; }
        public string FullName { get; set; }
        public string ChartOfAccountsName { get; set; }
        //public int PersonID { get; set; }
        public string Email { get; set; }
        public string AccountName { get; set; }
        public string ServiceName { get; set; }
        public bool InvoicePaid { get; set; }
        public decimal InvoiceDetailTotalAmount { get; set; }
        public decimal InvoiceDetailPrice { get; set; }
        public DateTime dateTime { get; set; }
        public int LocationId { get; set; }
        public string LocationName { get; set; }


    }
    public class ContractsExpiringIn30DaysVM
    {
        public Nullable<int> IsSignedFilter { get; set; }
        public int LocationID { get; set; }
        public string LocationName { get; set; }
        public string MyriadID { get; set; }
        public string FullName { get; set; }
        public string Occupancy { get; set; }
        public string Room { get; set; }
        public bool ContractStatus { get; set; }
        public DateTime MoveIn { get; set; }
        public DateTime MoveOut { get; set; }
        public DateTime? CheckIn { get; set; }
        public DateTime? CheckOut { get; set; }
        public string ContractValidity { get; set; }
    }
    public class DepositInvoicesVM
    {
        public int? InvoiceId { get; set; }
        public string InvoiceCode { get; set; }
        public string LocationName { get; set; }
        public string FullName { get; set; }
        public string InvoiceDate { get; set; }
        public string Remarks { get; set; }
        public decimal NetAmount { get; set; }
        public decimal TotalCreditAmount { get; set; }
        public decimal InvoicePrice { get; set; }
        public string Status { get; set; }
        public Nullable<bool> IsPaid { get; set; }
        public Nullable<bool> Refunded { get; set; }
        public Nullable<int> ParentInvoiceId { get; set; }
        public Nullable<int> CreditNoteId { get; set; }
        public int personId { get; set; }
        public decimal TaxAmount { get; set; }
        public string TaxIds { get; set; }
        public DateTime CreatedDate { get; set; }
        public string InvoiceCreatedDate { get; set; }
        public int CreatedBy { get; set; }
        public string InvoiceCreatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int UpdatedBy { get; set; }
        public int LocationId { get; set; }
        public int? InvoiceTypeId { get; set; }
        public int? TermID { get; set; }
        public string PaymentCode { get; set; }
        public List<DepositDetailVM> DepositDetailViewModel { get; set; }
        public int? PaymentTypeId { get; set; }
        public string PaymentTypeName { get; set; }
        public decimal Amount { get; set; }
        public Nullable<decimal> DebitAmount { get; set; }
        public Nullable<decimal> CreditAmount { get; set; }
        public Nullable<decimal> TotalDiscountAmount { get; set; }
        public Nullable<decimal> DiscountPercentage { get; set; }
        public Nullable<decimal> DiscountAmount { get; set; }
        public bool IsCreditNote { get; set; }
        public decimal CreditNoteAmount { get; set; }
        public string CreditNoteRemarks { get; set; }

    }
    public class DepositDetailVM
    {
        public int InvoiceId { get; set; }
        public int Serviceid { get; set; }
        public string ServiceName { get; set; }
        public decimal Price { get; set; }
        public string Description { get; set; }
        public string TaxIds { get; set; }
        public string TaxesName { get; set; }
        public Nullable<decimal> TaxAmount { get; set; }
        public Nullable<decimal> TotalAmount { get; set; }
        public Nullable<System.DateTime> FromDate { get; set; }
        public Nullable<System.DateTime> ToDate { get; set; }
        public int LocationId { get; set; }


    }
    public class DepositPaymentVM
    {
        public int Id { get; set; }
        public DateTime PaymentDate { get; set; }
        public string Code { get; set; }
        public int? InvoiceId { get; set; }
        public int StudentId { get; set; }
        public Nullable<decimal> DebitAmount { get; set; }
        public Nullable<decimal> CreditAmount { get; set; }
        public string Remarks { get; set; }
        public int? PaymentTypeId { get; set; }
        public string PaymentTypeName { get; set; }
        public bool IsApproved { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public int LocationId { get; set; }
        public int ApprovedBy { get; set; }
        public int? CreditNotedId { get; set; }


    }
    public class LiabIlityBalanceVM
    {
        public int StudentId { get; set; }
        public string MyriadID { get; set; }
        public string FullName { get; set; }
        public string CheckOutDate { get; set; }
        public string RoomType { get; set; }
        public decimal RoomDepositAmount { get; set; }
        public decimal DepositAmountCollected { get; set; }
        public decimal DepositAmountRefunded { get; set; }
        public decimal LiabilityBalance { get; set; }
        public int LocationId { get; set; }
        public string LocationName { get; set; }

    }
    public class DetailLiabilityBalanceVM
    {
        public int StudentId { get; set; }
        public string MyriadID { get; set; }
        public string FullName { get; set; }
        public int LocationId { get; set; }
        public string LocationName { get; set; }
        public DateTime InvoiceDate { get; set; }
        public string DepositMemoNo { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public decimal RunningBalance { get; set; }
    }
    public class TaxDetailVM
    {
        public int LocationID { get; set; }
        public string Location { get; set; }
        public int AccountId { get; set; }
        public string AccountName { get; set; }
        public string InvoiceCode { get; set; }
        public int ServiceId { get; set; }
        public string ServiceName { get; set; }
        public int PersonID { get; set; }
        public string MyriadID { get; set; }
        public string FullName { get; set; }
        public DateTime InvoiceDate { get; set; }
        public decimal Price { get; set; }
        public string TaxId { get; set; }
        public string TaxNames { get; set; }
        public decimal TaxAmount { get; set; }

    }
    public class HistoryForcastVm
    {
        public DateTime Date { get; set; }
        public int TotalOccupiedBedSpace { get; set; }
        public int ArrivalRoom { get; set; }
        public decimal OccupancyPercentage { get; set; }
        public decimal TotalRevenue { get; set; }

        public Decimal AvgBedSpaceRate { get; set; }
        public int DeptBedSpace { get; set; }
        public int NoShowBedSpace { get; set; }
        public int OccupiedAndHeadAdltChildrn { get; set; }
    }

    public class OccupancyForecastVm
    {
        public DateTime ForecastDate { get; set; }
        public string Gender { get; set; }
        public string RoomType { get; set; }
        public int TotalCapacity { get; set; }
        public int FutureDayOut { get; set; }
        public int OccupiedBeds { get; set; }
        public int VacantBeds { get; set; }
        public decimal OccupancyRate { get; set; }

        public int ExpectedCheckIns { get; set; }
        public int ExpectedCheckOuts { get; set; }
        public int LocationId { get; set; }
    }

    public class ManagerDailyVM
    {
        public int TotalBedSpace { get; set; }
        public int BedOccupied { get; set; }
        public int AvailableBed { get; set; }
        public int NoShowBedSpace { get; set; }
        public int ArrivalBeds { get; set; }
        public int ArrivalPersons { get; set; }
        public int DepartureBedSpace { get; set; }
        public int DeparturePersons { get; set; }
        public decimal BedsRevenue { get; set; }
        public decimal OtherRevenue { get; set; }
        public decimal Payment { get; set; }
        public int ArrivalBedsTomorrow { get; set; }
        public int DepartureRoomsTomorrow { get; set; }
        public int ReservationsMadeToday { get; set; }
        public decimal RoomsOccupied { get; set; }
        public int InHouseAdultsResidents { get; set; }
        public int TotalInHousePersons { get; set; }
        public int InHouseChildrenGuests { get; set; }

    }
    public class RoomInventoryStats
    {
        public string BuildingName { get; set; }
        public int CheckInCount { get; set; }
        public int NoShow { get; set; }
        public int CheckedOut { get; set; }
        public int DueForCheckout { get; set; }
        public int TotalInHouse { get; set; }
        public int Vacancy { get; set; }
        public int MaintenanceBedSpaces { get; set; }
        public string RoomTypeName { get; set; }
        public int RoomsCount { get; set; }
        public int TotalBedSpaces { get; set; }
        public int LocationId { get; set; }
    }
    public class SwappedBedSpacesVM
    {
        public int LocationID { get; set; }
        public int PersonID { get; set; }
        public int BedSpaceplacementId { get; set; }
        //public string Location { get; set; }
        public int BedSpaceID { get; set; }
        public string OldBedSpace { get; set; }
        public string NewBedSpace { get; set; }
        public string Remarks { get; set; }
        public DateTime SwapDate { get; set; }
        public string SwappedBy { get; set; }
        public string MyriadID { get; set; }
        public string FullName { get; set; }
        public DateTime MoveIn { get; set; }
        public DateTime MoveOut { get; set; }
        public DateTime? CheckIn { get; set; }
        public DateTime? CheckOut { get; set; }

    }
    public class VacancyByRoomType
    {
        public string RoomName { get; set; }
        public int EmptyRoomCount { get; set; }
        public int LocationId { get; set; }
    }
    public class BuildinngStats
    {
        public List<RoomInventoryStats> RoomStats { get; set; }
        public List<VacancyByRoomType> VacancyStats { get; set; }
    }

    public class AccountingVoucherVM
    {
        public int LocationId { get; set; }
        public DateTime VoucherDate { get; set; }
        public string VoucherTypeName { get; set; }
        public string VoucherNumber { get; set; }
        public string LedgerName { get; set; }
        public Decimal? LedgerAmount { get; set; }
        public string LedgerAmountDrCr { get; set; }
        public string VoucherNarration { get; set; }
    }

    public class AccountingVouchersResult

    {
        public List<AccountingVoucher> Vouchers { get; set; } = new List<AccountingVoucher>();
        public int FilteredRecords { get; set; }
        public string ErrorMessage { get; set; }

    }

    public class AccountingVoucher
    {
        public int LocationId { get; set; }
        public DateTime? VoucherDate { get; set; }
        public string VoucherTypeName { get; set; }
        public string VoucherNumber { get; set; }
        public string LedgerName { get; set; }
        public string MyriadID { get; set; }
        public decimal? LedgerAmount { get; set; }
        public string LedgerAmountDrCr { get; set; }
        public string VoucherNarration { get; set; }
        public int? InvoiceTypeId { get; set; }
        public int TotalCount { get; set; }
        public int TotalRecords { get { return TotalCount; } set { TotalCount = value; } }
    }

    public class AccountingVoucherBinding
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string ReportType { get; set; }
        public string orderBy { get; set; }
        public string orderDir { get; set; }
        public string query { get; set; }
        public string start { get; set; }
        public string length { get; set; }
        public string draw { get; set; }
        public Search search { get; set; }
        public List<string> SelectedColumns { get; set; }
    }
    public class ReverseInvoiceModel
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public int? LocationId { get; set; }
        public int InvoiceTypeId { get; set; }
        public string Remarks { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal NetTotalAmount { get; set; }
        public List<ReverseInvoiceDetailModel> InvoiceDetails { get; set; }
    }

    public class ReverseInvoiceDetailModel
    {
        public int Id { get; set; }
        public int InvoicingId { get; set; }
        public int ServiceId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public decimal Price { get; set; }
        public decimal TotalAmount { get; set; }
        public string TaxesIds { get; set; }
        public decimal DiscountPercentage { get; set; }
    }
}
