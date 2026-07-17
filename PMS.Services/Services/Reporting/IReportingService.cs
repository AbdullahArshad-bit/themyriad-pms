using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PMS.DTO.ViewModels.PaymentViewModels;
using PMS.DTO.ViewModels.ReportingViewModels;

namespace PMS.Services.Services.Reporting
{
    public interface IReportingService
    {
        List<ReportingVM> GetReport(DateTime? FromDate, DateTime? ToDate, int? studentid = 0);

        List<HistoryForcastVm> GetHistoryReport(DateTime? FromDate, DateTime? ToDate);

        List<ServicesDetailVM> GetServicesDetailReport(DateTime? FromDate, DateTime? ToDate, int? ServiceId, int? studentid, int? TermId, int? userid, int? type);

        List<PaymentDetailReportVM> GetPaymentsDetailReport(DateTime? FromDate, DateTime? ToDate, int? PaymentId, int? StudentId, int? userid, int? type);
        Task<List<PaymentDetailReportVM>> GetPaymentsDetailReportAsync(DateTime? FromDate, DateTime? ToDate, int? PaymentId, int? StudentId, int? userid, int? type);

        List<ShiftEndReportVM> GetShiftEndReport(DateTime? FromDate, DateTime? ToDate, int PaymentId, int userid);

        List<ResidentTrialBalanceVM> GetResidentTrailBalanceReport(ResidentDetailBinding request, DateTime? fromDate, DateTime? toDate, int Start, int Length, bool isSummaryReport = true, int checkout = 0, int studentId = 0, string searchValue = null);

        List<ResidentTrialBalanceVM> ExportResidentTrailBalanceReport(DateTime? FromDate, DateTime? ToDate, int? checkout = 0, int? studentid = 0);

        decimal[] GetGrandTotal(DateTime? fromDate, DateTime? toDate, int checkout = 0, int studentId = 0);

        List<ResidentDetailTrialBalanceVM> ResidentDetailTrialBalanceReport(ResidentDetailBinding request, DateTime? fromDate, DateTime? toDate, int Start, int Length, bool isSummaryReport = true, int studentId = 0, string searchValue = null);

        List<VoucherReportVM> GetVoucherReport(VoucherBinding request, DateTime? fromDate, DateTime? toDate, int Start, int Length, bool isSummaryReport = true, int accountId = 0, int studentId = 0, string searchValue = null);

        List<VoucherReportVM> GetVoucherReportForExcel(DateTime? fromDate, DateTime? toDate, int? accountId = 0, int? studentId = 0);

        List<VoucherSummaryReportVM> GetVoucherSummaryReport(DateTime? fromDate, DateTime? toDate);

        List<ResidentDetailTrialBalanceVM> ExportResidentDetailTrialBalanceReport(DateTime? FromDate, DateTime? ToDate, int? studentid = 0);

        List<RevenueDetailVM> GetRevenueDetailReport(DateTime? FromDate, DateTime? ToDate, int? isDefferd, int TermId);

        List<ComplaintHistoryVM> GetComplaintHistoryReport(int? StatusId = 0, int? CreatedBy = 0, int? UpdatedBy = 0);

        List<RoomInventoryVM> GetRoomInventoryReport();

        List<BookingReportVM> GetBookingReport(DateTime? FromDate, DateTime? ToDate);
        List<InHouseByUniversityReportVM> GetInHouseByUniversity(DateTime? fromDate, DateTime? toDate, int[] universityId, int? checkout = 0, int? studentid = 0);

        List<TransportationBookingReportVM> GetTransportationBookingReport(DateTime? FromDate, DateTime? ToDate, int? DepartureTimeId = 0, int? VehicleId = 0, int? StudentId = 0, int? RouteId = 0);

        List<AccountLiabilityVM> GetAccountLiabilityReport(int? StudentId = 0, int? AccountId = 0);
        List<ContractsExpiringIn30DaysVM> GetContractsExpiringIn30Days(int? IsSignedFilter = 2);

        List<DepositInvoicesVM> GetDepositInvoices(int? StudentId = 0, int? InvoiceId = 0, int? Refunded = 0);

        List<LiabIlityBalanceVM> GetLiabilityBalanceReport(DateTime? FromDate, DateTime? ToDate, int? StudentId = 0);
        List<DetailLiabilityBalanceVM> GetDetailLiabilityBalancesReport(DateTime? FromDate, DateTime? ToDate, int StudentId);
         
        List<EF.ChartOfAccount> GetChartOfAccounts();

        bool CancelSeat(int id);

        List<TaxDetailVM> GetTaxDetailReport(DateTime? FromDate, DateTime? ToDate, int? AccountId);

        List<HistoryForcastVm> GetHistoryForcastReport(DateTime? FromDate, DateTime? ToDate);

        List<OccupancyForecastVm> GetOccupancyForecastReport(DateTime? FromDate, DateTime? ToDate);

        List<ManagerDailyVM> GetManagerDailyReport(DateTime? FilterDate);

        List<SubInvoicePrintReportVM> GetSubInvoiceReport(int Id);

        List<InvoiceDetailReportVM> GetInvoiceDetailReport(int Id, string username);

        List<PaymentTransactionDetailReportVM> GetPaymentTransactionDetailReport(int Id);

        List<ResidentTrialBalanceVM> GetResidentTrialBalanceDetailPDFReport(DateTime? FromDate, DateTime? ToDate, int start, int length, int? studentid = 0);

        Task<List<PaymentDetailReportVM>> GetPaymentDetailPDFReportAsync(DateTime? FromDate, DateTime? ToDate, int? PaymentId, int? StudentId, int? userid, int? type);

        List<SwappedBedSpacesVM> GetSwappedBedSpacesReport(DateTime? FromDate, DateTime? ToDate, int? BedSpaceID);

        BuildinngStats GetBuildingStatsReport(DateTime? Today);
        List<AgeingReportVM> GetAgeingReport(AgeingReportBinding request, DateTime? fromDate, DateTime? toDate, int Start, int Length, int StudentId = 0, string searchValue = null);
        List<AgeingReportVM> ExportAgeingReport(DateTime? FromDate, DateTime? ToDate, int? studentid = 0);

        List<AccountingVoucherVM> AccountingVouchersReport(DateTime? FromDate, DateTime? ToDate, string ReportType);

        List<AccountingVoucher> GetAccountingVouchers(AccountingVoucherBinding request, DateTime? fromDate, DateTime? toDate, int start = 0, int length = 10, string searchValue = null);
        List<AccountingVoucher> ExportAccountingVoucher(DateTime? fromDate, DateTime? toDate, string reportType, int start = 0, int length = 10, string searchValue = null, string sortColumn = "VoucherDate", string sortDirection = "ASC", int? locationId = null);
        //for student portal
        List<TransportationBookingReportVM> GetStudentTransportationBookingReport(int StudentId);
         
        bool CancelStudentSeat(int id);
    } 
}
 