using PMS.DTO.ViewModels;
using PMS.EF;
using PMS.Services.Services.AuditLogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Services.Services.VoucherSystem
{
    public interface IVoucherService
    {
        //VouchersResponse GetAll(VoucherBinding request, string QueryBY, string searchValue, string start, string length,
        //    string query = null, string orderBy = null, string orderDir = "asc", DateTime? FromDate = null, DateTime? ToDate = null);
        Voucher GetById(int? id);
        List<VoucherDetailVM> GetVoucherDetail(int voucherId);

        void CreateVoucherWithDetails(VoucherCreationRequest request, IAuditLogsService auditLogsService = null);
        void UpdateVoucherWithDetails(int voucherId, VoucherCreationRequest request, IAuditLogsService auditLogsService = null);
        //void UpdatePaymentVoucher(StudentLedger studentLedger, IAuditLogsService auditLogsService);
    }
}
