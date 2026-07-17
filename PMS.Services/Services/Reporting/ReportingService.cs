using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.Entity;
using System.Linq;
using System.Web;
using PMS.Common.Classes;
using PMS.DTO.ViewModels.ReportingViewModels;
using PMS.EF;
using PMS.Services.Services.Correspondence;
using PMS.Services.Services.Email;
using PMS.Services.Services.LocationContext;
using PMS.Services.Services.Notifications;
using static System.Data.Entity.Infrastructure.Design.Executor;
using System.Threading.Tasks;

namespace PMS.Services.Services.Reporting

{
    public class ReportingService : IReportingService
    {
        private readonly PMS.Repository.UnitOfWork.UnitOfWork<PMSEntities> uow;
        private readonly INotificationService notificationService;
        private readonly ICorrespondenceService correspondenceService;
        private readonly IEmailService emailService;
        private readonly ILocationContextService locationContextService;

        public ReportingService(PMS.Repository.UnitOfWork.UnitOfWork<PMSEntities> _uow, INotificationService _notificationService, ICorrespondenceService _correspondenceService,
            IEmailService _emailService, ILocationContextService _locationContextService)
        {
            this.uow = _uow;
            notificationService = _notificationService;
            correspondenceService = _correspondenceService;
            emailService = _emailService;
            locationContextService = _locationContextService;
        }

        public List<ReportingVM> GetReport(DateTime? FromDate, DateTime? ToDate, int? studentid = 0)
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();

            SqlParameter param = new SqlParameter("@FromDate", FromDate);
            if (FromDate == null)
            {
                param.Value = DBNull.Value;
            }

            param.SqlDbType = SqlDbType.DateTime;

            SqlParameter param1 = new SqlParameter("@ToDate", ToDate);
            param1.SqlDbType = SqlDbType.DateTime;
            if (ToDate == null)
            {
                param1.Value = DBNull.Value;
            }
            SqlParameter param2 = new SqlParameter("@StudentId", studentid);

            var result = uow.Context.Database.SqlQuery<ReportingVM>("ReportingView @FromDate,@ToDate,@StudentId", param, param1, param2).Where(x => assignedLocationIds.Contains((int)x.LocationId)).ToList();

            return result;
        }
        public List<ServicesDetailVM> GetServicesDetailReport(DateTime? FromDate, DateTime? ToDate, int? ServiceId, int? studentid, int? TermId, int? userid, int? type)
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();

            SqlParameter param = new SqlParameter("@FromDate", FromDate.GetValueOrDefault().ToString("yyyy-MM-dd"));
            SqlParameter param1 = new SqlParameter("@ToDate", ToDate.GetValueOrDefault().ToString("yyyy-MM-dd"));
            SqlParameter param2 = new SqlParameter("@ServiceId", ServiceId.ToString());
            SqlParameter param3 = new SqlParameter("@StudentId", studentid.ToString());
            SqlParameter param4 = new SqlParameter("@TermId", TermId.ToString());
            SqlParameter param5 = new SqlParameter("@UserId", userid.ToString());
            SqlParameter param6 = new SqlParameter("@Type", type.ToString());
            var result = uow.Context.Database.SqlQuery<ServicesDetailVM>("EXEC SPGetServicesDetailReport @FromDate,@ToDate,@ServiceId,@StudentId, @TermId, @UserId, @Type", param, param1, param2, param3, param4, param5, param6).Where(x => assignedLocationIds.Contains((int)x.LocationId))?.ToList();

            return result;
        }

        public List<PaymentDetailReportVM> GetPaymentsDetailReport(DateTime? FromDate, DateTime? ToDate, int? PaymentId, int? StudentId, int? userid, int? type)
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();

            SqlParameter param = new SqlParameter("@FromDate", FromDate.GetValueOrDefault().ToString("yyyy-MM-dd"));
            SqlParameter param1 = new SqlParameter("@ToDate", ToDate.GetValueOrDefault().ToString("yyyy-MM-dd"));
            SqlParameter param2 = new SqlParameter("@PaymentId", PaymentId.ToString());
            SqlParameter param3 = new SqlParameter("@StudentId", StudentId.ToString());
            SqlParameter param4 = new SqlParameter("@UserId", userid.ToString());
            SqlParameter param5 = new SqlParameter("@Type", type.ToString());
            var result = uow.Context.Database.SqlQuery<PaymentDetailReportVM>("EXEC SPGetPaymentsDetailReport @FromDate,@ToDate,@PaymentId,@StudentId,@UserId, @Type", param, param1, param2, param3, param4, param5).Where(x => assignedLocationIds.Contains((int)x.LocationId)).ToList();

            return result;
        }

        public async Task<List<PaymentDetailReportVM>> GetPaymentsDetailReportAsync(DateTime? FromDate, DateTime? ToDate, int? PaymentId, int? StudentId, int? userid, int? type)
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();

            var parameters = new[]
            {
                new SqlParameter("@FromDate", SqlDbType.DateTime) { Value = (object)FromDate ?? DBNull.Value },
                new SqlParameter("@ToDate", SqlDbType.DateTime) { Value = (object)ToDate ?? DBNull.Value },
                new SqlParameter("@PaymentId", SqlDbType.Int) { Value = PaymentId.GetValueOrDefault() > 0 ? (object)PaymentId.Value : DBNull.Value },
                new SqlParameter("@StudentId", SqlDbType.Int) { Value = StudentId.GetValueOrDefault() > 0 ? (object)StudentId.Value : DBNull.Value },
                new SqlParameter("@UserId", SqlDbType.Int) { Value = userid.GetValueOrDefault() > 0 ? (object)userid.Value : DBNull.Value },
                new SqlParameter("@Type", SqlDbType.Int) { Value = type.GetValueOrDefault() > 0 ? (object)type.Value : DBNull.Value }
            };

            var data = await uow.Context.Database.SqlQuery<PaymentDetailReportVM>(
                "EXEC SPGetPaymentsDetailReport @FromDate,@ToDate,@PaymentId,@StudentId,@UserId,@Type",
                parameters).ToListAsync();

            return data.Where(x => assignedLocationIds.Contains(x.LocationId)).ToList();
        }

        public List<ShiftEndReportVM> GetShiftEndReport(DateTime? FromDate, DateTime? ToDate, int PaymentId, int userid)
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();

            SqlParameter param = new SqlParameter("@FromDate", FromDate.GetValueOrDefault().ToString("yyyy-MM-dd"));
            SqlParameter param1 = new SqlParameter("@ToDate", ToDate.GetValueOrDefault().ToString("yyyy-MM-dd"));
            SqlParameter param2 = new SqlParameter("@PaymentId", PaymentId);
            SqlParameter param3 = new SqlParameter("@UserId", userid);
            var result = uow.Context.Database.SqlQuery<ShiftEndReportVM>("EXEC ShiftEndReport @FromDate,@ToDate,@PaymentId,@UserId", param, param1, param2, param3).Where(x => assignedLocationIds.Contains((int)x.LocationId)).ToList();
            return result;
        }

        public List<ResidentTrialBalanceVM> GetResidentTrailBalanceReport(ResidentDetailBinding request, DateTime? fromDate, DateTime? toDate, int Start, int Length, bool isSummaryReport = true, int checkout = 0, int studentId = 0, string searchValue = null)
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();


            var fromDateParam = fromDate.HasValue ? new SqlParameter("@FromDate", fromDate) : new SqlParameter("@FromDate", DBNull.Value) { SqlDbType = SqlDbType.DateTime };
            var toDateParam = toDate.HasValue ? new SqlParameter("@ToDate", toDate) : new SqlParameter("@ToDate", DBNull.Value) { SqlDbType = SqlDbType.DateTime };
            var locationIDParam = new SqlParameter("@LocationId", assignedLocationIds.FirstOrDefault());
            var startParam = new SqlParameter("@Start", Start);
            var lengthParam = new SqlParameter("@Length", Length);

            int strValue = isSummaryReport ? 1 : 0;
            var isSummaryReportParam = new SqlParameter("@forreport", strValue);

            var checkoutParam = new SqlParameter("@CheckOut", checkout);
            var studentIdParam = new SqlParameter("@StudentId", studentId);
            var sortorder = new SqlParameter("@SortOrder", request.orderDir);
            var sortcolumn = new SqlParameter("@SortColumn", request.orderBy);
            if (searchValue == null)
            {
                searchValue = "";
            }
            if (request.search.column == null)
            {
                request.search.column = "";
            }
            SqlParameter selectedColumnsParam;

            if (request.SelectedColumns != null && request.SelectedColumns.Any())
            {
                var joinedString = string.Join(",", request.SelectedColumns);
                selectedColumnsParam = new SqlParameter("@SelectedColumns", joinedString);
            }
            else
            {

                selectedColumnsParam = new SqlParameter("@SelectedColumns", "");
            }
            var searchValueParam = new SqlParameter("@SearchValue", searchValue);
            var columnname = new SqlParameter("@SearchColumn", request.search.column);
            string query = "EXEC SPGetResidentTrialBalance @FromDate, @ToDate,@LocationId, @Start, @Length, @forreport,@CheckOut, @StudentId, @SearchColumn, @SearchValue, @SelectedColumns,@SortOrder,@SortColumn";
            Console.WriteLine(query);
            var result = uow.Context.Database.SqlQuery<ResidentTrialBalanceVM>(query, fromDateParam, toDateParam, locationIDParam, startParam, lengthParam, isSummaryReportParam, checkoutParam, studentIdParam, columnname, searchValueParam, selectedColumnsParam, sortorder, sortcolumn)
                .ToList();


            return result;

        }
        public List<AgeingReportVM> GetAgeingReport(AgeingReportBinding request, DateTime? fromDate, DateTime? toDate, int Start, int Length, int StudentId = 0, string searchValue = null)
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();

            var fromDateParam = fromDate.HasValue ? new SqlParameter("@FromDate", fromDate) : new SqlParameter("@FromDate", DBNull.Value) { SqlDbType = SqlDbType.DateTime };
            var toDateParam = toDate.HasValue ? new SqlParameter("@ToDate", toDate) : new SqlParameter("@ToDate", DBNull.Value) { SqlDbType = SqlDbType.DateTime };
            var locationIDParam = new SqlParameter("@LocationId", assignedLocationIds.FirstOrDefault());

            var startParam = new SqlParameter("@Start", Start);
            var lengthParam = new SqlParameter("@Length", Length);

            var studentIdParam = new SqlParameter("@StudentId", StudentId);
            var sortorder = new SqlParameter("@SortOrder", request.orderDir);
            var sortcolumn = new SqlParameter("@SortColumn", request.orderBy);
            if (searchValue == null)
            {
                searchValue = "";
            }
            if (request.search.column == null)
            {
                request.search.column = "";
            }
            SqlParameter selectedColumnsParam;

            if (request.SelectedColumns != null && request.SelectedColumns.Any())
            {
                var joinedString = string.Join(",", request.SelectedColumns);
                selectedColumnsParam = new SqlParameter("@SelectedColumns", joinedString);
            }
            else
            {

                selectedColumnsParam = new SqlParameter("@SelectedColumns", "");
            }

            var searchValueParam = new SqlParameter("@SearchValue", searchValue);
            var columnname = new SqlParameter("@SearchColumn", request.search.column);


            string query = "EXEC AgeingReport @FromDate, @ToDate,@LocationId, @Start, @Length, @StudentId, @SearchColumn, @SearchValue, @SortOrder,@SortColumn,@SelectedColumns";
            Console.WriteLine(query);
            var result = uow.Context.Database.SqlQuery<AgeingReportVM>(query, fromDateParam, toDateParam, locationIDParam, startParam, lengthParam, studentIdParam, columnname, searchValueParam, sortorder, sortcolumn, selectedColumnsParam)
                .ToList();

            return result;
        }

        public decimal[] GetGrandTotal(DateTime? fromDate, DateTime? toDate, int checkout = 0, int studentId = 0)
        {
            var grandTotal = new decimal[12];

            try
            {

                //var records = uow.Context.SPGetResidentTrialBalance(fromDate, toDate, 0, int.MaxValue, true, checkout, studentId, "", "", "").ToList();

                //foreach (var record in records)
                //{
                //    grandTotal[8] += record.OpeningBalance ?? 0;
                //    grandTotal[9] += record.DebitAmount ?? 0;
                //    grandTotal[10] += record.CreditAmount;
                //    grandTotal[11] += record.Balance ?? 0;
                //    grandTotal[12] += record.ExclusiveOpeningBalance ?? 0;
                //}
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception Message: " + ex.Message);
                Console.WriteLine("Inner Exception: " + ex.InnerException?.Message);
                throw;
            }

            return grandTotal;
        }

        public List<ResidentTrialBalanceVM> ExportResidentTrailBalanceReport(DateTime? FromDate, DateTime? ToDate, int? checkout = 0, int? studentid = 0)
        {
            try
            {
                var assignedLocationIds = locationContextService.GetAssignedLocationIds();

                SqlParameter param1 = new SqlParameter("@FromDate", FromDate);
                param1.SqlDbType = SqlDbType.DateTime;
                if (FromDate == null)
                {
                    param1.Value = DBNull.Value;
                }

                SqlParameter param2 = new SqlParameter("@ToDate", ToDate);
                param2.SqlDbType = SqlDbType.DateTime;
                if (ToDate == null)
                {
                    param2.Value = DBNull.Value;
                }
                var locationIDParam = new SqlParameter("@LocationId", assignedLocationIds.FirstOrDefault());

                SqlParameter param3 = new SqlParameter("@CheckOut", checkout);
                SqlParameter param4 = new SqlParameter("@StudentId", studentid);


                var result = uow.Context.Database.SqlQuery<ResidentTrialBalanceVM>("EXEC SPExportResidentTrialBalance @FromDate, @ToDate, @LocationId, @CheckOut, @StudentId", param1, param2, locationIDParam, param3, param4).OrderBy(x => x.MyriadID).ToList();

                return result;
            }
            catch (Exception ex)
            {

                throw;
            }

        }
        public List<AgeingReportVM> ExportAgeingReport(DateTime? FromDate, DateTime? ToDate, int? studentid = 0)
        {
            try
            {
                var assignedLocationIds = locationContextService.GetAssignedLocationIds();

                SqlParameter param1 = new SqlParameter("@FromDate", FromDate);
                param1.SqlDbType = SqlDbType.DateTime;
                if (FromDate == null)
                {
                    param1.Value = DBNull.Value;
                }

                SqlParameter param2 = new SqlParameter("@ToDate", ToDate);
                param2.SqlDbType = SqlDbType.DateTime;
                if (ToDate == null)
                {
                    param2.Value = DBNull.Value;
                }
                var param3 = new SqlParameter("@LocationId", assignedLocationIds.FirstOrDefault());


                SqlParameter param4 = new SqlParameter("@StudentId", studentid);


                var result = uow.Context.Database.SqlQuery<AgeingReportVM>("EXEC SPExportAgeingReport @FromDate, @ToDate, @LocationId, @StudentId", param1, param2, param3, param4).OrderBy(x => x.MyriadID).ToList();

                return result;
            }
            catch (Exception ex)
            {

                throw;
            }

        }


        public List<ResidentDetailTrialBalanceVM> ResidentDetailTrialBalanceReport(ResidentDetailBinding request, DateTime? fromDate, DateTime? toDate, int Start, int Length, bool isSummaryReport = true, int studentId = 0, string searchValue = null)
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();

            var fromDateParam = fromDate.HasValue ? new SqlParameter("@FromDate", fromDate.GetValueOrDefault().ToString("yyyy-MM-dd")) : new SqlParameter("@FromDate", DBNull.Value) { SqlDbType = SqlDbType.DateTime };
            var toDateParam = toDate.HasValue ? new SqlParameter("@ToDate", toDate.GetValueOrDefault().ToString("yyyy-MM-dd")) : new SqlParameter("@ToDate", DBNull.Value) { SqlDbType = SqlDbType.DateTime };
            var locationIDParam = new SqlParameter("@LocationId", assignedLocationIds.FirstOrDefault());

            var startParam = new SqlParameter("@Start", Start);
            var lengthParam = new SqlParameter("@Length", Length);

            int strValue = isSummaryReport ? 1 : 0;
            var isSummaryReportParam = new SqlParameter("@forreport", strValue);
            var studentIdParam = new SqlParameter("@StudentId", studentId);


            if (searchValue == null)
            {
                searchValue = "";
            }
            if (request.search.column == null)
            {
                request.search.column = "";
            }
            SqlParameter selectedColumnsParam;

            if (request.SelectedColumns != null && request.SelectedColumns.Any())
            {
                var joinedString = string.Join(",", request.SelectedColumns);
                selectedColumnsParam = new SqlParameter("@SelectedColumns", joinedString);
            }
            else
            {

                selectedColumnsParam = new SqlParameter("@SelectedColumns", "");
            }
            var searchValueParam = new SqlParameter("@SearchValue", searchValue);
            var columnname = new SqlParameter("@SearchColumn", request.search.column);
            if (request.orderDir == null)
            {
                request.orderDir = "";
            }

            var sortorder = new SqlParameter("@SortOrder", request.orderDir);

            if (request.orderBy == null)
            {
                request.orderBy = "";
            }

            var sortcolumn = new SqlParameter("@SortColumn", request.orderBy);




            //string query = "EXEC SPGetResidentDetailTrialBalance @FromDate, @ToDate, @Start, @Length, @forreport, @StudentId, @SearchValue,@SearchColumn,@SelectedColumns";


            var result = uow.Context.Database.SqlQuery<ResidentDetailTrialBalanceVM>("EXEC SPGetResidentDetailTrialBalance @FromDate, @ToDate,@LocationId, @Start,@Length,@forreport, @StudentId,@SearchColumn,@SearchValue,@SelectedColumns,@SortOrder,@SortColumn", fromDateParam, toDateParam, locationIDParam, startParam, lengthParam, isSummaryReportParam, studentIdParam, columnname, searchValueParam, selectedColumnsParam, sortorder, sortcolumn).ToList();

            return result;
        }

        public List<ResidentDetailTrialBalanceVM> ExportResidentDetailTrialBalanceReport(DateTime? FromDate, DateTime? ToDate, int? studentid = 0)
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();


            SqlParameter param = new SqlParameter("@FromDate", FromDate);

            if (FromDate == null)
            {
                param.Value = DBNull.Value;
            }

            param.SqlDbType = SqlDbType.DateTime;

            SqlParameter param1 = new SqlParameter("@ToDate", ToDate);
            param1.SqlDbType = SqlDbType.DateTime;
            if (ToDate == null)
            {
                param1.Value = DBNull.Value;
            }
            var locationIDParam = new SqlParameter("@LocationId", assignedLocationIds.FirstOrDefault());
            SqlParameter param2 = new SqlParameter("@StudentId", studentid);

            var result = uow.Context.Database.SqlQuery<ResidentDetailTrialBalanceVM>("SPExportResidentDetailTrialBalance @FromDate,@ToDate,@LocationId,@StudentId", param, param1, locationIDParam, param2).ToList();
            return result;
        }

        public List<RevenueDetailVM> GetRevenueDetailReport(DateTime? FromDate, DateTime? ToDate, int? isDefferd, int TermId)
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();

            SqlParameter param1 = new SqlParameter("@FromDate", (object)FromDate ?? DBNull.Value);
            SqlParameter param2 = new SqlParameter("@ToDate", (object)ToDate ?? DBNull.Value);
            SqlParameter param3 = new SqlParameter("@isDefferd", isDefferd);
            SqlParameter param4 = new SqlParameter("@TermId", TermId);
            SqlParameter param5 = new SqlParameter("@LocationId", assignedLocationIds.FirstOrDefault());

            var result = uow.Context.Database.SqlQuery<RevenueDetailVM>("EXEC SPGetRevenueReport @FromDate, @ToDate, @isDefferd, @TermId, @LocationId",
                param1, param2, param3, param4, param5).ToList();

            return result;
        }
        public List<ComplaintHistoryVM> GetComplaintHistoryReport(int? StatusId = 0, int? CreatedBy = 0, int? UpdatedBy = 0)
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();

            SqlParameter param1 = new SqlParameter("@StatusId", StatusId.ToString());
            SqlParameter param2 = new SqlParameter("@CreatedBy", CreatedBy.ToString());
            SqlParameter param3 = new SqlParameter("@UpdatedBy", UpdatedBy.ToString());
            var result = uow.Context.Database.SqlQuery<ComplaintHistoryVM>("EXEC SPGetComplaintHistoryReport @StatusId,@CreatedBy,@UpdatedBy", param1, param2, param3).Where(x => assignedLocationIds.Contains((int)x.LocationId)).ToList();
            return result;
        }

        public List<RoomInventoryVM> GetRoomInventoryReport()
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();

            var result = uow.Context.Database.SqlQuery<RoomInventoryVM>("EXEC SPGetRoomInventoryReport").Where(x => assignedLocationIds.Contains((int)x.LocationId)).ToList();
            return result;
        }

        public List<BookingReportVM> GetBookingReport(DateTime? FromDate, DateTime? ToDate)
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();

            SqlParameter param = new SqlParameter("@FromDate", FromDate.GetValueOrDefault().ToString("yyyy-MM-dd"));
            SqlParameter param1 = new SqlParameter("@ToDate", ToDate.GetValueOrDefault().ToString("yyyy-MM-dd"));
            var result = uow.Context.Database.SqlQuery<BookingReportVM>("SPGetBookingReport @FromDate,@ToDate", param, param1).Where(x => assignedLocationIds.Contains((int)x.LocationId)).ToList();
            return result;
        }

        public List<InHouseByUniversityReportVM> GetInHouseByUniversity(DateTime? fromDate, DateTime? toDate, int[] universityId, int? checkout = 0, int? studentid = 0)
        {
            // Convert university IDs array to CSV
            string universityIdsCsv = (universityId != null && universityId.Length > 0)
                                        ? string.Join(",", universityId)
                                        : "0"; // default fallback
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();

            var fromDateParam = fromDate.HasValue ? new SqlParameter("@FromDate", fromDate) : new SqlParameter("@FromDate", DBNull.Value) { SqlDbType = SqlDbType.DateTime };
            var toDateParam = toDate.HasValue ? new SqlParameter("@ToDate", toDate) : new SqlParameter("@ToDate", DBNull.Value) { SqlDbType = SqlDbType.DateTime };
            var locationIDParam = new SqlParameter("@LocationId", assignedLocationIds.FirstOrDefault());
            var checkoutParam = new SqlParameter("@CheckOut", checkout.ToString());
            var universityIdParam = new SqlParameter("@UniversityId", universityIdsCsv);
            var studentidParam = new SqlParameter("@StudentId", studentid.ToString());

            var result = uow.Context.Database.SqlQuery<InHouseByUniversityReportVM>("SPGetInHouseByUniversity @FromDate, @ToDate, @LocationId, @CheckOut, @UniversityId, @StudentId", fromDateParam, toDateParam, locationIDParam, checkoutParam, universityIdParam, studentidParam).OrderBy(x => x.MyriadID).ToList();
            return result;
        }

        public List<TransportationBookingReportVM> GetTransportationBookingReport(DateTime? FromDate, DateTime? ToDate, int? DepartureTimeId = 0, int? VehicleId = 0, int? StudentId = 0, int? RouteId = 0)
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();

            SqlParameter param = new SqlParameter("@FromDate", FromDate.GetValueOrDefault().ToString("yyyy-MM-dd"));
            SqlParameter param1 = new SqlParameter("@ToDate", ToDate.GetValueOrDefault().ToString("yyyy-MM-dd"));
            SqlParameter param2 = new SqlParameter("@DepartureTimeId", DepartureTimeId.ToString());
            SqlParameter param3 = new SqlParameter("@VehicleId", VehicleId.ToString());
            SqlParameter param4 = new SqlParameter("@StudentId", StudentId.ToString());
            SqlParameter param5 = new SqlParameter("@RouteId", RouteId.ToString());
            var result = uow.Context.Database.SqlQuery<TransportationBookingReportVM>("EXEC SPGetTransportationBookingReport @FromDate,@ToDate,@DepartureTimeId,@VehicleId,@StudentId,@RouteId", param, param1, param2, param3, param4, param5).Where(x => assignedLocationIds.Contains((int)x.LocationID)).ToList();

            return result;
        }

        public List<AccountLiabilityVM> GetAccountLiabilityReport(int? StudentId = 0, int? AccountId = 0)
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();

            SqlParameter param = new SqlParameter("@StudentId", StudentId.ToString());
            SqlParameter param1 = new SqlParameter("@AccountId", AccountId.ToString());
            var result = uow.Context.Database.SqlQuery<AccountLiabilityVM>("SPGetAccountLiabilityReport @StudentId,@AccountId", param, param1).Where(x => assignedLocationIds.Contains((int)x.LocationId)).ToList();
            return result;
        }

        public List<VoucherReportVM> GetVoucherReport(VoucherBinding request, DateTime? fromDate, DateTime? toDate, int Start, int Length, bool isSummaryReport = true, int accountId = 0, int studentId = 0, string searchValue = null)
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();

            var fromDateParam = fromDate.HasValue ? new SqlParameter("@FromDate", fromDate.GetValueOrDefault().ToString("yyyy-MM-dd")) : new SqlParameter("@FromDate", DBNull.Value) { SqlDbType = SqlDbType.DateTime };
            var toDateParam = toDate.HasValue ? new SqlParameter("@ToDate", toDate.GetValueOrDefault().ToString("yyyy-MM-dd")) : new SqlParameter("@ToDate", DBNull.Value) { SqlDbType = SqlDbType.DateTime };
            var locationIDParam = new SqlParameter("@LocationId", assignedLocationIds.FirstOrDefault());

            var startParam = new SqlParameter("@Start", Start);
            var lengthParam = new SqlParameter("@Length", Length);

            int strValue = isSummaryReport ? 1 : 0;
            var isSummaryReportParam = new SqlParameter("@forreport", strValue);
            var studentIdParam = new SqlParameter("@StudentId", studentId);
            var accountIdParam = new SqlParameter("@AccountId", accountId);


            if (searchValue == null)
            {
                searchValue = "";
            }
            if (request.search.column == null)
            {
                request.search.column = "";
            }
            SqlParameter selectedColumnsParam;

            if (request.SelectedColumns != null && request.SelectedColumns.Any())
            {
                var joinedString = string.Join(",", request.SelectedColumns);
                selectedColumnsParam = new SqlParameter("@SelectedColumns", joinedString);
            }
            else
            {

                selectedColumnsParam = new SqlParameter("@SelectedColumns", "");
            }
            var searchValueParam = new SqlParameter("@SearchValue", searchValue);
            var columnname = new SqlParameter("@SearchColumn", request.search.column);
            if (request.orderDir == null)
            {
                request.orderDir = "";
            }

            var sortorder = new SqlParameter("@SortOrder", request.orderDir);

            if (request.orderBy == null)
            {
                request.orderBy = "";
            }

            var sortcolumn = new SqlParameter("@SortColumn", request.orderBy);

            var result = uow.Context.Database.SqlQuery<VoucherReportVM>("EXEC SP_VoucherReport @FromDate, @ToDate,@LocationId, @Start,@Length,@forreport, @AccountId, @StudentId,@SearchColumn,@SearchValue,@SelectedColumns,@SortOrder,@SortColumn",
                fromDateParam, toDateParam, locationIDParam, startParam, lengthParam, isSummaryReportParam, accountIdParam, studentIdParam, columnname, searchValueParam, selectedColumnsParam, sortorder, sortcolumn).ToList();

            return result;
        }

        public List<VoucherReportVM> GetVoucherReportForExcel(DateTime? fromDate, DateTime? toDate, int? accountId = 0, int? studentId = 0)
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();

            var parameters = new List<SqlParameter>
    {
        fromDate.HasValue ? new SqlParameter("@FromDate", SqlDbType.DateTime) { Value = fromDate.Value } : new SqlParameter("@FromDate", SqlDbType.DateTime) { Value = DBNull.Value },
        toDate.HasValue ? new SqlParameter("@ToDate", SqlDbType.DateTime) { Value = toDate.Value } : new SqlParameter("@ToDate", SqlDbType.DateTime) { Value = DBNull.Value },
        new SqlParameter("@LocationId", SqlDbType.Int) { Value = assignedLocationIds.FirstOrDefault() },
        new SqlParameter("@Start", SqlDbType.Int) { Value = 0 },
        new SqlParameter("@Length", SqlDbType.Int) { Value = int.MaxValue },
        new SqlParameter("@forreport", SqlDbType.Bit) { Value = false },
        new SqlParameter("@StudentId", SqlDbType.Int) { Value = studentId ?? 0 },
        new SqlParameter("@AccountId", SqlDbType.Int) { Value = accountId ?? 0 },
        new SqlParameter("@SearchColumn", SqlDbType.VarChar, 50) { Value = "" },
        new SqlParameter("@SearchValue", SqlDbType.VarChar, 65) { Value = "" },
        new SqlParameter("@selectColumns", SqlDbType.NVarChar, 300) { Value = "" },
        new SqlParameter("@SortOrder", SqlDbType.NVarChar, 5) { Value = "" },
        new SqlParameter("@SortColumn", SqlDbType.NVarChar, 30) { Value = "" }
    };

            var result = uow.Context.Database.SqlQuery<VoucherReportVM>("EXEC SP_VoucherReport @FromDate, @ToDate, @LocationId, @Start, @Length, @forreport, @AccountId, @StudentId, @SearchColumn, @SearchValue, @selectColumns, @SortOrder, @SortColumn",
                parameters.ToArray()).ToList();

            return result;
        }

        public List<VoucherSummaryReportVM> GetVoucherSummaryReport(DateTime? fromDate, DateTime? toDate)
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();

            var fromDateParam = fromDate.HasValue ? new SqlParameter("@FromDate", SqlDbType.DateTime) { Value = fromDate.Value } : new SqlParameter("@FromDate", SqlDbType.DateTime) { Value = DBNull.Value };
            var toDateParam = toDate.HasValue ? new SqlParameter("@ToDate", SqlDbType.DateTime) { Value = toDate.Value } : new SqlParameter("@ToDate", SqlDbType.DateTime) { Value = DBNull.Value };
            var locationIdParam = new SqlParameter("@LocationId", SqlDbType.Int) { Value = assignedLocationIds.FirstOrDefault() };

            var result = uow.Context.Database.SqlQuery<VoucherSummaryReportVM>("EXEC SP_VoucherSummaryReport @FromDate, @ToDate, @LocationId",
                fromDateParam, toDateParam, locationIdParam).ToList();

            return result;
        }

        public List<ContractsExpiringIn30DaysVM> GetContractsExpiringIn30Days(int? IsSignedFilter = 2)
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();

            SqlParameter param = new SqlParameter("@IsSignedFilter", IsSignedFilter.ToString());
            var result = uow.Context.Database.SqlQuery<ContractsExpiringIn30DaysVM>("SPGetContractsExpiringIn30Days @IsSignedFilter", param).Where(x => assignedLocationIds.Contains((int)x.LocationID)).OrderBy(x => x.MyriadID).ToList();
            return result;
        }

        public List<LiabIlityBalanceVM> GetLiabilityBalanceReport(DateTime? FromDate, DateTime? ToDate, int? StudentId = 0)
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();


            SqlParameter param = new SqlParameter("@FromDate", FromDate.Value.ToString("yyyy-MM-dd"));
            SqlParameter param1 = new SqlParameter("@ToDate", ToDate.Value.ToString("yyyy-MM-dd"));
            SqlParameter param2 = new SqlParameter("@StudentId", StudentId.ToString());
            SqlParameter locationIDParam = new SqlParameter("@LocationId", assignedLocationIds.FirstOrDefault().ToString());


            var result = uow.Context.Database.SqlQuery<LiabIlityBalanceVM>("SPGetLiabilityBalancesReport @FromDate, @ToDate, @StudentId, @LocationId", param, param1, param2, locationIDParam).ToList();
            return result;
        }

        public List<DetailLiabilityBalanceVM> GetDetailLiabilityBalancesReport(DateTime? FromDate, DateTime? ToDate, int StudentId)
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();

            SqlParameter fromParam = new SqlParameter("@FromDate", FromDate.HasValue ? FromDate.Value.ToString("yyyy-MM-dd") : (object)DBNull.Value);
            SqlParameter toParam = new SqlParameter("@ToDate", ToDate.HasValue ? ToDate.Value.ToString("yyyy-MM-dd") : (object)DBNull.Value);
            SqlParameter studentParam = new SqlParameter("@StudentId", StudentId.ToString());
            SqlParameter locationParam = new SqlParameter("@LocationId", assignedLocationIds.FirstOrDefault().ToString());

            var result = uow.Context.Database.SqlQuery<DetailLiabilityBalanceVM>(
                "SPGetDetailLiabilityBalancesReport @FromDate, @ToDate, @StudentId, @LocationId",
                fromParam, toParam, studentParam, locationParam
            ).ToList();

            return result;
        }

        public List<DepositInvoicesVM> GetDepositInvoices(int? StudentId = 0, int? InvoiceId = 0, int? Refunded = 0)
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();

            SqlParameter param = new SqlParameter("@StudentId", StudentId.ToString());
            SqlParameter param1 = new SqlParameter("@InvoiceId", InvoiceId.ToString());
            SqlParameter param2 = new SqlParameter("@Refunded", Refunded.ToString());
            var result = uow.Context.Database.SqlQuery<DepositInvoicesVM>("EXEC SPGetDepositInvoices @StudentId, @InvoiceId, @Refunded", param, param1, param2).Where(x => assignedLocationIds.Contains((int)x.LocationId)).ToList();

            return result;
        }

        public List<EF.ChartOfAccount> GetChartOfAccounts()
        {
            return uow.GenericRepository<EF.ChartOfAccount>().Table.Where(x => x.Status == true).ToList();
        }

        public bool CancelSeat(int id)
        {
            try
            {
                var res = uow.GenericRepository<EF.VehicleBooking>().Table.Where(x => x.Id == id).FirstOrDefault();
                res.Status = "Cancelled";
                uow.GenericRepository<EF.VehicleBooking>().Update(res);
                uow.SaveChanges();
                // Email
                var NotifyEmail = correspondenceService.GetEmailMessagesByActionId(((int)Enumeration.CorrespondenceAction.TransportCancelEmail), res.Person.LocationId ?? 0);
                if (NotifyEmail != null)
                {
                    var body = NotifyEmail.EmailMessageBody;
                    emailService.SendEmailAsync(Convert.ToString(NotifyEmail.EmailMessageSubject), body, true, res.Person.Email, NotifyEmail.EmailMessageSenderID);
                }
                // Send Notification
                var Description = "Your seat booking has been cancelled";
                notificationService.SendNotification(null, res.StudentId, "Student", "Seat Cancelled", Description, "/Student/Notification", PMS.Common.Globals.User.Email);
                //END notification
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public List<InvoiceDetailReportVM> GetInvoiceDetailReport(int Id, string username)
        {
            SqlParameter param1 = new SqlParameter("@InvoiceId", Id);
            SqlParameter usermaster_username_param = new SqlParameter("@Username", username);
            var result = uow.Context.Database.SqlQuery<InvoiceDetailReportVM>("EXEC GetInvoicePrintReport @InvoiceId,@Username", param1, usermaster_username_param).ToList();
            return result;
        }

        public List<SubInvoicePrintReportVM> GetSubInvoiceReport(int Id)
        {
            SqlParameter param1 = new SqlParameter("@InvoiceId", Id);
            var result = uow.Context.Database.SqlQuery<SubInvoicePrintReportVM>("EXEC SubInvoicePrintReport @InvoiceId", param1).ToList();
            return result;
        }

        public List<PaymentTransactionDetailReportVM> GetPaymentTransactionDetailReport(int Id)
        {
            SqlParameter param1 = new SqlParameter("@StudentLedgerId", Id);
            var result = uow.Context.Database.SqlQuery<PaymentTransactionDetailReportVM>("EXEC GetPaymentTransactionPrintReport @StudentLedgerId", param1).ToList();
            return result;
        }

        public List<TaxDetailVM> GetTaxDetailReport(DateTime? FromDate, DateTime? ToDate, int? AccountId)
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();

            SqlParameter param = new SqlParameter("@FromDate", FromDate.GetValueOrDefault().ToString("yyyy-MM-dd"));
            SqlParameter param1 = new SqlParameter("@ToDate", ToDate.GetValueOrDefault().ToString("yyyy-MM-dd"));
            SqlParameter param2 = new SqlParameter("@AccountId", AccountId.ToString());
            var result = uow.Context.Database.SqlQuery<TaxDetailVM>("EXEC SPGetUserTaxDetailReport @FromDate,@ToDate,@AccountId", param, param1, param2).Where(x => assignedLocationIds.Contains((int)x.LocationID)).ToList();

            return result;
        }

        public List<HistoryForcastVm> GetHistoryForcastReport(DateTime? FromDate, DateTime? ToDate)
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();

            SqlParameter param = new SqlParameter("@FromDate", FromDate.GetValueOrDefault().ToString("yyyy-MM-dd"));
            SqlParameter param1 = new SqlParameter("@ToDate", ToDate.GetValueOrDefault().ToString("yyyy-MM-dd"));
            SqlParameter param2 = new SqlParameter("@LocationId", assignedLocationIds[0]);

            var result = uow.Context.Database.SqlQuery<HistoryForcastVm>("EXEC SPGetHistoryForcastReport @FromDate,@ToDate,@LocationId", param, param1, param2).ToList();

            return result;
        }
       
        public List<HistoryForcastVm> GetHistoryReport(DateTime? FromDate, DateTime? ToDate)
        {

            var assignedLocationIds = locationContextService.GetAssignedLocationIds();

            SqlParameter param = new SqlParameter("@FromDate", FromDate);
            if (FromDate == null)
            {
                param.Value = DBNull.Value;
            }

            param.SqlDbType = SqlDbType.DateTime;

            SqlParameter param1 = new SqlParameter("@ToDate", ToDate);
            param1.SqlDbType = SqlDbType.DateTime;
            if (ToDate == null)
            {
                param1.Value = DBNull.Value;
            }
            SqlParameter param2 = new SqlParameter("@LocationId", assignedLocationIds[0]);

            var result = uow.Context.Database.SqlQuery<HistoryForcastVm>("SPGetHistoryForcastReport @FromDate,@ToDate,@LocationId", param, param1, param2).ToList();

            return result;
        }

        public List<OccupancyForecastVm> GetOccupancyForecastReport(DateTime? FromDate, DateTime? ToDate)
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();

            var fromDateParam = FromDate.HasValue ? new SqlParameter("@FromDate", FromDate) : new SqlParameter("@FromDate", DBNull.Value) { SqlDbType = SqlDbType.DateTime };
            var toDateParam = ToDate.HasValue ? new SqlParameter("@ToDate", ToDate) : new SqlParameter("@ToDate", DBNull.Value) { SqlDbType = SqlDbType.DateTime };
            var locationIDParam = new SqlParameter("@LocationId", assignedLocationIds.FirstOrDefault());
            var result = uow.Context.Database.SqlQuery<OccupancyForecastVm>("EXEC SPGetOccupancyForecastReport @FromDate,@ToDate,@LocationId", fromDateParam, toDateParam, locationIDParam).ToList();

            return result;
        }


        public List<ManagerDailyVM> GetManagerDailyReport(DateTime? FilterDate)
        {
            SqlParameter param = new SqlParameter("@filterDate", FilterDate);
            if (FilterDate == null)
            {
                param.Value = DBNull.Value;
            }

            param.SqlDbType = SqlDbType.DateTime;


            var result = uow.Context.Database.SqlQuery<ManagerDailyVM>("SPGetManagerDailyReport @filterDate", param).ToList();

            return result;
        }

        public List<ResidentTrialBalanceVM> GetResidentTrialBalanceDetailPDFReport(DateTime? FromDate, DateTime? ToDate, int start, int length, int? studentid = 0)
        {
            SqlParameter param = new SqlParameter("@FromDate", FromDate);
            SqlParameter param1 = new SqlParameter("@ToDate", ToDate);
            SqlParameter param2 = new SqlParameter("@studentid", studentid);
            SqlParameter param3 = new SqlParameter("@start", start);
            SqlParameter param4 = new SqlParameter("@length", length);
            var result = uow.Context.Database.SqlQuery<ResidentTrialBalanceVM>("EXEC SPGetResidentTrialBalance @FromDate,@ToDate,@StudentId,@start,@length", param, param1, param2, param3, param4).ToList();
            return result;
        }

        public async Task<List<PaymentDetailReportVM>> GetPaymentDetailPDFReportAsync(DateTime? FromDate, DateTime? ToDate, int? PaymentId, int? StudentId, int? userid, int? type)
        {
            var result = (await GetPaymentsDetailReportAsync(FromDate, ToDate, PaymentId, StudentId, userid, type)).Select(x => new PaymentDetailReportVM
            {
                TransactionCode = x.TransactionCode,
                PaymentType = x.PaymentType,
                Location = x.Location,
                MyriadID = x.MyriadID,
                FullName = x.FullName,
                TransactionDate = x.TransactionDate,
                Remarks = x.Remarks,
                NetAmount = x.NetAmount,
                CompanyName = x.CompanyName,
                VATNo = x.VATNo,
                LocationId = x.LocationId
            }).ToList();

            return result;
        }

        public List<SwappedBedSpacesVM> GetSwappedBedSpacesReport(DateTime? FromDate, DateTime? ToDate, int? BedSpaceID)
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();

            SqlParameter param = new SqlParameter("@FromDate", FromDate.GetValueOrDefault().ToString("yyyy-MM-dd"));
            SqlParameter param1 = new SqlParameter("@ToDate", ToDate.GetValueOrDefault().ToString("yyyy-MM-dd"));
            SqlParameter param2 = new SqlParameter("@BedSpaceID", BedSpaceID.ToString());
            var result = uow.Context.Database.SqlQuery<SwappedBedSpacesVM>("EXEC SPGetSwappedBedSpaceHistoryReport @FromDate,@ToDate,@BedSpaceID", param, param1, param2).Where(x => assignedLocationIds.Contains((int)x.LocationID)).ToList();

            return result;
        }

        public BuildinngStats GetBuildingStatsReport(DateTime? Today)
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();

            SqlParameter param = new SqlParameter("@Today", Today);
            if (Today == null)
            {
                param.Value = DBNull.Value;
            }

            param.SqlDbType = SqlDbType.DateTime;

            var result = uow.Context.Database.SqlQuery<RoomInventoryStats>("SPGetBuildingStats @Today", param).Where(x => assignedLocationIds.Contains((int)x.LocationId)).ToList();
            var result1 = uow.Context.Database.SqlQuery<VacancyByRoomType>("SPGetVacancyByRoomType").Where(x => assignedLocationIds.Contains((int)x.LocationId)).ToList();
            var res = new BuildinngStats();
            res.RoomStats = result;
            res.VacancyStats = result1;
            return res;
        }
        public List<AccountingVoucherVM> AccountingVouchersReport(DateTime? FromDate, DateTime? ToDate, string ReportType)
        {
            var assignedLocationIds = locationContextService.GetAssignedLocationIds();

            if (ReportType == null)
            {
                ReportType = "Receipt";
            }
            SqlParameter param = new SqlParameter("@FromDate", FromDate.GetValueOrDefault().ToString("yyyy-MM-dd"));
            SqlParameter param1 = new SqlParameter("@ToDate", ToDate.GetValueOrDefault().ToString("yyyy-MM-dd"));
            SqlParameter param3 = new SqlParameter("@ReportType", ReportType);

            var result = uow.Context.Database.SqlQuery<AccountingVoucherVM>("AccountingVouchers @FromDate,@ToDate,@ReportType", param, param1, param3).Where(x => assignedLocationIds.Contains((int)x.LocationId)).ToList();
            return result;
        }

        public List<AccountingVoucher> GetAccountingVouchers(AccountingVoucherBinding request, DateTime? fromDate, DateTime? toDate, int start = 0, int length = 10, string searchValue = null)
        {
            try
            {
                // Get assigned location IDs (following your existing pattern)
                var assignedLocationIds = locationContextService.GetAssignedLocationIds();


                // Get sorting parameters from request
                string sortColumn = request.orderBy ?? "VoucherDate";
                string sortDirection = request.orderDir ?? "ASC";

                // Create SQL parameters matching the stored procedure exactly
                var parameters = new List<SqlParameter>
{
    new SqlParameter("@FromDate", fromDate ?? (object)DBNull.Value) { SqlDbType = SqlDbType.Date },
    new SqlParameter("@ToDate", toDate ?? (object)DBNull.Value) { SqlDbType = SqlDbType.Date },
    new SqlParameter("@ReportType", request.ReportType ?? (object)DBNull.Value) { SqlDbType = SqlDbType.NVarChar, Size = 50 },
    new SqlParameter("@LocationId", assignedLocationIds.FirstOrDefault()),
    new SqlParameter("@Start", start) { SqlDbType = SqlDbType.Int },
    new SqlParameter("@Length", length) { SqlDbType = SqlDbType.Int },
    new SqlParameter("@SearchValue", searchValue ?? (object)DBNull.Value) { SqlDbType = SqlDbType.NVarChar, Size = 100 },
    new SqlParameter("@SortColumn", sortColumn) { SqlDbType = SqlDbType.NVarChar, Size = 50 },
    new SqlParameter("@SortDirection", sortDirection) { SqlDbType = SqlDbType.NVarChar, Size = 4 }
};

                // Execute stored procedure with proper error handling
                var data = uow.Context.Database.SqlQuery<AccountingVoucher>(
                    "EXEC [dbo].[AccountingVouchers] @FromDate, @ToDate, @ReportType, @LocationId, @Start, @Length, @SearchValue, @SortColumn, @SortDirection",
                    parameters.ToArray()
                ).ToList();

                return data;
            }
            catch (SqlException sqlEx)
            {
                // Handle SQL-specific errors
                System.Diagnostics.Debug.WriteLine($"SQL Error in GetAccountingVouchers: {sqlEx.Message}");

                // Check if the error contains our custom error message from the stored procedure
                if (sqlEx.Message.Contains("ErrorMessage"))
                {
                    // Handle stored procedure errors specifically
                    System.Diagnostics.Debug.WriteLine($"Stored Procedure Error: {sqlEx.Message}");
                }

                return new List<AccountingVoucher>();
            }
            catch (Exception ex)
            {
                // Log the general error
                System.Diagnostics.Debug.WriteLine($"General Error in GetAccountingVouchers: {ex.Message}");

                return new List<AccountingVoucher>();
            }
        }

        public List<AccountingVoucher> ExportAccountingVoucher(DateTime? fromDate, DateTime? toDate, string reportType, int start = 0, int length = 10, string searchValue = null, string sortColumn = "VoucherDate", string sortDirection = "ASC", int? locationId = null)
        {


            var result = new AccountingVouchersResult
            {
                Vouchers = new List<AccountingVoucher>(),
                FilteredRecords = 0
            };

            var assignedLocationIds = locationContextService.GetAssignedLocationIds();

            var parameters = new List<SqlParameter>
        {
            new SqlParameter("@FromDate", fromDate ?? (object)DBNull.Value) { SqlDbType = SqlDbType.Date },
            new SqlParameter("@ToDate", toDate ?? (object)DBNull.Value) { SqlDbType = SqlDbType.Date },
            new SqlParameter("@ReportType", reportType ?? (object)DBNull.Value) { SqlDbType = SqlDbType.NVarChar, Size = 50 },
            new SqlParameter("@LocationId", assignedLocationIds.FirstOrDefault()),
            new SqlParameter("@Start", start) { SqlDbType = SqlDbType.Int },
            new SqlParameter("@Length",SqlDbType.Int){ Value = int.MaxValue },
            new SqlParameter("@SearchValue", searchValue ?? (object)DBNull.Value) { SqlDbType = SqlDbType.NVarChar, Size = 100 },
            new SqlParameter("@SortColumn", sortColumn ?? "VoucherDate") { SqlDbType = SqlDbType.NVarChar, Size = 50 },
            new SqlParameter("@SortDirection", sortDirection ?? "ASC") { SqlDbType = SqlDbType.NVarChar, Size = 4 }
        };

            var data = uow.Context.Database.SqlQuery<AccountingVoucher>(
                "EXEC [dbo].[AccountingVouchers] @FromDate, @ToDate, @ReportType, @LocationId, @Start, @Length, @SearchValue, @SortColumn, @SortDirection",
                parameters.ToArray()
            ).ToList();

            return data;
        }


        //For Student Portal
        public List<TransportationBookingReportVM> GetStudentTransportationBookingReport(int StudentId)
        {
            SqlParameter param = new SqlParameter("@StudentId", StudentId);

            var result = uow.Context.Database.SqlQuery<TransportationBookingReportVM>("EXEC SPStudentGetTransportationBookingReport @StudentId", param).ToList();

            return result;
        }
        public bool CancelStudentSeat(int id)
        {
            try
            {
                var res = uow.GenericRepository<EF.VehicleBooking>().Table.Where(x => x.Id == id).FirstOrDefault();
                res.Status = "Cancelled";
                uow.GenericRepository<EF.VehicleBooking>().Update(res);
                uow.SaveChanges();
                var NotifyEmail = correspondenceService.GetEmailMessagesByActionId(((int)Enumeration.CorrespondenceAction.TransportCancelEmail), res.Person.LocationId ?? 0);
                if (NotifyEmail != null)
                {
                    var body = NotifyEmail.EmailMessageBody;

                    emailService.SendEmailAsync(Convert.ToString(NotifyEmail.EmailMessageSubject), body, true, PMS.Common.Globals.User.Email, NotifyEmail.EmailMessageSenderID);
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}
